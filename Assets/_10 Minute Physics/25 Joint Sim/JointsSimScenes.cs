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

        //DisplayMesh(mesh);

        //Clear existing simulation
        //this.simulator.clear();
        //this.rigidBodies.clear();

        //we need to separate the mehes which are RigidBox, joint or visual. The RigidBox are the cubic meshes and visual the more detailed meshes overlaying the simple ones
        //We need to show both and change between them by pressing "Toggle View" button

        //Pass 1: Create all rigid bodies
        //for (const mesh of data.meshes) {
        //    if (this.isRigidBody(mesh))
        //    {
        //        this.createRigidBody(mesh);
        //    }
        //}

        //Pass 2: Create joints and visual meshes
        //for (const mesh of data.meshes) {
        //    if (this.isJoint(mesh))
        //    {
        //        this.createJoint(mesh);
        //    }
        //    else if (this.isVisual(mesh))
        //    {
        //        this.createVisualMesh(mesh);
        //    }
        //}

        //this.simulator.simulationView = false;
        //this.simulator.toggleView();

        //console.log(`Loaded scene with ${ this.rigidBodies.size}
        //rigid bodies`);
    }



    private static void DisplayMesh(JointMesh mesh)
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
