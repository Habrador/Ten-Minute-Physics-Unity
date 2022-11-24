using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Simple and unbreakable simulation of soft bodies using Extended Position Based Dynamics (XPBD)
//Based on https://matthias-research.github.io/pages/tenMinutePhysics/index.html
public class SoftBodyController : MonoBehaviour
{
    public GameObject softBodyMeshPrefabGO;


    private List<SoftBodySimulation> allSoftBodies = new ();

    private int numberOfBodies = 1;

    private const int SEED = 0;



    private void Start()
    {
        Random.InitState(SEED);
    
        for (int i = 0; i < numberOfBodies; i++)
        {
            GameObject bunnyGO = Instantiate(softBodyMeshPrefabGO);

            MeshFilter meshFilter = bunnyGO.GetComponent<MeshFilter>();

            TetrahedronData softBodyMesh = new StanfordBunny();

            Vector3 startPos = new Vector3(0f + Random.Range(0, 10), 20f, 0f);

            float bunnyScale = 2f;

            SoftBodySimulation softBodySimulation = new SoftBodySimulation(meshFilter, softBodyMesh, startPos, bunnyScale);

            allSoftBodies.Add(softBodySimulation);
        }
    }



    private void Update()
    {
        foreach(SoftBodySimulation softBody in allSoftBodies)
        {
            softBody.MyUpdate();
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
