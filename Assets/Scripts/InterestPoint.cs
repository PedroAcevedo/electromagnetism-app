using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class InterestPoint : MonoBehaviour
{
    public int TimeActive = 2;
    public GameObject nextPoint;
    public GameObject MarchingRef;

    private Color touchedColor = Color.black;
    private Color originColor = new Color(0.0f, 0.0f, 0.0f);

    public void OnTriggerEnter(Collider other)
    {
        this.gameObject.GetComponent<Renderer>().material.color = touchedColor;

        if (this.showLabel())
        {
            changeValue();
        }

        if (nextPoint != null)
        {
            nextPoint.SetActive(true);
        }
    }

    public bool showLabel()
    {
        return MarchingRef.GetComponent<SimulationController>().getCurrentMode();
    }

    public void Reset()
    {
        this.gameObject.GetComponent<Renderer>().material.color = originColor;
        this.gameObject.transform.GetChild(1).gameObject.GetComponent<TextMeshPro>().text = "";
    }

    public void changeValue()
    {
        this.gameObject.transform.GetChild(1).gameObject.GetComponent<TextMeshPro>().text 
            = MarchingRef.GetComponent<SimulationController>().getPointValue(this.gameObject.transform.position) + " N";
    }

}
