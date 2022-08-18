using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rectangle : BilliardTable
{
    private readonly float xWidth = 14f;
    private readonly float zWidth = 10f;



    public override void Init()
    {
        MeshFilter mf = this.gameObject.GetComponent<MeshFilter>();

        GenerateQuadMesh(mf, transform.position, xWidth, zWidth);
    }



    public override bool HandleBallCollision(Ball ball, float restitution)
    {
        bool isColliding = false;

        Vector3 tablePos = transform.position;

        float halfX = xWidth * 0.5f;
        float halfZ = zWidth * 0.5f;

        if (ball.pos.x > tablePos.x + halfX - ball.radius)
        {
            ball.pos.x = tablePos.x + halfX - ball.radius;
            ball.vel.x *= -1f;

            isColliding = true;
        }
        else if (ball.pos.x < tablePos.x - halfX + ball.radius)
        {
            ball.pos.x = tablePos.x - halfX + ball.radius;
            ball.vel.x *= -1f;

            isColliding = true;
        }

        if (ball.pos.z > tablePos.z + halfZ - ball.radius)
        {
            ball.pos.z = tablePos.z + halfZ - ball.radius;
            ball.vel.z *= -1f;

            isColliding = true;
        }
        else if (ball.pos.z < tablePos.z - halfZ + ball.radius)
        {
            ball.pos.z = tablePos.z - halfZ + ball.radius;
            ball.vel.z *= -1f;

            isColliding = true;
        }

        return isColliding;
    }

    

    public override bool IsBallOutsideOfTable(Vector3 ballPos, float ballRadius)
    {
        Vector3 tablePos = transform.position;

        float halfX = xWidth * 0.5f;
        float halfZ = zWidth * 0.5f;

        if (ballPos.x > tablePos.x + halfX - ballRadius)
        {
            return true;
        }
        if (ballPos.x < tablePos.x - halfX + ballRadius)
        {
            return true;
        }
        if (ballPos.z > tablePos.z + halfZ - ballRadius)
        {
            return true;
        }
        if (ballPos.z < tablePos.z - halfZ + ballRadius)
        {
            return true;
        }

        return false;
    }



    private void GenerateQuadMesh(MeshFilter mf, Vector3 center, float xWidth, float zWidth)
    {
        float halfX = xWidth * 0.5f;
        float halfZ = zWidth * 0.5f;

        Vector3 TL = new (center.x - halfX, 0f, center.z - halfZ);
        Vector3 TR = new (center.x - halfX, 0f, center.z + halfZ);
        Vector3 BL = new (center.x + halfX, 0f, center.z - halfZ);
        Vector3 BR = new (center.x + halfX, 0f, center.z + halfZ);

        Vector3[] vertices = { TL, TR, BL, BR };

        int[] triangles = { 
            0, 1, 3,
            0, 3, 2
        };

        //Generate the mesh
        Mesh m = new();

        m.SetVertices(vertices);
        m.SetTriangles(triangles, 0);

        m.RecalculateNormals();

        mf.sharedMesh = m;
    }



    public override bool IsBallInHole(Ball ball)
    {
        return false;
    }
}
