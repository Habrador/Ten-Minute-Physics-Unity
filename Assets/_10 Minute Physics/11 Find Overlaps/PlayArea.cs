using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Display a grid and handle ball-environment collisions
public class PlayArea
{
    //Grid settings
    private readonly int numberOfCells;
    private readonly float cellSize;

    //Display the grid with line mesh
    private Material gridMaterial;
    private Mesh gridMesh;
    private Mesh borderMesh;

    //Getters
    public float GridWidth => numberOfCells * cellSize;

    public Vector3 GridCenter
    {
        get 
        {
            float center = GridWidth * 0.5f;

            return new(center, 0f, center);
        }
    }



    public PlayArea(float _gridWidth, float _cellSize)
    {
        this.numberOfCells = Mathf.RoundToInt(_gridWidth / _cellSize);
        this.cellSize = _cellSize;
    }



    //
    // Display the grid
    //

    //The grid is just for display purposes. Because we use spatial hashing so we are not limited to a fixed grid
    public void DisplayMap(bool showGrid)
    {
        //Display the grid with lines
        if (gridMaterial == null)
        {
            gridMaterial = new Material(Shader.Find("Unlit/Color"));

            gridMaterial.color = Color.black;
        }

        if (gridMesh == null)
        {
            gridMesh = InitGridMesh();
            borderMesh = InitBorderMesh();
        }

        //Display the mesh
        if (showGrid)
        {
            Graphics.DrawMesh(gridMesh, Vector3.zero, Quaternion.identity, gridMaterial, 0, Camera.main, 0);
        }
        else
        {
            Graphics.DrawMesh(borderMesh, Vector3.zero, Quaternion.identity, gridMaterial, 0, Camera.main, 0);
        }
    }



    //Generate a line mesh that displays the grid
    private Mesh InitGridMesh()
    {
        //Generate the vertices
        List<Vector3> lineVertices = new();

        //Y is up
        Vector3 linePosX = Vector3.zero;
        Vector3 linePosZ = Vector3.zero;

        for (int x = 0; x <= numberOfCells; x++)
        {
            lineVertices.Add(linePosX);
            lineVertices.Add(linePosX + Vector3.right * GridWidth);

            lineVertices.Add(linePosZ);
            lineVertices.Add(linePosZ + Vector3.forward * GridWidth);

            linePosX += Vector3.forward * cellSize;
            linePosZ += Vector3.right * cellSize;
        }


        //Generate the indices
        List<int> indices = new();

        for (int i = 0; i < lineVertices.Count; i++)
        {
            indices.Add(i);
        }


        //Generate the mesh
        Mesh gridMesh = new();

        gridMesh.SetVertices(lineVertices);
        gridMesh.SetIndices(indices, MeshTopology.Lines, 0);


        return gridMesh;
    }



    //Generate a line mesh that displays the border
    private Mesh InitBorderMesh()
    {
        //Generate the vertices
        List<Vector3> lineVertices = new();

        //Y is up
        Vector3 BL = new(0f, 0f, 0f);
        Vector3 BR = new(GridWidth, 0f, 0f);
        Vector3 TR = new(GridWidth, 0f, GridWidth);
        Vector3 TL = new(0f, 0f, GridWidth);

        lineVertices.Add(BL);
        lineVertices.Add(BR);
        lineVertices.Add(TR);
        lineVertices.Add(TL);
        lineVertices.Add(BL);

        //Generate the indices
        List<int> indices = new();

        for (int i = 0; i < lineVertices.Count; i++)
        {
            indices.Add(i);
        }


        //Generate the mesh
        Mesh gridMesh = new();

        gridMesh.SetVertices(lineVertices);
        gridMesh.SetIndices(indices, MeshTopology.LineStrip, 0);


        return gridMesh;
    }



    //
    // Handle ball-environment collision
    //

    //Make balls bounce against the edge of the grid
    public bool HandleBallEnvironmentCollision(Ball ball)
    {
        bool isColliding = false;

        float halfX = GridWidth * 0.5f;
        float halfZ = GridWidth * 0.5f;

        Vector3 gridCenter = new(halfX, 0f, halfZ);

        //x
        if (ball.pos.x > gridCenter.x + halfX - ball.radius)
        {
            ball.pos.x = gridCenter.x + halfX - ball.radius;
            ball.vel.x *= -1f;

            isColliding = true;
        }
        else if (ball.pos.x < gridCenter.x - halfX + ball.radius)
        {
            ball.pos.x = gridCenter.x - halfX + ball.radius;
            ball.vel.x *= -1f;

            isColliding = true;
        }

        //z
        if (ball.pos.z > gridCenter.z + halfZ - ball.radius)
        {
            ball.pos.z = gridCenter.z + halfZ - ball.radius;
            ball.vel.z *= -1f;

            isColliding = true;
        }
        else if (ball.pos.z < gridCenter.z - halfZ + ball.radius)
        {
            ball.pos.z = gridCenter.z - halfZ + ball.radius;
            ball.vel.z *= -1f;

            isColliding = true;
        }

        return isColliding;
    }
}
