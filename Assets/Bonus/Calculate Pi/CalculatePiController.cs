using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

//Based on "3Blue1Brown" YouTube series where he calculates decimals in pi
//The idea is that you collide boxes with different mass and a wall and
//calculates how many collisions which is apaprently the same as decimals in pi
//Simulation is in 1d space so we dont take rotations/gravity into account
public class CalculatePiController : MonoBehaviour
{
    float smallBoxPos_x;
    float largeBoxPos_x;

    float smallBoxVel_x = 0f;
    float largeBoxVel_x = -2f;

    float smallBoxMass;
    float largeBoxMass;

    //Size of the box doesnt matter here, mass is important
    //we just say we change density which is easier to experiment with
    float smallBoxSize = 1f;
    float largeBoxSize = 2f;

    private Transform smallBoxTrans;
    private Transform largeBoxTrans;

    //Sim settings
    private int subSteps = 1;



    private void Start()
    {
        //Small
        GameObject smallBoxObj = GameObject.CreatePrimitive(PrimitiveType.Cube);

        this.smallBoxTrans = smallBoxObj.transform;

        Vector3 smallBoxStartPos = new(2f, 0f, 0f);

        this.smallBoxTrans.position = smallBoxStartPos;
        this.smallBoxTrans.localScale = Vector3.one * smallBoxSize;

        smallBoxObj.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        smallBoxObj.GetComponent<MeshRenderer>().material.color = UnityEngine.Color.blue;

        //Large
        GameObject largeBoxObj = GameObject.CreatePrimitive(PrimitiveType.Cube);

        this.largeBoxTrans = largeBoxObj.transform;

        Vector3 largeBoxStartPos = new(7f, 0f, 0f);

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
        float dt = Time.deltaTime;

        float sdt = dt / (float)subSteps;

        for (int i = 0; i < subSteps; i++)
        {
            //Simulate one step
            Simulate(sdt);
        }
    }



    private void Simulate(float dt)
    {
        //No gravity or acceleration so no need to update vel

        //Cache prev pos
        float smallBoxPosPrev_x = this.smallBoxPos_x;
        float largeBoxPosPrev_x = this.largeBoxPos_x;

        //Update pos
        this.smallBoxPos_x += this.smallBoxVel_x * dt;
        this.largeBoxPos_x += this.largeBoxVel_x * dt;


        //Collision checks

        //With each other

        //With left wall (which we know is at 0)

        //Only need to check the small box
        float smallBoxLeftPos = this.smallBoxPos_x - (smallBoxSize * 0.5f);

        if (smallBoxLeftPos < 0f)
        {
            //Move the box
            this.smallBoxPos_x = smallBoxSize * 0.5f;

            //Flip x vel
            this.smallBoxVel_x *= -1f;
        }



        //Fix velocity
        this.smallBoxVel_x = (smallBoxPosPrev_x - this.smallBoxPos_x) / dt;
        this.largeBoxVel_x = (largeBoxPosPrev_x - this.largeBoxPos_x) / dt;
    }
}
