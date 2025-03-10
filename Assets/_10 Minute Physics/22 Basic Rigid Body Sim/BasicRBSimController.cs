using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

//Based on "22 How to write a basic rigid body simulator using position based dynamics"
//from https://matthias-research.github.io/pages/tenMinutePhysics/index.html
//Simulate rigidbodies with XPBD - Extended Position Based Dynamics
//Read paper "Detailed rigid body simulation with xpbd" because
//some of the equations in the youtube tutorial are inaccurate and missing
//TODO:
//- Use doubles instead of floats
//- Figure out whats in local space in whats in global space
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
    private readonly int numSubSteps = 1;

    

    private void Start()
    {
        //Default scene
        InitScene(Scenes.Chain);
    }



    private void Update()
    {
        rbSimulator.MyUpdate();

        MouseInteraction();
    }



    private void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;

        rbSimulator.MyFixedUpdate(dt, numSubSteps);
    }



    private void InitScene(Scenes scene)
    {
        rbSimulator = new RigidBodySimulator(gravity);

        //Childs crib mobile but several connected to each other
        if (scene == Scenes.CribMobile)
        {
            InitCribMobileScene();
        }
        //Boxes hanging from the roof connected to each other by a rope
        else if (scene == Scenes.Chain)
        {
            InitChainScene();
        }
        else
        {
            Debug.Log("There's no scene to init!");
        }
    }



    //______________________
    //      ___|___
    //     |       |
    //   __|__     0
    //  |     |     
    //__|__   0
    private void InitCribMobileScene()
    {
        bool unilateral = true;
        float compliance = 0f;

        float length = 0.9f;
        float thickness = 0.04f;
        float height = 0.3f;
        float baseRadius = 0.08f;
        float distance = 0.5f * length - thickness;

        Vector3 barSize = new Vector3(length, thickness, thickness);
        Vector3 angles = new Vector3(0.0f, 0.0f, 0.0f);
        Vector3 barPos = new Vector3(0.0f, 2.5f, 0.0f);

        //Build the tree of rigidbodies
        MyRigidBody prevBar = null;

        int numLevels = 5;

        float volBar = length * thickness * thickness;
        float volTree = 2.0f * 4.0f / 3.0f * Mathf.PI * baseRadius * baseRadius * baseRadius + volBar;

        List<float> radii = new List<float>() { baseRadius };

        for (int i = 1; i < numLevels; i++)
        {
            // sphere of volume volTree
            float radius = Mathf.Pow(3.0f / 4.0f / Mathf.PI * volTree, 1.0f / 3.0f);
            //Push
            radii.Add(radius);
            volTree = 2.0f * volTree + volBar;
        }

        for (int i = 0; i < numLevels; i++)
        {
            float radius = radii[numLevels - i - 1];
            MyRigidBody bar = new MyRigidBody(this.rbSimulator, MyRigidBody.Types.Box, barSize, density, barPos, angles);
            rbSimulator.AddRigidBody(bar);

            Vector3 p0 = new Vector3(barPos.x, barPos.y + 0.5f * thickness, barPos.z);
            Vector3 p1 = new Vector3(barPos.x, barPos.y + height - 0.5f * thickness, barPos.z);
            DistanceConstraint barConstraint = new DistanceConstraint(this.rbSimulator, bar, prevBar, p0, p1, height - thickness, compliance, unilateral);
            rbSimulator.AddDistanceConstraint(barConstraint);

            Vector3 spherePos = new Vector3(barPos.x + distance, barPos.y - height, barPos.z);
            Vector3 sphereSize = new Vector3(radius, radius, radius);
            MyRigidBody sphere = new MyRigidBody(this.rbSimulator, MyRigidBody.Types.Sphere, sphereSize, density, spherePos, angles);
            rbSimulator.AddRigidBody(sphere);

            Vector3 p0_other = new Vector3(spherePos.x, spherePos.y + 0.5f * radius, spherePos.z);
            Vector3 p1_other = new Vector3(spherePos.x, spherePos.y + height - 0.5f * thickness, spherePos.z);
            DistanceConstraint sphereConstraint = new DistanceConstraint(this.rbSimulator, sphere, bar, p0_other, p1_other, height - thickness, compliance, unilateral);
            rbSimulator.AddDistanceConstraint(sphereConstraint);

            //Last one
            if (i == numLevels - 1)
            {
                spherePos.x -= 2.0f * distance;
                MyRigidBody sphere_last = new MyRigidBody(this.rbSimulator, MyRigidBody.Types.Sphere, sphereSize, density, spherePos, angles);
                rbSimulator.AddRigidBody(sphere_last);

                p0.x -= 2.0f * distance;
                p1.x -= 2.0f * distance;
                DistanceConstraint sphereConstraint_last = new DistanceConstraint(this.rbSimulator, sphere_last, bar, p0, p1, height - thickness, compliance, unilateral);
                rbSimulator.AddDistanceConstraint(sphereConstraint_last);
            }

            prevBar = bar;
            barPos.y -= height;
            barPos.x -= distance;
        }
    }


    //4 boxes attached to each other, top box is attached to roof
    //The "rope" between each box is not attached to the center if the boxes except the first
    //____________________
    //        _|_
    //       |___|
    //        _|_
    //       |___|
    private void InitChainScene()
    {
        bool unilateral = false;
        float compliance = 0.001f;

        //Size of the box
        Vector3 boxSize = new Vector3(0.1f, 0.1f, 0.1f);
        //Center of the box
        Vector3 boxPos = new Vector3(0.0f, 2.5f, 0.0f);
        //If the box has some rotation
        Vector3 boxAngles = Vector3.zero;

        //Width of the mesh we use to display the constraint
        float width = 0.01f;
        float fontSize = 0.03f;
        
        //Distance between each box
        float dist = 0.2f;

        //Build the chain
        float prevY = 2.5f;
        float prevSize = 0f;

        MyRigidBody prevBox = null;

        //4 boxes
        for (int level = 0; level < 4; level++)
        {
            prevY = boxPos.y;
            boxPos.y -= dist + boxSize.y;

            //Add box
            MyRigidBody box = new MyRigidBody(this.rbSimulator, MyRigidBody.Types.Box, boxSize, density, boxPos, boxAngles, fontSize);
            box.damping = 5f;

            rbSimulator.AddRigidBody(box);

            //Add constraint
            Vector3 p0 = new Vector3(0.4f * prevSize, boxPos.y + 0.5f * boxSize.y, 0.0f);
            Vector3 p1 = new Vector3(0.4f * prevSize, prevY - 0.5f * prevSize, 0.0f);
            
            //Prevbox first iteration is null = roof
            DistanceConstraint barConstraint = new DistanceConstraint(this.rbSimulator, box, prevBox, p0, p1, p1.y - p0.y, compliance, unilateral, width, fontSize);
            
            rbSimulator.AddDistanceConstraint(barConstraint);

            //Data for next iteration
            prevBox = box;
            prevSize = boxSize.y;
            //Cuberoot = They get 1.2599 bigger each update
            boxSize *= Mathf.Pow(2f, 1f / 3f);
        }
    }



    //Interact with the scene by using mouse
    private void MouseInteraction()
    {
        //Use raycasting
        //p - position where ray intersects with the collider
        //d - distance from position to camera
        //r - p but in local space of the body
        //Create distance constraint

        //On mouse move -> update p by using distance d and camera dir
    }
}