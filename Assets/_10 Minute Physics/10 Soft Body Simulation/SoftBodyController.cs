using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Simple and unbreakable simulation of soft bodies using Extended Position Based Dynamics (XPBD)
//Based on https://matthias-research.github.io/pages/tenMinutePhysics/index.html
public class SoftBodyController : MonoBehaviour
{
    public GameObject softBodyMeshPrefabGO;


    private SoftBodySimulation softBodySimulation;



    private void Start()
    {
        GameObject bunnyGO = Instantiate(softBodyMeshPrefabGO);
    
        MeshFilter meshFilter = bunnyGO.GetComponent<MeshFilter>();

        TetrahedronData softBodyMesh = new StanfordBunny();

        Vector3 startPos = new Vector3(0f, 20f, 0f);

        float bunnyScale = 2f;

        softBodySimulation = new SoftBodySimulation(meshFilter, softBodyMesh, startPos, bunnyScale);
    }



    private void Update()
    {
        softBodySimulation.MyUpdate();
    }



    private void FixedUpdate()
    {
        softBodySimulation.MyFixedUpdate();
    }



    private void OnDestroy()
    {
        Mesh mesh = softBodySimulation.MyOnDestroy();

        Destroy(mesh);
    }
}
