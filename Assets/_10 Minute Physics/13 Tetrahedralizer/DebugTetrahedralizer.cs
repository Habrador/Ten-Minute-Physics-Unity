using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugTetrahedralizer : MonoBehaviour
{
    public Transform testMeshTransform;

    public Transform testPointTransform;


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

        TestPointMesh(testMeshTransform, testPointTransform);
    }



    //
    // Custom raycast
    //

    //Test point mesh intersection
    private void TestPointMesh(Transform meshTransform, Transform testPointTransform)
    {
        Mesh mesh = meshTransform.GetComponent<MeshFilter>().mesh;

        CustomMesh customMesh = new CustomMesh(mesh);

        //Vector3 pointLocal = meshTransform.InverseTransformPoint(point);

        meshTransform.GetComponent<MeshRenderer>().enabled = false;

        //Convert to triangle data structure to make it easier to display
        //The triangles are now in global space
        List<Triangle> meshTriangles = ConvertMeshToTriangles(meshTransform);

        //See if the point is intersecting any of the triangles
        Ray ray = new Ray(testPointTransform.position, testPointTransform.forward);

        DebugRayTriangleIntersection(meshTriangles, ray);

        DisplayRay(testPointTransform);

        DisplayShapes.DrawWireframeMesh(meshTriangles, 0.02f, false);

        //if (StandardizedMethods.IsPointInsideMesh(customMesh, pointLocal))
        //{
        //    Debug.Log("Inside");
        //}
        //else
        //{
        //    Debug.Log("Outside");
        //}
    }



    private void DebugRayTriangleIntersection(List<Triangle> meshTriangles, Ray ray)
    {
        if (StandardizedMethods.IsRayHittingMesh(ray, meshTriangles, out CustomHit bestHit))
        {
            //Mark the triangle has being hit so we can display it with a different color
            meshTriangles[bestHit.index].isIntersecting = true;
        
            Debug.Log("Hit");
        }
        else
        {
            Debug.Log("Miss");
        }
    }



    private void DisplayRay(Transform testPoint)
    {
        float rayLength = 100f;

        Vector3 a = testPoint.position;
        Vector3 b = a + testPoint.forward * rayLength;

        DisplayShapes.DrawLine(new List<Vector3>() { a, b }, DisplayShapes.ColorOptions.Yellow);
    }



    //Convert a Unity mesh to a list of triangles in global space
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
