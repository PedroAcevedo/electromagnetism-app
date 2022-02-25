using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RaycastingHand : MonoBehaviour
{
    public GameObject RHand;            // VR right controller
    public GameObject LHand;            // VR left controller

    private bool LeftHander = false;

    // Update is called once per frame
    void Update()
    {
        if (OVRInput.GetDown(OVRInput.Button.Three))
        {
            LeftHander = !LeftHander;

            RHand.GetComponent<UnityEngine.XR.Interaction.Toolkit.XRInteractorLineVisual>().enabled = !RHand.GetComponent<UnityEngine.XR.Interaction.Toolkit.XRInteractorLineVisual>().enabled;
            LHand.GetComponent<UnityEngine.XR.Interaction.Toolkit.XRInteractorLineVisual>().enabled = !LHand.GetComponent<UnityEngine.XR.Interaction.Toolkit.XRInteractorLineVisual>().enabled;
        }
    }

    public void GoToSimulation()
    {
        int left = 0;

        if (LeftHander)
        {
            left = 1;
        }

        PlayerPrefs.SetInt("LeftHander", left);
        SceneManager.LoadScene("GameScene");
    }
}
