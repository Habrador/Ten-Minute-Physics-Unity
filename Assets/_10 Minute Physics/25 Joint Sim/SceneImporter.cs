using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using XPBD;



public class SceneImporter
{
    private XPBDPhysicsSimulator simulator;

    //name -> RigidBody lookup
    //Used to connect joints to rbs
    private Dictionary<string, MyRigidBody> rigidBodies;



    //public SceneImporter(simulator, scene)
    public SceneImporter(XPBDPhysicsSimulator simulator)
    {
        this.simulator = simulator;
        //this.scene = scene;
        this.rigidBodies = new();
    }



    public void LoadScene(string fileName)
    {
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

        //Clear existing simulation (we do this outside this method in the Controller)
        //this.simulator.Clear();
        this.rigidBodies.Clear();

        //We need to separate the meshes which are RigidBox, ...Joint or Visual. The RigidBox are the cubic meshes and Visual the more detailed meshes overlaying the simple ones
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



    //Extract position and rotation from transform
    private void ExtractPosAndRot(JointMesh mesh, out Vector3 pos, out Quaternion rot)
    {
        pos = new(
            mesh.transform.position[0],
            mesh.transform.position[1],
            mesh.transform.position[2]
        );

        rot = new(
            mesh.transform.rotation[0],
            mesh.transform.rotation[1],
            mesh.transform.rotation[2],
            mesh.transform.rotation[3]
        );
    }



    //
    // Init a rigidbody
    //

    private void CreateRigidBody(JointMesh mesh)
    {
        JointProperties props = mesh.properties;

        string simType = props.simType;

        //Extract position and rotation from transform
        ExtractPosAndRot(mesh, out Vector3 pos, out Quaternion quat);

        //Arent eurler and angles the same here???
        //const euler = new THREE.Euler().setFromQuaternion(quat);
        //const angles = new THREE.Vector3(euler.x, euler.y, euler.z);
        Vector3 angles = quat.eulerAngles;

        MyRigidBody rigidBody;

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
            
            rigidBody = new MyRigidBody(MyRigidBody.Types.Box, size, props.density, pos, angles);
        }
        else
        {
            Debug.Log($"Unknown rigid body type: ${ simType }");

            return;
        }

        //Store in lookup table
        this.rigidBodies[mesh.name] = rigidBody;

        //Register with simulator
        this.simulator.AddRigidBody(rigidBody);

        Debug.Log($"Created ${ simType }: ${ mesh.name }");
    }



    //Given a set of vertices, calculate the bounding box
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

        // Configure joint based on type
        if (simType == "BallJoint")
        {
            //float swingMax = props.swingMax ?? Number.MAX_VALUE
            //float twistMin = props.twistMin ?? -Number.MAX_VALUE
            //float twistMax = props.twistMax ?? Number.MAX_VALUE
            //float damping = props.damping ?? 0.0;

            //Our floats cant be null, not sure how setting it to Number.MAX_VALUE will affect stuff later on...
            float swingMax = props.swingMax;
            float twistMin = props.twistMin;
            float twistMax = props.twistMax;
            float damping = props.damping;

            joint.InitBallJoint(swingMax, twistMin, twistMax, damping);
        }
        else if (simType == "HingeJoint")
        {
            //float swingMin = props.swingMin ?? -Number.MAX_VALUE
            //float swingMax = props.swingMax ?? Number.MAX_VALUE;
            //bool hasTargetAngle = props.targetAngle !== undefined;
            //float targetAngle = props.targetAngle ?? 0.0;
            //float compliance = props.targetAngleCompliance ?? 0.0;
            //float damping = props.damping ?? 0.0;

            float swingMin = props.swingMin;
            float swingMax = props.swingMax;
            bool hasTargetAngle = true; //TODO FIX THIS
            float targetAngle = props.targetAngle;
            float compliance = props.targetAngleCompliance;
            float damping = props.damping;

            joint.InitHingeJoint(swingMin, swingMax, hasTargetAngle, targetAngle, compliance, damping);
        }
        else if (simType == "ServoJoint")
        {
            //let swingMin = props.swingMin ?? -Number.MAX_VALUE
            //let swingMax = props.swingMax ?? Number.MAX_VALUE;

            float swingMin = props.swingMin;
            float swingMax = props.swingMax;

            joint.InitServo(swingMin, swingMax);
        }
        else if (simType == "MotorJoint")
        {
            //float velocity = props.velocity;
            
            //Why is he stting velocity to 3, because he forgot to add it in the json???
            float velocity = 3f;
            
            joint.InitMotor(velocity);
        }
        else if (simType == "DistanceJoint")
        {
            float restDistance = props.restDistance;
            float compliance = props.compliance;
            float damping = props.damping;

            joint.InitDistanceJoint(restDistance, compliance, damping);
        }
        else if (simType == "PrismaticJoint")
        {
            //let distanceMin = props.distanceMin ?? -Number.MAX_VALUE
            //let distanceMax = props.distanceMax ?? Number.MAX_VALUE;
            //let compliance = props.compliance ?? 0.0;
            //let damping = props.damping ?? 0.0;
            //let hasTarget = props.distanceTarget !== undefined;
            //let targetDistance = props.posTarget ?? 0.0;
            //let twistMin = props.twistMin ?? -Number.MAX_VALUE;
            //let twistMax = props.twistMax ?? Number.MAX_VALUE;

            float distanceMin = props.distanceMin;
            float distanceMax = props.distanceMax;
            float compliance = props.compliance;
            float damping = props.damping;
            bool hasTarget = true; //TODO FIX THIS
            float targetDistance = props.posTarget;
            float twistMin = props.twistMin;
            float twistMax = props.twistMax;

            joint.InitPrismaticJoint(distanceMin, distanceMax, twistMin, twistMax, hasTarget, targetDistance, compliance, damping);
        }
        else if (simType == "CylinderJoint")
        {
            //let hasDistanceLimits = props.distanceMin !== undefined && props.distanceMax !== undefined;
            //let distanceMin = props.distanceMin ?? -Number.MAX_VALUE
            //let distanceMax = props.distanceMax ?? Number.MAX_VALUE;
            //let twistMin = props.twistMin ?? -Number.MAX_VALUE;
            //let twistMax = props.twistMax ?? Number.MAX_VALUE;

            //let hasDistanceLimits = props.distanceMin !== undefined && props.distanceMax !== undefined;
            float distanceMin = props.distanceMin;
            float distanceMax = props.distanceMax;
            float twistMin = props.twistMin;
            float twistMax = props.twistMax;

            //Has 8 parameters, why is he using only 4???
            //joint.InitCylinderJoint(distanceMin, distanceMax, twistMin, twistMax);
        }
        else
        {
            Debug.Log($"Unknown joint type: ${ simType }");
            return;
        }

        //Add visuals (the small coordinate system showing how the joint connetcs)
        joint.AddVisuals();

        //Register with simulator
        //this.simulator.addJoint(joint);

        Debug.Log($"Created ${ simType}: ${ mesh.name } connecting ${ props.parent1 } to ${ props.parent2}");
    }



    //
    // Init a visual mesh
    //

    private void CreateVisualMesh(JointMesh mesh)
    {
         JointProperties props = mesh.properties;
    
         string parentName = props.parent;

        // Look up parent body
        if (!this.rigidBodies.TryGetValue(parentName, out MyRigidBody parentBody))
        {
            Debug.Log($"Parent body not found for visual mesh: ${ parentName}");
            return;
        }

        //Get visual mesh transform
        ExtractPosAndRot(mesh, out Vector3 visualPos, out Quaternion visualRot);

        //Transform visual mesh to parent body local space

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

        //Create Unity geometry from transformed data
        GameObject visualMesh = GameObject.CreatePrimitive(PrimitiveType.Cube);

        //Add new mesh
        Mesh actualMesh = new()
        {
            vertices = transformedVertices.ToArray(),
            triangles = mesh.triangles,
            normals = transformedNormals.ToArray()
        };

        visualMesh.GetComponent<MeshFilter>().mesh = actualMesh;

        //Add color
        Color color = Color.white;

        if (props.color != null)
        {
            color = new Color(props.color[0], props.color[1], props.color[2]);
        }

        visualMesh.GetComponent<MeshRenderer>().material.color = color;

        //Not sure what this means? The simplifed mesh is the collider???
        //visualMesh.body = parentBody; // For raycasting
        //visualMesh.GetComponent<MeshCollider>().sharedMesh = parentBody.rbVisualObj.GetComponent<MeshFilter>().sharedMesh;
        //We dont have to do this, default mesh collider is the mesh in mesh filter... maybe form performance reasons?

        parentBody.visualObjects.SetDetailedObject(visualMesh);

        //parentBody.rbDetailedObj = visualMesh;
        //parentBody.rbDetailedTrans = visualMesh.transform;

        parentBody.UpdateMeshes();

        //this.scene.add(visualMesh);

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
