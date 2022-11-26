using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class TetrahedronData
{
    //Used to find the opposite 3 vertices of a vertex
    public static int[][] volIdOrder = new int[][] { 
        new int[] { 1, 3, 2 }, 
        new int[] { 0, 2, 3 }, 
        new int[] { 0, 3, 1 }, 
        new int[] { 0, 1, 2 } 
    };

    public static Vector3Int[] volIdOrder2 = new Vector3Int[] {
        new Vector3Int( 1, 3, 2),
        new Vector3Int( 0, 2, 3),
        new Vector3Int( 0, 3, 1),
        new Vector3Int(0, 1, 2)
    };

    //Vertex coordinates [x1, y1, z1, x2, y2, z3,...] which is easier to save than using Vector3
    public abstract float[] GetVerts
    {
        get;
    }

    //Indices of the 4 vertices in each tetra
    //To get the total amount of tetras: GetTetIds.Length / 4
    //To get all 4 vertices of a tetra where nr is less than GetTetIds.Length / 4
    //int id0 = tetIds[4 * nr + 0];
    //int id1 = tetIds[4 * nr + 1];
    //int id2 = tetIds[4 * nr + 2];
    //int id3 = tetIds[4 * nr + 3];
    //To get the actual vertex you have to multiply the id with 3 (and you get the x coordinate)
    public abstract int[] GetTetIds
    {
        get;
    }

    //Indices of the tetra edges
    public abstract int[] GetTetEdgeIds
    {
        get;
    }

    //Triangles that form the surface of the mesh
    //mesh.triangles = GetTetSurfaceTriIds because you can assign these directly it means they work as if the verts had been ordered as p1, p2, p3 and not x1, y1, z1, x2,...???
    public abstract int[] GetTetSurfaceTriIds
    {
        get;
    }



    //
    // Data we can get based on the arrays
    //

    //How many tetrahedrons are there?
    public int GetNumberOfTetrahedrons => GetTetIds.Length / 4;

    //How many vertices are there?
    public int GetNumberOfVertices => GetVerts.Length / 3;

    //How many edges are there?
    //There are 2 vertices per edge, hence we have to divide by 2 to get how many edges we have
    public int GetNumberOfEdges => GetTetEdgeIds.Length / 2;

}
