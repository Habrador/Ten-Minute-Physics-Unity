using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Find neighboring particles of same size faster with Spatial Hashing which is a Spatial Partitioning Design Pattern
//We are no longer constrained to a grid of a specific size, we can use an unbouded grid which has infinite size
//Based on https://www.youtube.com/watch?v=D2M8jTtKi44
//2d space x,y, but 3d space is also possible
public class SpatialHashing
{
    //The size of a cell in an infinite grid = spacing
    //This cell should be the same size as 2*radius of the particles
    private readonly float cellSize;
    //We can use any size of the array because we are in an unbounded grid
    //tableSize = 2 * maxParticles often works well according to the video
    private readonly int tableSize;
    //From this array we can figure out how many particles are in a cell
    //We can also find out which particles are in a cell bcause it references the sortedParticles array
    public readonly int[] table;
    //Same length as #particles in the simulation
    //Particles in the same cell are next to each other in this array
    //The items in this array are indices in the particlePositions array
    public readonly int[] sortedParticles;

    //Example
    //5 particles in a 5x4 grid -> numX = 5, numY = 4
    // ___ ___ ___ ___ ___
    //|___|___|___|___|___|
    //|___|5,1|___|_3_|___|
    //|___|___|___|___|___|
    //|___|___|4,2|___|___|
    //table: numX * numY + 1
    //[0 0 0 0 0 0 2 2 2 2 2 2 4 4 5 5 5 5 5 5 5]
    //To go from 2d index in the grid to 1d index in table, we use i_table = xi * numY + yi 
    //sortedParticles: 5 because 5 particles
    //[5 1 2 4 3]
    //
    //How do we use it?
    //
    //Particle 3 -> xi = 3 and yi = 1 -> i_table = xi * numY + yi = 3 * 4 + 1 = 13 
    //table[13] = 4 which is an index in sortedParticles[4] = 3 which is the particle we are interested in
    //How many particles in the cell? table[13 + 1] - table[13] = 5 - 4 = 1 particle 
    //
    //Particle 4 -> xi = 2, yi = 3 -> i_table = 2 * 4 + 3 = 11
    //table[11] = 2 -> sortedParticles[2] = 2 which is a particle in the same cell as particle 4
    //How many particles in the cell? table[11 + 1] - table[11] = 4 - 2 = 2 particles 

    //In an unbounded grid we dont have numX and numY, but the same principle as above is true,
    //BUT particles far away from each other may end up in the same 1d cell
    //i_table = hash(xi, yi) % tableSize
    //_|___|___|___|___|___|_
    //_|___|___|___|___|___|_
    //_|___|5,1|___|_3_|___|_
    //_|___|___|___|___|___|_
    //_|___|___|4,2|___|___|_
    // |   |   |   |   |   |
    //table:
    //[0 0 0 0 0 0 0 0 3 3 3 3 3 3 5 5 5 5 5 5] (length tableSize)
    //sortedParticles:
    //[3 4 2 1 5]
    //Because of hash collision particle 3, 4, 2 are in the same 1d cell

    //For debugging
    //private readonly int[] isParticleInCell;

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
    public SpatialHashing(float cellSize, int numberOfParticles, int tableSize)
    {
        this.cellSize = cellSize;
        this.tableSize = tableSize;

        //+1 because we need a guard
        this.table = new int[tableSize + 1];
        this.sortedParticles = new int[numberOfParticles];
        
        //Debugging
        //this.isParticleInCell = new int[tableSize + 1];
    }



    //
    // Help methods
    //

    //Convert from Vector3 world pos to cell pos
    public Vector2Int ConvertFromWorldToCell(float x, float y)
    {
        //It works like this if cell size is 2:
        //pos.x is 1.8, then cellX will be 1.8/2 = 0.9 -> 0
        //pos.x is 2.1, then cellX will be 2.1/2 = 1.05 -> 1
        int cellX = Mathf.FloorToInt(x / cellSize);
        int cellY = Mathf.FloorToInt(y / cellSize);

        Vector2Int cellPos = new(cellX, cellY);

        return cellPos;
    }



    //The grid is in 2d but the array is 1d which is more efficient, so this will convert between them
    public int Get1DArrayIndex(Vector2Int cellPos)
    {
        //Bounded grid
        //int index = cellPos.x * numberOfCells + cellPos.y;

        //Unbounded grid
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
        int arrayIndex = Mathf.Abs(hash) % this.tableSize;

        return arrayIndex;
    }
    


    //
    // Update the data structure when particles have moved
    //

    public void AddParticlesToGrid(Vector2[] particlePositions)
    {
        //Reset
        //[0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0]
        System.Array.Fill(table, 0);
        
        //Debugging
        //System.Array.Fill(isParticleInCell, 0);


        //Add particles
        //[0 0 0 0 0 2 0 0 0 0 0 2 0 1 0 0 0 0 0 0 0] <- 5 particles are in 3 cells
        foreach (Vector2 particlePos in particlePositions)
        {
            Vector2Int cellPos = ConvertFromWorldToCell(particlePos.x, particlePos.y);

            int index = Get1DArrayIndex(cellPos);

            table[index] += 1;

            //isParticleInCell[index] = 1;
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
            Vector2 thisParticlePos = particlePositions[i];
        
            Vector2Int cellPos = ConvertFromWorldToCell(thisParticlePos.x, thisParticlePos.y);

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


        //displayString = "";

        //foreach (int integer in isParticleInCell)
        //{
        //    displayString += integer + "";
        //}

        //Debug.Log(displayString);


        displayString = "";

        foreach (int integer in sortedParticles)
        {
            displayString += integer + "";
        }

        Debug.Log(displayString);
    }
}
