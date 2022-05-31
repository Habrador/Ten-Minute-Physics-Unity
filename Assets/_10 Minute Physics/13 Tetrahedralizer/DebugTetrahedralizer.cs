using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugTetrahedralizer : MonoBehaviour
{
    public Transform testMeshTransform;

    public Transform testPoint;


    //private List<Triangle> meshTriangles;



    public void Start()
    {
        //meshTriangles = ConvertMeshToTriangles(testMeshTransform);
    }



    public void Update()
    {
        //To display the mesh as wireframe we use DrawMesh which allocated more and more memory
        Resources.UnloadUnusedAssets();

        System.GC.Collect();

        TestPointMesh(testMeshTransform, testPoint.position);
    }



    //
    // Custom raycast
    //

    //Test point mesh intersection
    private void TestPointMesh(Transform meshTransform, Vector3 point)
    {
        Mesh mesh = meshTransform.GetComponent<MeshFilter>().mesh;

        CustomMesh customMesh = new CustomMesh(mesh);

        Vector3 pointLocal = meshTransform.InverseTransformPoint(point);

        meshTransform.GetComponent<MeshRenderer>().enabled = false;

        //Convert to triangle data structure to make it easier to display
        //The triangles are in global position
        List<Triangle> meshTriangles = ConvertMeshToTriangles(meshTransform);

        DisplayShapes.DrawWireframeMesh(meshTriangles, true);

        //if (StandardizedMethods.IsPointInsideMesh(customMesh, pointLocal))
        //{
        //    Debug.Log("Inside");
        //}
        //else
        //{
        //    Debug.Log("Outside");
        //}
    }


    private List<Triangle> ConvertMeshToTriangles(Transform meshTransform)
    {
        Mesh mesh = meshTransform.GetComponent<MeshFilter>().mesh;

        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;

        List<Triangle> triangleStructure = new List<Triangle>();

        for (int i = 0; i < triangles.Length; i += 3)
        {
            Vector3 a = vertices[triangles[i + 0]];
            Vector3 b = vertices[triangles[i + 1]];
            Vector3 c = vertices[triangles[i + 2]];

            Vector3 aGlobal = meshTransform.TransformPoint(a);
            Vector3 bGlobal = meshTransform.TransformPoint(b);
            Vector3 cGlobal = meshTransform.TransformPoint(c);

            Triangle newTriangle = new Triangle(aGlobal, bGlobal, cGlobal);

            triangleStructure.Add(newTriangle);
        }

        return triangleStructure;
    }
}
