using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Based on https://www.youtube.com/watch?v=D2M8jTtKi44
public class SpatialHashing
{
    //Store particles in this data structure

    private readonly float cellSize;

    //We can use any size of the array (except 0) if we are using "Spatial Hashing"
    //Is called tableSize in the YT video
    //tableSize = #particles often works well according to the video
    private readonly int tableSize = 10;
    public readonly int[] particlesInCells;
    //Same length as all particles in the simulation
    public readonly int[] particles;
    //For debugging
    private readonly int[] isParticleInCell;

    //Help array to easier check surrounding cells
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



    //Important that cellSize and particle diameter is the same!
    public SpatialHashing(float cellSize, int numberOfParticles)
    {
        this.cellSize = cellSize;

        //+1 because we need a guard
        this.particlesInCells = new int[tableSize + 1];
        this.particles = new int[numberOfParticles];
        this.isParticleInCell = new int[tableSize + 1];
    }



    //
    // Help methods
    //

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



    //The grid is in 2d but the array is 1d which is more efficient, so this will convert between them
    public int Get1DArrayIndex(Vector2Int cellPos)
    {
        //If we had been using a data structure which is numberOfCellsX * numberOfCellsY which is less efficient
        //int index = cellPos.x * numberOfCells + cellPos.y;

        int index = GetSpatialHashingIndex(cellPos);

        return index;
    }



    //Get a 1D array index from any cell position if we dont want to deal with a grid with a specific size, such as 10x10. The grid can now be almost infinite large
    //The drawback is that some particles far away may be located in the same position in the array, but the overall performance should be faster
    public int GetSpatialHashingIndex(Vector2Int cellPos)
    {
        //^ is Bitwise XOR 
        
        //2d
        int h = (cellPos.x * 92837111) ^ (cellPos.y * 689287499);

        //3d
        //int h = (cellPos.x * 92837111) ^ (cellPos.y * 689287499) ^ (cellPos.z * 283923481);

        //h can be negative if a cellPos is negative, which is why we need the Abs
        int hashPos = System.Math.Abs(h) % this.tableSize;

        return hashPos;
    }
    


    //
    // Update the data structure when particles have moved
    //

    public void AddParticlesToGrid(List<Vector3> particlePositions)
    {
        //Reset
        for (int i = 0; i < particlesInCells.Length; i++)
        {
            particlesInCells[i] = 0;

            isParticleInCell[i] = 0; 
        }


        //Add particles
        foreach (Vector3 particlePos in particlePositions)
        {
            Vector2Int cellPos = ConvertFromWorldToCell(particlePos);

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

        for (int i = 0; i < particlePositions.Count; i++)
        {
            Vector3 thisParticlePos = particlePositions[i];
        
            Vector2Int cellPos = ConvertFromWorldToCell(thisParticlePos);

            int index = Get1DArrayIndex(cellPos);

            particlesInCells[index] -= 1;

            particles[particlesInCells[index]] = i;
        }
    }



    //
    // Debug
    //

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
}
