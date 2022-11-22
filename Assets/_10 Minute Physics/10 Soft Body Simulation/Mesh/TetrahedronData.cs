using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class TetrahedronData
{
    public static int[][] volIdOrder = new int[][] { 
        new int[] { 1, 3, 2 }, 
        new int[] { 0, 2, 3 }, 
        new int[] { 0, 3, 1 }, 
        new int[] { 0, 1, 2 } 
    };

    //Vertices coordinates [x1, y1, z1, x2, y2, z3,...] after each other
    public abstract float[] GetVerts
    {
        get;
    }

    //Indices of the 4 coordinates in each tetra. So to get the total amount of tetras, you GetTetIds.Length / 4
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

}
