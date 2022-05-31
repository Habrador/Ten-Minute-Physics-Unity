using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Based on "Writing a Tetrahedralizer for Blender"
//https://matthias-research.github.io/pages/tenMinutePhysics/
public class TetraController : MonoBehaviour
{
    public Transform meshTransform;


    public void TetrahedralizeMesh()
    {
        //Debug.Log("Hello");

        //Settings
        int resolution = 10;

        int minQuality = -3;

        bool oneFacePerTet = true;

        float tetScale = 0.8f;

        //Convert the mesh to global space
        CustomMesh mesh = new CustomMesh(meshTransform, true);

        Tetrahedralizer.CreateTetrahedralization(mesh, resolution, minQuality, oneFacePerTet, tetScale);
    }
}
