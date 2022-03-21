using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneController : MonoBehaviour
{
    public bool LeftHander = false;
    public static UserReportController controller;
    public GameObject player;

    private SceneData introScene;
    private Timer timer;
    private DataCollectionController dataController;
    private bool StopTrack = false;

    void Start()
    {
        controller = new UserReportController();

        Debug.Log("Delimitated user controller");

        introScene = new SceneData();
        controller.SceneInfo(introScene);

        dataController = new DataCollectionController();

        InvokeRepeating("reportUser", 2.0f, 2.0f);

        timer = new Timer();
        timer.start();

    }

    void reportUser()
    {
        if (!StopTrack)
        {
            dataController.reportUser(ref introScene, player);
        }
    }

    void Update()
    {
        if (!StopTrack)
        {
            dataController.ButtonsPressed(ref introScene);

            timer.run();
        }
    }

    void resetPlayerPosition()
    {
        var OVRplayer = player.transform.GetChild(1).GetChild(0); //.GetComponent<OVRPlayerController>();
        //OVRplayer.enabled = false;
        OVRplayer.position = new Vector3(0.0f, OVRplayer.position.y, -15f);
        OVRplayer.rotation = Quaternion.identity;
        //OVRplayer.enabled = true;
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

        timer.stop();

        resetPlayerPosition();

        //SceneManager.LoadSceneAsync("GameScene");

       //Debug.Log("Its open, lets wait");
    }
}
