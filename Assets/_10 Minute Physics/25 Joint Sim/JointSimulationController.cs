using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XPBD;

//Based on "25 Joint simulation made simple"
//from https://matthias-research.github.io/pages/tenMinutePhysics/index.html
//Simulate joints with XPBD - Extended Position Based Dynamics
public class JointSimulationController : MonoBehaviour
{
    private XPBDPhysicsSimulator rbSimulator;

    //The scenes we can chose from
    private enum Scenes
    {
        BasicJoints,
        Steering,
        Pendulums
    }

    //The scene data is in json files with data for all meshes, joints, etc
    private Dictionary<Scenes, string> jointScenes = new()
    {
        { Scenes.BasicJoints, "basicJoints.json" },
        { Scenes.Steering, "steering.json" },
        { Scenes.Pendulums, "pendulum.json" },
    };

    SceneImporter sceneImporter;

    //Simulation settings
    //In this sim gravity is 9.81 - not 10
    private Vector3 gravity = new(0f, -9.81f, 0f);
    //Needed to calculate mass
    //private readonly float density = 1000f;
    //How many steps each FixedUpdate
    //Was 10 in tutorial
    private readonly int numSubSteps = 10;

    //Mouse interaction
    private bool hasSelectedRb = false;
    private float d;
    private Camera thisCamera;



    private void Start()
    {
        //Default scene
        InitScene(Scenes.BasicJoints);

        thisCamera = Camera.main;
    }



    private void Update()
    {
        rbSimulator.MyUpdate();

        //Should maybe be in LateUpdate()???
        MouseInteraction();
    }



    private void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;

        rbSimulator.MyFixedUpdate(dt, numSubSteps);
    }



    private void OnGUI()
    {
        MainGUI();

        List<MyRigidBody> allRbs = rbSimulator.allRigidBodies;

        foreach (MyRigidBody thisRb in allRbs)
        {
            thisRb.DisplayData();
        }

        List<DistanceConstraint> allConstraints = rbSimulator.allDistanceConstraints;

        foreach (DistanceConstraint thisConstraint in allConstraints)
        {
            thisConstraint.DisplayData();
        }
    }



    //Create a new rb simulation envrionment with some objects we want to simulate
    private void InitScene(Scenes scene)
    {
        if (sceneImporter == null)
        {
            sceneImporter = new SceneImporter(this.rbSimulator);
        }
    
        //Destroy previous scene
        if (rbSimulator != null)
        {
            rbSimulator.Dispose();
        }

        //Create a new rb simulator
        rbSimulator = new XPBDPhysicsSimulator(gravity);

        sceneImporter.LoadScene(jointScenes[scene]);
    }



    //Interact with the scene by using mouse
    private void MouseInteraction()
    {
        //Try to select rb
        if (Input.GetMouseButtonDown(0) && hasSelectedRb == false)
        {
            //Raycasting
            Ray ray = thisCamera.ScreenPointToRay(Input.mousePosition);

            //If we hit a collider
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                //Each gameobject in Unity has a unique identifer which we can use to find it among the rb we simulate
                int id = hit.transform.gameObject.GetInstanceID();

                //Debug.Log(hit.transform.gameObject.GetInstanceID());
                //Debug.Log("hit");

                //Find the rigidbody with this id in the list of all rbs in the simulator
                List<MyRigidBody> allRigidBodies = rbSimulator.allRigidBodies;

                foreach (MyRigidBody thisRb in allRigidBodies)
                {
                    //If the ids match
                    if (thisRb.rbVisualObj.GetInstanceID() == id)
                    {
                        //Debug.Log("Identified the rb");

                        //Data

                        //p_m - position where ray intersects with the collider
                        Vector3 p = hit.point;

                        //d - distance from position we hit to mouse
                        this.d = hit.distance;

                        //Create a distance constraint
                        rbSimulator.StartDrag(thisRb, hit.point);

                        hasSelectedRb = true;

                        break;
                    }
                }
            }
        }



        //Drag selected rb
        if (hasSelectedRb == true)
        {
            //On mouse move -> update p by using distance d and new mouse ray
            Ray ray = thisCamera.ScreenPointToRay(Input.mousePosition);

            //Update p_m using d
            Vector3 p_m = ray.origin + ray.direction * d;

            rbSimulator.Drag(p_m);

            //Vector3 p0 = rbSimulator.dragConstraint.worldPos0;

            //Debug.DrawLine(p0, p_m, UnityEngine.Color.blue);
        }



        //Deselect rb
        if (Input.GetMouseButtonUp(0) && hasSelectedRb == true)
        {
            rbSimulator.EndDrag();

            hasSelectedRb = false;
        }
    }



    //Buttons to select which collision algorithm to use
    //Text to display info to see the difference between the algorithms
    private void MainGUI()
    {
        GUILayout.BeginHorizontal("box");

        int fontSize = 20;

        RectOffset offset = new(5, 5, 5, 5);


        //Buttons
        GUIStyle buttonStyle = new(GUI.skin.button)
        {
            //buttonStyle.fontSize = 0; //To reset because fontSize is cached after you set it once 

            fontSize = fontSize,
            margin = offset
        };

        if (GUILayout.Button($"Basic joints", buttonStyle))
        {
            InitScene(Scenes.BasicJoints);
        }
        if (GUILayout.Button("Steering", buttonStyle))
        {
            InitScene(Scenes.Steering);
        }
        if (GUILayout.Button("Pendulums", buttonStyle))
        {
            InitScene(Scenes.Pendulums);
        }

        GUILayout.EndHorizontal();
    }


}
