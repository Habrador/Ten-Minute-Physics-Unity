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



    //Create a new rb simulation envrionment with some objects we want to simulate
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
    //   __|__     0 <- the center of this one is at the same height as the bar to the left of it
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
        //How far the bar changes in x direction each level
        float distance = 0.5f * length - thickness;

        Vector3 barSize = new Vector3(length, thickness, thickness);
        Vector3 angles = new Vector3(0.0f, 0.0f, 0.0f);
        Vector3 barPos = new Vector3(0.0f, 2.5f, 0.0f);

        //Build the tree of rigidbodies
        MyRigidBody prevBar = null;

        int numLevels = 5;

        float volBar = length * thickness * thickness;
        
        //Precalculate the radius of all spheres 
        float volTree = 2f * 4f / 3f * Mathf.PI * baseRadius * baseRadius * baseRadius + volBar;

        List<float> radii = new List<float>() { baseRadius };

        for (int i = 1; i < numLevels; i++)
        {
            float radius = Mathf.Pow(3f / 4f / Mathf.PI * volTree, 1f / 3f);
            
            radii.Add(radius);
            
            volTree = 2.0f * volTree + volBar;
        }

        for (int i = 0; i < numLevels; i++)
        {
            //The horizontal bar - they have the same size
            MyRigidBody bar = new(MyRigidBody.Types.Box, barSize, density, barPos, angles);

            rbSimulator.AddRigidBody(bar);


            //The line the bar is hanging from which attaches to the roof or the previous bar
            Vector3 p0_bar = new Vector3(barPos.x, barPos.y + 0.5f * thickness, barPos.z);
            Vector3 p1_bar = new Vector3(barPos.x, barPos.y + height - 0.5f * thickness, barPos.z);

            DistanceConstraint barConstraint = new(bar, prevBar, p0_bar, p1_bar, height - thickness, compliance, unilateral);
            
            rbSimulator.AddDistanceConstraint(barConstraint);


            //The sphere hanging from the right side of the bar
            float radius = radii[numLevels - i - 1];

            Vector3 spherePos = new Vector3(barPos.x + distance, barPos.y - height, barPos.z);
            Vector3 sphereSize = new Vector3(radius, radius, radius);

            MyRigidBody sphere = new(MyRigidBody.Types.Sphere, sphereSize, density, spherePos, angles);
            
            rbSimulator.AddRigidBody(sphere);


            //The line which attaches the sphere to the bar
            Vector3 p0_sphere = new Vector3(spherePos.x, spherePos.y + 0.5f * radius, spherePos.z);
            Vector3 p1_sphere = new Vector3(spherePos.x, spherePos.y + height - 0.5f * thickness, spherePos.z);
            
            DistanceConstraint sphereConstraint = new(sphere, bar, p0_sphere, p1_sphere, height - thickness, compliance, unilateral);
            
            rbSimulator.AddDistanceConstraint(sphereConstraint);


            //On the lowest level we have a sphere which is also attached to the left side of the bar
            if (i == numLevels - 1)
            {
                spherePos.x -= 2.0f * distance;
                
                MyRigidBody sphere_last = new(MyRigidBody.Types.Sphere, sphereSize, density, spherePos, angles);
                
                rbSimulator.AddRigidBody(sphere_last);

                //The line which attaches the sphere to the bar
                p0_sphere.x -= 2.0f * distance;
                p1_sphere.x -= 2.0f * distance;

                DistanceConstraint sphereConstraint_last = new(sphere_last, bar, p0_sphere, p1_sphere, height - thickness, compliance, unilateral);
                
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
            MyRigidBody box = new MyRigidBody(MyRigidBody.Types.Box, boxSize, density, boxPos, boxAngles, fontSize);
            box.damping = 5f;

            rbSimulator.AddRigidBody(box);

            //Add constraint
            Vector3 p0 = new Vector3(0.4f * prevSize, boxPos.y + 0.5f * boxSize.y, 0.0f);
            Vector3 p1 = new Vector3(0.4f * prevSize, prevY - 0.5f * prevSize, 0.0f);
            
            //Prevbox first iteration is null = roof
            DistanceConstraint barConstraint = new DistanceConstraint(box, prevBox, p0, p1, p1.y - p0.y, compliance, unilateral, width, fontSize);
            
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
}