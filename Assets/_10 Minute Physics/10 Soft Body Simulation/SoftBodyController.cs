using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Simple and unbreakable simulation of soft bodies
//Based on https://matthias-research.github.io/pages/tenMinutePhysics/index.html
[RequireComponent(typeof(MeshRenderer), typeof(MeshFilter))]
public class SoftBodyController : MonoBehaviour
{
    private SoftBodySimulation softBodySimulation;



    private void Start()
    {
        MeshFilter meshFilter = this.GetComponent<MeshFilter>();

        SoftBodyMesh softBodyMesh = new StanfordBunny();

        softBodySimulation = new SoftBodySimulation(meshFilter, softBodyMesh);
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
