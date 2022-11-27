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
    private readonly List<SoftBodySimulationVectors> allSoftBodies = new ();

    private int numberOfBodies = 3;

    private const int SEED = 0;

    //What we use to grab the balls
    private Grabber grabber;

    private bool simulate = true;

    private readonly Color[] colors = new Color[] { Color.red, Color.blue, Color.green, Color.yellow, Color.cyan };

    

    private void Start()
    {
        Random.InitState(SEED);

        TetrahedronData softBodyMesh = new StanfordBunny();

        for (int i = 0; i < numberOfBodies; i++)
        {
            GameObject bunnyGO = Instantiate(softBodyMeshPrefabGO);

            MeshFilter meshFilter = bunnyGO.GetComponent<MeshFilter>();


            //Random pos
            float halfPlayground = 5f;

            float randomX = Random.Range(-halfPlayground, halfPlayground);
            float randomZ = Random.Range(-halfPlayground, halfPlayground);

            Vector3 startPos = new Vector3(randomX, 10f, randomZ);


            //Random scale
            float bunnyScale = Random.Range(2f, 5f);


            //Random color
            MeshRenderer mr = bunnyGO.GetComponent<MeshRenderer>();

            Material mat = mr.material;

            mat.color = colors[Random.Range(0, colors.Length)];
            

            //SoftBodySimulationTutorial softBodySim = new SoftBodySimulationTutorial(meshFilter, softBodyMesh, startPos, bunnyScale);
            SoftBodySimulationVectors softBodySim = new SoftBodySimulationVectors(meshFilter, softBodyMesh, startPos, bunnyScale);


            allSoftBodies.Add(softBodySim);
        }

        //Init the grabber
        grabber = new Grabber(Camera.main);

        Cursor.visible = true;

        Cursor.SetCursor(cursorTexture, Vector2.zero, CursorMode.ForceSoftware);
    }



    private void Update()
    {
        foreach (SoftBodySimulationVectors softBody in allSoftBodies)
        {
            softBody.MyUpdate();
        }

        grabber.MoveGrab();

        //Pause simulation
        if (Input.GetKeyDown(KeyCode.P))
        {
            simulate = !simulate;
        }
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
        //Timers.Reset();
    
        if (!simulate)
        {
            return;
        }

        foreach (SoftBodySimulationVectors softBody in allSoftBodies)
        {
            softBody.MyFixedUpdate();
        }

        //Timers.Display();
    }



    private void OnDestroy()
    {
        foreach (SoftBodySimulationVectors softBody in allSoftBodies)
        {
            Mesh mesh = softBody.MyOnDestroy();

            Destroy(mesh);
        }
    }
}
