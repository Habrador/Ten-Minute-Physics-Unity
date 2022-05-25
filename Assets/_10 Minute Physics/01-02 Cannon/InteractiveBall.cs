using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractiveBall : Ball
{
    public InteractiveBall(Transform ballTransform) : base(ballTransform)
    {

    }



    public void SimulateBall(float dt, Vector3 gravity)
    {
        vel += gravity * dt;
        pos += vel * dt;
    }



    public void HandleWallCollision()
    {
        //Make sure the ball is within the area, which is 5 m in all directions (except y)
        //If outside, reset ball and mirror velocity
        float halfSimSize = 5f - radius;

        //x
        if (pos.x < -halfSimSize)
        {
            pos.x = -halfSimSize;
            vel.x *= -1f;
        }
        else if (pos.x > halfSimSize)
        {
            pos.x = halfSimSize;
            vel.x *= -1f;
        }

        //y
        if (pos.y < 0f + radius)
        {
            pos.y = 0f + radius;
            vel.y *= -1f;
        }
        //Sky is the limit, so no collision detection in y-positive direction

        //z
        if (pos.z < -halfSimSize)
        {
            pos.z = -halfSimSize;
            vel.z *= -1f;
        }
        else if (pos.z > halfSimSize)
        {
            pos.z = halfSimSize;
            vel.z *= -1f;
        }
    }
}
