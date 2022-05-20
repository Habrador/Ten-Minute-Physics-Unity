using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Based on "Providing user interaction with a 3d scene"
//https://matthias-research.github.io/pages/tenMinutePhysics/
public class UserInteractionController : MonoBehaviour
{
    public Transform ballTrans;

    private Vector3 ballVel;
    private Vector3 ballPos;

    private int subSteps = 5;

    private Vector3 gravity = new Vector3(0f, -9.81f, 0f);

    private bool canSimulate = false;

    private float ballRadius;



    private void Start()
    {
        //Init pos and vel
        ballPos = ballTrans.position;

        ballVel = new Vector3(3f, 5f, 2f);

        canSimulate = true;

        ballRadius = ballTrans.localScale.x * 0.5f;
    }



    private void Update()
    {
        //Update the visual position of the ball
        ballTrans.position = ballPos;
    }



    private void FixedUpdate()
    {
        if (!canSimulate)
        {
            return;
        }
        

        //Update pos and vel
        float sdt = Time.fixedDeltaTime / (float)subSteps;

        //Debug.Log(ballPos);

        for (int i = 0; i < subSteps; i++)
        {
            ballVel += gravity * sdt;
            ballPos += ballVel * sdt;

            //Debug.Log(ballVel);
        }


        //Collision detection

        //Make sure the all is within the area, which is 5 m in all directions (except y)
        //If outside, reset ball and mirror velocity
        float halfSimSize = 5f + ballRadius;
        
        if (ballPos.x < -halfSimSize)
        {
            ballPos.x = -halfSimSize;
            ballVel.x *= -1f; 
        }
        if (ballPos.x > halfSimSize)
        {
            ballPos.x = halfSimSize;
            ballVel.x *= -1f;
        }

        if (ballPos.y < 0f + ballRadius)
        {
            ballPos.y = 0f + ballRadius;
            ballVel.y *= -1f;
        }
        //Sky is the limit
        //if (ballPos.y > halfSimSize)
        //{
        //    ballPos.y = halfSimSize;
        //    ballVel.y *= -1f;
        //}

        if (ballPos.z < -halfSimSize)
        {
            ballPos.z = -halfSimSize;
            ballVel.z *= -1f;
        }
        if (ballPos.z > halfSimSize)
        {
            ballPos.z = halfSimSize;
            ballVel.z *= -1f;
        }
        
    }
    
}
