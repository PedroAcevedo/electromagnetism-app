using UnityEngine;
using Unity.Collections;
using System.Collections.Generic;
using Unity.Jobs;
using Unity.Mathematics;
using Nezix;

/// Simple example showing how to use the Marching Cubes implementation

public class MCBexample : MonoBehaviour {

	MarchingCubesBurst mcb;
    public MeshFilter meshFilter;       // Mesh for marching cubes visualization
    public GameObject reference;

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

    private Vector3[] particles = { new Vector3(1.67f, 1.07f, -5.519f), new Vector3(-3.92f, 0.07f, -1.03f), new Vector3(-0.57f, 5.53f, -0.65f)}; 
    private float[] charges = { 1.0f, 1.0f, 1.0f };
    private int n = 3;
    const float K = (9 * 10 ^ 9);
    int numOfElements = 7;

    void Start()
    {
        MarchingCubesRoutine();
    }

    void Update()
    {
        if (OVRInput.GetDown(OVRInput.Button.One))
        {
            MarchingCubesRoutine();
        }
    }

    void MarchingCubesRoutine() {
        //Create fake density map

        int3 gridSize;
		gridSize.x = 256;
		gridSize.y = 256;
		gridSize.z = 256;

		int totalSize = gridSize.x * gridSize.y * gridSize.z;
		float dx = 2.0f;//Size of one voxel
		float3[] positions = new float3[totalSize];
		float[] densVal = new float[totalSize];
		Vector3 oriXInv = Vector3.zero; //Why this name ? Because you might need to invert the X axis

        int idPos = 0;
        int id = 0;
		for (int i = 0; i < gridSize.x; i++) {
			float x = -10.0f + i * dx;
			for (int j = 0; j < gridSize.y; j++) {
				float y =   -10.0f + j * dx;
				for (int k = 0; k < gridSize.z; k++) {
                    float z = -10.0f + k * dx;
					densVal[id++] = electromagnetism3(new Vector3(x, y, z));
                    positions[idPos++] = new float3(x, y, z);
                    //GameObject duplicate = Instantiate(reference);
                    //duplicate.transform.position = new Vector3(x, y, z);
                }
            }
		}


		//Instantiate the MCB class
		mcb = new MarchingCubesBurst(positions, densVal, gridSize, oriXInv, dx);

		//Compute an iso surface, this can be called several time without modifying mcb
		float isoValue = 1.5f;
		mcb.computeIsoSurface(isoValue);

		Vector3[] newVerts = mcb.getVertices();
		Vector3[] newNorms = mcb.getNormals();
		if (newVerts.Length == 0) {
			Debug.Log("Empty mesh");
			mcb.Clean();
		}

		//Invert x of each vertex
		for (int i = 0; i < newVerts.Length; i++) {
			newVerts[i].x *= -1;
			newNorms[i].x *= -1;
		}
		int[] newTri = mcb.getTriangles();
		Color32[] newCols = new Color32[newVerts.Length];
		Color32 w = Color.white;
		for (int i = 0; i < newCols.Length; i++) {
			newCols[i] = w;
		}

		GameObject newMeshGo = new GameObject("testDX");

		Mesh newMesh = new Mesh();
		newMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
		newMesh.vertices = newVerts;
		newMesh.triangles = newTri;
		newMesh.colors32 = newCols;
		newMesh.normals = newNorms;

		meshFilter.mesh = newMesh;

		//Material mat = new Material(Shader.Find("Particles/Standard Surface"));

		//mr.material = mat;
        
        //When done => free data
        mcb.Clean();
	}



    float electromagnetism3(Vector3 currentPosition)
    {
        float totalForce = 0;

        for (int i = 0; i < n; ++i)
        {
            float r = Vector3.Distance(particles[i], currentPosition);
            totalForce = totalForce + electricField(currentPosition, particles[i], charges[i]) * blendFunction1(r); //Math.Abs(force(currentPosition, particles[i].transform.position, 1, charges[i]));
        }

        return totalForce;
    }
    float electricField(Vector3 actualPoint, Vector3 particlePosition, float charge)
    {
        float distance = Vector3.Distance(particlePosition, actualPoint);

        return Mathf.Abs((K * charge) / (float)Mathf.Pow(distance, 2.0f));
    }

    float implicitFunction(Vector3 p)
    {
        int max = 7;
        numOfElements = numOfElements < 1 ? 1 : (numOfElements > max ? max : numOfElements);

        float result = 0.0f;
        for (int i = 0; i < numOfElements; i++)
        {
            float r = Vector3.Distance(elements[i], p);
            switch (0)
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
        return 1.0f / (float)Mathf.Pow(r, 2.0f);
    }

    float blendFunction1(float r)
    {
        float a = 0.8f;
        float b = 0.1f;
        return (float)a * (float)Mathf.Exp(-b * (float)Mathf.Pow(r, 2.0f));
    }

    float blendFunction2(float r)
    {
        float R = 10f;

        if (r >= R)
        {
            return 0;
        }

        return 1 - (4 / 9 * (float)Mathf.Pow(r, 6.0f) / (float)Mathf.Pow(R, 6.0f))
            + (17 / 9 * (float)Mathf.Pow(r, 4.0f) / (float)Mathf.Pow(R, 4.0f))
            + (22 / 9 * (float)Mathf.Pow(r, 2.0f) / (float)Mathf.Pow(R, 2.0f));

    }

}