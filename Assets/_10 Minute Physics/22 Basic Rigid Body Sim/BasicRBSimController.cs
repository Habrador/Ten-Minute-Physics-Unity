using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XPBD;


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
    //Gravity is set to 10 to easier see if we get a correct result
    private Vector3 gravity = new(0f, -10.0f, 0f);
    //Needed to calculate mass
    private readonly float density = 1000f;
    //How many steps each FixedUpdate
    //Was 10 in tutorial
    private readonly int numSubSteps = 10;

    //Mouse interaction
    Interaction interaction;
    


    private void Start()
    {
        //Default scene
        InitScene(Scenes.Chain);

        this.interaction = new Interaction(Camera.main, this.rbSimulator);
    }



    private void Update()
    {
        rbSimulator.MyUpdate();

        //Should maybe be in LateUpdate()???
        this.interaction.DragWithMouse();
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