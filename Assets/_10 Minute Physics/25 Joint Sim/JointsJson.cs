using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//The easiest way to load a json file seems to be to try to recreate a class that corresponds to the json data
//Unless you want an external library...

[System.Serializable]
public class JointsJson
{
    public JointMesh[] meshes;
    public string exportInfo;
}


[System.Serializable]
public class JointMesh
{
    public string name;
    public float[] vertices;
    public float[] normals;
    public int[] triangles;
    public JointTransform transform;
    public string properties;
}


[System.Serializable]
public class JointTransform
{
    public float[] position;
    public float[] rotation;
}
