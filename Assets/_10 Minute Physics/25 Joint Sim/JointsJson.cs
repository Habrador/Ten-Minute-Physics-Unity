using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//The easiest way to load a json file seems to be to try to recreate a class that corresponds to the json data
//Unless you use an external library...

//Example: basicJoints
//meshes is an array of length 40:
//We simulate 8 joints
//each joint has 2 basic meshes (simType = RigidBox) which we use in the physics simulation because we can only simulate boxes or spheres
//each joint has 2 visual pleasing meshes (simType = Visual)
//each joint has 1 mesh with data where it's located, type of joint, etc (simType = PrismaticJoint, CylinderJoint, etc)
// -> 8 * 2 + 8 * 2 + 8 * 1 = 40 meshes

[System.Serializable]
public class JointsJson
{
    public JointMesh[] meshes;
    public JointExportInfo exportInfo;
}


[System.Serializable]
public class JointMesh
{
    public string name;
    public float[] vertices;
    public float[] normals;
    public int[] triangles;
    public JointTransform transform;
    public JointProperties properties;
}


[System.Serializable]
public class JointTransform
{
    public float[] position;
    public float[] rotation;
}


[System.Serializable]
public class JointProperties
{
    public string simType; //RigidBox, Visual, or the name of the joint type
    //These parameters depent on which joint we have
    //Get default values if doesnt exist in json file
    public float density;
    public string parent1;
    public string parent2;
    public float swingMax;
    public float swingMin;
    public float twistMax;
    public float twistMin;
    public float damping;
    public float compliance;
    public float distanceMax;
    public float distanceMin;
    public float distanceTarget;
    public float posTarget;
    public float velocityMax;
    public float velocityMin;
    public float targetAngle;
    public float targetAngleCompliance;
    public float[] color;
    public string parent;
    public float restDistance;
}


[System.Serializable]
public class JointExportInfo
{
    public string blenderVersion;
    public int exportTime;
    public int totalMeshes;
}
