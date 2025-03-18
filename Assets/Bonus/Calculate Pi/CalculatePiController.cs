using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

//Based on "3Blue1Brown" YouTube series where he calculates decimals in pi
//The idea is that you collide boxes with different mass and a wall and
//calculates how many collisions which is apaprently the same as decimals in pi
//Simulation is in 1d space so we dont take rotations into account
public class CalculatePiController : MonoBehaviour
{
    Vector2 smallBoxPos;
    Vector2 largeBoxPos;

    Vector2 smallBoxVel;
    Vector2 largeBoxVel;

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

        Vector3 smallBoxStartPos = new(2f, smallBoxSize * 0.5f, 0f);

        this.smallBoxTrans.position = smallBoxStartPos;
        this.smallBoxTrans.localScale = Vector3.one * smallBoxSize;

        smallBoxObj.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        smallBoxObj.GetComponent<MeshRenderer>().material.color = UnityEngine.Color.blue;

        //Large
        GameObject largeBoxObj = GameObject.CreatePrimitive(PrimitiveType.Cube);

        this.largeBoxTrans = largeBoxObj.transform;

        Vector3 largeBoxStartPos = new(7f, largeBoxSize * 0.5f, 0f);

        this.largeBoxTrans.position = largeBoxStartPos;
        this.largeBoxTrans.localScale = Vector3.one * largeBoxSize;

        largeBoxObj.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        largeBoxObj.GetComponent<MeshRenderer>().material.color = UnityEngine.Color.red;
    }



    private void Update()
    {
        
    }



    private void FixedUpdate()
    {
        float dt = Time.deltaTime;

        float sdt = dt / (float)subSteps;

        for (int i = 0; i < subSteps; i++)
        {
            //Simulate one step
        }
    }
}
