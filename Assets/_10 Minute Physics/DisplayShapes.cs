using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Help class to display geometric shapes
public static class DisplayShapes
{
    //Draw a circle in 2d space
    public static void DrawCircle(Vector3 circleCenter, float radius, Color color, bool isXSpace = true)
    {
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        mat.color = color;

        
        //Generate the vertices and the indices
        int circleResolution = 100;

        List<Vector3> vertices = new List<Vector3>();
        List<int> indices = new List<int>();

        float angleStep = 360f / circleResolution;

        float angle = 0f;

        for (int i = 0; i < circleResolution + 1; i++)
        {
            float x = radius * Mathf.Cos(angle * Mathf.Deg2Rad);
            float y = radius * Mathf.Sin(angle * Mathf.Deg2Rad);

            Vector3 vertex = new Vector3(x, y, 0f) + circleCenter;

            if (!isXSpace)
            {
                vertex = new Vector3(0f, y, x) + circleCenter;
            }
            

            vertices.Add(vertex);
            indices.Add(i);

            angle += angleStep;
        }

        //Generate the mesh
        Mesh m = new Mesh();

        m.SetVertices(vertices);
        m.SetIndices(indices, MeshTopology.LineStrip, 0);

        //Display the mesh
        Graphics.DrawMesh(m, Vector3.zero, Quaternion.identity, mat, 0, Camera.main, 0);
    }



    public static void DrawLineSegments(List<Vector3> vertices, Color color)
    {
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        mat.color = color;

        //Generate the indices
        List<int> indices = new List<int>();

        for (int i = 0; i < vertices.Count; i++)
        {
            indices.Add(i);
        }

        //Generate the mesh
        Mesh m = new Mesh();

        m.SetVertices(vertices);
        m.SetIndices(indices, MeshTopology.LineStrip, 0);

        //Display the mesh
        Graphics.DrawMesh(m, Vector3.zero, Quaternion.identity, mat, 0, Camera.main, 0);
    }
}
