using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InterestPoint : MonoBehaviour
{
    public int TimeActive = 2;
    public GameObject nextPoint;

    private Color touchedColor = Color.black;
    private Color originColor = new Color(0.0f, 0.0f, 0.0f);

    public void OnTriggerEnter(Collider other)
    {
        Debug.Log("Interest point touched");

        this.gameObject.GetComponent<Renderer>().material.color = touchedColor;

        if (nextPoint != null)
        {
            nextPoint.SetActive(true);
        }
    }

    public void Reset()
    {
        this.gameObject.GetComponent<Renderer>().material.color = originColor;
    }

}
