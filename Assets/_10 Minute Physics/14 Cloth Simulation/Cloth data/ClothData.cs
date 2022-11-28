using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class ClothData
{
    //Vertex coordinates [x1, y1, z1, x2, y2, z3,...] which is easier to save than using Vector3
    public abstract float[] GetVerts
    {
        get;
    }


    //Triangles that form the surface of the mesh
    //mesh.triangles = GetTetSurfaceTriIds because you can assign these directly it means they work as if the verts had been ordered as p1, p2, p3 and not x1, y1, z1, x2,...???
    public abstract int[] FaceTriIds
    {
        get;
    }
}
