using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class CustomPhysics
{
    public static void HandleBallBallCollision(Ball b1, Ball b2, float restitution)
    {
        //Direction from b1 to b2
        Vector3 dir = b2.pos - b1.pos;

        //The distance between the balls
        float d = dir.magnitude;

        //The balls are not colliding
        if (d == 0f || d > b1.radius + b2.radius)
        {
            return;
        }

        //Normalized direction
        dir = dir.normalized;


        //Update positions

        //The distace each ball should move so they no longer intersect 
        float corr = (b1.radius + b2.radius - d) * 0.5f;

        //Move the balls apart along the dir vector
        b1.pos += dir * -corr; //-corr because dir goes from b1 to b2
        b2.pos += dir * corr;


        //Update velocities

        //Collisions can only change velocity components along the penetration direction

        //The part of each balls velocity along dir
        float v1 = Vector3.Dot(b1.vel, dir);
        float v2 = Vector3.Dot(b2.vel, dir);

        float m1 = b1.mass;
        float m2 = b2.mass;

        //Assume the objects are stiff
        float new_v1 = (m1 * v1 + m2 * v2 - m2 * (v1 - v2) * restitution) / (m1 + m2);
        float new_v2 = (m1 * v1 + m2 * v2 - m1 * (v2 - v1) * restitution) / (m1 + m2);

        //Change velocity components along dir
        b1.vel += dir * (new_v1 - v1);
        b2.vel += dir * (new_v2 - v2);
    }



}
