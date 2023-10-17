using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActualBilliardTable : BilliardTable
{
    //Public
    public Transform borderEdgesParent;

    public Transform innerDimensionsParent;

    public Transform bigHolesParent;
    public Transform smallHolesParent;

    public Material tableClothMaterial;
    public Material holeMaterial;
    public Material tableBorderMaterial;
    public Material tableClothSideMaterial;


    //Private

    //The diameter of a pool ball is 57 mm
    private readonly float ballRadius = 0.0285f;

    //The holes are roughly twice the size
    private readonly float bigHoleRadius = 0.11f;
    private readonly float smallHoleRadius = 0.08f;

    //The different meshes
    private List<Mesh> sideMeshes = new List<Mesh>();
    private List<Mesh> holesMeshes = new List<Mesh>();
    private Mesh tableClothMesh;
    private Mesh surroundingMesh;

    //The border edges for collision detection
    //private List<Vector3> borderVertices;
    //This one has an extra vertex at the end which is the same as first
    private List<Vector3> borderEdges;


    public override void Init()
    {
        GenerateMeshes();
    }



    private void GenerateMeshes()
    {
        List<Vector3> borderVertices = new();

        foreach (Transform child in borderEdgesParent)
        {
            borderVertices.Add(child.position);
        }

        borderEdges = new List<Vector3>(borderVertices);

        borderEdges.Add(borderEdges[0]);


        float tableSideDepth = 0.1f;

        //Cloth
        tableClothMesh = GenerateClothMesh(borderVertices, tableSideDepth * 0.9f);


        //Side
        sideMeshes.Add(GenerateBetweenHolesMesh(4,  borderVertices));
        sideMeshes.Add(GenerateBetweenHolesMesh(10, borderVertices));
        sideMeshes.Add(GenerateBetweenHolesMesh(17, borderVertices));
        sideMeshes.Add(GenerateBetweenHolesMesh(24, borderVertices));
        sideMeshes.Add(GenerateBetweenHolesMesh(30, borderVertices));
        sideMeshes.Add(GenerateBetweenHolesMesh(37, borderVertices));


        //Border
        surroundingMesh = GenerateOutsideMesh(borderVertices, tableSideDepth);


        //Holes
        foreach (Transform child in bigHolesParent)
        {
            holesMeshes.Add(DisplayShapes.GenerateCircleMesh_XZ(child.position, bigHoleRadius, 20));
        }

        foreach (Transform child in smallHolesParent)
        {
            holesMeshes.Add(DisplayShapes.GenerateCircleMesh_XZ(child.position, smallHoleRadius, 20));
        }
    }



    public override bool HandleBallEnvironmentCollision(Ball ball, float restitution = 1)
    {
        bool isColliding = BallCollisionHandling.HandleBallWallEdgesCollision(ball, borderEdges, restitution);

        return isColliding;
    }



    public override bool IsBallInHole(Ball ball)
    {
        bool isInHole = false;

        foreach (Transform child in bigHolesParent)
        {
            isInHole = IsBallInHole(ball.pos, ball.radius, child.position, bigHoleRadius);

            if (isInHole)
            {
                return true;
            }
        }

        foreach (Transform child in smallHolesParent)
        {
            isInHole = IsBallInHole(ball.pos, ball.radius, child.position, smallHoleRadius);

            if (isInHole)
            {
                return true;
            }
        }

        return isInHole;
    }



    private bool IsBallInHole(Vector3 ballPos, float ballRadius, Vector3 holePos, float holeRadius)
    {
        float distHoleBall = (ballPos - holePos).magnitude;

        float boleInHoleDist = holeRadius - ballRadius;

        if (distHoleBall < boleInHoleDist)
        {
            return true;
        }

        return false;
    }



    public override bool IsBallOutsideOfTable(Vector3 ballPos, float ballRadius)
    {
        throw new System.NotImplementedException();
    }



    private void OnDrawGizmos()
    {
        //Display the outline
        DisplayEdgesFromChildren(borderEdgesParent);

        //DisplayEdgesFromChildrent(innerDimensionsParent);



        //Display the holes
        Gizmos.color = Color.black;

        //Debug.Log(bigHoleRadius);

        foreach (Transform child in bigHolesParent)
        {
            Gizmos.DrawSphere(child.position, bigHoleRadius);
        }

        foreach (Transform child in smallHolesParent)
        {
            Gizmos.DrawSphere(child.position, smallHoleRadius);
        }
    }


    private Mesh GenerateBetweenHolesMesh(int index, List<Vector3> vertices)
    {
        Vector3[] verts = {
            vertices[UsefulMethods.ClampListIndex(index + 0, vertices.Count)],
            vertices[UsefulMethods.ClampListIndex(index + 1, vertices.Count)],
            vertices[UsefulMethods.ClampListIndex(index + 2, vertices.Count)],
            vertices[UsefulMethods.ClampListIndex(index + 3, vertices.Count)],
            vertices[UsefulMethods.ClampListIndex(index + 4, vertices.Count)],
            vertices[UsefulMethods.ClampListIndex(index + 5, vertices.Count)],
        };

        int[] tris = {
            0, 1, 2,
            2, 3, 0,
            0, 3, 5,
            3, 4, 5
        };

        Mesh m = new Mesh();

        m.vertices = verts;
        m.triangles = tris;

        m.RecalculateNormals();

        return m;
    }



    private Mesh GenerateOutsideMesh(List<Vector3> vertices, float depth)
    {
        Vector3 c1 = vertices[3];
        Vector3 c2 = vertices[16];
        Vector3 c3 = vertices[23];
        Vector3 c4 = vertices[36];

        Vector3 c1_outer = c1 + Vector3.forward * depth + Vector3.left * depth;
        Vector3 c2_outer = c2 - Vector3.forward * depth + Vector3.left * depth;
        Vector3 c3_outer = c3 - Vector3.forward * depth - Vector3.left * depth;
        Vector3 c4_outer = c4 + Vector3.forward * depth - Vector3.left * depth;

        Vector3[] verts = { c1, c2 , c3, c4, c1_outer, c2_outer, c3_outer, c4_outer };

        int[] tris = {
            0, 1, 4,
            4, 1, 5,
            5, 1, 2,
            5, 2, 6,
            6, 2, 3,
            6, 3, 7,
            7, 3, 0,
            7, 0, 4
        };

        Mesh m = new Mesh();

        m.vertices = verts;
        m.triangles = tris;

        m.RecalculateNormals();

        return m;
    }


    private Mesh GenerateClothMesh(List<Vector3> vertices, float depth)
    {
        Vector3 c1 = vertices[3];
        Vector3 c2 = vertices[16];
        Vector3 c3 = vertices[23];
        Vector3 c4 = vertices[36];

        Vector3 c1_outer = c1 + Vector3.forward * depth + Vector3.left * depth;
        Vector3 c2_outer = c2 - Vector3.forward * depth + Vector3.left * depth;
        Vector3 c3_outer = c3 - Vector3.forward * depth - Vector3.left * depth;
        Vector3 c4_outer = c4 + Vector3.forward * depth - Vector3.left * depth;

        Vector3[] verts = { c1_outer, c2_outer, c3_outer, c4_outer };

        int[] tris = {
            0, 2, 1,
            0, 3, 2
        };

        Mesh m = new Mesh();

        m.vertices = verts;
        m.triangles = tris;

        m.RecalculateNormals();

        return m;
    }



    private void DisplayEdgesFromChildren(Transform parent)
    {
        if (parent == null)
        {
            return;
        }
    
        List<Vector3> verts = new();

        foreach (Transform child in parent)
        {
            verts.Add(child.position);
        }


        Gizmos.color = Color.white;

        for (int i = 1; i < verts.Count; i++)
        {
            Gizmos.DrawLine(verts[i - 1], verts[i]);
        }

        Gizmos.DrawLine(verts[^1], verts[0]);


        //Display the table meshes
        List<Mesh> meshes = new List<Mesh>();

        meshes.Add(GenerateBetweenHolesMesh(4, verts));
        meshes.Add(GenerateBetweenHolesMesh(10, verts));
        meshes.Add(GenerateBetweenHolesMesh(17, verts));
        meshes.Add(GenerateBetweenHolesMesh(24, verts));
        meshes.Add(GenerateBetweenHolesMesh(30, verts));
        meshes.Add(GenerateBetweenHolesMesh(37, verts));

        foreach (Mesh m in meshes)
        {
            Gizmos.DrawMesh(m);
        }

        Gizmos.DrawMesh(GenerateOutsideMesh(verts, 0.1f));

        //Cloth
        //Gizmos.DrawMesh(GenerateClothMesh(verts, 0.1f * 0.9f));
    }

    public override void MyUpdate()
    {
        Vector3 sideMeshPos = Vector3.zero;
    
        Vector3 holePos = -Vector3.up * 0.01f;
    
        Vector3 clothPos = -Vector3.up * 0.02f;

        foreach (Mesh m in holesMeshes)
        {
            Graphics.DrawMesh(m, holePos, Quaternion.identity, holeMaterial, 0);
        }

        foreach (Mesh m in sideMeshes)
        {
            Graphics.DrawMesh(m, sideMeshPos, Quaternion.identity, tableClothSideMaterial, 0);
        }

        Graphics.DrawMesh(tableClothMesh, clothPos, Quaternion.identity, tableClothMaterial, 0);

        Graphics.DrawMesh(surroundingMesh, sideMeshPos, Quaternion.identity, tableBorderMaterial, 0);
    }
}
