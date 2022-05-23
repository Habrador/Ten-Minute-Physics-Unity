using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PinballMachine
{
    public static class PinballCollisions
    {
        public static Vector3 GetClosestPointOnLineSegment(Vector3 p, Vector3 a, Vector3 b)
        {
            //Special case when a = b, meaning that the the denominator is 0 and we get an error
            Vector3 ab = b - a;

            float denominator = Vector3.Dot(ab, ab);

            //If a = b, then return just one of the points
            if (denominator == 0f)
            {
                return a;
            }

            //Find the closest point from p to the line segment a-b
            float t = Vector3.Dot(p - a, ab) / denominator;

            //Clamp so we always get a point on the line segment
            t = Mathf.Clamp01(t);

            //Find the coordinate of this point
            Vector3 c = a + t * ab;

            return c;
        }



        //Similar to ball-ball collision but obstacles don't move
        public static void HandleBallObstacleCollision(Ball ball, Obstacle obs)
        {
            //Check if the balls are colliding (obs is assumed to be a ball as well)
            bool areColliding = CustomPhysics.AreBallsColliding(ball.pos, obs.pos, ball.radius, obs.radius);

            if (!areColliding)
            {
                return;
            }


            //Update position

            //Direction from obstacle to ball
            Vector3 dir = ball.pos - obs.pos;

            //The actual distancae
            float d = dir.magnitude;

            //Normalized direction
            dir = dir.normalized;

            //Obstacle if fixed so this is the distace the ball should move to no longer intersect
            //Which is why theres no 0.5 like inn ball-ball collision
            float corr = ball.radius + obs.radius - d;

            //Move the ball along the dir vector
            ball.pos += dir * corr;


            //Update velocity

            //The part of the balls velocity along dir which is the velocity being affected
            float v = Vector3.Dot(ball.vel, dir);

            //Change velocity components along dir by first removing the old velocity
            //...then add the new velocity from the jet bumpers which gives the ball a push 
            ball.vel += dir * (obs.pushVel - v);


            //Increase score if needed
        }



        public static void HandleBallFlipperCollision(Ball ball, Flipper flipper)
        {
            //First check if they collide
            Vector3 closest = GetClosestPointOnLineSegment(ball.pos, flipper.pos, flipper.GetTip());

            Vector3 dir = ball.pos - closest;

            //The distance sqr between the ball and the closest point on the flipper
            float dSqr = dir.sqrMagnitude;

            float minAlloweDistance = ball.radius + flipper.radius;

            //The ball is not colliding
            //Square minAlloweDistance because we are using distance square which is faster
            if (dSqr == 0f || dSqr > minAlloweDistance * minAlloweDistance)
            {
                return;
            }


            //Update position

            //The distance between the ball and the closest point on the flipper
            float d = dir.magnitude;

            dir = dir.normalized;

            //Move the ball outside of the flipper
            float corr = ball.radius + flipper.radius - d;

            ball.pos += dir * corr;


            //Update velocity

            //Calculate the velocity of the flipper at the contact point

            //Vector from rotation center to contact point
            Vector3 radius = (closest - flipper.pos) + dir * flipper.radius;

            //Contact velocity by turning the vector 90 degress and scaling with angular velocity
            Vector3 surfaceVel = radius.Perp() * flipper.currentAngularVel;

            //The flipper can only modify the component of the balls velocity along the penetration direction dir
            float v = Vector3.Dot(ball.vel, dir);

            float vNew = Vector3.Dot(surfaceVel, dir);

            //Remove the balls old velocity and add the new velocity
            ball.vel += dir * (vNew - v);
        }



        //Assumer the border is counter-clockwise
        //The first point on the border also has to be included at the end of the list
        public static void HandleBallBorderCollision(Ball ball, List<Vector3> border, float restitution)
        {
            //We need at least a triangle (the start and end are the same point, thus the 4)
            if (border.Count < 4)
            {
                return;
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
                Vector3 c = GetClosestPointOnLineSegment(ball.pos, a, b);

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
                    return;
                }

                //The ball is colliding with the wall, so push it in again
                ball.pos += dir * (ball.radius - dist);
            }
            //Push in the opposite direction because the ball is outside of the wall (to the right)
            else
            {
                //We have to push it dist so it ends up on the wall, and then radius so it ends up outside of the wall
                ball.pos += -dir * (ball.radius + dist);
            }


            //Update vel

            //Collisions can only change velocity components along the penetration direction
            float v = Vector3.Dot(ball.vel, d);

            float vNew = Mathf.Abs(v) * restitution;

            //Remove the old velocity and add the new velocity
            ball.vel += d * (vNew - v);
        }
    }
}