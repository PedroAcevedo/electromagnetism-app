using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System;

public class SetRayOnHand : MonoBehaviour
{
    public GameObject Hand;            // VR right controller
    OVRInputModule m_InputModule;
    public GameObject HandSelector;
    public bool isLeft;

    // Start is called before the first frame update
    void Start()
    {
        m_InputModule = FindObjectOfType<OVRInputModule>();
    }

    void SetActiveController(Transform c)
    {
        m_InputModule.rayTransform = c;
    }

    public void OnTriggerEnter(Collider other)
    {
        m_InputModule.enabled = true;
        SetActiveController(Hand.transform);
        HandSelector.GetComponent<SceneController>().LeftHander = isLeft;
    }


}
