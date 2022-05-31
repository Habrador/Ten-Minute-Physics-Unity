using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CustomMesh
{
    public Vector3[] verts;
    public int[] tris;

    public CustomMesh(Mesh mesh)
    {
        this.verts = mesh.vertices;
        this.tris = mesh.triangles;
    }
}
