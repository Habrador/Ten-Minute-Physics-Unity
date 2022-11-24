using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class BallCollisionHandling
{
    public static bool AreBallsColliding(Vector3 p1, Vector3 p2, float r1, float r2)
    {
        bool areColliding = true;

        //The distance sqr between the balls
        float dSqr = (p2- p1).sqrMagnitude;

        float minAllowedDistance = r1 + r2;

        //The balls are not colliding (or they are exactly at the same position)
        //Square minAllowedDistance because we are using distance Square, which is faster 
        //They might be at the same position if we check if the ball is colliding with itself, which might be faster than checking if the other ball is not the same as the ball 
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



    //The walls are a list if edges ordered counter-clockwise
    //The first point on the border also has to be included at the end of the list
    public static bool HandleBallWallEdgesCollision(Ball ball, List<Vector3> border, float restitution)
    {
        //We need at least a triangle (the start and end are the same point, thus the 4)
        if (border.Count < 4)
        {
            return false;
        }


        //Find closest point on the border and related data to the line segment the point is on
        Vector3 closest = Vector3.zero;
        Vector3 ab = Vector3.zero;
        Vector3 wallNormal = Vector3.zero;

        float minDistSqr = 0f;

        //The border should include both the start and end points which are at the same location
        for (int i = 0; i < border.Count - 1; i++)
        {
            Vector3 a = border[i];
            Vector3 b = border[i + 1];
            Vector3 c = UsefulMethods.GetClosestPointOnLineSegment(ball.pos, a, b);

            //Using the square is faster
            float testDistSqr = (ball.pos - c).sqrMagnitude;

            //If the distance is smaller or its the first run of the algorithm
            if (i == 0 || testDistSqr < minDistSqr)
            {
                minDistSqr = testDistSqr;

                closest = c;

                ab = b - a;

                wallNormal = ab.Perp();
            }
        }


        //Update pos
        Vector3 d = ball.pos - closest;

        float dist = d.magnitude;

        //Special case if we end up exactly on the border 
        //If so we use the normal of the line segment to push out the ball
        if (dist == 0f)
        {
            d = wallNormal;
            dist = wallNormal.magnitude;
        }

        //The direction from the closest point on the wall to the ball
        Vector3 dir = d.normalized;

        //If they point in the same direction, meaning the ball is to the left of the wall
        if (Vector3.Dot(dir, wallNormal) >= 0f)
        {
            //The ball is not colliding with the wall
            if (dist > ball.radius)
            {
                return false;
            }

            //The ball is colliding with the wall, so push it in again
            ball.pos += dir * (ball.radius - dist);
        }
        //Push in the opposite direction because the ball is outside of the wall (to the right)
        else
        {
            //We have to push it dist so it ends up on the wall, and then radius so it ends up outside of the wall
            ball.pos += dir * -(ball.radius + dist);
        }


        //Update vel

        //Collisions can only change velocity components along the penetration direction
        float v = Vector3.Dot(ball.vel, dir);

        float vNew = Mathf.Abs(v) * restitution;

        //Remove the old velocity and add the new velocity
        ball.vel += dir * (vNew - v);

        return true;
    }
}
