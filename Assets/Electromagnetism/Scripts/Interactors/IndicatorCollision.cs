using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IndicatorCollision : MonoBehaviour
{
    private GameObject currentParticle;

    public void OnTriggerStay(Collider other)
    {
        if (other.gameObject.name.Contains("Particle"))
        {
            currentParticle = other.gameObject;
        }
        
    }

    void OnTriggerExit(Collider other)
    {
        currentParticle = null;
    }

    public bool isOccupied()
    {
        if (currentParticle != null)
        {
            if (!currentParticle.GetComponent<OVRGrabbable>().isGrabbed)
            {
                return true;
            }
        }

        return false;
    }

    public void cleanIndicator()
    {
        currentParticle = null;
    }
}
