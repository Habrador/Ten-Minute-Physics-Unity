using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class BallCollisionHandling
{
    //Are two balls colliding in 3d space?
    //If they are at the same pos we assume they are NOT colliding because it makes calculations simpler
    public static bool AreBallsColliding(Vector3 p1, Vector3 p2, float r1, float r2)
    {
        bool areColliding = true;

        //The distance sqr between the balls
        float distSqr = (p2- p1).sqrMagnitude;

        float minAllowedDistance = r1 + r2;

        //The balls are not colliding (or they are exactly at the same position)
        //Square minAllowedDistance because we are using distance Square, which is faster 
        //They might be at the same position if we check if the ball is colliding with itself, which might be faster than checking if the other ball is not the same as the ball 
        if (distSqr == 0f || distSqr > minAllowedDistance * minAllowedDistance)
        {
            areColliding = false;
        }

        return areColliding;
    }



    //2d space using components
    public static bool AreDiscsColliding(Disc disc_1, Disc disc_2)
    {
        bool areColliding = true;

        //Direction from disc 1 to disc 2
        float dir_x = disc_2.x - disc_1.x;
        float dir_y = disc_2.y - disc_1.y;

        float distSqr = dir_x * dir_x + dir_y * dir_y;

        float minAllowedDistance = disc_1.radius + disc_2.radius;

        //The balls are not colliding (or they are exactly at the same position)
        //Square minAllowedDistance because we are using distance Square, which is faster 
        //They might be at the same position if we check if the ball is colliding with itself, which might be faster than checking if the other ball is not the same as the ball 
        if (distSqr == 0f || distSqr > minAllowedDistance * minAllowedDistance)
        {
            areColliding = false;
        }

        return areColliding;
    }



    //Two balls are colliding in 3d space
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
        float distance = dir.magnitude;

        Vector3 dir_normalized = dir.normalized;

        //The distace each ball should move so they no longer intersect 
        float corr = (b1.radius + b2.radius - distance) * 0.5f;

        //Move the balls apart along the dir vector
        b1.pos += dir_normalized * -corr; //-corr because dir goes from b1 to b2
        b2.pos += dir_normalized * corr;


        //Update velocities

        //Collisions can only change velocity components along the penetration direction

        //The part of each balls velocity along dir (penetration direction)
        //The velocity is now in 1D making it easier to use standardized physics equations
        float v1 = Vector3.Dot(b1.vel, dir_normalized);
        float v2 = Vector3.Dot(b2.vel, dir_normalized);

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
        b1.vel += dir_normalized * (new_v1 - v1);
        b2.vel += dir_normalized * (new_v2 - v2);
    }



    //Two discs are colliding in 2d space, using components
    public static bool HandleDiscDiscCollision(Disc disc_1, Disc disc_2, float restitution)
    {
        bool areColliding = true;

        //Check if the balls are colliding
        areColliding = AreDiscsColliding(disc_1, disc_2);

        if (!areColliding)
        {
            areColliding = false;
        
            return areColliding;
        }

        //Direction from ball 1 to ball 2
        float dir_x = disc_2.x - disc_1.x;
        float dir_y = disc_2.y - disc_1.y;

        //The distance between the balls
        float distance = Mathf.Sqrt(dir_x * dir_x + dir_y * dir_y);

        //Normalized direction
        float normalized_dir_x = dir_x / distance;
        float normalized_dir_y = dir_y / distance;


        //Update positions so the spheres dont overlap
        float overlap = disc_1.radius + disc_2.radius - distance;

        overlap *= 0.5f;

        //How far each ball should move and in which direction to no longer overlap
        //0.5 because each ball should move half of the overlap
        float corr_x = overlap * normalized_dir_x;
        float corr_y = overlap * normalized_dir_y;
        
        //-corr because dir goes from sphere 1 to sphere 2
        disc_1.x -= corr_x;
        disc_1.y -= corr_y;
        disc_2.x += corr_x;
        disc_2.y += corr_y;


        //Update velocities after collision

        //Collisions can only change velocity components along the penetration direction
        
        //The part of each balls velocity along dir (penetration direction)
        //The velocity is now in 1D making it easier to use standardized physics equations
        
        //Dot product
        float v1 = disc_1.vx * normalized_dir_x + disc_1.vy * normalized_dir_y;
        float v2 = disc_2.vx * normalized_dir_x + disc_2.vy * normalized_dir_y;

        float m1 = disc_1.mass;
        float m2 = disc_2.mass;

        //If we assume the objects are stiff we can calculate the new velocities after collision
        float new_v1 = (m1 * v1 + m2 * v2 - m2 * (v1 - v2) * restitution) / (m1 + m2);
        float new_v2 = (m1 * v1 + m2 * v2 - m1 * (v2 - v1) * restitution) / (m1 + m2);

        //Change velocity components along dir
        //Subtract the old velocity because it doesnt exist anymore and then add the new velocity
        disc_1.vx += normalized_dir_x * (new_v1 - v1);
        disc_1.vy += normalized_dir_y * (new_v1 - v1);
        disc_2.vx += normalized_dir_x * (new_v2 - v2);
        disc_2.vy += normalized_dir_y * (new_v2 - v2);
        

        /*
        //From tutorial which might be more optimized but more difficult to understand
        //Relative velocity
        float vrx = disc_2.vx - disc_1.vx;
        float vry = disc_2.vy - disc_1.vy;

        //Velocity normal?
        float vn = vrx * normalized_dir_x + vry * normalized_dir_y;

        if (vn > 0f)
        {
            return areColliding;
        }

        float j = -(1f + restitution) * vn / (1f / disc_1.mass + 1f / disc_2.mass);

        disc_1.vx -= (j * normalized_dir_x) / disc_1.mass;
        disc_1.vy -= (j * normalized_dir_y) / disc_1.mass;
        disc_2.vx += (j * normalized_dir_x) / disc_2.mass;
        disc_2.vy += (j * normalized_dir_y) / disc_2.mass;
        */

        return areColliding;
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
