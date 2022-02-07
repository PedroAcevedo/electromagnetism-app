using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class SHOIntegrator : Integrator {

	double k = 1;
	double m = 1;
	public double [] x;
    public float[] charges;
    public GameObject[] particles;

    public SHOIntegrator(GameObject[] particles, float[] charges)
    {
        this.particles = particles;
        this.charges = charges;
    }


    public void SetIC(double x0, double v0) {
		x = new double[2];
		Init (2); // allocates memory for Integrator varaibles
		x [0] = x0;
		x [1] = v0;
    }

    public override void RatesOfChange (double[] x, double[] xdot, double t)
	{
        double[] evaluation = EDir(t, x);
        Debug.Log("Evaluation values -> " + evaluation[0] + ", " + evaluation[1]);
        //xdot[0] = x[1]; // + (evaluation[0] == Double.NaN ? 0 : evaluation[0]);
        //xdot[1] = -(k / m) * x[0]; // x[0] + (evaluation[1] == Double.NaN ? 1 : evaluation[1]);
        xdot[0] = x[1] + (evaluation[0] == Double.NaN ? 0 : evaluation[0]);
        xdot[1] = x[0] + (evaluation[1] == Double.NaN ? 1 : evaluation[1]);
    }

    double[] pointCharge(float charge, Vector3 position, double x, double y)
    {
        double[] chargeValue = new double[2];

        chargeValue[0] = charge * (x - position.x) / Math.Pow((Math.Pow(x - position.x, 2.0f) + Math.Pow(y - position.y, 2.0f)), 0.5f);
        chargeValue[1] = charge * (y - position.y) / Math.Pow(Math.Pow(x - position.x, 2.0f) + Math.Pow(y - position.y, 2.0f), 0.5f);

        return chargeValue;
    }

    double[] ETotal(double x, double y)
    {
        double[] Exy = new double[2];

        Exy[0] = 0;
        Exy[1] = 0;

        for (int i = 0; i < charges.Length; i++)
        {
            //Debug.Log(i);
            //Debug.Log(charges[i]);
            //Debug.Log(particles[i].transform.position);
            //Debug.Log(x);
            //Debug.Log(y);
            double[] E = pointCharge(charges[i], particles[i].transform.position, x, y);

            Exy[0] += E[0];
            Exy[1] += E[1];
        }

        return Exy;
    }

    double[] EDir(double t, double[] y)
    {
        double[] Exy = ETotal(y[0], y[1]);

        double n = Math.Sqrt(Math.Pow(Exy[0] + Exy[1], 2.0f) * Exy[1]);

        Exy[0] /= n;
        Exy[1] /= n;

        return Exy;
    }


}
