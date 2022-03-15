using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneController : MonoBehaviour
{
    public bool LeftHander = false;
    public GameObject loadingScreen;
    public static UserReportController controller;
    public GameObject player;

    private SceneData introScene;
    private Timer timer;
    private GameObject cubeTest;
    private DataCollectionController dataController;

    void Start()
    {
        controller = new UserReportController();

        introScene = new SceneData();
        controller.SceneInfo(introScene);

        dataController = new DataCollectionController();

        InvokeRepeating("reportUser", 2.0f, 2.0f);

        timer = new Timer();
        cubeTest = GameObject.Find("RotationCube");
        timer.start();

    }

    void reportUser()
    {
        dataController.reportUser(ref introScene, player);
    }

    void Update()
    {
        dataController.ButtonsPressed(ref introScene);

        cubeTest.transform.rotation = player.transform.rotation;

        timer.run();
    }

    public void GoToSimulation()
    {
        introScene.UIClick += 1;
        introScene.sceneTime = timer.currentTime;

        int left = 0;


        if (LeftHander)
        {
            left = 1;
        }

        PlayerPrefs.SetInt("LeftHander", left);

        loadingScreen.SetActive(true);

        SceneManager.LoadSceneAsync("GameScene");

        Debug.Log("Its open, lets wait");
    }
}
