using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class SoftBodyMesh
{
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

    public abstract int[] GetTetSurfaceTriIds
    {
        get;
    }
}
