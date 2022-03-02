using System.Collections;
using System.Collections.Generic;
using OVRTouchSample;
using UnityEngine;

public class CircleVibration : MonoBehaviour
{
    public GameObject RHand;            // VR right controller
    public GameObject LHand;            // VR left controller
    public float intensity;

    public void OnTriggerEnter(Collider other)
    { 
        if (other.gameObject.name == "RightHandAnchor")
        {
            OVRInput.SetControllerVibration(1, intensity, OVRInput.Controller.RTouch);
        }

        if(other.gameObject.name == "LeftHandAnchor")
        {
            OVRInput.SetControllerVibration(1, intensity, OVRInput.Controller.LTouch);
        }
    }
}
