using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UserInteraction;


//Simple and unbreakable simulation of soft bodies using Extended Position Based Dynamics (XPBD)
//Based on https://matthias-research.github.io/pages/tenMinutePhysics/index.html
public class SoftBodyController : MonoBehaviour
{
    //Public
    public GameObject softBodyMeshPrefabGO;
    
    public Texture2D cursorTexture;


    //Private
    private List<SoftBodySimulation> allSoftBodies = new ();

    private int numberOfBodies = 1;

    private const int SEED = 0;

    //What we use to grab the balls
    private Grabber grabber;



    private void Start()
    {
        Random.InitState(SEED);
    
        for (int i = 0; i < numberOfBodies; i++)
        {
            GameObject bunnyGO = Instantiate(softBodyMeshPrefabGO);

            MeshFilter meshFilter = bunnyGO.GetComponent<MeshFilter>();

            TetrahedronData softBodyMesh = new StanfordBunny();

            float halfPlayground = 4f;

            float randomX = Random.Range(-halfPlayground, halfPlayground);
            float randomZ = Random.Range(-halfPlayground, halfPlayground);

            Vector3 startPos = new Vector3(randomX, 10f, randomZ);

            float bunnyScale = 2f;

            SoftBodySimulation softBodySimulation = new SoftBodySimulation(meshFilter, softBodyMesh, startPos, bunnyScale);

            allSoftBodies.Add(softBodySimulation);
        }

        //Init the grabber
        grabber = new Grabber(Camera.main);

        Cursor.visible = true;

        Cursor.SetCursor(cursorTexture, Vector2.zero, CursorMode.ForceSoftware);
    }



    private void Update()
    {
        foreach(SoftBodySimulation softBody in allSoftBodies)
        {
            softBody.MyUpdate();
        }

        grabber.MoveGrab();
    }



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



    private void FixedUpdate()
    {
        foreach (SoftBodySimulation softBody in allSoftBodies)
        {
            softBody.MyFixedUpdate();
        }
    }



    private void OnDestroy()
    {
        foreach (SoftBodySimulation softBody in allSoftBodies)
        {
            Mesh mesh = softBody.MyOnDestroy();

            Destroy(mesh);
        }
    }
}
