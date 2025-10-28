using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;


public static class JointsSimScenes
{
    private static readonly string filePathEditor = "Assets/_10 Minute Physics/25 Joint Sim/";

    public static void InitBasicJointsScene(RigidBodySimulator rbSimulator, float density)
    {
        //meshes which is an array where each item has: 
        // - name
        // - vertices
        // - normals
        // - triangles
        // - transform
        //      - position
        //      - rotation
        // - properties, which has variable length and can be simType (RigidBox, joint or visual), density, parent, damping, which depends on joint type, etc...
        //"exportInfo":{"blenderVersion":"4.5.2 LTS","exportTime":133,"totalMeshes":30}}

        //we need to separate the mehes which are RigidBox, joint or visual. The RigidBox are the cubic meshes and visual the more detailed meshes overlaying the simple ones
        //We need to show both and change between them by pressing "Toggle View" button

        string fileName = "basicJoints";

        //filePathEditor includes a last /
        string filePath = filePathEditor + fileName + ".json";

        string JSONAsString = File.ReadAllText(filePathEditor + fileName + ".json");

        //Debug.Log(JSONAsString);

        JointsJson data = JsonUtility.FromJson<JointsJson>(JSONAsString);

        Debug.Log(data.meshes.Length);

        //// Scene definitions
        //const jointScenes = [
        //        { path: './basicJoints.json', name: 'Basic Joints' },
        //        { path: './steering.json', name: 'Steering' },
        //        { path: './pendulum.json', name: 'Pendulum' }
        //    ];


        //// Parse JSON if it's a string
        //const data = typeof jsonData === 'string' ? JSON.parse(jsonData) : jsonData;

        //// Check if scene data is empty
        //if (!data || Object.keys(data).length === 0 || !data.meshes || data.meshes.length === 0)
        //{
        //    console.warn('Empty scene data provided. No objects to load.');
        //    return;
        //}

        //// Clear existing simulation
        //this.simulator.clear();
        //this.rigidBodies.clear();

        //// Pass 1: Create all rigid bodies
        //for (const mesh of data.meshes) {
        //    if (this.isRigidBody(mesh))
        //    {
        //        this.createRigidBody(mesh);
        //    }
        //}

        //// Pass 2: Create joints and visual meshes
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

    public static void InitSteeringScene(RigidBodySimulator rbSimulator, float density)
    {

    }

    public static void InitPendulumsScene(RigidBodySimulator rbSimulator, float density)
    {

    }

}
