using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using OVRTouchSample;
using System;
using UnityEngine.SceneManagement;
using UnityEditor;
using System.Linq;

public class MarchingCubesCustom : MonoBehaviour
{
    //Public variables
    public MeshFilter meshFilter;       // Mesh for marching cubes visualization
    public int dimension;               // Resolution of the surface
    public int n;                       // Number of particles
    public int GridSize;
    public GameObject[] particles;
    public float[] charges;
    public GameObject RHand;            // VR right controller
    public GameObject LHand;            // VR left controller
    public GameObject MenuCanvas;

    //For debugging 
    public bool DEBUG_GRID = false;
    public GameObject referenceText;
    public GameObject reference;

    //Boundary values for Marching Cubes
    int MINX;
    int MAXX;
    int MINY;
    int MAXY;
    int MINZ;
    int MAXZ;
    const float K = (9 * 10 ^ 9);      //Coulomb's law constant
    int nX, nY, nZ;                    //number of cells on each axis for Marching cubes

    private float maxCharge = -1.0f;
    private float minCharge = 10000.0f;
    private float maxChargeLog = -1.0f;
    private float minChargeLog = 10000.0f;
    private float minDistance = 10000.0f;
    Vector4[] points;                  // Vertex on the grid
    float[] pointsCharges;             // Electric field applied of each point of the grid 

    private float prom = 0;
    private float promLog = 0;

    int numTriangles = 0;         //Obtained by Marching Cubes

    // Variables being changed on runtime by user.
    int blendingFuncFlag = 1;
    int numOfElements = 7;  
    float minValueForSingle = 0.5f;
    Vector3[] lookUpTable = {
        new Vector3(1.0f, 1.0f, 1.0f),
        new Vector3(1.0f, 1.0f, -1.0f),
        new Vector3(1.0f, -1.0f, 1.0f),
        new Vector3(1.0f, -1.0f, -1.0f),
        new Vector3(-1.0f, 1.0f, 1.0f),
        new Vector3(-1.0f, 1.0f, -1.0f),
        new Vector3(-1.0f, -1.0f, 1.0f),
        new Vector3(-1.0f, -1.0f, -1.0f),
    };

    //factor of influence
    Vector3[] elements = {
        new Vector3(0.0f, 0.0f, 0.0f),
        new Vector3(1.0f, 0.0f, 0.0f),
        new Vector3(0.0f, 0.0f, -1.0f),
        new Vector3(-1.0f, 0.0f, 0.0f),
        new Vector3(0.0f, 0.0f, 1.0f),
        new Vector3(0.0f, 1.0f, 0.0f),
        new Vector3(0.0f, -1.0f, 0.0f),
    };
    float[] c = { 2.3f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f };

    // Lists for Mesh cosntruction
    HashSet<Vector3> vertices = new HashSet<Vector3>();
    Int32[] triangles;
    List<NODE> nodes = new List<NODE>();
    List<Vector3> QuadrantsLimits = new List<Vector3>();
    List<String> QuadrantsElements = new List<String>();
    int root = 0;

    // Lines class
    ParticleLines lineController;


    //Update actual view
    bool updateSurface = false;

    // Menu options
    public bool showLines = true;
    public bool Mode2D = true;
    public bool hapticFeedback = true;
    public bool showSurface = true;
    public bool simpleMode = true;
    public bool showMenu = true;
    public bool particleInteraction = true;



    // Start is called before the first frame update
    void Start()
    {
        MINX = -1 * GridSize;
        MAXX = GridSize;
        MINY = -1 * GridSize;
        MAXY = GridSize;
        MINZ = -1 * GridSize;
        MAXZ = GridSize;

        for (int i = 0; i < lookUpTable.Length; ++i)
        {
            QuadrantsLimits.Add(new Vector3(GridSize * lookUpTable[i].x, GridSize * lookUpTable[i].y, GridSize * lookUpTable[i].z));
            QuadrantsElements.Add("");
        }

        for (int i = 0; i < charges.Length; ++i)
        {
            particles[i].SetActive(false);

            if (charges[i] < 0)
            {
                particles[i].GetComponent<Renderer>().material.SetColor("_Color", Color.blue);
            }
        }

        nX = dimension;
        nY = dimension;
        nZ = dimension;
        createGrid();
        runMarchingCubes();
        lineController = new ParticleLines(particles, charges);
        if (showLines) {
            lineController.Draw(this.Mode2D);
        }
    }

    void FixedUpdate()
    {
        if (updateSurface == true)
        {
            runMarchingCubes();
            updateSurface = false;
        }

        if (showLines)
        {

            for (int i = 0; i < particles.Length; ++i)
            {
                if (particles[i].transform.hasChanged)
                {
                    particles[i].transform.hasChanged = false;
                    lineController.Draw(this.Mode2D);
                    break;
                }
            }

        }

    }

    void Update()
    {
        //findHandsVibration();
        if (hapticFeedback)
        {
            findHandsVibrationOptimized();
        }

        if (OVRInput.GetDown(OVRInput.Button.One))
        {
            updateSurface = true;
            //StartCoroutine(MarchingCubesRoutine());
        }

    }

    // Update is called once per frame
    void ClearMeshData()
    {
        //vertices.Clear();
        triangles = new Int32[numTriangles*3];
        nodes.Clear();
    }

    void clasifyPoint(Vector3 point, int index)
    {
        int quad = -1;

        for (int i = 0; i < QuadrantsLimits.Count; ++i)
        {
            if(Vector3.Distance(QuadrantsLimits[i], point) <= (GridSize*2)-1)
            {
                quad = i;
                break;
            }
        }

        QuadrantsElements[quad] += index + "-";
    } 

    int findQuadrant(Vector3 point)
    {
        if (point.x >= MINX && point.x <= MAXX && point.y >= MINY && point.y <= MAXY && point.z >= MINZ && point.z <= MAXZ)
        {
            for (int i = 0; i < QuadrantsLimits.Count; ++i)
            {
                if (Vector3.Distance(QuadrantsLimits[i], point) <= (GridSize * 2) - 1)
                {
                    return i;
                }
            }
        }

        return -1;
    }

    private TRIANGLE[] Triangles;
     
    void runMarchingCubes()
    {
        float minValue = minValueForSingle;

        Triangles = MarchingCubes(minValue);

        if (showSurface)
        {
            setMesh();
        }

        for (int i = 0; i < QuadrantsLimits.Count; ++i)
        {
            Debug.Log(QuadrantsElements[i]);
        }

        //for (int i = 0; i < points.Length - 1; ++i)
        //{
        //    GameObject duplicate = Instantiate(reference);
        //    duplicate.transform.position = new Vector3(points[i].x, points[i].y, points[i].z);
        //    GameObject duplicateText = Instantiate(referenceText);
        //    duplicateText.GetComponent<TextMeshPro>().text = i + " - " + points[i].x.ToString("F2") + "," + points[i].y.ToString("F2") + "," + points[i].z.ToString("F2");
        //    duplicateText.transform.position = new Vector3(points[i].x, points[i].y, points[i].z);
        //}

        //if (meshFilter && DEBUG_GRID)
        //{
        //    for (int i = 0; i < points.Length - 1; ++i)
        //    {
        //        GameObject duplicate = Instantiate(reference);
        //        duplicate.transform.position = new Vector3(points[i].x, points[i].y, points[i].z);
        //        GameObject duplicateText = Instantiate(referenceText);
        //        duplicateText.GetComponent<TextMeshPro>().text = normalizeCharge(pointsCharges[i]) + "";
        //        duplicateText.transform.position = new Vector3(points[i].x, points[i].y, points[i].z);
        //    }

        //    var savePath = "Assets/electric.asset";
        //    Debug.Log("Saved Mesh to:" + savePath);
        //    AssetDatabase.CreateAsset(meshFilter.mesh, savePath);
        //}
    }

    public void setMesh()
    {
        List<Vector3> verticesList = vertices.ToList();

        ClearMeshData();

        for (int i = 0; i < numTriangles; ++i)
        {
            Triangles[i].charge = normalizeCharge(Triangles[i].charge);

            Vector3 vertice1 = new Vector3(Triangles[i].points[0].x, Triangles[i].points[0].y, Triangles[i].points[0].z);
            Vector3 vertice2 = new Vector3(Triangles[i].points[1].x, Triangles[i].points[1].y, Triangles[i].points[1].z);
            Vector3 vertice3 = new Vector3(Triangles[i].points[2].x, Triangles[i].points[2].y, Triangles[i].points[2].z);

            triangles[(i * 3)] = verticesList.IndexOf(vertice1);
            triangles[(i * 3) + 1] = verticesList.IndexOf(vertice2);
            triangles[(i * 3) + 2] = verticesList.IndexOf(vertice3);
        }


        Mesh mesh = new Mesh();
        mesh.vertices = verticesList.ToArray();
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        meshFilter.mesh = mesh;

        //Array.Clear(Triangles, 0, Triangles.Length);
    }

    private IEnumerator MarchingCubesRoutine()
    {
        runMarchingCubes();
        yield return null;
    }

    float electromagnetism3(Vector3 currentPosition)
    {
        float totalForce = 0;

        for (int i = 0; i < n; ++i)
        {
            float r = Vector3.Distance(particles[i].transform.position, currentPosition);
            totalForce = totalForce + electricField(currentPosition, particles[i].transform.position, charges[i]) * blendFunction0(r); //Math.Abs(force(currentPosition, particles[i].transform.position, 1, charges[i]));
        }

        return totalForce;
    }

    float electromagnetismCharge(Vector3 currentPosition)
    {
        float totalForce = 0;

        for (int i = 0; i < n; i++)
        {
            totalForce = totalForce + electricField(currentPosition, particles[i].transform.position, charges[i]); //Math.Abs(force(currentPosition, particles[i].transform.position, 1, charges[i]));
        }

        return totalForce;
    }

    float electricField(Vector3 actualPoint, Vector3 particlePosition, float charge)
    {
        float distance = Vector3.Distance(particlePosition, actualPoint);

        return Math.Abs((K * charge) / (float)Math.Pow(distance, 2.0));
    }

    float implicitFunction(Vector3 p)
    {
        int max = 7;
        numOfElements = numOfElements < 1 ? 1 : (numOfElements > max ? max : numOfElements);

        float result = 0.0f;
        for (int i = 0; i < numOfElements; i++)
        {
            float r = Vector3.Distance(elements[i], p);
            switch (blendingFuncFlag)
            {
                case 0: result += c[i] * blendFunction0(r); break;
                case 1: result += c[i] * blendFunction1(r); break;
                case 2: result += c[i] * blendFunction2(r); break;
            }
        }
        return result;
    }

    float blendFunction0(float r)
    {
        return 1.0f / (float)Math.Pow(r, 2.0);
    }

    float blendFunction1(float r)
    {
        float a = 0.8f;
        float b = 0.1f;
        return (float)a * (float)Math.Exp(-b * (float)Math.Pow(r, 2.0));
    }

    float blendFunction2(float r)
    {
        float R = 4f;

        if (r >= R)
        {
            return 0;
        }

        return 1 - (4 / 9 * (float)Math.Pow(r, 6.0) / (float)Math.Pow(R, 6.0))
            + (17 / 9 * (float)Math.Pow(r, 4.0) / (float)Math.Pow(R, 4.0))
            + (22 / 9 * (float)Math.Pow(r, 2.0) / (float)Math.Pow(R, 2.0));

    }

    Vector3 cross(Vector3 x, Vector3 y)
    {
        return new Vector3(
            x.y * y.z - y.y * x.z,
            x.z * y.x - y.z * x.x,
            x.x * y.y - y.x * x.y);
    }

    Vector3 normalize(Vector3 x)
    {
        return x * (float)(1 / Math.Sqrt(x.x * x.x + x.y * x.y + x.z * x.z));
    }

    float normalizeCharge(float charge)
    {
        return Math.Abs((charge - minCharge) / (maxCharge - minCharge));
    }

    Vector3 intersection(Vector4 p1, Vector4 p2, float value) // Linear interpolation
    {
        Vector3 p;
        if (p1.w != p2.w)
            p = new Vector3(p1.x, p1.y, p1.z) + (new Vector3(p2.x, p2.y, p2.z) - new Vector3(p1.x, p1.y, p1.z)) / (p2.w - p1.w) * (value - p1.w);
        else
            p = new Vector3(p1.x, p1.y, p1.z);
        return p;
    }

    public void findHandsVibration()
    {
        float RAmplitude = 0;
        float LAmplitude = 0;

        Vector3 RPos = RHand.transform.position;
        Vector3 LPos = LHand.transform.position;

        int YtimesZ = (nY + 1) * (nZ + 1);    //for extra speed
        for (int i = 0; i < nX + 1; ++i)
        {
            int ni = i * YtimesZ;                       //for speed
            for (int j = 0; j < nY + 1; ++j)
            {
                int nj = j * (nZ + 1);             //for speed
                for (int k = 0; k < nZ + 1; ++k)
                {
                    if (RAmplitude == 0)
                    {
                        float rTemp = IsInMyCube(new Vector3(points[ni + nj + k].x, points[ni + nj + k].y, points[ni + nj + k].z), RPos, ni + nj + k);
                        if (rTemp != -1)
                        {
                            RAmplitude = rTemp;
                        }
                    }

                    if (LAmplitude == 0)
                    {
                        float lTemp = IsInMyCube(new Vector3(points[ni + nj + k].x, points[ni + nj + k].y, points[ni + nj + k].z), LPos, ni + nj + k);
                        if (lTemp != -1)
                        {
                            LAmplitude = lTemp;

                        }
                    }

                    if (RAmplitude != 0 && LAmplitude != 0)
                    {
                        Debug.Log(RAmplitude);
                        Debug.Log(LAmplitude);
                        break;
                    };
                }
            }

        }

        OVRInput.SetControllerVibration(1, RAmplitude, OVRInput.Controller.RTouch);
        OVRInput.SetControllerVibration(1, LAmplitude, OVRInput.Controller.LTouch);
    }

    public void findHandsVibrationOptimized()
    {
        float RAmplitude = 0;
        float LAmplitude = 0;

        Vector3 RPos = RHand.transform.position;
        Vector3 LPos = LHand.transform.position;

        int quadIndexR = findQuadrant(RPos);
        int quadIndexL = findQuadrant(LPos);

        if (quadIndexR == quadIndexL && quadIndexR != -1)
        {
            String[] indexQuad = QuadrantsElements[quadIndexR].Split('-');

            for (int i = 0; i < indexQuad.Length - 1; ++i)
            {
                if (RAmplitude == 0)
                {
                    float rTemp = IsInMyCube(new Vector3(points[Int32.Parse(indexQuad[i])].x, points[Int32.Parse(indexQuad[i])].y, points[Int32.Parse(indexQuad[i])].z), RPos, Int32.Parse(indexQuad[i]));
                    if (rTemp != -1)
                    {
                        RAmplitude = rTemp;
                    }
                }

                if (LAmplitude == 0)
                {
                    float lTemp = IsInMyCube(new Vector3(points[Int32.Parse(indexQuad[i])].x, points[Int32.Parse(indexQuad[i])].y, points[Int32.Parse(indexQuad[i])].z), LPos, Int32.Parse(indexQuad[i]));
                    if (lTemp != -1)
                    {
                        LAmplitude = lTemp;

                    }
                }

                if (RAmplitude != 0 && LAmplitude != 0)
                {
                    Debug.Log(RAmplitude);
                    Debug.Log(LAmplitude);
                    break;
                };
            }
        }
        else
        {

            if(quadIndexR != -1)
            {
                String[] indexQuadR = QuadrantsElements[quadIndexR].Split('-');
                for (int i = 0; i < indexQuadR.Length - 1; ++i)
                {
                    float rTemp = IsInMyCube(new Vector3(points[Int32.Parse(indexQuadR[i])].x, points[Int32.Parse(indexQuadR[i])].y, points[Int32.Parse(indexQuadR[i])].z), RPos, Int32.Parse(indexQuadR[i]));
                    if (rTemp != -1)
                    {
                        RAmplitude = rTemp;
                        break;
                    }
                }
            }

            if (quadIndexL != -1)
            {
                String[] indexQuadL = QuadrantsElements[quadIndexL].Split('-');

                for (int i = 0; i < indexQuadL.Length - 1; ++i)
                {
                    float lTemp = IsInMyCube(new Vector3(points[Int32.Parse(indexQuadL[i])].x, points[Int32.Parse(indexQuadL[i])].y, points[Int32.Parse(indexQuadL[i])].z), LPos, Int32.Parse(indexQuadL[i]));
                    if (lTemp != -1)
                    {
                        LAmplitude = lTemp;
                        break;
                    }
                }
            }

        }

        OVRInput.SetControllerVibration(1, RAmplitude, OVRInput.Controller.RTouch);
        OVRInput.SetControllerVibration(1, LAmplitude, OVRInput.Controller.LTouch);
    }


    public void findHandsVibrationOptimized2()
    {
        float RAmplitude = 0;
        float LAmplitude = 0;

        Vector3 RPos = RHand.transform.position;
        Vector3 LPos = LHand.transform.position;

        RAmplitude = amplitudeByNear(RPos);

        //LAmplitude = amplitudeByNear(LPos);

        OVRInput.SetControllerVibration(1, RAmplitude, OVRInput.Controller.RTouch);
        OVRInput.SetControllerVibration(1, LAmplitude, OVRInput.Controller.LTouch);
    }

    float amplitudeByNear(Vector3 gridPosition)
    {
        decimal x = 0;
        decimal y = 0;
        decimal z = 0;
        float amplitude = 0;
        decimal diffG = 0;


        if (gridPosition.x >= MINX && gridPosition.x <= MAXX && gridPosition.y >= MINY && gridPosition.y <= MAXY && gridPosition.z >= MINZ && gridPosition.z <= MAXZ) {

            if (!simpleMode)
            {
                amplitude = 1;
            }
            else
            {
                diffG = (MAXX * 2) / dimension;

                if (gridPosition.x > 0)
                {
                    x += (dimension / 2)  + (MAXX / diffG) - ((decimal)(MAXX - Mathf.Abs(gridPosition.x)) / diffG);
                }
                else
                {
                    x += ((decimal)(MAXX - Math.Abs(gridPosition.x)) / diffG);
                }

                x = x * (dimension + 1) * (dimension + 1);

                diffG = (MAXY * 2) / dimension;

                if (gridPosition.y > 0)
                {
                    y += (dimension / 2) + (MAXY / diffG) - ((decimal)(MAXY - Mathf.Abs(gridPosition.y)) / diffG);

                } else
                {
                    y += ((decimal)(MAXY - Mathf.Abs(gridPosition.y)) / diffG);
                }


                y = y * (dimension + 1);

                diffG = (MAXZ * 2) / dimension;

                if (gridPosition.z > 0)
                {
                    z += (dimension / 2) + (MAXZ / diffG) - ((decimal)(MAXZ - Mathf.Abs(gridPosition.z)) / diffG);
                } else
                {
                   z += ((decimal)(MAXZ - Mathf.Abs(gridPosition.z)) / diffG);
                }

                //Debug.Log("GridPos -> " + x + "," + y + "," + z + " - " + (x + y + z));


                Debug.Log(gridPosition + " - " + (int)(x + y + z));


                amplitude = normalizeCharge(pointsCharges[(int)(x + y + z)]);
            }
        }

        return amplitude;
    }

    float findHandIntersection(float score, int currentNode)
    {
        if (Math.Abs(score - nodes[currentNode].Score) < 0.5)
        {
            return normalizeCharge(nodes[currentNode].Charge);
        }
        else
        {
            if (nodes[currentNode].RightChild != -1 && score < nodes[currentNode].Score)
            {
                return findHandIntersection(score, nodes[currentNode].RightChild);
            }
            else
            {
                if (nodes[currentNode].LeftChild != -1 && score > nodes[currentNode].Score)
                {
                    return findHandIntersection(score, nodes[currentNode].LeftChild);
                }
                else
                {
                    return 0;
                }
            }
        }
    }

    public void showLine(bool value)
    {
        this.showLines = value;
        if (value)
        {
            lineController.Draw(this.Mode2D);
        }
        else
        {
            lineController.CleanLines();
        }
    }

    public void Mode2DState(bool value)
    {
        this.Mode2D = value;
        if(this.showLines)
            lineController.Draw(this.Mode2D);
    }

    public void hapticState(bool value)
    {
        this.hapticFeedback = value;
    }

    public void hapticSimple(bool value)
    {
        this.simpleMode = value;
    }

    public void showSurfaceState(bool value)
    {
        this.showSurface = value;
        if (value)
        {
            setMesh();
        }
        else
        {
            meshFilter.mesh = null;
        }
    }

    public void showForceDirection(bool value)
    {
        this.lineController.showForces = value;
        if (this.showLines)
            lineController.Draw(this.Mode2D);
    }

    public void removeParticleInteraction(bool value)
    {
        for (int i = 0; i < particles.Length; ++i)
        {
            particles[i].GetComponent<OVRGrabbable>().enabled = !particles[i].GetComponent<OVRGrabbable>().enabled;
            particles[i].GetComponent<SphereCollider>().enabled = !particles[i].GetComponent<SphereCollider>().enabled;
        }
    }

    public void onModeA()
    {
        showLines = true;
        particleInteraction = true;
        MenuCanvas.SetActive(false);
        //RHand.GetComponent<UnityEngine.XR.Interaction.Toolkit.XRInteractorLineVisual>().enabled = showMenu;


        for (int i = 0; i < charges.Length; ++i)
        {
            particles[i].SetActive(true);
        }

        //Mode A conditions
        hapticFeedback = false;
        simpleMode = false;
        showSurface = true;
    }

    public void onModeB()
    {
        showLines = true;
        particleInteraction = true;
        MenuCanvas.SetActive(false);
        //RHand.GetComponent<UnityEngine.XR.Interaction.Toolkit.XRInteractorLineVisual>().enabled = showMenu;

        for (int i = 0; i < charges.Length; ++i)
        {
            particles[i].SetActive(true);
        }

        //Mode B conditions
        hapticFeedback = true;
        simpleMode = true;
        Mode2D = false;
        showSurface = true;
    }

    public float IsInMyCube(Vector3 gridPosition, Vector3 location, int index)
    {
        float amplitude = -1;

        if (Vector3.Distance(gridPosition, location) < 0.25)
        {
            if(pointsCharges[index] < maxCharge && simpleMode)
            {
                 amplitude = normalizeCharge(pointsCharges[index]);
            } else
            {
                amplitude = 1;
            }
        } 

        return amplitude;
    }

    private Vector3 stepSize;

    void createGrid()
    {
        points = new Vector4[(nX + 1) * (nY + 1) * (nZ + 1)];
        pointsCharges = new float[(nX + 1) * (nY + 1) * (nZ + 1)];
        stepSize = new Vector3((float)(MAXX - MINX) / (float)nX, (float)(MAXX - MINY) / (float)nY, (float)(MAXZ - MINZ) / (float)nZ); 

        Debug.Log(stepSize);

        int YtimesZ = (nY + 1) * (nY + 1);    //for extra speed
        for (int i = 0; i < nX + 1; ++i)
        {
            int ni = i * YtimesZ;                       //for speed
            float vertX = MINX + i * stepSize.x;
            for (int j = 0; j < nY + 1; ++j)
            {
                int nj = j * (nZ + 1);             //for speed
                float vertY = MINY + j * stepSize.y;
                for (int k = 0; k < nZ + 1; ++k)
                {
                    Vector4 vert = new Vector4(vertX, vertY, MINZ + k * stepSize.z, 0);

                    int ind = ni + nj + k;

                    points[ind] = vert;
                    pointsCharges[ind] = (float)Math.Log10(electromagnetismCharge(new Vector3(vert.x, vert.y, vert.z)));
                    clasifyPoint(new Vector3(vert.x, vert.y, vert.z), ind);

                }
            }
        }
    }

    //	MARCHING CUBES	//
    TRIANGLE[] MarchingCubes(float minValue)
    {
        for (int i = 0; i < (nX + 1) * (nY + 1) * (nZ + 1); ++i)
        {
            points[i].w = electromagnetism3(new Vector3(points[i].x, points[i].y, points[i].z));/*(step 3)*/
            pointsCharges[i] = (float)Math.Log10(electromagnetismCharge(new Vector3(points[i].x, points[i].y, points[i].z)));

            if (pointsCharges[i] > maxCharge)
            {
                maxCharge = pointsCharges[i];
            }
            else
            {
                if (pointsCharges[i] < minCharge)
                {
                    minCharge = pointsCharges[i];
                }
            }
	    }

	TRIANGLE[] triangles = new TRIANGLE[3 * nX * nY * nZ];    //this should be enough space, if not change 4 to 5
    numTriangles = 0;

    int YtimeZ = (nY + 1) * (nZ + 1);
    //go through all the points
    for (int i = 0; i < nX; ++i)           //x axis
        for (int j = 0; j < nY; ++j)       //y axis
            for (int k = 0; k < nZ; ++k)   //z axis
            {
                //initialize vertices
                Vector4[] verts = new Vector4[8];
                int ind = i * YtimeZ + j * (nZ + 1) + k;
                /*(step 3)*/

                verts[0] = points[ind];
                verts[1] = points[ind + YtimeZ];
                verts[2] = points[ind + YtimeZ + 1];
                verts[3] = points[ind + 1];
                verts[4] = points[ind + (nZ + 1)];
                verts[5] = points[ind + YtimeZ + (nZ + 1)];
                verts[6] = points[ind + YtimeZ + (nZ + 1) + 1];
                verts[7] = points[ind + (nZ + 1) + 1];

                //get the index
                int cubeIndex = 0;
                for (int n = 0; n < 8; n++)
                    /*(step 4)*/
                    if (verts[n].w <= minValue) cubeIndex |= (1 << n);

                    //check if its completely inside or outside
                    /*(step 5)*/
                    if (cubeIndex == 0 || cubeIndex == 255) 
                        continue;

                //get intersection vertices on edges and save into the array    
                Vector3[] intVerts = new Vector3[12];
                /*(step 6)*/
                if ((edgeTable[cubeIndex] & 1) > 0) intVerts[0] = intersection(verts[0], verts[1], minValue);
                if ((edgeTable[cubeIndex] & 2) > 0) intVerts[1] = intersection(verts[1], verts[2], minValue);
                if ((edgeTable[cubeIndex] & 4) > 0) intVerts[2] = intersection(verts[2], verts[3], minValue);
                if ((edgeTable[cubeIndex] & 8) > 0) intVerts[3] = intersection(verts[3], verts[0], minValue);
                if ((edgeTable[cubeIndex] & 16) > 0) intVerts[4] = intersection(verts[4], verts[5], minValue);
                if ((edgeTable[cubeIndex] & 32) > 0) intVerts[5] = intersection(verts[5], verts[6], minValue);
                if ((edgeTable[cubeIndex] & 64) > 0) intVerts[6] = intersection(verts[6], verts[7], minValue);
                if ((edgeTable[cubeIndex] & 128) > 0) intVerts[7] = intersection(verts[7], verts[4], minValue);
                if ((edgeTable[cubeIndex] & 256) > 0) intVerts[8] = intersection(verts[0], verts[4], minValue);
                if ((edgeTable[cubeIndex] & 512) > 0) intVerts[9] = intersection(verts[1], verts[5], minValue);
                if ((edgeTable[cubeIndex] & 1024) > 0) intVerts[10] = intersection(verts[2], verts[6], minValue);
                if ((edgeTable[cubeIndex] & 2048) > 0) intVerts[11] = intersection(verts[3], verts[7], minValue);

                    //now build the triangles using triTable
                    for (int n = 0; triTable[cubeIndex, n] != -1; n += 3)
                    {
                        vertices.Add(intVerts[triTable[cubeIndex, n + 1]]);
                        vertices.Add(intVerts[triTable[cubeIndex, n]]);
                        vertices.Add(intVerts[triTable[cubeIndex, n + 2]]);

                        triangles[numTriangles] = new TRIANGLE(new Vector3[] { intVerts[triTable[cubeIndex, n + 2]], intVerts[triTable[cubeIndex, n + 1]], intVerts[triTable[cubeIndex, n]] }, new Vector3(0, 0, 0));
                        numTriangles++;
                    }


                }   //END OF FOR LOOP

        return triangles;
    }

    void insertNode(int index, int current)
    {
        bool swInsertion = true;
        int currentNode = current;

        while (swInsertion == true)
        {
            if (nodes[index].Score < nodes[currentNode].Score)
            {
                if (nodes[currentNode].RightChild != -1)
                {
                    currentNode = nodes[currentNode].RightChild;
                }
                else
                {
                    nodes[currentNode].RightChild = index;
                    swInsertion = false;
                }
            }
            else
            {
                if (nodes[currentNode].LeftChild != -1)
                {
                    currentNode = nodes[currentNode].LeftChild;
                }
                else
                {
                    nodes[currentNode].LeftChild = index;
                    swInsertion = false;
                }
            }
        }
    }

    static int[] edgeTable ={
    0x0  , 0x109, 0x203, 0x30a, 0x406, 0x50f, 0x605, 0x70c,
    0x80c, 0x905, 0xa0f, 0xb06, 0xc0a, 0xd03, 0xe09, 0xf00,
    0x190, 0x99 , 0x393, 0x29a, 0x596, 0x49f, 0x795, 0x69c,
    0x99c, 0x895, 0xb9f, 0xa96, 0xd9a, 0xc93, 0xf99, 0xe90,
    0x230, 0x339, 0x33 , 0x13a, 0x636, 0x73f, 0x435, 0x53c,
    0xa3c, 0xb35, 0x83f, 0x936, 0xe3a, 0xf33, 0xc39, 0xd30,
    0x3a0, 0x2a9, 0x1a3, 0xaa , 0x7a6, 0x6af, 0x5a5, 0x4ac,
    0xbac, 0xaa5, 0x9af, 0x8a6, 0xfaa, 0xea3, 0xda9, 0xca0,
    0x460, 0x569, 0x663, 0x76a, 0x66 , 0x16f, 0x265, 0x36c,
    0xc6c, 0xd65, 0xe6f, 0xf66, 0x86a, 0x963, 0xa69, 0xb60,
    0x5f0, 0x4f9, 0x7f3, 0x6fa, 0x1f6, 0xff , 0x3f5, 0x2fc,
    0xdfc, 0xcf5, 0xfff, 0xef6, 0x9fa, 0x8f3, 0xbf9, 0xaf0,
    0x650, 0x759, 0x453, 0x55a, 0x256, 0x35f, 0x55 , 0x15c,
    0xe5c, 0xf55, 0xc5f, 0xd56, 0xa5a, 0xb53, 0x859, 0x950,
    0x7c0, 0x6c9, 0x5c3, 0x4ca, 0x3c6, 0x2cf, 0x1c5, 0xcc ,
    0xfcc, 0xec5, 0xdcf, 0xcc6, 0xbca, 0xac3, 0x9c9, 0x8c0,
    0x8c0, 0x9c9, 0xac3, 0xbca, 0xcc6, 0xdcf, 0xec5, 0xfcc,
    0xcc , 0x1c5, 0x2cf, 0x3c6, 0x4ca, 0x5c3, 0x6c9, 0x7c0,
    0x950, 0x859, 0xb53, 0xa5a, 0xd56, 0xc5f, 0xf55, 0xe5c,
    0x15c, 0x55 , 0x35f, 0x256, 0x55a, 0x453, 0x759, 0x650,
    0xaf0, 0xbf9, 0x8f3, 0x9fa, 0xef6, 0xfff, 0xcf5, 0xdfc,
    0x2fc, 0x3f5, 0xff , 0x1f6, 0x6fa, 0x7f3, 0x4f9, 0x5f0,
    0xb60, 0xa69, 0x963, 0x86a, 0xf66, 0xe6f, 0xd65, 0xc6c,
    0x36c, 0x265, 0x16f, 0x66 , 0x76a, 0x663, 0x569, 0x460,
    0xca0, 0xda9, 0xea3, 0xfaa, 0x8a6, 0x9af, 0xaa5, 0xbac,
    0x4ac, 0x5a5, 0x6af, 0x7a6, 0xaa , 0x1a3, 0x2a9, 0x3a0,
    0xd30, 0xc39, 0xf33, 0xe3a, 0x936, 0x83f, 0xb35, 0xa3c,
    0x53c, 0x435, 0x73f, 0x636, 0x13a, 0x33 , 0x339, 0x230,
    0xe90, 0xf99, 0xc93, 0xd9a, 0xa96, 0xb9f, 0x895, 0x99c,
    0x69c, 0x795, 0x49f, 0x596, 0x29a, 0x393, 0x99 , 0x190,
    0xf00, 0xe09, 0xd03, 0xc0a, 0xb06, 0xa0f, 0x905, 0x80c,
    0x70c, 0x605, 0x50f, 0x406, 0x30a, 0x203, 0x109, 0x0   };

    static int[,] triTable = new int[,]
    {{-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {0, 8, 3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {0, 1, 9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {1, 8, 3, 9, 8, 1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {1, 2, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {0, 8, 3, 1, 2, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {9, 2, 10, 0, 2, 9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {2, 8, 3, 2, 10, 8, 10, 9, 8, -1, -1, -1, -1, -1, -1, -1},
    {3, 11, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {0, 11, 2, 8, 11, 0, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {1, 9, 0, 2, 3, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {1, 11, 2, 1, 9, 11, 9, 8, 11, -1, -1, -1, -1, -1, -1, -1},
    {3, 10, 1, 11, 10, 3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {0, 10, 1, 0, 8, 10, 8, 11, 10, -1, -1, -1, -1, -1, -1, -1},
    {3, 9, 0, 3, 11, 9, 11, 10, 9, -1, -1, -1, -1, -1, -1, -1},
    {9, 8, 10, 10, 8, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {4, 7, 8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {4, 3, 0, 7, 3, 4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {0, 1, 9, 8, 4, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {4, 1, 9, 4, 7, 1, 7, 3, 1, -1, -1, -1, -1, -1, -1, -1},
    {1, 2, 10, 8, 4, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {3, 4, 7, 3, 0, 4, 1, 2, 10, -1, -1, -1, -1, -1, -1, -1},
    {9, 2, 10, 9, 0, 2, 8, 4, 7, -1, -1, -1, -1, -1, -1, -1},
    {2, 10, 9, 2, 9, 7, 2, 7, 3, 7, 9, 4, -1, -1, -1, -1},
    {8, 4, 7, 3, 11, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {11, 4, 7, 11, 2, 4, 2, 0, 4, -1, -1, -1, -1, -1, -1, -1},
    {9, 0, 1, 8, 4, 7, 2, 3, 11, -1, -1, -1, -1, -1, -1, -1},
    {4, 7, 11, 9, 4, 11, 9, 11, 2, 9, 2, 1, -1, -1, -1, -1},
    {3, 10, 1, 3, 11, 10, 7, 8, 4, -1, -1, -1, -1, -1, -1, -1},
    {1, 11, 10, 1, 4, 11, 1, 0, 4, 7, 11, 4, -1, -1, -1, -1},
    {4, 7, 8, 9, 0, 11, 9, 11, 10, 11, 0, 3, -1, -1, -1, -1},
    {4, 7, 11, 4, 11, 9, 9, 11, 10, -1, -1, -1, -1, -1, -1, -1},
    {9, 5, 4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {9, 5, 4, 0, 8, 3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {0, 5, 4, 1, 5, 0, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {8, 5, 4, 8, 3, 5, 3, 1, 5, -1, -1, -1, -1, -1, -1, -1},
    {1, 2, 10, 9, 5, 4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {3, 0, 8, 1, 2, 10, 4, 9, 5, -1, -1, -1, -1, -1, -1, -1},
    {5, 2, 10, 5, 4, 2, 4, 0, 2, -1, -1, -1, -1, -1, -1, -1},
    {2, 10, 5, 3, 2, 5, 3, 5, 4, 3, 4, 8, -1, -1, -1, -1},
    {9, 5, 4, 2, 3, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {0, 11, 2, 0, 8, 11, 4, 9, 5, -1, -1, -1, -1, -1, -1, -1},
    {0, 5, 4, 0, 1, 5, 2, 3, 11, -1, -1, -1, -1, -1, -1, -1},
    {2, 1, 5, 2, 5, 8, 2, 8, 11, 4, 8, 5, -1, -1, -1, -1},
    {10, 3, 11, 10, 1, 3, 9, 5, 4, -1, -1, -1, -1, -1, -1, -1},
    {4, 9, 5, 0, 8, 1, 8, 10, 1, 8, 11, 10, -1, -1, -1, -1},
    {5, 4, 0, 5, 0, 11, 5, 11, 10, 11, 0, 3, -1, -1, -1, -1},
    {5, 4, 8, 5, 8, 10, 10, 8, 11, -1, -1, -1, -1, -1, -1, -1},
    {9, 7, 8, 5, 7, 9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {9, 3, 0, 9, 5, 3, 5, 7, 3, -1, -1, -1, -1, -1, -1, -1},
    {0, 7, 8, 0, 1, 7, 1, 5, 7, -1, -1, -1, -1, -1, -1, -1},
    {1, 5, 3, 3, 5, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {9, 7, 8, 9, 5, 7, 10, 1, 2, -1, -1, -1, -1, -1, -1, -1},
    {10, 1, 2, 9, 5, 0, 5, 3, 0, 5, 7, 3, -1, -1, -1, -1},
    {8, 0, 2, 8, 2, 5, 8, 5, 7, 10, 5, 2, -1, -1, -1, -1},
    {2, 10, 5, 2, 5, 3, 3, 5, 7, -1, -1, -1, -1, -1, -1, -1},
    {7, 9, 5, 7, 8, 9, 3, 11, 2, -1, -1, -1, -1, -1, -1, -1},
    {9, 5, 7, 9, 7, 2, 9, 2, 0, 2, 7, 11, -1, -1, -1, -1},
    {2, 3, 11, 0, 1, 8, 1, 7, 8, 1, 5, 7, -1, -1, -1, -1},
    {11, 2, 1, 11, 1, 7, 7, 1, 5, -1, -1, -1, -1, -1, -1, -1},
    {9, 5, 8, 8, 5, 7, 10, 1, 3, 10, 3, 11, -1, -1, -1, -1},
    {5, 7, 0, 5, 0, 9, 7, 11, 0, 1, 0, 10, 11, 10, 0, -1},
    {11, 10, 0, 11, 0, 3, 10, 5, 0, 8, 0, 7, 5, 7, 0, -1},
    {11, 10, 5, 7, 11, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {10, 6, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {0, 8, 3, 5, 10, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {9, 0, 1, 5, 10, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {1, 8, 3, 1, 9, 8, 5, 10, 6, -1, -1, -1, -1, -1, -1, -1},
    {1, 6, 5, 2, 6, 1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {1, 6, 5, 1, 2, 6, 3, 0, 8, -1, -1, -1, -1, -1, -1, -1},
    {9, 6, 5, 9, 0, 6, 0, 2, 6, -1, -1, -1, -1, -1, -1, -1},
    {5, 9, 8, 5, 8, 2, 5, 2, 6, 3, 2, 8, -1, -1, -1, -1},
    {2, 3, 11, 10, 6, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {11, 0, 8, 11, 2, 0, 10, 6, 5, -1, -1, -1, -1, -1, -1, -1},
    {0, 1, 9, 2, 3, 11, 5, 10, 6, -1, -1, -1, -1, -1, -1, -1},
    {5, 10, 6, 1, 9, 2, 9, 11, 2, 9, 8, 11, -1, -1, -1, -1},
    {6, 3, 11, 6, 5, 3, 5, 1, 3, -1, -1, -1, -1, -1, -1, -1},
    {0, 8, 11, 0, 11, 5, 0, 5, 1, 5, 11, 6, -1, -1, -1, -1},
    {3, 11, 6, 0, 3, 6, 0, 6, 5, 0, 5, 9, -1, -1, -1, -1},
    {6, 5, 9, 6, 9, 11, 11, 9, 8, -1, -1, -1, -1, -1, -1, -1},
    {5, 10, 6, 4, 7, 8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {4, 3, 0, 4, 7, 3, 6, 5, 10, -1, -1, -1, -1, -1, -1, -1},
    {1, 9, 0, 5, 10, 6, 8, 4, 7, -1, -1, -1, -1, -1, -1, -1},
    {10, 6, 5, 1, 9, 7, 1, 7, 3, 7, 9, 4, -1, -1, -1, -1},
    {6, 1, 2, 6, 5, 1, 4, 7, 8, -1, -1, -1, -1, -1, -1, -1},
    {1, 2, 5, 5, 2, 6, 3, 0, 4, 3, 4, 7, -1, -1, -1, -1},
    {8, 4, 7, 9, 0, 5, 0, 6, 5, 0, 2, 6, -1, -1, -1, -1},
    {7, 3, 9, 7, 9, 4, 3, 2, 9, 5, 9, 6, 2, 6, 9, -1},
    {3, 11, 2, 7, 8, 4, 10, 6, 5, -1, -1, -1, -1, -1, -1, -1},
    {5, 10, 6, 4, 7, 2, 4, 2, 0, 2, 7, 11, -1, -1, -1, -1},
    {0, 1, 9, 4, 7, 8, 2, 3, 11, 5, 10, 6, -1, -1, -1, -1},
    {9, 2, 1, 9, 11, 2, 9, 4, 11, 7, 11, 4, 5, 10, 6, -1},
    {8, 4, 7, 3, 11, 5, 3, 5, 1, 5, 11, 6, -1, -1, -1, -1},
    {5, 1, 11, 5, 11, 6, 1, 0, 11, 7, 11, 4, 0, 4, 11, -1},
    {0, 5, 9, 0, 6, 5, 0, 3, 6, 11, 6, 3, 8, 4, 7, -1},
    {6, 5, 9, 6, 9, 11, 4, 7, 9, 7, 11, 9, -1, -1, -1, -1},
    {10, 4, 9, 6, 4, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {4, 10, 6, 4, 9, 10, 0, 8, 3, -1, -1, -1, -1, -1, -1, -1},
    {10, 0, 1, 10, 6, 0, 6, 4, 0, -1, -1, -1, -1, -1, -1, -1},
    {8, 3, 1, 8, 1, 6, 8, 6, 4, 6, 1, 10, -1, -1, -1, -1},
    {1, 4, 9, 1, 2, 4, 2, 6, 4, -1, -1, -1, -1, -1, -1, -1},
    {3, 0, 8, 1, 2, 9, 2, 4, 9, 2, 6, 4, -1, -1, -1, -1},
    {0, 2, 4, 4, 2, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {8, 3, 2, 8, 2, 4, 4, 2, 6, -1, -1, -1, -1, -1, -1, -1},
    {10, 4, 9, 10, 6, 4, 11, 2, 3, -1, -1, -1, -1, -1, -1, -1},
    {0, 8, 2, 2, 8, 11, 4, 9, 10, 4, 10, 6, -1, -1, -1, -1},
    {3, 11, 2, 0, 1, 6, 0, 6, 4, 6, 1, 10, -1, -1, -1, -1},
    {6, 4, 1, 6, 1, 10, 4, 8, 1, 2, 1, 11, 8, 11, 1, -1},
    {9, 6, 4, 9, 3, 6, 9, 1, 3, 11, 6, 3, -1, -1, -1, -1},
    {8, 11, 1, 8, 1, 0, 11, 6, 1, 9, 1, 4, 6, 4, 1, -1},
    {3, 11, 6, 3, 6, 0, 0, 6, 4, -1, -1, -1, -1, -1, -1, -1},
    {6, 4, 8, 11, 6, 8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {7, 10, 6, 7, 8, 10, 8, 9, 10, -1, -1, -1, -1, -1, -1, -1},
    {0, 7, 3, 0, 10, 7, 0, 9, 10, 6, 7, 10, -1, -1, -1, -1},
    {10, 6, 7, 1, 10, 7, 1, 7, 8, 1, 8, 0, -1, -1, -1, -1},
    {10, 6, 7, 10, 7, 1, 1, 7, 3, -1, -1, -1, -1, -1, -1, -1},
    {1, 2, 6, 1, 6, 8, 1, 8, 9, 8, 6, 7, -1, -1, -1, -1},
    {2, 6, 9, 2, 9, 1, 6, 7, 9, 0, 9, 3, 7, 3, 9, -1},
    {7, 8, 0, 7, 0, 6, 6, 0, 2, -1, -1, -1, -1, -1, -1, -1},
    {7, 3, 2, 6, 7, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {2, 3, 11, 10, 6, 8, 10, 8, 9, 8, 6, 7, -1, -1, -1, -1},
    {2, 0, 7, 2, 7, 11, 0, 9, 7, 6, 7, 10, 9, 10, 7, -1},
    {1, 8, 0, 1, 7, 8, 1, 10, 7, 6, 7, 10, 2, 3, 11, -1},
    {11, 2, 1, 11, 1, 7, 10, 6, 1, 6, 7, 1, -1, -1, -1, -1},
    {8, 9, 6, 8, 6, 7, 9, 1, 6, 11, 6, 3, 1, 3, 6, -1},
    {0, 9, 1, 11, 6, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {7, 8, 0, 7, 0, 6, 3, 11, 0, 11, 6, 0, -1, -1, -1, -1},
    {7, 11, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {7, 6, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {3, 0, 8, 11, 7, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {0, 1, 9, 11, 7, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {8, 1, 9, 8, 3, 1, 11, 7, 6, -1, -1, -1, -1, -1, -1, -1},
    {10, 1, 2, 6, 11, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {1, 2, 10, 3, 0, 8, 6, 11, 7, -1, -1, -1, -1, -1, -1, -1},
    {2, 9, 0, 2, 10, 9, 6, 11, 7, -1, -1, -1, -1, -1, -1, -1},
    {6, 11, 7, 2, 10, 3, 10, 8, 3, 10, 9, 8, -1, -1, -1, -1},
    {7, 2, 3, 6, 2, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {7, 0, 8, 7, 6, 0, 6, 2, 0, -1, -1, -1, -1, -1, -1, -1},
    {2, 7, 6, 2, 3, 7, 0, 1, 9, -1, -1, -1, -1, -1, -1, -1},
    {1, 6, 2, 1, 8, 6, 1, 9, 8, 8, 7, 6, -1, -1, -1, -1},
    {10, 7, 6, 10, 1, 7, 1, 3, 7, -1, -1, -1, -1, -1, -1, -1},
    {10, 7, 6, 1, 7, 10, 1, 8, 7, 1, 0, 8, -1, -1, -1, -1},
    {0, 3, 7, 0, 7, 10, 0, 10, 9, 6, 10, 7, -1, -1, -1, -1},
    {7, 6, 10, 7, 10, 8, 8, 10, 9, -1, -1, -1, -1, -1, -1, -1},
    {6, 8, 4, 11, 8, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {3, 6, 11, 3, 0, 6, 0, 4, 6, -1, -1, -1, -1, -1, -1, -1},
    {8, 6, 11, 8, 4, 6, 9, 0, 1, -1, -1, -1, -1, -1, -1, -1},
    {9, 4, 6, 9, 6, 3, 9, 3, 1, 11, 3, 6, -1, -1, -1, -1},
    {6, 8, 4, 6, 11, 8, 2, 10, 1, -1, -1, -1, -1, -1, -1, -1},
    {1, 2, 10, 3, 0, 11, 0, 6, 11, 0, 4, 6, -1, -1, -1, -1},
    {4, 11, 8, 4, 6, 11, 0, 2, 9, 2, 10, 9, -1, -1, -1, -1},
    {10, 9, 3, 10, 3, 2, 9, 4, 3, 11, 3, 6, 4, 6, 3, -1},
    {8, 2, 3, 8, 4, 2, 4, 6, 2, -1, -1, -1, -1, -1, -1, -1},
    {0, 4, 2, 4, 6, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {1, 9, 0, 2, 3, 4, 2, 4, 6, 4, 3, 8, -1, -1, -1, -1},
    {1, 9, 4, 1, 4, 2, 2, 4, 6, -1, -1, -1, -1, -1, -1, -1},
    {8, 1, 3, 8, 6, 1, 8, 4, 6, 6, 10, 1, -1, -1, -1, -1},
    {10, 1, 0, 10, 0, 6, 6, 0, 4, -1, -1, -1, -1, -1, -1, -1},
    {4, 6, 3, 4, 3, 8, 6, 10, 3, 0, 3, 9, 10, 9, 3, -1},
    {10, 9, 4, 6, 10, 4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {4, 9, 5, 7, 6, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {0, 8, 3, 4, 9, 5, 11, 7, 6, -1, -1, -1, -1, -1, -1, -1},
    {5, 0, 1, 5, 4, 0, 7, 6, 11, -1, -1, -1, -1, -1, -1, -1},
    {11, 7, 6, 8, 3, 4, 3, 5, 4, 3, 1, 5, -1, -1, -1, -1},
    {9, 5, 4, 10, 1, 2, 7, 6, 11, -1, -1, -1, -1, -1, -1, -1},
    {6, 11, 7, 1, 2, 10, 0, 8, 3, 4, 9, 5, -1, -1, -1, -1},
    {7, 6, 11, 5, 4, 10, 4, 2, 10, 4, 0, 2, -1, -1, -1, -1},
    {3, 4, 8, 3, 5, 4, 3, 2, 5, 10, 5, 2, 11, 7, 6, -1},
    {7, 2, 3, 7, 6, 2, 5, 4, 9, -1, -1, -1, -1, -1, -1, -1},
    {9, 5, 4, 0, 8, 6, 0, 6, 2, 6, 8, 7, -1, -1, -1, -1},
    {3, 6, 2, 3, 7, 6, 1, 5, 0, 5, 4, 0, -1, -1, -1, -1},
    {6, 2, 8, 6, 8, 7, 2, 1, 8, 4, 8, 5, 1, 5, 8, -1},
    {9, 5, 4, 10, 1, 6, 1, 7, 6, 1, 3, 7, -1, -1, -1, -1},
    {1, 6, 10, 1, 7, 6, 1, 0, 7, 8, 7, 0, 9, 5, 4, -1},
    {4, 0, 10, 4, 10, 5, 0, 3, 10, 6, 10, 7, 3, 7, 10, -1},
    {7, 6, 10, 7, 10, 8, 5, 4, 10, 4, 8, 10, -1, -1, -1, -1},
    {6, 9, 5, 6, 11, 9, 11, 8, 9, -1, -1, -1, -1, -1, -1, -1},
    {3, 6, 11, 0, 6, 3, 0, 5, 6, 0, 9, 5, -1, -1, -1, -1},
    {0, 11, 8, 0, 5, 11, 0, 1, 5, 5, 6, 11, -1, -1, -1, -1},
    {6, 11, 3, 6, 3, 5, 5, 3, 1, -1, -1, -1, -1, -1, -1, -1},
    {1, 2, 10, 9, 5, 11, 9, 11, 8, 11, 5, 6, -1, -1, -1, -1},
    {0, 11, 3, 0, 6, 11, 0, 9, 6, 5, 6, 9, 1, 2, 10, -1},
    {11, 8, 5, 11, 5, 6, 8, 0, 5, 10, 5, 2, 0, 2, 5, -1},
    {6, 11, 3, 6, 3, 5, 2, 10, 3, 10, 5, 3, -1, -1, -1, -1},
    {5, 8, 9, 5, 2, 8, 5, 6, 2, 3, 8, 2, -1, -1, -1, -1},
    {9, 5, 6, 9, 6, 0, 0, 6, 2, -1, -1, -1, -1, -1, -1, -1},
    {1, 5, 8, 1, 8, 0, 5, 6, 8, 3, 8, 2, 6, 2, 8, -1},
    {1, 5, 6, 2, 1, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {1, 3, 6, 1, 6, 10, 3, 8, 6, 5, 6, 9, 8, 9, 6, -1},
    {10, 1, 0, 10, 0, 6, 9, 5, 0, 5, 6, 0, -1, -1, -1, -1},
    {0, 3, 8, 5, 6, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {10, 5, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {11, 5, 10, 7, 5, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {11, 5, 10, 11, 7, 5, 8, 3, 0, -1, -1, -1, -1, -1, -1, -1},
    {5, 11, 7, 5, 10, 11, 1, 9, 0, -1, -1, -1, -1, -1, -1, -1},
    {10, 7, 5, 10, 11, 7, 9, 8, 1, 8, 3, 1, -1, -1, -1, -1},
    {11, 1, 2, 11, 7, 1, 7, 5, 1, -1, -1, -1, -1, -1, -1, -1},
    {0, 8, 3, 1, 2, 7, 1, 7, 5, 7, 2, 11, -1, -1, -1, -1},
    {9, 7, 5, 9, 2, 7, 9, 0, 2, 2, 11, 7, -1, -1, -1, -1},
    {7, 5, 2, 7, 2, 11, 5, 9, 2, 3, 2, 8, 9, 8, 2, -1},
    {2, 5, 10, 2, 3, 5, 3, 7, 5, -1, -1, -1, -1, -1, -1, -1},
    {8, 2, 0, 8, 5, 2, 8, 7, 5, 10, 2, 5, -1, -1, -1, -1},
    {9, 0, 1, 5, 10, 3, 5, 3, 7, 3, 10, 2, -1, -1, -1, -1},
    {9, 8, 2, 9, 2, 1, 8, 7, 2, 10, 2, 5, 7, 5, 2, -1},
    {1, 3, 5, 3, 7, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {0, 8, 7, 0, 7, 1, 1, 7, 5, -1, -1, -1, -1, -1, -1, -1},
    {9, 0, 3, 9, 3, 5, 5, 3, 7, -1, -1, -1, -1, -1, -1, -1},
    {9, 8, 7, 5, 9, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {5, 8, 4, 5, 10, 8, 10, 11, 8, -1, -1, -1, -1, -1, -1, -1},
    {5, 0, 4, 5, 11, 0, 5, 10, 11, 11, 3, 0, -1, -1, -1, -1},
    {0, 1, 9, 8, 4, 10, 8, 10, 11, 10, 4, 5, -1, -1, -1, -1},
    {10, 11, 4, 10, 4, 5, 11, 3, 4, 9, 4, 1, 3, 1, 4, -1},
    {2, 5, 1, 2, 8, 5, 2, 11, 8, 4, 5, 8, -1, -1, -1, -1},
    {0, 4, 11, 0, 11, 3, 4, 5, 11, 2, 11, 1, 5, 1, 11, -1},
    {0, 2, 5, 0, 5, 9, 2, 11, 5, 4, 5, 8, 11, 8, 5, -1},
    {9, 4, 5, 2, 11, 3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {2, 5, 10, 3, 5, 2, 3, 4, 5, 3, 8, 4, -1, -1, -1, -1},
    {5, 10, 2, 5, 2, 4, 4, 2, 0, -1, -1, -1, -1, -1, -1, -1},
    {3, 10, 2, 3, 5, 10, 3, 8, 5, 4, 5, 8, 0, 1, 9, -1},
    {5, 10, 2, 5, 2, 4, 1, 9, 2, 9, 4, 2, -1, -1, -1, -1},
    {8, 4, 5, 8, 5, 3, 3, 5, 1, -1, -1, -1, -1, -1, -1, -1},
    {0, 4, 5, 1, 0, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {8, 4, 5, 8, 5, 3, 9, 0, 5, 0, 3, 5, -1, -1, -1, -1},
    {9, 4, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {4, 11, 7, 4, 9, 11, 9, 10, 11, -1, -1, -1, -1, -1, -1, -1},
    {0, 8, 3, 4, 9, 7, 9, 11, 7, 9, 10, 11, -1, -1, -1, -1},
    {1, 10, 11, 1, 11, 4, 1, 4, 0, 7, 4, 11, -1, -1, -1, -1},
    {3, 1, 4, 3, 4, 8, 1, 10, 4, 7, 4, 11, 10, 11, 4, -1},
    {4, 11, 7, 9, 11, 4, 9, 2, 11, 9, 1, 2, -1, -1, -1, -1},
    {9, 7, 4, 9, 11, 7, 9, 1, 11, 2, 11, 1, 0, 8, 3, -1},
    {11, 7, 4, 11, 4, 2, 2, 4, 0, -1, -1, -1, -1, -1, -1, -1},
    {11, 7, 4, 11, 4, 2, 8, 3, 4, 3, 2, 4, -1, -1, -1, -1},
    {2, 9, 10, 2, 7, 9, 2, 3, 7, 7, 4, 9, -1, -1, -1, -1},
    {9, 10, 7, 9, 7, 4, 10, 2, 7, 8, 7, 0, 2, 0, 7, -1},
    {3, 7, 10, 3, 10, 2, 7, 4, 10, 1, 10, 0, 4, 0, 10, -1},
    {1, 10, 2, 8, 7, 4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {4, 9, 1, 4, 1, 7, 7, 1, 3, -1, -1, -1, -1, -1, -1, -1},
    {4, 9, 1, 4, 1, 7, 0, 8, 1, 8, 7, 1, -1, -1, -1, -1},
    {4, 0, 3, 7, 4, 3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {4, 8, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {9, 10, 8, 10, 11, 8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {3, 0, 9, 3, 9, 11, 11, 9, 10, -1, -1, -1, -1, -1, -1, -1},
    {0, 1, 10, 0, 10, 8, 8, 10, 11, -1, -1, -1, -1, -1, -1, -1},
    {3, 1, 10, 11, 3, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {1, 2, 11, 1, 11, 9, 9, 11, 8, -1, -1, -1, -1, -1, -1, -1},
    {3, 0, 9, 3, 9, 11, 1, 2, 9, 2, 11, 9, -1, -1, -1, -1},
    {0, 2, 11, 8, 0, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {3, 2, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {2, 3, 8, 2, 8, 10, 10, 8, 9, -1, -1, -1, -1, -1, -1, -1},
    {9, 10, 2, 0, 9, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {2, 3, 8, 2, 8, 10, 0, 1, 8, 1, 10, 8, -1, -1, -1, -1},
    {1, 10, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {1, 3, 8, 9, 1, 8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {0, 9, 1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {0, 3, 8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}};
}

public struct TRIANGLE
{
    public Vector3[] points;
    public float charge;

    public TRIANGLE(Vector3[] pointsT, Vector3 normalT)
    {
        points = pointsT;
        charge = 0;
    }
}

