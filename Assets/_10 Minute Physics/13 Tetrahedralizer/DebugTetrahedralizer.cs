using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugTetrahedralizer : MonoBehaviour
{
    public Transform testMeshTransform;

    public Transform testPointTransform;
      


    public void Start()
    {
        
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
        //meshTransform.GetComponent<MeshRenderer>().enabled = false;

        //Convert mesh to global space
        CustomMesh customMesh = new CustomMesh(meshTransform, true);

        
        DebugRayTriangleIntersection(customMesh, testPointTransform);


        //DebugPointMeshIntersection(customMesh, testPointTransform.position);
    }



    private void DebugPointMeshIntersection(CustomMesh customMesh, Vector3 point)
    {
        if (Intersections.IsPointInsideMesh(customMesh.vertices.ToArray(), customMesh.triangles.ToArray(), point))
        {
            Debug.Log("Inside");
        }
        else
        {
            Debug.Log("Outside");
        }
    }



    private void DebugRayTriangleIntersection(CustomMesh customMesh, Transform testPointTransform)
    {
        //Generate the ray
        Ray ray = new Ray(testPointTransform.position, testPointTransform.forward);

        if (Intersections.IsRayHittingMesh(ray, customMesh.vertices.ToArray(), customMesh.triangles.ToArray(), out CustomHit bestHit))
        {        
            Debug.Log("Hit");
        }
        else
        {
            Debug.Log("Miss");
        }


        //Display the ray
        DisplayRay(testPointTransform);

        //Display the mesh

        //Mark the triangle has being hit so we can display it with a different color
        if (bestHit != null)
        {
            customMesh.isMarked[bestHit.index] = true;
        }


        DisplayShapes.DrawWireframeMesh(customMesh, 0.02f, true);
    }



    private void DisplayRay(Transform testPoint)
    {
        float rayLength = 100f;

        Vector3 a = testPoint.position;
        Vector3 b = a + testPoint.forward * rayLength;

        DisplayShapes.DrawLine(new List<Vector3>() { a, b }, DisplayShapes.ColorOptions.Yellow);
    }
}
