using System.Collections;
using System.Collections.Generic;
using UnityEngine;


//Based on "22 How to write a basic rigid body simulator using position based dynamics"
//from https://matthias-research.github.io/pages/tenMinutePhysics/index.html
//Simulate rigidbodies with XPBD - Extended Position Based Dynamics
//Read paper "Detailed rigid body simulation with xpbd" because
//some of the equations in the youtube tutorial are inaccurate and missing
//TODO:
//- Use doubles instead of floats
//- Figure out whats in local space in whats in global space
//- Whats the deal with unilateral?
//- Use some kind of sphere texture to easier see rotation, the tutorial is using hafl red half white
public class BasicRBSimController : MonoBehaviour
{
    private RigidBodySimulator rbSimulator;

    //The scenes we can chose from
    private enum Scenes
    {
        CribMobile,
        Chain
    }

    //Simulation settings
    private Vector3 gravity = new(0f, -10.0f, 0f);
    //Needed to calculate mass
    private readonly float density = 1000f;
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
        InitScene(Scenes.Chain);

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
        //Destroy previous scene
        if (rbSimulator != null)
        {
            rbSimulator.Dispose();
        }
    
        //Create a new rb simulator
        rbSimulator = new RigidBodySimulator(gravity);

        //Childs crib mobile but several connected to each other
        if (scene == Scenes.CribMobile)
        {
            BasicRBSimScenes.InitCribMobileScene(rbSimulator, density);
        }
        //Boxes hanging from the roof connected to each other by a rope
        else if (scene == Scenes.Chain)
        {
            BasicRBSimScenes.InitChainScene(rbSimulator, density);
        }
        else
        {
            Debug.Log("There's no scene to init!");
        }
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

        if (GUILayout.Button($"Crib mobile", buttonStyle))
        {
            InitScene(Scenes.CribMobile);
        }
        if (GUILayout.Button("Chain", buttonStyle))
        {
            InitScene(Scenes.Chain);
        }

        GUILayout.EndHorizontal();
    }
}