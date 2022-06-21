using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Based on "Writing a Tetrahedralizer for Blender"
//https://matthias-research.github.io/pages/tenMinutePhysics/
public class TetraController : MonoBehaviour
{
    public Transform meshTransform;

    private List<Vector3> debugPoints = new List<Vector3>();


    public void TetrahedralizeMesh()
    {
        Debug.Log("Tetrahedralizer started!");

        //Settings
        int resolution = 10;

        int minQuality = -3;

        bool oneFacePerTet = true;

        float tetScale = 0.8f;

        //Convert the mesh to global space
        CustomMesh mesh = new CustomMesh(meshTransform, true);

        CustomMesh tetras = Tetrahedralizer.CreateTetrahedralization(mesh, resolution, minQuality, oneFacePerTet, tetScale, debugPoints);

        Debug.Log("Tetrahedralizer completed!");
    }



    private void OnDrawGizmos()
    {
        //foreach (Vector3 v in debugPoints)
        //{
        //    Gizmos.DrawSphere(v, 0.01f);
        //}
    }
}
