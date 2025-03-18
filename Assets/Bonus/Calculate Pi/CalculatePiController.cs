using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

//Based on "3Blue1Brown" YouTube series where he calculates decimals in pi
//https://www.youtube.com/watch?v=HEfHFsfGXjs
//The idea is that you collide boxes with different mass and a wall and
//calculates how many collisions which is apaprently the same as decimals in pi
//Simulation is in 1d space so we dont take rotations/gravity into account
public class CalculatePiController : MonoBehaviour
{
    float smallBoxPos_x = 2f;
    float largeBoxPos_x = 4f;

    //Should start stationary
    float smallBoxVel_x = 0f;
    //Some velocity
    //Should be small or we wont see anything because the boxes are travelling far away
    float largeBoxVel_x = -0.05f;

    //Always 1 kg
    float smallBoxMass = 1f;
    //Can change (PI = 3.141592653)
    //1 kg -> 3 collisions (in this sim: 3)
    //100 kg -> 31 (in this sim: 31)
    //10 000 kg -> 314 (in this sim: 314)
    //1 000 000 kg -> 3141
    float largeBoxMass = 10000f;

    //Size of the box doesnt matter here, mass is important
    //we just say we change density which is easier to experiment with
    float smallBoxSize = 1f;
    float largeBoxSize = 2f;

    //To display what we simulate
    private Transform smallBoxTrans;
    private Transform largeBoxTrans;

    //Sim settings
    //This is for simulation accuracy
    private int subSteps = 5;
    //This is for simulation speed
    //The more decimals we want the more time the simulation takes
    private int speedUpSteps = 10;

    //This should approximate pi
    private int collisions;

    //Physics settings

    //No friction

    //Perfect elastic
    float restitution = 1f;



    private void Start()
    {
        //Create the boxes we can see
    
        //Small
        GameObject smallBoxObj = GameObject.CreatePrimitive(PrimitiveType.Cube);

        this.smallBoxTrans = smallBoxObj.transform;

        Vector3 smallBoxStartPos = new(this.smallBoxPos_x, 0f, 0f);

        this.smallBoxTrans.position = smallBoxStartPos;
        this.smallBoxTrans.localScale = Vector3.one * smallBoxSize;

        smallBoxObj.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        smallBoxObj.GetComponent<MeshRenderer>().material.color = UnityEngine.Color.blue;

        //Large
        GameObject largeBoxObj = GameObject.CreatePrimitive(PrimitiveType.Cube);

        this.largeBoxTrans = largeBoxObj.transform;

        Vector3 largeBoxStartPos = new(this.largeBoxPos_x, 0f, 0f);

        this.largeBoxTrans.position = largeBoxStartPos;
        this.largeBoxTrans.localScale = Vector3.one * largeBoxSize;

        largeBoxObj.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        largeBoxObj.GetComponent<MeshRenderer>().material.color = UnityEngine.Color.red;
    }



    private void Update()
    {
        //Because we simulate in 1d we pretend during simulation the boxes are at the same y-coordinate
        Vector3 smallBoxPosVisual = new(this.smallBoxPos_x, smallBoxSize * 0.5f, 0f);
        Vector3 largeBoxPosVisual = new(this.largeBoxPos_x, largeBoxSize * 0.5f, 0f);

        this.smallBoxTrans.position = smallBoxPosVisual;
        this.largeBoxTrans.position = largeBoxPosVisual;
    }



    private void FixedUpdate()
    {
        for (int j = 0; j < speedUpSteps; j++)
        {
            float dt = Time.deltaTime;

            float sdt = dt / (float)subSteps;

            for (int i = 0; i < subSteps; i++)
            {
                //Simulate one step
                Simulate(sdt);
            }
        }
    }



    private void Simulate(float dt)
    {
        //No gravity or acceleration so no need to update vel

        //Update pos
        this.smallBoxPos_x += this.smallBoxVel_x * dt;
        this.largeBoxPos_x += this.largeBoxVel_x * dt;


        //Collision with each other
        
        //Treat the cubes as discs which works fine because we simulate in 1d 
        Disc smallDisc = new(this.smallBoxPos_x, 0f, this.smallBoxVel_x, 0f, this.smallBoxSize * 0.5f);
        Disc largeDisc = new(this.largeBoxPos_x, 0f, this.largeBoxVel_x, 0f, this.largeBoxSize * 0.5f);

        smallDisc.mass = this.smallBoxMass;
        largeDisc.mass = this.largeBoxMass;

        bool areColliding = BallCollisionHandling.HandleDiscDiscCollision(smallDisc, largeDisc, restitution);

        if (areColliding)
        {
            this.smallBoxPos_x = smallDisc.x;
            this.largeBoxPos_x = largeDisc.x;

            this.smallBoxVel_x = smallDisc.vx;
            this.largeBoxVel_x = largeDisc.vx;

            collisions += 1;
        }

        
        //Collision with left wall (which we know is at 0)

        //Only the small box can collide with the left wall
        float smallBoxLeftPos = this.smallBoxPos_x - (smallBoxSize * 0.5f);

        if (smallBoxLeftPos < 0f)
        {
            //Move the box so it doesnt collide anymore
            this.smallBoxPos_x = smallBoxSize * 0.5f;

            //Flip x vel
            this.smallBoxVel_x *= -1f;

            collisions += 1;
        }

        Debug.Log(collisions);


        //Cant fix velocities because we update velocity when they collide 
    }
}
