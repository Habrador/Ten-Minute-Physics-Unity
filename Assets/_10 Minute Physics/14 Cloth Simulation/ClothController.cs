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
        Random.InitState(SEED);

        ClothData clothData = new ClothDataTutorial();


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

            Vector3 startPos = Vector3.zero;


            //Scale
            //float clothScale = Random.Range(2f, 5f);

            float clothScale = 1f;


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

        //grabber.MoveGrab();

        ////Pause simulation
        //if (Input.GetKeyDown(KeyCode.P))
        //{
        //    simulate = !simulate;
        //}
    }
    

    /*
    private void LateUpdate()
    {
        if (Input.GetMouseButtonDown(0))
        {
            List<IGrabbable> temp = new List<IGrabbable>(allSoftBodies);

            grabber.StartGrab(temp);
        }

        if (Input.GetMouseButtonUp(0))
        {
            grabber.EndGrab();
        }
    }

    */
    
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
}
