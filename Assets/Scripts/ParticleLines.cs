using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;


public class ParticleLines
{
    public GameObject[] particles;
    public float[] charges;
    public bool showForces;

    private float particleRadius = 0.25f;
    private Vector3[] lookUpTable = {
        new Vector3(0.0f, 5.0f, 0.0f),
        new Vector3(0.0f, 4.0f, 0.0f),
        new Vector3(0.0f, 3.0f, 0.0f),
        new Vector3(0.0f, 2.0f, 0.0f),
        new Vector3(0.0f, 1.0f, 0.0f),
        new Vector3(0.0f, 0.0f, 0.0f),
        new Vector3(0.0f, -1.0f, 0.0f),
        new Vector3(0.0f, -2.0f, 0.0f),
        new Vector3(0.0f, -3.0f, 0.0f),
        new Vector3(0.0f, -4.0f, 0.0f),
        new Vector3(0.0f, -5.0f, 0.0f),
    };

    private float lineDefaultWidth = 0.010f;
    private float minimumInteractionDistance = 10.0f;
    private List<GameObject> lines = new List<GameObject>();
    private List<GameObject> arrows = new List<GameObject>();
    private SHOIntegrator theIntegrator;
    private double t = 0.0;
    private double h = 0.01;
    private int FIELD_LINES = 10;
    private float eps;
    private float EPSILON;
    private float e = 1.60217733E-19f;
    private int linesLimit = 10;

    public ParticleLines(GameObject[] particles, float[] charges)
    {
        this.particles = particles;
        this.charges = charges;
        this.eps = particleRadius / 50;
        this.EPSILON = eps / 1.0E5f;
        this.showForces = true;
        Array.Sort(charges, particles);
    }

    void AddNewLineRenderer(Vector3 start, Vector3 end, Color color, int linePos, bool otherSide = false, float duration = 55.0f)
    {
        GameObject go = new GameObject($"LineRenderer_particle_1");
        LineRenderer goLineRenderer = go.AddComponent<LineRenderer>();
        goLineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        goLineRenderer.startColor = Color.black;
        goLineRenderer.endColor = Color.black;
        goLineRenderer.startWidth = lineDefaultWidth;
        goLineRenderer.endWidth = lineDefaultWidth;

        List<Vector3> pointList = BezierCurve(start, end, 20, linePos);

        if (otherSide)
        {
            goLineRenderer.positionCount = (int)pointList.Count / 2;
            goLineRenderer.SetPositions(pointList.GetRange(0, (int)pointList.Count / 2).ToArray());
        }
        else
        {
            goLineRenderer.positionCount = pointList.Count;
            goLineRenderer.SetPositions(pointList.ToArray());
        }

        Vector3[] vertices = new Vector3[goLineRenderer.positionCount];
        goLineRenderer.GetPositions(vertices);
        for (int i = 1; i < vertices.Length - 2; i += 2)
        {
            GameObject arrow = GameObject.Instantiate(Resources.Load("Prefabs/arrow"), pointList[i], Quaternion.identity) as GameObject;
            arrow.transform.LookAt(pointList[i + 1]);
            // Debug.Log(pointList[i + 1]);
            arrows.Add(arrow);
            //arrow.transform.rotation = Quaternion.Euler(arrow.transform.rotation.eulerAngles.x, 0, arrow.transform.rotation.eulerAngles.z);

            Vector3 diff = pointList[i] - pointList[i + 1];
            diff.Normalize();

            float rot_z = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;
            arrow.transform.rotation = Quaternion.Euler(0f, 0f, rot_z - 180);

            if (i < 4 || i > vertices.Length - 4)
            {
                arrow.transform.localScale -= new Vector3(0.0103f, 0.0103f, 0.0103f);
            }

        }

        //Remove the lines
        //go.SetActive(false);

        lines.Add(go);
        //GameObject.Destroy(goLineRenderer, duration);
    }

    void AddNewLineRendererList(Color color, List<Vector3> pointList, float charge)
    {
        GameObject go = new GameObject($"LineRenderer_particle_1");
        LineRenderer goLineRenderer = go.AddComponent<LineRenderer>();
        goLineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        goLineRenderer.startColor = Color.black;
        goLineRenderer.endColor = Color.black;
        goLineRenderer.startWidth = lineDefaultWidth;
        goLineRenderer.endWidth = lineDefaultWidth;

        goLineRenderer.positionCount = pointList.Count;
        goLineRenderer.SetPositions(pointList.ToArray());

        if (this.showForces)
        {
            int numberArrow = pointList.Count / 6;

            if(charge > 0)
            {
                for (int i = 1; i < pointList.Count - numberArrow; i += numberArrow)
                {
                    AddArrow(pointList[i], pointList[i + 1]);
                }
            }
            else
            {
                for (int i = pointList.Count - numberArrow; i > 1; i -= numberArrow)
                {
                    AddArrow(pointList[i], pointList[i - 1]);
                }
            }

        }

        lines.Add(go);
    }

    List<Vector3> BezierCurve(Vector3 start, Vector3 end, int vertexCount, int linePosition)
    {
        List<Vector3> pointList = new List<Vector3>();

        Vector3 midpoint = new Vector3((start.x + end.x) / 2, (start.y + end.y) / 2, (start.z + end.z) / 2);

        for (float ratio = 0; ratio <= 1; ratio += 1.0f / vertexCount)
        {
            Vector3 tangentLineVertex1 = Vector3.Lerp(start, start + lookUpTable[linePosition], ratio);
            Vector3 tangentLineVertex2 = Vector3.Lerp(start + lookUpTable[linePosition], end + lookUpTable[linePosition], ratio);
            Vector3 resultsTanget1 = Vector3.Lerp(tangentLineVertex1, tangentLineVertex2, ratio);
            Vector3 resultsTanget2 = Vector3.Lerp(end + lookUpTable[linePosition], end, ratio);

            pointList.Add(Vector3.Lerp(resultsTanget1, resultsTanget2, ratio));
        }

        return pointList;
    }

    private float circleY(float h, float k, float x)
    {
        return Mathf.Sqrt(Mathf.Pow(particleRadius, 2.0f) - Mathf.Pow(x - h, 2.0f)) + k;
    }

    public void Draw(bool mode)
    {
        CleanLines();

        for (int i = 0; i < particles.Length; i++)
        {
            //drawParticleLines(i);
            //drawElectricLines(i);
            if (mode)
            {
                drawElectricLinesParticles2D(i);
            }
            else
            {
                drawElectricLinesParticles3D(i);
            }
        }
    }

    public void CleanLines()
    {
        if (lines.Count > 0)
        {
            foreach (GameObject line in lines)
                GameObject.Destroy(line);
            foreach (GameObject arrow in arrows)
                GameObject.Destroy(arrow);
        }
    }

    public void AddArrow(Vector3 position, Vector3 nextPosition)
    {
        GameObject arrow = GameObject.Instantiate(Resources.Load("Prefabs/arrow"), position, Quaternion.identity) as GameObject;
        arrow.transform.LookAt(nextPosition);
        // Debug.Log(pointList[i + 1]);
        arrows.Add(arrow);
        //arrow.transform.rotation = Quaternion.Euler(arrow.transform.rotation.eulerAngles.x, 0, arrow.transform.rotation.eulerAngles.z);

        Vector3 diff = position - nextPosition;
        diff.Normalize();

        float rot_z = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;
        arrow.transform.rotation = Quaternion.Euler(0f, 0f, rot_z - 180);
        arrow.transform.localScale -= new Vector3(0.0f, 0.00781458f, 0.0f);
    }

    int previus = -1;
    void drawParticleLines(int index)
    {
        if (charges[index] > 0)
        {
            bool left = false;
            bool right = true;
            for (int j = 0; j < particles.Length; j++)
            {

                if (j != index)
                {
                    bool toFarAway = (Vector3.Distance(particles[index].transform.position, particles[j].transform.position)) < minimumInteractionDistance;

                    if (particles[index].transform.position.x < particles[j].transform.position.x)
                    {
                        if (charges[j] < 0 && toFarAway)
                        {
                            Debug.Log("From positive to negative - 1");
                            drawLines(index, j, true, true);
                            previus = j;
                        }
                        else
                        {
                            Debug.Log("From positive to positive - 1");
                            drawLines(index, index, right, false);
                            right = !right;
                        }
                    }
                    else
                    {
                        if (charges[j] < 0 && toFarAway)
                        {
                            Debug.Log("From positive to negative - 2");
                            drawLines(index, j, false, true);
                            previus = j;
                        }
                        else
                        {
                            Debug.Log("From positive to positive - 2");
                            drawLines(index, index, left, false);
                            left = !left;
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="from">start particle</param>
    /// <param name="to">end particle</param>
    /// <param name="direction">true if to the right and false to the left</param>
    /// <param name="opposite">opposite signs </param>
    public void drawLines(int from, int to, bool direction, bool opposite)
    {
        float interval = (particleRadius * 2) / lookUpTable.Length;

        for (int i = 0; i < lookUpTable.Length; i++)
        {

            float y1 = circleY(particles[from].transform.position.x, particles[from].transform.position.y, particles[from].transform.position.x - particleRadius + interval * i);
            float y2 = 0.0f;
            float zMove = 0.0f;
            float xMove = 0.0f;

            if (opposite)
            {
                y2 = circleY(particles[to].transform.position.x, particles[to].transform.position.y, particles[to].transform.position.x - particleRadius + interval * i);
            }
            else
            {
                y2 = particles[from].transform.position.y + particleRadius + interval * i;

                //if(previus != -1)
                //{
                if (direction)
                {
                    xMove = 5.0f;

                }
                else
                {
                    xMove = -5.0f;
                }
                //}

            }

            AddNewLineRenderer(new Vector3(particles[from].transform.position.x, y1, particles[from].transform.position.z), new Vector3(particles[to].transform.position.x - xMove, y2, particles[to].transform.position.z - zMove), new Color(1.0f, 0.0f, 0.0f), i, !opposite);
        }

    }

    void drawElectricLinesParticles3D(int index)
    {
        for (int i = 0; i < FIELD_LINES; i++)
        {

            float x = particles[index].transform.position.x + eps * Mathf.Cos((float)(2 * Math.PI * i / FIELD_LINES));
            float y = particles[index].transform.position.y + eps * Mathf.Sin((float)(2 * Math.PI * i / FIELD_LINES));
            float z = particles[index].transform.position.z + eps * Mathf.Sin((float)(2 * Math.PI * i / FIELD_LINES));

            bool reachedAnotherCharge = false;
           
            // Check for infinite loop 
            bool infiniteLoop = false;
            int count = 0;
            float[] oldXs = { 0.0f, 0.0f };
            float[] oldYs = { 0.0f, 0.0f };
            float[] oldZs = { 0.0f, 0.0f };

            List<Vector3> lineField = new List<Vector3>();
            while (!reachedAnotherCharge && !infiniteLoop
                     && x > -linesLimit && x < linesLimit && y > -linesLimit && y < linesLimit
                     && z > -linesLimit && z < linesLimit)
            {

                // find the field (Ex, Ey, Ez) and field strength E at (x,y.z)
                float[] E = ETotal(x, y, z);
                float n = (float)Mathf.Sqrt(E[0] * E[0] + E[1] * E[1] + E[2] * E[2]);

                // if charge is negative the line needs to go backwards
                if (charges[index] > 0)
                {
                    x += E[0] / n * eps;
                    y += E[1] / n * eps;
                    z += E[2] / n * eps;
                }
                else
                {
                    x -= E[0] / n * eps;
                    y -= E[1] / n * eps;
                    z -= E[2] / n * eps;
                }

                lineField.Add(new Vector3(x, y, z));

                // stop in infinite loop
                if (Math.Abs(x - oldXs[0]) < EPSILON && Math.Abs(y - oldYs[0]) < EPSILON && Math.Abs(z - oldZs[0]) < EPSILON)
                {
                    infiniteLoop = true;
                }
                int index2 = count++ % 2;
                oldXs[index2] = x;
                oldYs[index2] = y;
                oldZs[index2] = z;


                // stop if the line ends in a charge
                for (int j = 0; j < charges.Length; j++)
                {
                    float dx = x - particles[j].transform.position.x;
                    float dy = y - particles[j].transform.position.y;
                    float dz = z - particles[j].transform.position.z;

                    if (Math.Sqrt(dx * dx + dy * dy + dz* dz) < eps) reachedAnotherCharge = true;
                }

            }

            AddNewLineRendererList(new Color(1.0f, 0.0f, 0.0f), lineField, charges[index]);

        }


    }

    void drawElectricLinesParticles2D(int index)
    {

        for (int i = 0; i < FIELD_LINES; i++)
        {
            float x = particles[index].transform.position.x + eps * Mathf.Cos((float)(2 * Math.PI * i / FIELD_LINES));
            float y = particles[index].transform.position.y + eps * Mathf.Sin((float)(2 * Math.PI * i / FIELD_LINES));

            bool reachedAnotherCharge = false;

            // Check for infinite loop 
            bool infiniteLoop = false;
            int count = 0;
            float[] oldXs = { 0.0f, 0.0f };
            float[] oldYs = { 0.0f, 0.0f };

            List<Vector3> lineField = new List<Vector3>();
            while (!reachedAnotherCharge && !infiniteLoop
                     && x > -linesLimit && x < linesLimit && y > -linesLimit && y < linesLimit)
            {

                // find the field (Ex, Ey, Ez) and field strength E at (x,y.z)
                float[] E = ETotal2D(x, y);
                float n = (float)Mathf.Sqrt(E[0] * E[0] + E[1] * E[1]);

                // if charge is negative the line needs to go backwards
                if (charges[index] > 0)
                {
                    x += E[0] / n * eps;
                    y += E[1] / n * eps;
                }
                else
                {
                    x -= E[0] / n * eps;
                    y -= E[1] / n * eps;
                }

                lineField.Add(new Vector3(x, y, 0));

                // stop in infinite loop
                if (Math.Abs(x - oldXs[0]) < EPSILON && Math.Abs(y - oldYs[0]) < EPSILON)
                {
                    infiniteLoop = true;
                }
                int index2 = count++ % 2;
                oldXs[index2] = x;
                oldYs[index2] = y;

                // stop if the line ends in a charge
                for (int j = 0; j < charges.Length; j++)
                {
                    float dx = x - particles[j].transform.position.x;
                    float dy = y - particles[j].transform.position.y;

                    if (Math.Sqrt(dx * dx + dy * dy) < eps) reachedAnotherCharge = true;
                }

            }

            AddNewLineRendererList(new Color(1.0f, 0.0f, 0.0f), lineField, charges[index]);

        }


    }

    public static float[] linspace(float startval, float endval, int steps)
    {
        float interval = (endval / Math.Abs(endval)) * Math.Abs(endval - startval) / (steps - 1);
        return (from val in Enumerable.Range(0, steps)
                select startval + (val * interval)).ToArray();
    }

    float[] pointCharge(float charge, Vector3 position, float x, float y, float z)
    {
        float distance = (float)Math.Pow(Math.Pow(x - position.x, 2.0f) + Math.Pow(y - position.y, 2.0f) + Math.Pow(z - position.z, 2.0f), 1.5f);

        float[] chargeOnPoint = new float[3];

        chargeOnPoint[0] = charge * (x - position.x) / distance;
        chargeOnPoint[1] = charge * (y - position.y) / distance;
        chargeOnPoint[2] = charge * (z - position.z) / distance;

        return chargeOnPoint;
    }

    float[] pointCharge2D(float charge, Vector3 position, float x, float y)
    {
        float distance = (float)Math.Pow(Math.Pow(x - position.x, 2.0f) + Math.Pow(y - position.y, 2.0f), 1.5f);

        float[] chargeOnPoint = new float[2];

        chargeOnPoint[0] = charge * (x - position.x) / distance;
        chargeOnPoint[1] = charge * (y - position.y) / distance;

        return chargeOnPoint;
    }

    float[] ETotal(float x, float y, float z)
    {
        float[] Exy = new float[3];

        Exy[0] = 0.0f;
        Exy[1] = 0.0f;
        Exy[2] = 0.0f;

        for (int i = 0; i < charges.Length; i++)
        {
            float xp = particles[i].transform.position.x;
            float yp = particles[i].transform.position.y;
            float zp = particles[i].transform.position.z;

            if (xp > -linesLimit && xp < linesLimit && yp > -linesLimit && yp < linesLimit
                     && zp > -linesLimit && zp < linesLimit)
            {
                float[] E = pointCharge(charges[i], particles[i].transform.position, x, y, z);

                Exy[0] = Exy[0] + E[0];
                Exy[1] = Exy[1] + E[1];
                Exy[2] = Exy[2] + E[2];
            }
        }

        return Exy;
    }

    float[] ETotal2D(float x, float y)
    {
        float[] Exy = new float[2];

        Exy[0] = 0.0f;
        Exy[1] = 0.0f;

        for (int i = 0; i < charges.Length; i++)
        {
            float[] E = pointCharge2D(charges[i], particles[i].transform.position, x, y);

            Exy[0] = Exy[0] + E[0];
            Exy[1] = Exy[1] + E[1];
        }

        return Exy;
    }


}
