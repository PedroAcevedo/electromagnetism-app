using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InterestPoint : MonoBehaviour
{
    public int TimeActive = 2;
    public GameObject nextPoint;

    public void OnTriggerEnter(Collider other)
    {
        Debug.Log("Interest point touched");

        Destroy(this.gameObject, TimeActive);

        if(nextPoint != null)
        {
            nextPoint.SetActive(true);
        }
    }

}
