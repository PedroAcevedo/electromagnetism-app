using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneController : MonoBehaviour
{
    public bool LeftHander = false;
    public static UserReportController controller;
    public GameObject player;
    public GameObject[] particles;
    public GameObject[] interactionPoints;


    private SceneData introScene;
    private Timer timer;
    private DataCollectionController dataController;
    private bool StopTrack = false;
    private bool isGrabbing = true;
    private float[] initialTimePerParticle;

    void Start()
    {
        controller = new UserReportController();

        Debug.Log("Delimitated user controller");

        introScene = new SceneData();
        controller.SceneInfo(introScene);

        dataController = new DataCollectionController();

        initialTimePerParticle = new float[particles.Length];

        for (int i = 0; i < initialTimePerParticle.Length; ++i)
        {
            initialTimePerParticle[0] = 0.0f;
            introScene.particlePositions.Add(new ParticleData(i == 0, particles[i].transform.position));
        }

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

    void validateGrab()
    {
        for (int i = 0; i < particles.Length; ++i)
        {
            if (particles[i].transform.hasChanged)
            {
                particles[i].transform.hasChanged = false;

                if (!isGrabbing)
                {
                    if (particles[i].GetComponent<OVRGrabbable>().isGrabbed)
                    {
                        isGrabbing = true;
                        initialTimePerParticle[i] = timer.currentTime;
                    }
                }
                break;
            }
        }

        bool isStatic = true;

        for (int i = 0; i < particles.Length; ++i)
        {
            if(!particles[i].GetComponent<OVRGrabbable>().isGrabbed && initialTimePerParticle[i] != 0.0f)
            {
                if(i == 0)
                {
                    introScene.positiveParticleGrabTime += (timer.currentTime - initialTimePerParticle[i]);
                }
                else
                {
                    introScene.negativeParticleGrabTime += (timer.currentTime - initialTimePerParticle[i]);
                }

                initialTimePerParticle[i] = 0.0f;
            }

            isStatic = isStatic && !particles[i].GetComponent<OVRGrabbable>().isGrabbed;
        }

        if (isGrabbing && isStatic)
        {
            isGrabbing = false;

            for (int i = 0; i < introScene.particlePositions.Count; ++i)
            {
                introScene.particlePositions[i].addPosition(particles[i].transform.position);
            }
        }
    }

    void Update()
    {
        if (!StopTrack)
        {
            dataController.ButtonsPressed(ref introScene);

            validateGrab();
            
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

        for (int i = 0; i < interactionPoints.Length; ++i)
        {
            float interactionTime = interactionPoints[i].GetComponent<CircleVibration>().interactionTime;
            introScene.interestPointDuration.Add(new InterestPointData(interactionPoints[i].transform.position, interactionTime));
        }

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
