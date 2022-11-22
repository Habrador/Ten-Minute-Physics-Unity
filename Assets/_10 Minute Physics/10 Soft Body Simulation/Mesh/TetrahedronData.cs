using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class TetrahedronData
{
    //Vertices coordinates [x1, y1, z1, x2, y2, z3,...] after each other
    public abstract float[] GetVerts
    {
        get;
    }

    public abstract int[] GetTetIds
    {
        get;
    }

    public abstract int[] GetTetEdgeIds
    {
        get;
    }

    //Triangles that form the surface of the mesh
    //mesh.triangles = GetTetSurfaceTriIds
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
