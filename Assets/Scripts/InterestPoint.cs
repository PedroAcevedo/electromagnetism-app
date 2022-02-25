using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InterestPoint : MonoBehaviour
{
    public int TimeActive = 2;
    public GameObject nextPoint;

    private Color touchedColor = Color.black;

    public void OnTriggerEnter(Collider other)
    {
        Debug.Log("Interest point touched");

        StartCoroutine(TemporarilyDeactivate(TimeActive));

        if (nextPoint != null)
        {
            nextPoint.SetActive(true);
        }
    }

    private IEnumerator TemporarilyDeactivate(float duration)
    {
        this.gameObject.GetComponent<Renderer>().material.color = touchedColor;
        yield return new WaitForSeconds(duration);
        this.gameObject.SetActive(false);
    }

}
