using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NODE
{
    private float score;
    private float charge;
    private int leftChild;
    private int rightChild;

    public NODE(float scoreValue, float chargeValue)
    {
        score = scoreValue;
        charge = chargeValue;
        leftChild = -1;
        rightChild = -1;
    }

    public float Score
    {
        get { return score; }
        set { score = value; }
    }

    public float Charge
    {
        get { return charge; }
        set { charge = value; }
    }

    public int LeftChild
    {
        get { return leftChild; }
        set { leftChild = value; }
    }


    public int RightChild
    {
        get { return rightChild; }
        set { rightChild = value; }
    }

}
