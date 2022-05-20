using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ball 
{
    public Vector3 vel;
    public Vector3 pos;

    private Transform ballTrans;

    public readonly float radius;



    public Ball(Vector3 ballVel, Transform ballTrans)
    {
        this.vel = ballVel;
        this.pos = ballTrans.position;
        this.ballTrans = ballTrans;

        this.radius = ballTrans.localScale.x * 0.5f;
    }



    public void UpdateVisualPostion()
    {
        ballTrans.position = pos;
    }



    public void SimulateBall(int subSteps)
    {
        float sdt = Time.fixedDeltaTime / (float)subSteps;

        Vector3 gravity = Vector3.zero;

        for (int step = 0; step < subSteps; step++)
        {
            vel += gravity * sdt;
            pos += vel * sdt;

            //Debug.Log(ballVel);
        }


        //WallCollisionDetection();
    }



    public void HandleWallCollision()
    {
        //Collision detection

        //Make sure the all is within the area, which is 5 m in all directions (except y)
        //If outside, reset ball and mirror velocity
        float halfSimSize = 5f - radius;

        if (pos.x < -halfSimSize)
        {
            pos.x = -halfSimSize;
            vel.x *= -1f;
        }
        if (pos.x > halfSimSize)
        {
            pos.x = halfSimSize;
            vel.x *= -1f;
        }

        //2d simulation
        //if (pos.y < 0f + radius)
        //{
        //    pos.y = 0f + 0.1f;
        //    vel.y *= -1f;
        //}
        //Sky is the limit
        //if (ballPos.y > halfSimSize)
        //{
        //    ballPos.y = halfSimSize;
        //    ballVel.y *= -1f;
        //}

        if (pos.z < -halfSimSize)
        {
            pos.z = -halfSimSize;
            vel.z *= -1f;
        }
        if (pos.z > halfSimSize)
        {
            pos.z = halfSimSize;
            vel.z *= -1f;
        }
    }
}
