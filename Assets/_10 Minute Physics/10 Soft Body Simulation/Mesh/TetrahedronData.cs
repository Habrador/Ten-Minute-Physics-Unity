using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class TetrahedronData
{
    //Vertices coordinates (x,y,z) after each other
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
}
