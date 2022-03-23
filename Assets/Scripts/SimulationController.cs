using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using OVRTouchSample;
using System;
using UnityEngine.SceneManagement;
using System.Linq;
using UnityEngine.EventSystems;

public class SimulationController : MonoBehaviour
{

    #region Public Fields

    //Public variables
    public MeshFilter meshFilter;       // Mesh for marching cubes visualization
    public int dimension;               // Resolution of the surface
    public int n;                       // Number of particles
    public int GridSize;
    public GameObject[] particles;
    public float[] charges;
    public GameObject RHand;            // VR right controller
    public GameObject LHand;            // VR left controller
    public UnityEngine.UI.Text playerIDLabel;
    public UnityEngine.UI.Text partLabel;
    public UnityEngine.UI.Text phaseLabel;
    public UnityEngine.UI.Text lobbyLabel;
    public UnityEngine.UI.Text phaseInstruction;
    public GameObject MenuCanvas;
    public GameObject SceneControl;
    public GameObject[] interestPoints;
    public GameObject Indicators;
    public int HMDNumber;           // Change between HeadSets

    //For debugging 
    public bool DEBUG_GRID = false;
    public GameObject referenceText;
    public GameObject reference;

    // Menu options
    public bool showLines;
    public bool Mode2D;
    public bool hapticFeedback;
    public bool showSurface;
    public bool simpleMode;
    public bool showMenu;
    public bool particleInteraction;

    // Phase control
    public List<string> instructions;

    #endregion

    #region MarchingCubes Fields

    //Boundary values for Marching Cubes
    int MINX;
    int MAXX;
    int MINY;
    int MAXY;
    int MINZ;
    int MAXZ;
    float K = 9e9f;      //Coulomb's law constant
    int nX, nY, nZ;                    //number of cells on each axis for Marching cubes

    Vector4[] points;                  // Vertex on the grid
    float[] pointsCharges;             // Electric field applied of each point of the grid 
    float[] pointsAngles;             // Electric field applied of each point of the grid 

    private float maxCharge = -1.0f;
    private float minCharge = 10000.0f;

    int numTriangles = 0;         //Obtained by Marching Cubes

    // Variables being changed on runtime by user.
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
    List<Vector3> QuadrantsLimits = new List<Vector3>();
    List<String> QuadrantsElements = new List<String>();

    private TRIANGLE[] Triangles;
    private Vector3 stepSize;

    #endregion

    #region Private Fields

    // Lines class
    private ParticleLines lineController;

    // For Scene control
    private int[] numberOfParticles = { 2, 2, 2, 3 };
    private int[] negativeCharges = { 0, 2, 1, 2 };
    private Vector3[] initialPositions = { new Vector3(0.0f, 3.0f, 0.0f), new Vector3(-3.0f, 0.0f, 0.0f), new Vector3(3.0f, 0.0f, 0.0f) };
    private GameObject[] particlesOnScene;
    private float[] chargesOnScene;
    private int currentScene = 0;
    private Transform MainCamera;
    private GameObject[] particleSignText;
    private GameObject arrowInField;
    private int simulationMode = -1;
    private int currentPhase = 0;

    //Update actual view
    private bool updateSurface = false;
    
    //User stats
    private GameObject player;
    private string playerID;
    public static UserReportController controller;


    private string[] PhaseNames = { "Exploration Phase", "Reference Phase", "Interactive Phase" };

    #endregion

    #region MonoBehaviour Callbacks

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

        particleSignText = new GameObject[charges.Length];

        for (int i = 0; i < charges.Length; ++i)
        {
            particles[i].SetActive(false);
            particleSignText[i] = particles[i].transform.GetChild(0).gameObject.transform.GetChild(0).gameObject;
            charges[i] = charges[i] * 1e-9f;
        }

        //Verify Hands
        //verifyHand();

        //User ID
        getUserID();

        // Select the scene
        arrowInField = Resources.Load("Prefabs/arrow_in_field") as GameObject;
        particlesOnScene = (UnityEngine.GameObject[])particles.Clone();
        chargesOnScene = (float[])charges.Clone();
        MainCamera = GameObject.Find("OVRCameraRig").transform;
        player = GameObject.Find("OVRPlayerController");
        setupCurrentScene();

        nX = dimension;
        nY = dimension;
        nZ = dimension;
        createGrid();
        lineController = new ParticleLines(particles, charges);
        if (showLines)
        {
            lineController.Draw(this.Mode2D);
        }
        runMarchingCubes();


    }

    void FixedUpdate()
    {
        if (updateSurface == true)
        {
            runMarchingCubes();
            updateSurface = false;
        }

        for (int i = 0; i < particles.Length; ++i)
        {
            if (particles[i].transform.hasChanged)
            {
                particles[i].transform.hasChanged = false;

                if (showLines)
                {
                    lineController.Draw(this.Mode2D);
                }
                if (showSurface)
                {
                    if (particles[i].GetComponent<OVRGrabbable>().isGrabbed)
                    {
                        showSurfaceState(false);
                        cleanPointsLabels();
                    }
                }
                break;
            }
        }

        bool isStatic = true;

        for (int i = 0; i < particles.Length; ++i)
        {
            isStatic = isStatic && !particles[i].GetComponent<OVRGrabbable>().isGrabbed;
        }

        if (!showSurface && isStatic && simulationMode != -1)
        {
            updateIsosurface();
        }
    }

    void Update()
    {
        //findHandsVibration();
        if (hapticFeedback)
        {
            findHandsVibrationOptimized();
        }

        if (OVRInput.GetDown(OVRInput.Button.Four))
        {
            returnToHome();
            //StartCoroutine(MarchingCubesRoutine());
        }

    }

    #endregion

    #region MarchingCubes Methods

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

        Debug.Log("Max value -->" + maxCharge);
        Debug.Log("Min value -->" + minCharge);

        //for (int i = 0; i < points.Length - 1; ++i)
        //{
        //    if (points[i].z == 0)
        //    {
        //        GameObject duplicate = Instantiate(reference);
        //        duplicate.transform.position = new Vector3(points[i].x, points[i].y, points[i].z);
        //        GameObject duplicateText = Instantiate(referenceText);
        //        duplicateText.GetComponent<TextMeshPro>().text = i + " - " + points[i].x.ToString("F2") + "," + points[i].y.ToString("F2") + "," + points[i].z.ToString("F2");
        //        duplicateText.transform.position = new Vector3(points[i].x, points[i].y, points[i].z);
        //    }
        //}

        //int contSpheres = 0;
        //if (meshFilter && DEBUG_GRID)
        //{
        //for (int i = 0; i < points.Length - 1; ++i)
        //{
        //    if (points[i].z < 0.4 && points[i].z > 0 && pointsCharges[i] > 0) //pointsCharges[i] == 0 && points[i].x > particles[1].transform.position.x && particles[0].transform.position.x > points[i].x && 20 > contSpheres )
        //    {
        //        GameObject duplicate = Instantiate(arrowInField);
        //        duplicate.transform.position = new Vector3(points[i].x, points[i].y, points[i].z);
        //        duplicate.transform.Rotate(0.0f, 0.0f, pointsAngles[i]);
        //        GameObject duplicateText = Instantiate(referenceText);
        //        duplicateText.GetComponent<TextMeshPro>().text = pointsCharges[i] + "";
        //        duplicateText.transform.position = new Vector3(points[i].x - 0.25f, points[i].y - 0.3f, points[i].z);
        //        //contSpheres++;
        //    }
        //}
        //}
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
    }

    void ClearMeshData()
    {
        vertices.Clear();
        triangles = new Int32[numTriangles * 3];
    }

    float normalizeCharge(float charge)
    {
        return Math.Abs((charge - minCharge) / (maxCharge - minCharge));
    }

    float electromagnetism3(Vector3 currentPosition)
    {
        float totalForce = 0;

        for (int i = 0; i < n; ++i)
        {
            float r = Vector3.Distance(particles[i].transform.position, currentPosition);
            totalForce = totalForce + electricField(currentPosition, particles[i].transform.position, charges[i]) * Utils.blendFunction0(r);
        }

        return totalForce;
    }

    float electromagnetismCharge(Vector3 currentPosition)
    {
        float totalForce = 0;

        for (int i = 0; i < n; i++)
        {
            float electric_magnitude = electricField(particles[i].transform.position, currentPosition, charges[i]);
            totalForce = totalForce + Utils.directionOfTheField(currentPosition, particles[i].transform.position, charges[i]) * electric_magnitude;
        }

        return totalForce;
    }

    float electricField(Vector3 a, Vector3 b, float charge)
    {
        float distance = new Vector3((b.x - a.x), (b.y - a.y), (b.z - a.z)).magnitude;

        return Math.Abs((K * charge) / (float)Math.Pow(distance, 2.0));
    }

    void createGrid()
    {
        points = new Vector4[(nX + 1) * (nY + 1) * (nZ + 1)];
        pointsCharges = new float[(nX + 1) * (nY + 1) * (nZ + 1)];
        //pointsAngles = new float[(nX + 1) * (nY + 1) * (nZ + 1)];
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
                    pointsCharges[ind] = (float)Math.Log10(electromagnetismCharge(new Vector3(vert.x, vert.y, vert.z)));
                    clasifyPoint(new Vector3(vert.x, vert.y, vert.z), ind);

                }
            }
        }
    }

    TRIANGLE[] MarchingCubes(float minValue)
    {
        for (int i = 0; i < (nX + 1) * (nY + 1) * (nZ + 1); ++i)
        {
            points[i].w = electromagnetism3(new Vector3(points[i].x, points[i].y, points[i].z));/*(step 3)*/
            pointsCharges[i] = (float)Math.Log10((float)(int)Math.Abs(electromagnetismCharge(new Vector3(points[i].x, points[i].y, points[i].z))));
            //pointsCharges[i] = (float)(int)Math.Abs(electromagnetismCharge(new Vector3(points[i].x, points[i].y, points[i].z)));//(float)Math.Log10((float)(int)Math.Abs(electromagnetismCharge(new Vector3(points[i].x, points[i].y, points[i].z))));//(float)(int)Math.Abs(electromagnetismCharge(new Vector3(points[i].x, points[i].y, points[i].z))); 
            //pointsAngles[i] = angle(new Vector3(points[i].x, points[i].y, points[i].z));

            if (pointsCharges[i] > maxCharge)
            {
                maxCharge = pointsCharges[i];
            }

            if (pointsCharges[i] < minCharge && pointsCharges[i] > 1e-5f && pointsCharges[i] > -Mathf.Infinity)
            {
                minCharge = pointsCharges[i];
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
                    if ((Utils.edgeTable[cubeIndex] & 1) > 0) intVerts[0] = Utils.intersection(verts[0], verts[1], minValue);
                    if ((Utils.edgeTable[cubeIndex] & 2) > 0) intVerts[1] = Utils.intersection(verts[1], verts[2], minValue);
                    if ((Utils.edgeTable[cubeIndex] & 4) > 0) intVerts[2] = Utils.intersection(verts[2], verts[3], minValue);
                    if ((Utils.edgeTable[cubeIndex] & 8) > 0) intVerts[3] = Utils.intersection(verts[3], verts[0], minValue);
                    if ((Utils.edgeTable[cubeIndex] & 16) > 0) intVerts[4] = Utils.intersection(verts[4], verts[5], minValue);
                    if ((Utils.edgeTable[cubeIndex] & 32) > 0) intVerts[5] = Utils.intersection(verts[5], verts[6], minValue);
                    if ((Utils.edgeTable[cubeIndex] & 64) > 0) intVerts[6] = Utils.intersection(verts[6], verts[7], minValue);
                    if ((Utils.edgeTable[cubeIndex] & 128) > 0) intVerts[7] = Utils.intersection(verts[7], verts[4], minValue);
                    if ((Utils.edgeTable[cubeIndex] & 256) > 0) intVerts[8] = Utils.intersection(verts[0], verts[4], minValue);
                    if ((Utils.edgeTable[cubeIndex] & 512) > 0) intVerts[9] = Utils.intersection(verts[1], verts[5], minValue);
                    if ((Utils.edgeTable[cubeIndex] & 1024) > 0) intVerts[10] = Utils.intersection(verts[2], verts[6], minValue);
                    if ((Utils.edgeTable[cubeIndex] & 2048) > 0) intVerts[11] = Utils.intersection(verts[3], verts[7], minValue);

                    //now build the triangles using triTable
                    for (int n = 0; Utils.triTable[cubeIndex, n] != -1; n += 3)
                    {
                        vertices.Add(intVerts[Utils.triTable[cubeIndex, n + 1]]);
                        vertices.Add(intVerts[Utils.triTable[cubeIndex, n]]);
                        vertices.Add(intVerts[Utils.triTable[cubeIndex, n + 2]]);

                        triangles[numTriangles] = new TRIANGLE(new Vector3[] { intVerts[Utils.triTable[cubeIndex, n + 2]], intVerts[Utils.triTable[cubeIndex, n + 1]], intVerts[Utils.triTable[cubeIndex, n]] }, new Vector3(0, 0, 0));
                        numTriangles++;
                    }


                }   //END OF FOR LOOP

        return triangles;
    }

    #endregion

    #region Public Methods

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
        if (this.showLines)
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
        for (int i = 0; i < particlesOnScene.Length; ++i)
        {
            particlesOnScene[i].GetComponent<Rigidbody>().freezeRotation  = value;

            if (value)
            {
                particlesOnScene[i].GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezePosition;
            }
            else
            {
                particlesOnScene[i].GetComponent<Rigidbody>().constraints = RigidbodyConstraints.None;
                particlesOnScene[i].GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotationZ;
            }
        }
    }

    public void updateIsosurface()
    {
        updateSurface = true;
        showSurface = true;
    }

    public string getPointValue(Vector3 point)
    {
        int quadIndexR = findQuadrant(point);

        float currentValue = 0;

        String[] indexQuad = QuadrantsElements[quadIndexR].Split('-');

        float lessDistance = 1000f;

        for (int i = 0; i < indexQuad.Length - 1; ++i)
        {
            Vector3 pointPos = new Vector3(points[Int32.Parse(indexQuad[i])].x, points[Int32.Parse(indexQuad[i])].y, points[Int32.Parse(indexQuad[i])].z);
            if (Vector3.Distance(pointPos, point) < lessDistance)
            {
                currentValue = pointsCharges[Int32.Parse(indexQuad[i])];
                lessDistance = Vector3.Distance(pointPos, point);
            }
        }

        currentValue = Mathf.Pow(10, currentValue);

        return currentValue.ToString("F2");
    }

    #endregion

    #region Scene Control

    //Setup the Scene
    void setupCurrentScene()
    {
        GameObject[] tempParticles = new GameObject[numberOfParticles[currentScene]];
        float[] tempCharges = new float[numberOfParticles[currentScene]];


        int negatives = negativeCharges[currentScene];

        for (int i = 0; i < tempParticles.Length; ++i)
        {
            tempParticles[i] = particlesOnScene[i];

            int signal = 1;

            if (negatives > 0)
            {
                signal = -1;
                negatives--;
            }

            tempCharges[i] = signal * chargesOnScene[i];


            if (tempCharges[i] < 0)
            {
                particlesOnScene[i].GetComponent<Renderer>().material.SetColor("_Color", Color.blue);
                particleSignText[i].GetComponent<TextMeshPro>().text = "-";
            }
            else
            {
                particlesOnScene[i].GetComponent<Renderer>().material.SetColor("_Color", Color.red);
                particleSignText[i].GetComponent<TextMeshPro>().text = "+";
            }

            particlesOnScene[i].transform.LookAt(new Vector3(0.0f, 5.30000019f, -15.0f));

        }

        particles = tempParticles;
        charges = tempCharges;
        n = particles.Length;
        partLabel.GetComponent<UnityEngine.UI.Text>().text = "Part " + (currentScene + 1);
        setPhaseLabel();
    }

    //Hand raycasting
    void verifyHand()
    {
        Debug.Log("Left Hander" + PlayerPrefs.GetInt("LeftHander"));

        if (PlayerPrefs.GetInt("LeftHander") == 1)
        {
            FindObjectOfType<OVRInputModule>().rayTransform = LHand.transform;
        }
        else
        {
            FindObjectOfType<OVRInputModule>().rayTransform = RHand.transform;
        }
    }

    //Reset interest point
    void resetInterestPoint()
    {
        if (interestPoints.Length > currentScene)
        {
            interestPoints[currentScene].SetActive(false);

            GameObject points = interestPoints[currentScene].transform.GetChild(0).gameObject;

            points.SetActive(true);
            points.GetComponent<InterestPoint>().Reset();

            for (int i = 1; i < interestPoints[currentScene].transform.childCount; i++)
            {
                points = interestPoints[currentScene].transform.GetChild(i).gameObject;
                points.SetActive(false);
                points.GetComponent<InterestPoint>().Reset();
            }
        }
    }

    public void nextScene()
    {
        ChangeScene(1);
    }

    public void backScene()
    {
        ChangeScene(-1);
    }

    public void returnToHome()
    {
        currentScene = 0;

        for (int i = 0; i < charges.Length; ++i)
        {
            particles[i].SetActive(false);
        }

        ChangeScene(0);

        simulationMode = -1;
        resetPlayerPosition();

        showLines = false;
        Mode2D = true;
        hapticFeedback = false;
        simpleMode = true;
        showMenu = false;
        particleInteraction = false;
        showSurfaceState(false);
        lineController.CleanLines();

        MenuCanvas.SetActive(true);
        SceneControl.SetActive(false);
    }

    public void ChangeScene(int direction)
    {
        resetInterestPoint();

        currentScene += direction;

        if (currentScene >= 0 && currentScene < numberOfParticles.Length)
        {
            for (int i = 0; i < charges.Length; ++i)
            {
                particles[i].SetActive(false);
            }

            resetPlayerPosition();

            maxCharge = -1.0f;
            minCharge = 10000.0f;

            showSurfaceState(false);

            setupCurrentScene();

            for (int i = 0; i < charges.Length; ++i)
            {
                particles[i].SetActive(true);
                particles[i].transform.position = initialPositions[i];
            }

            lineController.CleanLines();
            lineController = new ParticleLines(particles, charges);
            if (showLines)
            {
                lineController.Draw(this.Mode2D);
            }

            updateIsosurface();

        } else
        {
            if(currentScene == numberOfParticles.Length)
            {
                moveToLobby();
                // SAVE THE JSON FILE
                lobbyLabel.GetComponent<UnityEngine.UI.Text>().text = "The session is over, thanks for your participation. You can remove your headset now.";
                GameObject.Find("ReturnMain").SetActive(false);
            }
        }

    }

    public bool getCurrentMode()
    {
        return simulationMode == 0 || simulationMode == 2 || (simulationMode == 3 && showLines);
    }

    public void selectMode(int modeSelected)
    {
        simulationMode = modeSelected;

        switch (simulationMode)
        {
            case 0: // Condition 1: No force label
                showLine(true);
                hapticFeedback = false;
                simpleMode = false;
                break;
            case 1: // Condition 1: force label
                showLines = false;
                hapticFeedback = true;
                simpleMode = true;
                break;
            case 2: // Condition 1: force label
                showLines = true;
                hapticFeedback = true;
                simpleMode = true;
                break;
            case 3: // Condition 1: No force label only firts step
                showLines = false;
                hapticFeedback = true;
                simpleMode = true;
                break;
        }

        particleInteraction = true;
        MenuCanvas.SetActive(false);
        SceneControl.SetActive(true);

        for (int i = 0; i < charges.Length; ++i)
        {
            particles[i].SetActive(true);
        }

        showSurfaceState(true);

        SceneData scene = new SceneData();

        Debug.Log(SceneController.controller);

        SceneController.controller.SceneInfo(scene);

        SceneController.controller.SaveIntoJson(playerID);

    }

    public void selectCond1()
    {
        selectMode(0);
    }

    public void selectCond2()
    {
        selectMode(1);
    }

    public void selectCond3()
    {
        selectMode(2);
    }

    public void selectCond4()
    {
        selectMode(3);
    }

    public void returnToMain()
    {
        currentPhase = 0;
        resetPlayerPosition();
    }

    public void changePhase()
    {

        currentPhase++;
        cleanPointsLabels();

        switch (currentPhase)
        {
            case 1:
                if (interestPoints.Length > currentScene)
                {
                    interestPoints[currentScene].SetActive(true);
                    interestPoints[currentScene].transform.GetChild(0).gameObject.SetActive(true);
                }

                removeParticleInteraction(true);
                resetParticlePosition();
                setPhaseLabel();
                break;
            case 2:
                Indicators.SetActive(true);
                removeParticleInteraction(false);
                resetParticlePosition();
                setPhaseLabel();
                // Show indicators to move particles
                break;
            case 3:
                Indicators.SetActive(false);
                Indicators.GetComponent<IndicatorController>().resetIndicators();
                currentPhase = 0;

                if (simulationMode == 3 && !showLines)
                {
                    showLine(true);
                    resetInterestPoint();
                    resetParticlePosition();
                    setPhaseLabel();
                }
                else
                {

                    if(simulationMode == 3)
                    {
                        showLine(false);
                    }

                    nextScene();
                    moveToLobby();
                }

                
                
                break;
        }
    }

    #endregion

    #region Haptic Methods

    void clasifyPoint(Vector3 point, int index)
    {
        int quad = -1;

        for (int i = 0; i < QuadrantsLimits.Count; ++i)
        {
            if (Vector3.Distance(QuadrantsLimits[i], point) <= (GridSize * 2) - 1)
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

            float lessDistanceR = 1000;
            float lessDistanceL = 1000;

            for (int i = 0; i < indexQuad.Length - 1; ++i)
            {
                Vector3 pointPos = new Vector3(points[Int32.Parse(indexQuad[i])].x, points[Int32.Parse(indexQuad[i])].y, points[Int32.Parse(indexQuad[i])].z);

                if (Vector3.Distance(pointPos, RPos) < lessDistanceR)
                {
                    RAmplitude = pointsCharges[Int32.Parse(indexQuad[i])];
                    lessDistanceR = Vector3.Distance(pointPos, RPos);
                }

                if (Vector3.Distance(pointPos, LPos) < lessDistanceL)
                {
                    LAmplitude = pointsCharges[Int32.Parse(indexQuad[i])];
                    lessDistanceL = Vector3.Distance(pointPos, LPos);
                }
            }

        }
        else
        {

            if (quadIndexR != -1)
            {
                String[] indexQuadR = QuadrantsElements[quadIndexR].Split('-');

                float lessDistance = 1000f;

                for (int i = 0; i < indexQuadR.Length - 1; ++i)
                {
                    Vector3 pointPos = new Vector3(points[Int32.Parse(indexQuadR[i])].x, points[Int32.Parse(indexQuadR[i])].y, points[Int32.Parse(indexQuadR[i])].z);
                    if (Vector3.Distance(pointPos, RPos) < lessDistance)
                    {
                        RAmplitude = pointsCharges[Int32.Parse(indexQuadR[i])];
                        lessDistance = Vector3.Distance(pointPos, RPos);
                    }
                }
            }

            if (quadIndexL != -1)
            {
                String[] indexQuadL = QuadrantsElements[quadIndexL].Split('-');
                float lessDistance = 1000f;

                for (int i = 0; i < indexQuadL.Length - 1; ++i)
                {
                    Vector3 pointPos = new Vector3(points[Int32.Parse(indexQuadL[i])].x, points[Int32.Parse(indexQuadL[i])].y, points[Int32.Parse(indexQuadL[i])].z);

                    if (Vector3.Distance(pointPos, LPos) < lessDistance)
                    {
                        LAmplitude = pointsCharges[Int32.Parse(indexQuadL[i])];
                        lessDistance = Vector3.Distance(pointPos, LPos);
                    }

                }
            }

        }

        OVRInput.SetControllerVibration(1, RAmplitude, OVRInput.Controller.RTouch);
        OVRInput.SetControllerVibration(1, LAmplitude, OVRInput.Controller.LTouch);
    }

    #endregion

    #region Private Methods

    void resetPlayerPosition()
    {
        var OVRplayer = player.transform.GetChild(1).GetChild(0); //.GetComponent<OVRPlayerController>();
        //OVRplayer.enabled = false;
        OVRplayer.position = new Vector3(0.0f, OVRplayer.position.y, -15f);
        OVRplayer.rotation = Quaternion.identity;
        //OVRplayer.enabled = true;
    }

    void moveToLobby()
    {
        var OVRplayer = player.transform.GetChild(1).GetChild(0); //.GetComponent<OVRPlayerController>();
        OVRplayer.transform.position = new Vector3(-185.0f, OVRplayer.position.y, - 6f);
        OVRplayer.rotation = Quaternion.identity;

    }

    void getUserID()
    {
        if (PlayerPrefs.GetInt("playerID") == 0)
        {
            PlayerPrefs.SetInt("playerID", 1);
        }
        else
        {
            PlayerPrefs.SetInt("playerID", PlayerPrefs.GetInt("playerID") + 1);
        }


        playerID = "HMD" + HMDNumber + "-" + PlayerPrefs.GetInt("playerID");

        playerIDLabel.GetComponent<UnityEngine.UI.Text>().text = "Student ID: " + playerID;
    }

    void cleanPointsLabels()
    {
        if (interestPoints.Length > currentScene)
        {
            GameObject points = interestPoints[currentScene].transform.GetChild(0).gameObject;

            points.GetComponent<InterestPoint>().Reset();

            for (int i = 1; i < interestPoints[currentScene].transform.childCount; i++)
            {
                points = interestPoints[currentScene].transform.GetChild(i).gameObject;
                points.GetComponent<InterestPoint>().Reset();
            }
        }
    }

    void setPhaseLabel()
    {
        phaseLabel.GetComponent<UnityEngine.UI.Text>().text = PhaseNames[currentPhase];
        phaseInstruction.GetComponent<UnityEngine.UI.Text>().text = instructions[currentPhase];
    }

    void resetParticlePosition()
    {
        for (int i = 0; i < charges.Length; ++i)
        {
            particles[i].transform.position = initialPositions[i];
            updateIsosurface();
        }
    }
    #endregion
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

