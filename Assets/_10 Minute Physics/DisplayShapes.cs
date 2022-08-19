using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Help class to display geometric shapes
public static class DisplayShapes
{
    private static readonly Material matWhite;
    private static readonly Material matRed;
    private static readonly Material matBlue;
    private static readonly Material matYellow;
    private static readonly Material matGray;

    public enum ColorOptions
    {
        White, Red, Blue, Yellow, Gray
    }



    static DisplayShapes()
    {
        matWhite = new Material(Shader.Find("Universal Render Pipeline/Unlit"))
        {
            color = Color.white
        };

        matRed = new Material(Shader.Find("Universal Render Pipeline/Unlit"))
        {
            color = Color.red
        };

        matBlue = new Material(Shader.Find("Universal Render Pipeline/Unlit"))
        {
            color = Color.blue
        };

        matYellow = new Material(Shader.Find("Universal Render Pipeline/Unlit"))
        {
            color = Color.yellow
        };

        matGray = new Material(Shader.Find("Universal Render Pipeline/Unlit"))
        {
            color = Color.gray
        };
    }



    public static Material GetMaterial(ColorOptions color)
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
    public enum Space2D { XY, XZ, YX };

    public static void DrawCircle(Vector3 circleCenter, float radius, ColorOptions color, Space2D space)
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

            if (space == Space2D.YX)
            {
                vertex = new Vector3(0f, y, x) + circleCenter;
            }
            else if (space == Space2D.XZ)
            {
                vertex = new Vector3(x, 0f, y) + circleCenter;
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
        //Display the mesh
        Material material = GetMaterial(color);

        DrawLine(vertices, material);
    }

    public static void DrawLine(List<Vector3> vertices, Material material)
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

        Graphics.DrawMesh(m, Vector3.zero, Quaternion.identity, material, 0, Camera.main, 0);
    }



    //Draw vertices
    public static void DrawVertices(List<Vector3> vertices, Material material)
    {
        //Generate the indices
        List<int> indices = new ();

        for (int i = 0; i < vertices.Count; i++)
        {
            indices.Add(i);
        }

        //Generate the mesh
        Mesh m = new ();

        m.SetVertices(vertices);
        m.SetIndices(indices, MeshTopology.Points, 0);

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
        DrawCircle(a, radius, color, Space2D.XY);
        DrawCircle(b, radius, color, Space2D.XY);

        //Draw the two lines connecting the end points
        Vector3 vecAB = (a - b).normalized;

        //To get the normal to the line flip the coordinates and make one negative
        Vector3 normalAB = vecAB.Perp();

        DrawLine(new List<Vector3> { a + normalAB * radius, b + normalAB * radius }, color);
        DrawLine(new List<Vector3> { a - normalAB * radius, b - normalAB * radius }, color);
    }



    //Draw a wireframe mesh
    public static void DrawWireframeMesh(CustomMesh mesh, float squeezeDist = 0f, bool drawNormals = false)
    {
        List<Vector3> triangleLineSegments = new List<Vector3>();
        List<Vector3> intersectedTriangleLineSegments = new List<Vector3>();
        List<Vector3> normalsLineSegments = new List<Vector3>();

        Vector3[] vertices = mesh.vertices.ToArray();
        int[] triangles = mesh.triangles.ToArray();
        bool[] isMarked = mesh.isMarked.ToArray();

        for (int i = 0; i < triangles.Length; i += 3)
        {
            Vector3 a = vertices[triangles[i + 0]];
            Vector3 b = vertices[triangles[i + 1]];
            Vector3 c = vertices[triangles[i + 2]];

            Vector3 center = (a + b + c) / 3f;

            //Make the triangle smaller to make it easier to see separate triangles
            if (squeezeDist > 0f)
            {
                a += (center - a).normalized * squeezeDist;
                b += (center - b).normalized * squeezeDist;
                c += (center - c).normalized * squeezeDist;
            }

            List<Vector3> lineSegments = new List<Vector3>() { a, b, b, c, c, a };

            if (!isMarked[i])
            {
                triangleLineSegments.AddRange(lineSegments);
            }
            else
            {
                intersectedTriangleLineSegments.AddRange(lineSegments);
            }
            

            if (drawNormals)
            {
                Vector3 normalStart = center;
                Vector3 normalEnd = center + mesh.CalculateNormal(a, b, c) * 0.2f;

                normalsLineSegments.Add(normalStart);
                normalsLineSegments.Add(normalEnd);
            }
        }

        DrawLineSegments(triangleLineSegments, ColorOptions.White);
        DrawLineSegments(intersectedTriangleLineSegments, ColorOptions.Red);
        DrawLineSegments(normalsLineSegments, ColorOptions.Blue);
    }



    //Generate a circular mesh 
    public static Mesh GenerateCircleMesh(Vector3 circleCenter, float radius, int segments)
    {
        //Generate the vertices
        List<Vector3> vertices = UsefulMethods.GetCircleSegments_XZ(circleCenter, radius, segments);

        //Add the center to make it easier to trianglulate
        vertices.Insert(0, circleCenter);


        //Generate the triangles
        List<int> triangles = new();

        for (int i = 2; i < vertices.Count; i++)
        {
            triangles.Add(0);
            triangles.Add(i);
            triangles.Add(i - 1);
        }

        //Generate the mesh
        Mesh m = new();

        m.SetVertices(vertices);
        m.SetTriangles(triangles, 0);

        m.RecalculateNormals();

        return m;
    }
}
