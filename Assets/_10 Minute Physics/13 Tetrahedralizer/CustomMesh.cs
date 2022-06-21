using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CustomMesh
{
    public List<Vector3> vertices;

    public List<int> triangles;

    //Same size as triangles
    public List<bool> isMarked;

    public string name;



    public CustomMesh()
    {

    }



    public CustomMesh(Transform meshTransform, bool toGlobal)
    {
        Mesh mesh = meshTransform.GetComponent<MeshFilter>().sharedMesh;
    
        if (toGlobal)
        {
            Vector3[] verticesLocal = mesh.vertices;

            vertices = new List<Vector3>();

            for (int i = 0; i < verticesLocal.Length; i++)
            {
                vertices[i] = meshTransform.TransformPoint(verticesLocal[i]);
            }
        }
        else
        {
            vertices = new List<Vector3>(mesh.vertices);
        }

        triangles = new List<int>(mesh.triangles);

        //Default is false
        isMarked = new List<bool>();
    }



    public void AddTriangle(int i0, int i1, int i2)
    {
        triangles.Add(i0);
        triangles.Add(i1);
        triangles.Add(i2);
    }



    //Calculate the normal given three points
    public Vector3 CalculateNormal(Vector3 a, Vector3 b, Vector3 c)
    {
        //The normal of the triangle a-b-c (oriented counter-clockwise) is:
        Vector3 normal = Vector3.Cross(b - a, c - a).normalized;

        return normal;
    }



    //A list of all triangles, making it easier to display a triangle with a different color, etc
    //public List<Triangle> GetTriangles(List<int> markedTriangles = null)
    //{
    //    List<Triangle> tris = new List<Triangle>();

    //    for (int i = 0; i < triangles.Length; i += 3)
    //    {
    //        Vector3 a = vertices[triangles[i + 0]];
    //        Vector3 b = vertices[triangles[i + 1]];
    //        Vector3 c = vertices[triangles[i + 2]];

    //        Triangle t = new Triangle(a, b, c);

    //        //Should this triangle be marked
    //        if (markedTriangles != null && markedTriangles.Contains(i))
    //        {
    //            t.isIntersecting = true;
    //        }

    //        tris.Add(t);
    //    }

    //    return tris;
    //}
}
