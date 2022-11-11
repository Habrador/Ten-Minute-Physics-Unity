using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Based on https://www.youtube.com/watch?v=D2M8jTtKi44
public class GridMap
{
    //Grid settings
    private readonly int numberOfCells;
    private readonly float cellSize;

    //Display the grid with line mesh
    private Material gridMaterial;
    private Mesh gridMesh;

    //Store balls in this data structure
    //public readonly List<Ball>[] ballsInCellsSlow;
    //A more optimized way to store the balls
    public readonly int[] particlesInCells;
    public readonly int[] particles;
    public readonly int[] isParticleInCell;
    //We can use any size of the array (except 0) if we are using "Spatial Hashing"
    //Is called tableSize in the YT video
    //tableSize = numberOfBalls often works well according to the video
    private readonly int tableSize = 10;

    //Getters
    public float GridWidth => numberOfCells * cellSize;

    //Help array to check surrounding cells
    public static Vector2Int[] cellCoordinates = {
        new Vector2Int(-1,  1),
        new Vector2Int( 0,  1),
        new Vector2Int( 1,  1),
        new Vector2Int(-1,  0),
        new Vector2Int( 0,  0),
        new Vector2Int( 1,  0),
        new Vector2Int(-1, -1),
        new Vector2Int( 0, -1),
        new Vector2Int( 1, -1)
    };


    public GridMap(int numberOfCells, float cellSize, int numberOfParticles)
    {
        this.numberOfCells = numberOfCells;
        this.cellSize = cellSize;

        //this.ballsInCellsSlow = new List<Ball>[tableSize];

        //for (int i = 0; i < this.ballsInCellsSlow.Length; i++)
        //{
        //    this.ballsInCellsSlow[i] = new List<Ball>();
        //}

        //+1 because we need a guard
        this.particlesInCells = new int[tableSize + 1];
        this.particles = new int[numberOfParticles];
        this.isParticleInCell = new int[tableSize + 1];
    }



    public void DisplayGrid()
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
        }

        //Display the mesh
        Graphics.DrawMesh(gridMesh, Vector3.zero, Quaternion.identity, gridMaterial, 0, Camera.main, 0);
    }



    //Generate a line mesh
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



    //Convert from Vector3 world pos to cell pos
    public Vector2Int ConvertFromWorldToCell(Vector3 pos)
    {
        //It works like this if cell size is 2:
        //pos.x is 1.8, then cellX will be 1.8/2 = 0.9 -> 0
        //pos.x is 2.1, then cellX will be 2.1/2 = 1.05 -> 1
        int cellX = Mathf.FloorToInt(pos.x / cellSize);
        int cellZ = Mathf.FloorToInt(pos.z / cellSize); //z instead of y because y is up in Unity's coordinate system

        Vector2Int cellPos = new(cellX, cellZ);

        return cellPos;
    }



    //The center of the entire grid
    public Vector3 GetGridCenter()
    {
        float center = GridWidth / 2f;

        Vector3 centerCoordinate = new (center, 0f, center);

        return centerCoordinate;
    }



    //The grid is in 2d but the array is 1d which is more efficient, so this will convert between them
    public int Get1DArrayIndex(Vector2Int cellPos)
    {
        //If we had been using a data structure which is numberOfCellsX * numberOfCellsY which is less efficient
        //int index = cellPos.x * numberOfCells + cellPos.y;

        int index = SpatialHashing(cellPos);

        return index;
    }



    public int SpatialHashing(Vector2Int cellPos)
    {
        //^ is Bitwise XOR 
        //From the YT video:
        //var h = (xi * 92837111) ^ (yi * 689287499) ^ (zi * 283923481);
        //h can be negative of xi is negative, which is why we need the Abs
        //return Math.abs(h) % this.tableSize;

        int h = (cellPos.x * 92837111) ^ (cellPos.y * 689287499);

        int hashPos = System.Math.Abs(h) % this.tableSize;

        return hashPos;
    }



    //public void Reset()
    //{
    //    //for (int i = 0; i < this.ballsInCellsSlow.Length; i++)
    //    //{
    //    //    this.ballsInCellsSlow[i].Clear();
    //    //}
    //}



    public void AddParticlesToGrid(List<Billiard.BilliardBall> allBalls)
    {
        //Reset
        for (int i = 0; i < particlesInCells.Length; i++)
        {
            particlesInCells[i] = 0;

            isParticleInCell[i] = 0; 
        }


        //Add balls
        foreach (Ball thisBall in allBalls)
        {
            Vector2Int cellPos = ConvertFromWorldToCell(thisBall.pos);

            int index = Get1DArrayIndex(cellPos);

            particlesInCells[index] += 1;

            isParticleInCell[index] = 1;
        }


        //Fix data structure

        //Partial sums
        for (int i = 1; i < particlesInCells.Length; i++)
        {
            particlesInCells[i] += particlesInCells[i - 1];
        }

        for (int i = 0; i < allBalls.Count; i++)
        {
            Ball thisBall = allBalls[i];
        
            Vector2Int cellPos = ConvertFromWorldToCell(thisBall.pos);

            int index = Get1DArrayIndex(cellPos);

            particlesInCells[index] -= 1;

            particles[particlesInCells[index]] = i;
        }
    }


    public void DisplayDataStructures()
    {
        string displayString = "";

        foreach (int integer in particlesInCells)
        {
            displayString += integer + "";
        }

        Debug.Log(displayString);


        displayString = "";

        foreach (int integer in isParticleInCell)
        {
            displayString += integer + "";
        }

        Debug.Log(displayString);


        displayString = "";

        foreach (int integer in particles)
        {
            displayString += integer + "";
        }

        Debug.Log(displayString);
    }


    //public void AddBallToGrid(Ball ball)
    //{
    //    Vector2Int cellPos = ConvertFromWorldToCell(ball.pos);

    //    int index = Get1DArrayIndex(cellPos);

    //    //ballsInCellsSlow[index].Add(ball);

    //    ballsInCells[index] += 1;
    //}



    //Make balls bounce against the edge of the grid
    public bool HandleBallEnvironmentCollision(Ball ball)
    {
        bool isColliding = false;

        float halfX = GridWidth * 0.5f;
        float halfZ = GridWidth * 0.5f;

        Vector3 gridCenter = new (halfX, 0f, halfZ);

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
