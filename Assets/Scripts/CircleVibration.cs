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

        Vector3 RPos = RHand.transform.position;
        Vector3 LPos = LHand.transform.position;

        float distR = Vector3.Distance(RPos, this.transform.position);
        float distL = Vector3.Distance(LPos, this.transform.position);

        if (distR < distL)
        {
            OVRInput.SetControllerVibration(1, intensity, OVRInput.Controller.RTouch);
        } else
        {
            OVRInput.SetControllerVibration(1, intensity, OVRInput.Controller.LTouch);
        }
    }
}
