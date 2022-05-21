using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Simulate a bouncy cannon ball
//Based on: https://matthias-research.github.io/pages/tenMinutePhysics/
public class CannonController : MonoBehaviour
{
    //How to improve accuracy of simulation?
    //- Find formula with calculas - impossible for difficult problems
    //- More sophisticated integration - slower, no improvement when collision occurs
    //- Make dt small - works great! Introduce substepping

    //int n = 5
    //float sdt = dt/n

    //for n substeps
    //  v = v + g * sdt
    //  x = x + v * sdt 

    public Transform ballTrans;

    private Vector3 ballVel;
    private Vector3 ballPos;

    private float ballRadius;

    private int subSteps = 5;

    private Vector3 gravity = new Vector3(0f, -9.81f, 0f);



    private void Start()
    {
        //Init pos and vel
        ballPos = ballTrans.position;

        ballVel = new Vector3(3f, 5f, 2f);

        ballRadius = ballTrans.localScale.y * 0.5f;
    }



    private void Update()
    {
        //Update the visual position of the ball
        ballTrans.position = ballPos;
    }



    private void FixedUpdate()
    {
        //Update pos and vel
        float sdt = Time.fixedDeltaTime / (float)subSteps;

        for (int i = 0; i < subSteps; i++)
        {
            ballVel += gravity * sdt;
            ballPos += ballVel * sdt;
        }


        //Collision detection

        //Make sure the ball is within the area, which is 5 m in all directions (except y)
        //If outside, reset ball and mirror velocity
        float halfSimSize = 5f - ballRadius;
        
        //x
        if (ballPos.x < -halfSimSize)
        {
            ballPos.x = -halfSimSize;
            ballVel.x *= -1f; 
        }
        else if (ballPos.x > halfSimSize)
        {
            ballPos.x = halfSimSize;
            ballVel.x *= -1f;
        }

        //y
        if (ballPos.y < 0f + ballRadius)
        {
            ballPos.y = 0f + ballRadius;
            ballVel.y *= -1f;
        }
        //Sky is the limit, so no collision detection in y-positive direction

        //z
        if (ballPos.z < -halfSimSize)
        {
            ballPos.z = -halfSimSize;
            ballVel.z *= -1f;
        }
        else if (ballPos.z > halfSimSize)
        {
            ballPos.z = halfSimSize;
            ballVel.z *= -1f;
        }   
    }
}
