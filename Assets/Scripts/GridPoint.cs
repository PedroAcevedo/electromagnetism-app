using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridPoint
{
    private Vector3 position;
    private float charge;

    public Vector3 Position 
    {
        get { return position; }
        set { position = value; }
    }

    public float Charge
    {
        get { return charge; }
        set { charge = value; }
    }
}
