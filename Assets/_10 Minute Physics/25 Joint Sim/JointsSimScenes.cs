using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;


public static class JointsSimScenes
{
    private static readonly string filePathEditor = "Assets/_10 Minute Physics/25 Joint Sim/";

    public static void InitJointsScene(string fileName, RigidBodySimulator rbSimulator)
    {
        //string fileName = "basicJoints.json";

        //filePathEditor includes a last /
        string JSONAsString = File.ReadAllText(filePathEditor + fileName);

        JointsJson data = JsonUtility.FromJson<JointsJson>(JSONAsString);

        //Check if scene data is empty
        if (data == null || data.meshes == null || data.meshes.Length == 0)
        {
            Debug.Log("Empty scene data provided. No objects to load.");
            return;
        }

        //40 which is what we want!
        Debug.Log("Number of meshes: " + data.meshes.Length);

        //JointMesh mesh = data.meshes[0];

        //DisplayMeshData(mesh);

        //Clear existing simulation (we do this outside this method in the Controller)
        //this.simulator.clear();
        //this.rigidBodies.clear();

        //We need to separate the meshes which are RigidBox, joint or Visual. The RigidBox are the cubic meshes and visual the more detailed meshes overlaying the simple ones
        //We need to show both and change between them by pressing "Toggle View" button

        int rigidCount = 0;
        int visualCount = 0;
        int jointCount = 0;

        JointMesh[] meshes = data.meshes;

        //Pass 1: Create all rigid bodies
        foreach (JointMesh mesh in meshes) 
        {
            if (IsRigidBody(mesh))
            {
                //this.createRigidBody(mesh);
                rigidCount += 1;
            }
        }

        //Pass 2: Create joints and visual meshes
        foreach (JointMesh mesh in meshes)
        {
            if (IsJoint(mesh))
            {
                //this.createJoint(mesh);
                jointCount += 1;
            }
            else if (IsVisual(mesh))
            {
                //this.createVisualMesh(mesh);
                visualCount += 1;
            }
        }

        Debug.Log($"Found {rigidCount} RigidBodies, {jointCount} Joints, and {visualCount} Visual");

        //this.simulator.simulationView = false;
        //this.simulator.toggleView();

        //console.log(`Loaded scene with ${ this.rigidBodies.size}
        //rigid bodies`);
    }

    private static bool IsRigidBody(JointMesh mesh)
    {
        string simType = mesh.properties.simType;

        //We currently only have RigidBox, but in the future we might add other types
        //See if simtype starts with Rigid
        //Assuming the string has at least 5 characters which it should... 
        return simType[..5] == "Rigid";
    }

    private static bool IsJoint(JointMesh mesh)
    {
        string simType = mesh.properties.simType;

        //Joint is the last word, MotorJoint, HingeJoint, etc
        return simType[^5..] == "Joint";
    }

    private static bool IsVisual(JointMesh mesh)
    {
        string simType = mesh.properties.simType;

        //Visual is the entrie string, we never append stuff to it
        return simType == "Visual";
    }



    private static void DisplayMeshData(JointMesh mesh)
    {
        Debug.Log("Name: " + mesh.name);

        Debug.Log("Vertices: " + mesh.vertices.Length);
        Debug.Log($"[{string.Join(", ", mesh.vertices)}]");
        
        Debug.Log("Normals: " + mesh.normals.Length);
        Debug.Log($"[{string.Join(", ", mesh.normals)}]");
        
        Debug.Log("Triangles: " + mesh.triangles.Length);
        Debug.Log($"[{string.Join(", ", mesh.triangles)}]");
        
        Debug.Log($"Transform pos: [{string.Join(", ", mesh.transform.position)}]");
        Debug.Log($"Transform rot: [{string.Join(", ", mesh.transform.rotation)}]");
        
        Debug.Log($"Properties");

        JointProperties properties = mesh.properties;

        Debug.Log("simType: " + properties.simType);
        Debug.Log("density: " + properties.density);
        Debug.Log("parent1: " + properties.parent1);
        Debug.Log("parent2: " + properties.parent2);
        Debug.Log("swingMax: " + properties.swingMax);
    }

}
