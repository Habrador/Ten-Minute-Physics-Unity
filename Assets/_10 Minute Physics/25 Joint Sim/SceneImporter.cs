using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using XPBD;
using static UnityEngine.Rendering.DebugUI;



public class SceneImporter
{
    private XPBDPhysicsSimulator simulator;

    //name -> RigidBody lookup
    //Used to connect joints to rbs
    private Dictionary<string, MyRigidBody> rigidBodies;



    //public SceneImporter(simulator, scene)
    public SceneImporter()
    {
        this.rigidBodies = new();
    }



    public void LoadScene(string fileName, XPBDPhysicsSimulator simulator)
    {
        this.simulator = simulator;
    
        //string fileName = "basicJoints.json";
        
        string filePathEditor = "Assets/_10 Minute Physics/25 Joint Sim/Scenes/";

        //filePathEditor includes a last /
        string JSONAsString = File.ReadAllText(filePathEditor + fileName);

        JointsJson data = JsonUtility.FromJson<JointsJson>(JSONAsString);

        //Check if scene data is empty
        if (data == null || data.meshes == null || data.meshes.Length == 0)
        {
            Debug.Log("Empty scene data provided! No objects to load!");
            return;
        }

        //Debug.Log("Number of meshes: " + data.meshes.Length);

        //JointMesh mesh = data.meshes[0];

        //DisplayMeshData(mesh);

        //Clear existing simulation
        this.rigidBodies.Clear();

        //We need to separate the meshes which are RigidBox, ...Joint, or Visual. The RigidBox are the cubic meshes and Visual the more detailed meshes overlaying the simple ones
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
                CreateRigidBody(mesh);
                rigidCount += 1;
            }
        }

        //Pass 2: Create joints and visual meshes
        foreach (JointMesh mesh in meshes)
        {
            if (IsJoint(mesh))
            {
                CreateJoint(mesh);
                jointCount += 1;
            }
            else if (IsVisual(mesh))
            {
                CreateVisualMesh(mesh);
                visualCount += 1;
            }
        }

        Debug.Log($"Found {rigidCount} RigidBodies, {jointCount} Joints, and {visualCount} Visual meshes = {rigidCount + jointCount + visualCount} meshes. Total should be {data.exportInfo.totalMeshes}");

        //this.simulator.simulationView = false;
        //this.simulator.toggleView();

        Debug.Log($"Loaded scene with ${ this.rigidBodies.Count} rigid bodies");
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



    //Extract position and rotation from mesh.transform
    private void ExtractPosAndRot(JointMesh mesh, out Vector3 pos, out Quaternion rot)
    {
        float[] posArray = mesh.transform.position;
        float[] rotArray = mesh.transform.rotation;

        pos = new(posArray[0], posArray[1], posArray[2]);

        rot = new(rotArray[0], rotArray[1], rotArray[2], rotArray[3]);
    }



    //
    // Init a rigidbody
    //

    private void CreateRigidBody(JointMesh mesh)
    {
        JointProperties props = mesh.properties;

        string simType = props.simType;

        //Extract position and rotation from mesh.transform
        ExtractPosAndRot(mesh, out Vector3 pos, out Quaternion quat);

        //The objects are mirrored most likely because Blender is using some other coordinate system
        //Inverteing z seems to solve it for now
        pos.z *= -1f;

        MyRigidBody rigidBody;

        //Our rb system wants angles not quaternions when init a rb
        Vector3 angles = quat.eulerAngles;

        //Test that nullable works
        //This is a rb and it has never a damping because thats joint specific
        //Debug.Log(mesh.properties.damping);

        if (simType == "RigidBox")
        {
            //Calculate bounding box from vertices
            Vector3 size = CalculateBoundingBox(mesh.vertices);

            rigidBody = new MyRigidBody(MyRigidBody.Types.Box, size, props.density, pos, angles);
        }
        else if (simType == "RigidSphere")
        {
            //Calculate bounding sphere radius
            float radius = CalculateBoundingSphere(mesh.vertices);
            
            Vector3 size = new(radius, radius, radius);
            
            rigidBody = new MyRigidBody(MyRigidBody.Types.Sphere, size, props.density, pos, angles);
        }
        else
        {
            Debug.Log($"Unknown rigid body type: ${ simType }");

            return;
        }

        rigidBody.visualObjects.rbVisualObj.name = mesh.name;

        //Store in lookup table
        this.rigidBodies[mesh.name] = rigidBody;

        //Register with simulator
        this.simulator.AddRigidBody(rigidBody);

        Debug.Log($"Created ${ simType }: ${ mesh.name }");
    }



    //
    // Find the bounding box or sphere
    //
    
    //We are not using the mesh we get from the importer as the rb mesh
    //We are instead using that mesh to find the size of the box or the sphere
    //we then create when init the rb

    //Given a set of vertices, calculate the bounding box
    private Vector3 CalculateBoundingBox(float[] vertices)
    {
        float minX = float.MaxValue, maxX = -float.MaxValue;
        float minY = float.MaxValue, maxY = -float.MaxValue;
        float minZ = float.MaxValue, maxZ = -float.MaxValue;

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



    //Given a set of vertices, calculate the max radius 
    private float CalculateBoundingSphere(float[] vertices)
    {
        //Find center
        Vector3 center = Vector3.zero;

        for (int i = 0; i < vertices.Length; i += 3)
        {
            Vector3 thisVert = new(vertices[i], vertices[i + 1], vertices[i + 2]);

            center += thisVert;
        }

        int numVerts = vertices.Length / 3;

        center /= numVerts;

        //Find maximum distance from center
        float maxRadius = 0;
        
        for (int i = 0; i < vertices.Length; i += 3)
        {
            Vector3 thisVert = new(vertices[i], vertices[i + 1], vertices[i + 2]);
            
            float radius = (thisVert - center).magnitude;
            
            maxRadius = Mathf.Max(maxRadius, radius);
        }

        return maxRadius;
    }



    //
    // Init a joint
    //
    
    private void CreateJoint(JointMesh mesh)
    {
        JointProperties props = mesh.properties;

        string simType = props.simType;

        //Look up parent bodies
        if (!this.rigidBodies.TryGetValue(props.parent1, out MyRigidBody body0))
        {
            Debug.Log($"Parent body not found: ${ props.parent1 }");
            return;
        }

        if (!this.rigidBodies.TryGetValue(props.parent2, out MyRigidBody body1))
        {
            Debug.Log($"Parent body not found: ${ props.parent2 }");
            return;
        }


        //Joint position (use mesh position as anchor point)
        ExtractPosAndRot(mesh, out Vector3 jointPos, out Quaternion jointRot);

        //Create joint
        MyJoint joint = new(body0, body1, jointPos, jointRot);

        //Configure joint based on type
        if (simType == "BallJoint")
        {
            float swingMax = props.swingMax ?? float.MaxValue;
            float twistMin = props.twistMin ?? -float.MaxValue;
            float twistMax = props.twistMax ?? float.MaxValue;
            float damping = props.damping ?? 0f;

            joint.jointType.InitBallJoint(swingMax, twistMin, twistMax, damping);
        }
        else if (simType == "HingeJoint")
        {
            float swingMin = props.swingMin ?? -float.MaxValue;
            float swingMax = props.swingMax ?? float.MaxValue;
            bool hasTargetAngle = props.targetAngle != null;
            float targetAngle = props.targetAngle ?? 0f;
            float compliance = props.targetAngleCompliance ?? 0f;
            float damping = props.damping ?? 0f;

            joint.jointType.InitHingeJoint(swingMin, swingMax, hasTargetAngle, targetAngle, compliance, damping);
        }
        else if (simType == "ServoJoint")
        {
            float swingMin = props.swingMin ?? -float.MaxValue;
            float swingMax = props.swingMax ?? float.MaxValue;

            joint.jointType.InitServo(swingMin, swingMax);
        }
        else if (simType == "MotorJoint")
        {
            //float velocity = props.velocity ?? 0f;

            //Why is he stting velocity to 3, because he forgot to add it in the json???
            float velocity = 3f;
            
            joint.jointType.InitMotor(velocity);
        }
        else if (simType == "DistanceJoint")
        {
            float restDistance = props.restDistance ?? 0f;
            float compliance = props.compliance ?? 0f;
            float damping = props.damping ?? 0f;

            joint.jointType.InitDistanceJoint(restDistance, compliance, damping);
        }
        else if (simType == "PrismaticJoint")
        {
            float distanceMin = props.distanceMin ?? -float.MaxValue;
            float distanceMax = props.distanceMax ?? float.MaxValue;
            float compliance = props.compliance ?? 0f;
            float damping = props.damping ?? 0f;
            bool hasTarget = props.distanceTarget != null;
            float targetDistance = props.posTarget ?? 0f;
            float twistMin = props.twistMin ?? -float.MaxValue;
            float twistMax = props.twistMax ?? float.MaxValue;

            joint.jointType.InitPrismaticJoint(distanceMin, distanceMax, twistMin, twistMax, hasTarget, targetDistance, compliance, damping);
        }
        else if (simType == "CylinderJoint")
        {
            bool hasDistanceLimits = props.distanceMin != null && props.distanceMax != null;
            float distanceMin = props.distanceMin ?? -float.MaxValue;
            float distanceMax = props.distanceMax ?? float.MaxValue;
            float twistMin = props.twistMin ?? -float.MaxValue;
            float twistMax = props.twistMax ?? float.MaxValue;

            //Has 8 parameters, why is he using only 4???
            joint.jointType.InitCylinderJoint(distanceMin, distanceMax, twistMin, twistMax);
        }
        else
        {
            Debug.Log($"Unknown joint type: ${ simType }");
            return;
        }

        //Add visuals (the small coordinate system showing how the joint connects)
        joint.AddVisuals();

        //Register with simulator
        this.simulator.AddJoint(joint);

        Debug.Log($"Created ${ simType}: ${ mesh.name } connecting ${ props.parent1 } to ${ props.parent2}");
    }



    //
    // Init a visual mesh
    //

    private void CreateVisualMesh(JointMesh mesh)
    {
        JointProperties props = mesh.properties;
    
        string parentName = props.parent;

        //Look up parent body
        if (!this.rigidBodies.TryGetValue(parentName, out MyRigidBody parentBody))
        {
            Debug.Log($"Parent body not found for visual mesh: ${ parentName}");
            return;
        }

        //Get visual mesh transform
        ExtractPosAndRot(mesh, out Vector3 visualPos, out Quaternion visualRot);

        //Transform visual mesh to parent body local space

        List<Vector3> vertices = new();

        for (int i = 0; i < mesh.vertices.Length; i += 3)
        {
            Vector3 vertex = new(mesh.vertices[i], mesh.vertices[i + 1], mesh.vertices[i + 2]);

            vertices.Add(vertex);
        }

        List<Vector3> normals = new();

        if (mesh.normals != null)
        {
            for (int i = 0; i < mesh.normals.Length; i += 3)
            {
                Vector3 normal = new(mesh.normals[i], mesh.normals[i + 1], mesh.normals[i + 2]);

                normals.Add(normal);
            }
        }

        /*
        //const q_rel = parentBody.rot.clone().conjugate().multiply(visualRot);
        //For unit quaternions (which are normalized), the conjugate is the same as the inverse.
        Quaternion q_rel = Quaternion.Inverse(parentBody.rot) * visualRot;

        //const p_rel = visualPos.clone().sub(parentBody.pos).applyQuaternion(parentBody.rot.clone().conjugate());
        Vector3 p_rel = Quaternion.Inverse(parentBody.rot) * (parentBody.pos - visualPos); //???

        List<Vector3> transformedVertices = new();

        for (int i = 0; i < mesh.vertices.Length; i += 3)
        {
            Vector3 vertex = new(mesh.vertices[i], mesh.vertices[i + 1], mesh.vertices[i + 2]);

            //vertex.applyQuaternion(q_rel);
            //vertex.add(p_rel);
            vertex = q_rel * vertex + p_rel;

            transformedVertices.Add(vertex);
        }

        List<Vector3> transformedNormals = new();

        if (mesh.normals != null)
        {
            for (int i = 0; i < mesh.normals.Length; i += 3)
            {
                Vector3 normal = new(mesh.normals[i], mesh.normals[i + 1], mesh.normals[i + 2]);

                //normal.applyQuaternion(q_rel);
                normal = q_rel * normal;
                
                transformedNormals.Add(normal);
            }
        }
        */

        //Create Unity geometry from transformed data
        GameObject visualMesh = GameObject.CreatePrimitive(PrimitiveType.Cube);

        //Add new mesh
        Mesh actualMesh = new()
        {
            vertices = vertices.ToArray(),
            triangles = mesh.triangles,
            normals = normals.ToArray()
        };

        visualMesh.GetComponent<MeshFilter>().mesh = actualMesh;

        //Add color
        Color color = Color.white;

        if (props.color != null)
        {
            color = new Color(props.color[0], props.color[1], props.color[2]);
        }

        visualMesh.GetComponent<MeshRenderer>().material.color = color;

        //Deactivate the collider
        visualMesh.GetComponent<Collider>().enabled = false;

        visualMesh.name = mesh.name;

        parentBody.visualObjects.AddDetailedObject(visualMesh);

        parentBody.UpdateMeshes();

        Debug.Log($"Created visual mesh: ${ mesh.name } attached to ${ parentName } (transformed to body space)");
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
