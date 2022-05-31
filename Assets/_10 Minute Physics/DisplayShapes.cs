using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Help class to display geometric shapes
public static class DisplayShapes
{
    private static Material matWhite;
    private static Material matRed;
    private static Material matBlue;
    private static Material matYellow;

    public enum ColorOptions
    {
        White, Red, Blue, Yellow
    }



    static DisplayShapes()
    {
        matWhite = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        matWhite.color = Color.white;

        matRed = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        matRed.color = Color.red;

        matBlue = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        matBlue.color = Color.blue;

        matYellow = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        matYellow.color = Color.yellow;
    }



    private static Material GetMaterial(ColorOptions color)
    {
        return color switch
        {
            (ColorOptions.Red) => matRed,
            (ColorOptions.Blue) => matBlue,
            (ColorOptions.Yellow) => matYellow,
            (ColorOptions.White) => matWhite,
            _ => matWhite,
        };
    }



    //Draw a circle in 2d space
    public static void DrawCircle(Vector3 circleCenter, float radius, ColorOptions color, bool isXSpace = true)
    {        
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
        Material material = GetMaterial(color);

        Graphics.DrawMesh(m, Vector3.zero, Quaternion.identity, material, 0, Camera.main, 0);
    }



    //Draw a line which may consist of several segments, but it has to be connnected into one line
    public static void DrawLine(List<Vector3> vertices, ColorOptions color)
    {
        if (vertices.Count < 2)
        {
            return;
        }

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
        Material material = GetMaterial(color);

        Graphics.DrawMesh(m, Vector3.zero, Quaternion.identity, material, 0, Camera.main, 0);
    }



    //Draw line segments that are not necessarily connected
    public static void DrawLineSegments(List<Vector3> vertices, ColorOptions color)
    {
        if (vertices.Count < 2)
        {
            return;
        }
    
        //Generate the indices
        List<int> indices = new List<int>();

        for (int i = 0; i < vertices.Count; i++)
        {
            indices.Add(i);
        }

        //Generate the mesh
        Mesh m = new Mesh();

        m.SetVertices(vertices);
        m.SetIndices(indices, MeshTopology.Lines, 0);

        //Display the mesh
        Material material = GetMaterial(color);

        Graphics.DrawMesh(m, Vector3.zero, Quaternion.identity, material, 0, Camera.main, 0);
    }



    //Draw 2d capsule
    public static void DrawCapsule(Vector3 a, Vector3 b, float radius, ColorOptions color)
    {
        //Draw the end points
        DrawCircle(a, radius, color);
        DrawCircle(b, radius, color);

        //Draw the two lines connecting the end points
        Vector3 vecAB = (a - b).normalized;

        //To get the normal to the line flip the coordinates and make one negative
        Vector3 normalAB = vecAB.Perp();

        DrawLine(new List<Vector3> { a + normalAB * radius, b + normalAB * radius }, color);
        DrawLine(new List<Vector3> { a - normalAB * radius, b - normalAB * radius }, color);
    }



    //Draw a wireframe mesh
    public static void DrawWireframeMesh(List<Triangle> triangles, float squeezeDist = 0f, bool drawNormals = false)
    {
        List<Vector3> triangleLineSegments = new List<Vector3>();
        List<Vector3> intersectedTriangleLineSegments = new List<Vector3>();
        List<Vector3> normalsLineSegments = new List<Vector3>();

        foreach (Triangle t in triangles)
        {
            Vector3 a = t.a;
            Vector3 b = t.b;
            Vector3 c = t.c;

            //Make the triangle smaller to make it easier to see separate triangles
            if (squeezeDist > 0f)
            {
                Vector3 center = t.GetCenter;

                a += (center - a).normalized * squeezeDist;
                b += (center - b).normalized * squeezeDist;
                c += (center - c).normalized * squeezeDist;
            }

            List<Vector3> lineSegments = new List<Vector3>() { a, b, b, c, c, a };

            if (!t.isIntersecting)
            {
                triangleLineSegments.AddRange(lineSegments);
            }
            else
            {
                intersectedTriangleLineSegments.AddRange(lineSegments);
            }
            

            if (drawNormals)
            {
                Vector3 normalStart = t.GetCenter;
                Vector3 normalEnd = t.GetCenter + t.normal * 0.2f;

                normalsLineSegments.Add(normalStart);
                normalsLineSegments.Add(normalEnd);
            }
        }

        DrawLineSegments(triangleLineSegments, ColorOptions.White);
        DrawLineSegments(intersectedTriangleLineSegments, ColorOptions.Red);
        DrawLineSegments(normalsLineSegments, ColorOptions.Blue);
    }
}
