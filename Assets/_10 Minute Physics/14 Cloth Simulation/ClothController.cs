using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Basic cloth physics from https://matthias-research.github.io/pages/tenMinutePhysics/
public class ClothController : MonoBehaviour
{
    //Public
    public GameObject clothMeshPrefabGO;

    public Texture2D cursorTexture;


    //Private
    private readonly List<ClothSimulationTutorial> allCloth = new();

    private int numberOfBodies = 1;

    private const int SEED = 0;

    //What we use to grab the balls
    private Grabber grabber;

    private bool simulate = true;

    private readonly Color[] colors = new Color[] { Color.red, Color.blue, Color.green, Color.yellow, Color.cyan };



    private void Start()
    {
        //TestFindNeighbors();

        //return;


        Random.InitState(SEED);

        //ClothData clothData = new ClothDataTutorial();
        ClothData clothData = new ClothDataProcedural();


        for (int i = 0; i < numberOfBodies; i++)
        {
            GameObject clothGO = Instantiate(clothMeshPrefabGO);

            MeshFilter meshFilter = clothGO.GetComponent<MeshFilter>();

            /*
            //Pos
            float halfPlayground = 5f;

            float randomX = Random.Range(-halfPlayground, halfPlayground);
            float randomZ = Random.Range(-halfPlayground, halfPlayground);

            Vector3 startPos = new Vector3(randomX, 10f, randomZ);
            */

            Vector3 startPos = new Vector3(0f, -5f, 0f);


            //Scale
            //float clothScale = Random.Range(2f, 5f);

            float clothScale = 10f;


            //Random color
            /*
            MeshRenderer mr = clothGO.GetComponent<MeshRenderer>();

            Material mat = mr.material;

            mat.color = colors[Random.Range(0, colors.Length)];
            */

            ClothSimulationTutorial clothSim = new ClothSimulationTutorial(meshFilter, clothData, startPos, clothScale);
            //SoftBodySimulationVectors softBodySim = new SoftBodySimulationVectors(meshFilter, softBodyMesh, startPos, bunnyScale);


            allCloth.Add(clothSim);
        }

        //Init the grabber
        grabber = new Grabber(Camera.main);

        Cursor.visible = true;

        Cursor.SetCursor(cursorTexture, Vector2.zero, CursorMode.ForceSoftware);
    }



    private void Update()
    {
        foreach (ClothSimulationTutorial cloth in allCloth)
        {
            cloth.MyUpdate();
        }

        grabber.MoveGrab();

        ////Pause simulation
        //if (Input.GetKeyDown(KeyCode.P))
        //{
        //    simulate = !simulate;
        //}
    }



    private void LateUpdate()
    {
        if (Input.GetMouseButtonDown(0))
        {
            List<IGrabbable> temp = new List<IGrabbable>(allCloth);

            grabber.StartGrab(temp);
        }

        if (Input.GetMouseButtonUp(0))
        {
            grabber.EndGrab();
        }
    }



    private void FixedUpdate()
    {
        if (!simulate)
        {
            return;
        }

        foreach (ClothSimulationTutorial cloth in allCloth)
        {
            cloth.MyFixedUpdate();
        }
    }



    private void OnDestroy()
    {
        foreach (ClothSimulationTutorial cloth in allCloth)
        {
            Mesh mesh = cloth.MyOnDestroy();

            Destroy(mesh);
        }
    }



    private void TestFindNeighbors()
    {
        List<ClothEdge> edges = new();

        //T0: 1-3-2
        //T1: 0-1-2

        edges.Add(new ClothEdge(1, 3, 0));
        edges.Add(new ClothEdge(2, 3, 1));
        edges.Add(new ClothEdge(1, 2, 2));

        edges.Add(new ClothEdge(0, 1, 3));
        edges.Add(new ClothEdge(1, 2, 4));
        edges.Add(new ClothEdge(0, 2, 5));

        foreach (ClothEdge e in edges)
        {
            Debug.Log($"{e.id0}, {e.id1}, {e.edgeNr}");
        }


        edges.Sort((a, b) => ((a.id0 < b.id0) || (a.id0 == b.id0 && a.id1 < b.id1)) ? -1 : 1);

        Debug.Log("Sorted:");

        foreach (ClothEdge e in edges)
        {
            Debug.Log($"{e.id0}, {e.id1}, {e.edgeNr}");
        }


        //Find matching edges
        int[] neighbors = new int[edges.Count];

        //Init all edges to have no neighbors
        System.Array.Fill(neighbors, -1);

        //Find opposite edges
        /*
        int nr = 0;

        while (nr < edges.Count)
        {
            ClothEdge e0 = edges[nr];

            nr++;

            if (nr < edges.Count)
            {
                ClothEdge e1 = edges[nr];

                if (e0.id0 == e1.id0 && e0.id1 == e1.id1)
                {
                    neighbors[e0.edgeNr] = e1.edgeNr;
                    neighbors[e1.edgeNr] = e0.edgeNr;
                }

                nr++;
            }
        }
        */

        //Same result...
        for (int i = 0; i < edges.Count - 1; i++)
        {
            ClothEdge e0 = edges[i];
            ClothEdge e1 = edges[i + 1];

            if (e0.id0 == e1.id0 && e0.id1 == e1.id1)
            {
                neighbors[e0.edgeNr] = e1.edgeNr;
                neighbors[e1.edgeNr] = e0.edgeNr;
            }
        }

        Debug.Log("Neighbors:");

        foreach (int neighbor in neighbors)
        {
            Debug.Log(neighbor);
        }
    }



}