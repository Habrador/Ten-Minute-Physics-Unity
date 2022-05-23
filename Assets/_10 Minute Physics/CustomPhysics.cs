using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class CustomPhysics
{
    public static bool AreBallsColliding(Vector3 p1, Vector3 p2, float r1, float r2)
    {
        bool areColliding = true;

        //The distance sqr between the balls
        float dSqr = (p2- p1).sqrMagnitude;

        float minAllowedDistance = r1 + r2;

        //The balls are not colliding (or they are exactly at the same position)
        //Square minAllowedDistance because we are using distance Square, which is faster 
        if (dSqr == 0f || dSqr > minAllowedDistance * minAllowedDistance)
        {
            areColliding = false;
        }

        return areColliding;
    }



    public static void HandleBallBallCollision(Ball b1, Ball b2, float restitution)
    {
        //Check if the balls are colliding
        bool areColliding = AreBallsColliding(b1.pos, b2.pos, b1.radius, b2.radius);

        if (!areColliding)
        {
            return;
        }


        //Update positions

        //Direction from b1 to b2
        Vector3 dir = b2.pos - b1.pos;

        //The distance between the balls
        float d = dir.magnitude;

        dir = dir.normalized;

        //The distace each ball should move so they no longer intersect 
        float corr = (b1.radius + b2.radius - d) * 0.5f;

        //Move the balls apart along the dir vector
        b1.pos += dir * -corr; //-corr because dir goes from b1 to b2
        b2.pos += dir * corr;


        //Update velocities

        //Collisions can only change velocity components along the penetration direction

        //The part of each balls velocity along dir (penetration direction)
        //The velocity is now in 1D making it easier to use standardized physics equations
        float v1 = Vector3.Dot(b1.vel, dir);
        float v2 = Vector3.Dot(b2.vel, dir);

        float m1 = b1.mass;
        float m2 = b2.mass;

        //If we assume the objects are stiff we can calculate the new velocities after collision
        float new_v1 = (m1 * v1 + m2 * v2 - m2 * (v1 - v2) * restitution) / (m1 + m2);
        float new_v2 = (m1 * v1 + m2 * v2 - m1 * (v2 - v1) * restitution) / (m1 + m2);

        //Change velocity components along dir
        //First we need to subtract the old velocity because it doesnt exist anymore
        //b1.vel -= dir * v1;
        //b2.vel -= dir * v2;

        //And then add the new velocity
        //b1.vel += dir * new_v1;
        //b2.vel += dir * new_v2;

        //Which can be simplified to:
        b1.vel += dir * (new_v1 - v1);
        b2.vel += dir * (new_v2 - v2);
    }
}
