using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Find neighboring particles faster with Spatial Hashing which is a Spatial Partitioning Design Pattern
//We are no longer constrained to a grid of a specific size, we can use an unbouded grid which has infinite size
//Based on https://www.youtube.com/watch?v=D2M8jTtKi44
public class SpatialHashing
{
    //The size of a cell in an infinite grid
    private readonly float cellSize;

    //We can use any size of the array, but tableSize = #particles often works well according to the video
    private readonly int tableSize = 10;
    //From this array we can figure out how many particles are in a cell and it references the sortedParticles array
    public readonly int[] table;
    //Same length as #particles in the simulation
    //Particles in the same cell are next to each other in this array
    //The items in this array are indices in the particlePositions array
    //To figure out which particles are in a cell:
    //Step 1. Use the hash to find the index in the table array and figure out which index in the sortedParticles array the first particle in this cell has and how many particles are in this cell
    //Step 2. If we know the first index of a particle in the sortedParticles array and how many particles we have, we can figure out which positions these particles have by iterating over the particles in this cell 
    public readonly int[] sortedParticles;
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
        this.table = new int[tableSize + 1];
        this.sortedParticles = new int[numberOfParticles];
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
        int hash = (cellPos.x * 92837111) ^ (cellPos.y * 689287499);

        //3d
        //int h = (cellPos.x * 92837111) ^ (cellPos.y * 689287499) ^ (cellPos.z * 283923481);

        //h can be negative if a cellPos is negative, which is why we need the Abs
        int arrayIndex = System.Math.Abs(hash) % this.tableSize;

        return arrayIndex;
    }
    


    //
    // Update the data structure when particles have moved
    //

    public void AddParticlesToGrid(Vector3[] particlePositions)
    {
        //Reset
        //[0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0]
        System.Array.Fill(table, 0);
        System.Array.Fill(isParticleInCell, 0);


        //Add particles
        //[0 0 0 0 0 2 0 0 0 0 0 2 0 1 0 0 0 0 0 0 0] <- 5 particles are in 3 cells
        foreach (Vector3 particlePos in particlePositions)
        {
            Vector2Int cellPos = ConvertFromWorldToCell(particlePos);

            int index = Get1DArrayIndex(cellPos);

            table[index] += 1;

            isParticleInCell[index] = 1;
        }


        //Fix data structure

        //Partial sums
        //[0 0 0 0 0 2 2 2 2 2 2 4 4 5 5 5 5 5 5 5 5]
        for (int i = 1; i < table.Length; i++)
        {
            table[i] += table[i - 1];
        }

        //Fix the table and sortedParticles arrays
        //[0 0 0 0 0 0 2 2 2 2 2 2 4 4 5 5 5 5 5 5 5]
        //[5 1 2 4 3] <- particle numbers and NOT positions in the array
        //[4 0 1 3 2] <- sortedParticles array which references indices in the particlePositions array
        //Particle 5,1 are in cell with 1d-index 5 in the tableArray 
        //Particle 2,4 are in cell with 1d-index 11
        //Particle 3 is in cell with 1d-index 13
        //When the hashing function returns index 5 we look in the tableArray and see that the first particle in this cell has index 0 in the particle array
        //How many particles are in the cell? First look at the index after: 2, then subtract the 0, so 2 - 0 = 2 particles
        //What's the positions of these particles? They have indices 4 and 0 in the particlePositions array
        //How many in the cell with index 11? 4 - 2 = 2
        //index 13? 5 - 4 = 1
        for (int i = 0; i < particlePositions.Length; i++)
        {
            Vector3 thisParticlePos = particlePositions[i];
        
            Vector2Int cellPos = ConvertFromWorldToCell(thisParticlePos);

            int index = Get1DArrayIndex(cellPos);

            //Decrease
            table[index] -= 1;

            //Place the particle at this index
            //After adding particle 1:
            //[0 0 0 0 0 1 2 2 2 2 2 4 4 5 5 5 5 5 5 5 5] //The 1 is the index in allParticles where this particle should be added
            //[- 0 - - -] Particle #1 has been added to index 1 in the allParticles array. It has index 0 in the particlePostions array
            //After adding particle 2:
            //[0 0 0 0 0 0 2 2 2 2 2 3 4 5 5 5 5 5 5 5 5] //The 3 is the index in allParticles where this particle should be added
            //[- 0 - 1 -] Particle #2 has been added to index 3 in the allParticles array. It has index 1 in the particlePositions array
            sortedParticles[table[index]] = i;
        }
    }



    //
    // Debug
    //

    public void DisplayDataStructures()
    {
        string displayString = "";

        foreach (int integer in table)
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

        foreach (int integer in sortedParticles)
        {
            displayString += integer + "";
        }

        Debug.Log(displayString);
    }    
}
