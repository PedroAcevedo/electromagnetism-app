using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IndicatorController : MonoBehaviour
{

    public GameObject[] closeIndicators;
    public GameObject[] farIndicators;

    private bool showClose;

    // Start is called before the first frame update
    void Start()
    {
        showClose = true;
        IndicatorsActive(closeIndicators, true);
        IndicatorsActive(farIndicators, false);
    }

    // Update is called once per frame
    void Update()
    {
        if (showClose)
        {
            if (IndicatorsState(closeIndicators))
            {
                IndicatorsActive(closeIndicators, false);
                IndicatorsActive(farIndicators, true);
                showClose = false;
            }
        } 
        else
        {
            if (IndicatorsState(farIndicators))
            {
                IndicatorsActive(farIndicators, false);
            }
        }
    }


    bool IndicatorsState(GameObject[] indicators)
    {
        for (int i = 0; i < indicators.Length; ++i)
        {
            if(!indicators[i].GetComponent<IndicatorCollision>().isOccupied())
                return false;
        }
        return true;
    }


    void IndicatorsActive(GameObject[] indicators, bool state)
    {
        for (int i = 0; i < indicators.Length; ++i)
            indicators[i].SetActive(state);
    }

    void IndicatorsParticleReset(GameObject[] indicators)
    {
        for (int i = 0; i < indicators.Length; ++i)
        {
            indicators[i].GetComponent<IndicatorCollision>().cleanIndicator();
        }
    }

    public void resetIndicators()
    {
        showClose = true;
        IndicatorsActive(closeIndicators, true);
        IndicatorsParticleReset(closeIndicators);
        IndicatorsActive(farIndicators, false);
        IndicatorsParticleReset(farIndicators);
    }
}
