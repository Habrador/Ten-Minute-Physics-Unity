using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;



public class SceneImporter
{
    private RigidBodySimulator simulator;

    //name -> RigidBody lookup
    private Dictionary<string, MyRigidBody> rigidBodies;



    //public SceneImporter(simulator, scene)
    public SceneImporter(RigidBodySimulator simulator)
    {
        this.simulator = simulator;
        //this.scene = scene;
        this.rigidBodies = new();
    }



    public void LoadScene(string fileName)
    {
        //string fileName = "basicJoints.json";

        string filePathEditor = "Assets/_10 Minute Physics/25 Joint Sim/";

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
        //Debug.Log("Number of meshes: " + data.meshes.Length);

        //JointMesh mesh = data.meshes[0];

        //DisplayMeshData(mesh);

        //Clear existing simulation (we do this outside this method in the Controller)
        //this.simulator.Clear();
        this.rigidBodies.Clear();

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

        Debug.Log($"Found {rigidCount} RigidBodies, {jointCount} Joints, and {visualCount} Visual meshes = {rigidCount + jointCount + visualCount} meshes. Total should be {data.exportInfo.totalMeshes}");

        //this.simulator.simulationView = false;
        //this.simulator.toggleView();

        //console.log(`Loaded scene with ${ this.rigidBodies.size}
        //rigid bodies`);
    }



    //Identify mesh type based on simType string
    private static bool IsRigidBody(JointMesh mesh)
    {
        string simType = mesh.properties.simType;

        //We currently only have RigidBox, but in the future we might add other types
        //See if simtype starts with Rigid
        //Assuming the string has at least 5 characters which it always should... 
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

    private void CreateRigidBody(JointMesh mesh)
    {
        //const props = mesh.properties;
        //const simType = props.simType;
        //const density = props.density ?? 0.0;

        //// Extract position and rotation from transform
        //const pos = new THREE.Vector3(
        //    mesh.transform.position[0],
        //    mesh.transform.position[1],
        //    mesh.transform.position[2]
        //);

        //const quat = new THREE.Quaternion(
        //    mesh.transform.rotation[0],
        //    mesh.transform.rotation[1],
        //    mesh.transform.rotation[2],
        //    mesh.transform.rotation[3]
        //);
        //const euler = new THREE.Euler().setFromQuaternion(quat);
        //const angles = new THREE.Vector3(euler.x, euler.y, euler.z);

        //let rigidBody;

        //if (simType === 'RigidBox')
        //{
        //    // Calculate bounding box from vertices
        //    const size = this.calculateBoundingBox(mesh.vertices);
        //    rigidBody = new RigidBody(this.scene, "box", size, density, pos, angles);
        //}
        //else if (simType === 'RigidSphere')
        //{
        //    // Calculate bounding sphere radius
        //    const radius = this.calculateBoundingSphere(mesh.vertices);
        //    const size = new THREE.Vector3(radius, radius, radius);
        //    rigidBody = new RigidBody(this.scene, "sphere", size, density, pos, angles);
        //}
        //else
        //{
        //    console.warn(`Unknown rigid body type: ${ simType}`);
        //    return;
        //}

        //// Store in lookup table
        //this.rigidBodies.set(mesh.name, rigidBody);
        //this.simulator.addRigidBody(rigidBody);

        //console.log(`Created ${ simType}: ${ mesh.name}`);
    }



    private Vector3 CalculateBoundingBox(float[] vertices)
    {
        float minX = float.MaxValue, minY = float.MaxValue, minZ = float.MaxValue;
        float maxX = -float.MaxValue, maxY = -float.MaxValue, maxZ = -float.MaxValue;

        //Vertices are stored as [x, y, z, x, y, z, ...]
        for (int i = 0; i < vertices.Length; i += 3)
        {
            float x = vertices[i];
            float y = vertices[i + 1];
            float z = vertices[i + 2];

            minX = Mathf.Min(minX, x);
            maxX = Mathf.Max(maxX, x);
            minY = Mathf.Min(minY, y);
            maxY = Mathf.Max(maxY, y);
            minZ = Mathf.Min(minZ, z);
            maxZ = Mathf.Max(maxZ, z);
        }

        return new Vector3(
            (maxX - minX),
            (maxY - minY),
            (maxZ - minZ)
        );
    }



    private float CalculateBoundingSphere(float[] vertices)
    {
        //Find center which is the average?
        float centerX = 0f, centerY = 0f, centerZ = 0f;
        int numVerts = vertices.Length / 3;

        for (int i = 0; i < vertices.Length; i += 3)
        {
            centerX += vertices[i];
            centerY += vertices[i + 1];
            centerZ += vertices[i + 2];
        }

        centerX /= numVerts;
        centerY /= numVerts;
        centerZ /= numVerts;

        //Find maximum distance from center
        float maxRadius = 0;
        
        for (int i = 0; i < vertices.Length; i += 3)
        {
            float dx = vertices[i] - centerX;
            float dy = vertices[i + 1] - centerY;
            float dz = vertices[i + 2] - centerZ;
            
            float radius = Mathf.Sqrt(dx * dx + dy * dy + dz * dz);
            
            maxRadius = Mathf.Max(maxRadius, radius);
        }

        return maxRadius;
    }



    private void CreateJoint(JointMesh mesh)
    {
        //const props = mesh.properties;
        //const simType = props.simType;

        //// Look up parent bodies
        //const body0 = this.rigidBodies.get(props.parent1);
        //const body1 = this.rigidBodies.get(props.parent2);

        //if (!body0)
        //{
        //    console.error(`Parent body not found: ${ props.parent1}`);
        //    return;
        //}
        //if (!body1)
        //{
        //    console.error(`Parent body not found: ${ props.parent2}`);
        //    return;
        //}

        //// Joint position (use mesh position as anchor point)
        //const jointPos = new THREE.Vector3(
        //    mesh.transform.position[0],
        //    mesh.transform.position[1],
        //    mesh.transform.position[2]
        //);

        //// Joint rotation
        //const jointRot = new THREE.Quaternion(
        //    mesh.transform.rotation[0],
        //    mesh.transform.rotation[1],
        //    mesh.transform.rotation[2],
        //    mesh.transform.rotation[3]
        //);

        //// Create joint
        //const joint = new Joint(body0, body1, jointPos, jointRot);

        //// Configure joint based on type
        //if (simType === 'BallJoint')
        //{
        //    let swingMax = props.swingMax ?? Number.MAX_VALUE
        //                let twistMin = props.twistMin ?? -Number.MAX_VALUE
        //                let twistMax = props.twistMax ?? Number.MAX_VALUE
        //                let damping = props.damping ?? 0.0;

        //    joint.initBallJoint(swingMax, twistMin, twistMax, damping);
        //}
        //else if (simType === 'HingeJoint')
        //{
        //    let swingMin = props.swingMin ?? -Number.MAX_VALUE
        //                let swingMax = props.swingMax ?? Number.MAX_VALUE;
        //    let hasTargetAngle = props.targetAngle !== undefined;
        //    let targetAngle = props.targetAngle ?? 0.0;
        //    let compliance = props.targetAngleCompliance ?? 0.0;
        //    let damping = props.damping ?? 0.0;

        //    joint.initHingeJoint(swingMin, swingMax, hasTargetAngle, targetAngle, compliance, damping);
        //}
        //else if (simType === 'ServoJoint')
        //{
        //    let swingMin = props.swingMin ?? -Number.MAX_VALUE
        //                let swingMax = props.swingMax ?? Number.MAX_VALUE;

        //    joint.initServo(swingMin, swingMax);
        //}
        //else if (simType === 'MotorJoint')
        //{
        //    let velocity = props.velocity ?? 0.0;
        //    velocity = 3.0;
        //    joint.initMotor(velocity);
        //}
        //else if (simType === 'DistanceJoint')
        //{
        //    let restDistance = props.restDistance ?? 0.0;
        //    let compliance = props.compliance ?? 0.0;
        //    let damping = props.damping ?? 0.0;

        //    joint.initDistanceJoint(restDistance, compliance, damping);
        //}
        //else if (simType === 'PrismaticJoint')
        //{
        //    let distanceMin = props.distanceMin ?? -Number.MAX_VALUE
        //                let distanceMax = props.distanceMax ?? Number.MAX_VALUE;
        //    let compliance = props.compliance ?? 0.0;
        //    let damping = props.damping ?? 0.0;
        //    let hasTarget = props.distanceTarget !== undefined;
        //    let targetDistance = props.posTarget ?? 0.0;
        //    let twistMin = props.twistMin ?? -Number.MAX_VALUE;
        //    let twistMax = props.twistMax ?? Number.MAX_VALUE;
        //    joint.initPrismaticJoint(distanceMin, distanceMax, twistMin, twistMax, hasTarget, targetDistance, compliance, damping);
        //}
        //else if (simType === 'CylinderJoint')
        //{
        //    let hasDistanceLimits = props.distanceMin !== undefined && props.distanceMax !== undefined;
        //    let distanceMin = props.distanceMin ?? -Number.MAX_VALUE
        //                let distanceMax = props.distanceMax ?? Number.MAX_VALUE;
        //    let twistMin = props.twistMin ?? -Number.MAX_VALUE;
        //    let twistMax = props.twistMax ?? Number.MAX_VALUE;
        //    joint.initCylinderJoint(distanceMin, distanceMax, twistMin, twistMax);
        //}
        //else
        //{
        //    console.warn(`Unknown joint type: ${ simType}`);
        //    return;
        //}

        //// Add visuals and register with simulator
        //joint.addVisuals(this.scene);
        //this.simulator.addJoint(joint);

        //console.log(`Created ${ simType}: ${ mesh.name}
        //connecting ${ props.parent1}
        //to ${ props.parent2}`);
    }

    private void CreateVisualMesh(JointMesh mesh)
    {
    //    const props = mesh.properties;
    //    const parentName = props.parent;

    //    // Look up parent body
    //    const parentBody = this.rigidBodies.get(parentName);
    //    if (!parentBody)
    //    {
    //        console.error(`Parent body not found for visual mesh: ${ parentName}`);
    //        return;
    //    }

    //    // Get visual mesh transform
    //    const visualPos = new THREE.Vector3(
    //        mesh.transform.position[0],
    //        mesh.transform.position[1],
    //        mesh.transform.position[2]
    //    );

    //    const visualRot = new THREE.Quaternion(
    //        mesh.transform.rotation[0],
    //        mesh.transform.rotation[1],
    //        mesh.transform.rotation[2],
    //        mesh.transform.rotation[3]
    //    );

    //    // Transform visual mesh to parent body local space

    //    const q_rel = parentBody.rot.clone().conjugate().multiply(visualRot);
    //    const p_rel = visualPos.clone().sub(parentBody.pos).applyQuaternion(parentBody.rot.clone().conjugate());

    //    const transformedVertices = [];
    //    for (let i = 0; i < mesh.vertices.length; i += 3)
    //    {
    //        const vertex = new THREE.Vector3(mesh.vertices[i], mesh.vertices[i + 1], mesh.vertices[i + 2]);
    //        vertex.applyQuaternion(q_rel);
    //        vertex.add(p_rel);

    //        transformedVertices.push(vertex.x, vertex.y, vertex.z);
    //    }

    //    const transformedNormals = [];
    //    if (mesh.normals)
    //    {
    //        for (let i = 0; i < mesh.normals.length; i += 3)
    //        {
    //            const normal = new THREE.Vector3(mesh.normals[i], mesh.normals[i + 1], mesh.normals[i + 2]);
    //            normal.applyQuaternion(q_rel);
    //            transformedNormals.push(normal.x, normal.y, normal.z);
    //        }
    //    }

    //    // Create Three.js geometry from transformed data
    //    const geometry = new THREE.BufferGeometry();
    //    geometry.setAttribute('position', new THREE.Float32BufferAttribute(transformedVertices, 3));

    //    if (transformedNormals.length > 0)
    //    {
    //        geometry.setAttribute('normal', new THREE.Float32BufferAttribute(transformedNormals, 3));
    //    }

    //    if (mesh.triangles)
    //    {
    //        geometry.setIndex(mesh.triangles);
    //    }

    //    const color = props.color ? new THREE.Color(props.color[0], props.color[1], props.color[2]) : new THREE.Color(1, 1, 1);
    //    const material = new THREE.MeshPhongMaterial({ color: color });

    //                // Create mesh
    //                const visualMesh = new THREE.Mesh(geometry, material);
    //visualMesh.castShadow = true;
    //                visualMesh.receiveShadow = true;
    //                visualMesh.body = parentBody; // For raycasting

    //                parentBody.meshes.push(visualMesh);
    //                parentBody.updateMeshes();
                    
    //                this.scene.add(visualMesh);
                    
    //                console.log(`Created visual mesh: ${mesh.name attached to ${parentName} (transformed to body space)`);
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
