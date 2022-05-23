using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PinballMachine
{
    public static class PinballCollisions
    {
        public static bool IsBallCapsuleColliding(Vector3 p, float ballRadius, Vector3 a, Vector3 b, float capsuleRadius)
        {
            bool isColliding = false;

            //Find the closest point from the ball to the line segment a-b
            Vector3 c = GetClosestPointOnLineSegment(p, a, b);

            //Add a fake ball at this point and do circle-circle collision
            float distancePointLine = (p - c).sqrMagnitude;

            float allowedDistance = ballRadius + capsuleRadius;

            if (distancePointLine < allowedDistance * allowedDistance)
            {
                isColliding = true;
            }

            return isColliding;
        }



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



        //Similar to ball-ball but obstacles don't move
        public static void HandleBallObstacleCollision(Ball ball, Obstacle obs)
        {
            //Direction from b1 to b2
            Vector3 dir = ball.pos - obs.pos;

            //The distance between the balls
            float d = dir.magnitude;

            //The balls are not colliding
            if (d == 0f || d > ball.radius + obs.radius)
            {
                return;
            }

            //Normalized direction
            dir = dir.normalized;


            //Update positions

            //The distace each ball should move so they no longer intersect 
            float corr = ball.radius + obs.radius - d;

            //Move the balls apart along the dir vector
            ball.pos += dir * corr;


            //Update velocities

            //The part of each balls velocity along dir
            float v = Vector3.Dot(ball.vel, dir);

            //Change velocity components along dir
            ball.vel += dir * (obs.pushVel - v);


            //Increase score if needed
        }



        public static void HandleBallFlipperCollision(Ball ball, Flipper flipper)
        {
            Vector3 closest = GetClosestPointOnLineSegment(ball.pos, flipper.pos, flipper.GetTip());

            Vector3 dir = ball.pos - closest;

            //The distance between the ball and the closest point on the flipper
            float d = dir.magnitude;

            //The ball is not colliding
            if (d == 0f || d > ball.radius + flipper.radius)
            {
                return;
            }

            dir = dir.normalized;

            //Move the ball outside of the flipper
            float corr = ball.radius + flipper.radius - d;

            ball.pos += dir * corr;

            //Update velocity

            //Calculate the velocity of the flipper at the contact point

            //Vector from rotation center to contact point
            Vector3 radius = closest;

            radius += dir * flipper.radius;

            radius -= flipper.pos;

            //Contact velocity by turning it 90 degress and scaling with angular velocity
            Vector3 surfaceVel = radius.Perp() * flipper.currentAngularVel;

            //The flipper can only modify the component of the balls velocity along the penetration direction dir
            float v = Vector3.Dot(ball.vel, dir);

            float vNew = Vector3.Dot(surfaceVel, dir);

            ball.vel += dir * (vNew - v);
        }



        public static void HandleBallBorderCollision(Ball ball, List<Vector3> border, float restitution)
        {
            //We need at least a triangle
            if (border.Count < 3)
            {
                return;
            }

            //Find closest segment and related data to that segment
            //Vector3 d = Vector3.zero;
            Vector3 closest = Vector3.zero;
            Vector3 ab = Vector3.zero;
            Vector3 normal = Vector3.zero;

            float minDist = 0f;

            //The border should include both the start and end points which are at the same location
            for (int i = 0; i < border.Count - 1; i++)
            {
                Vector3 a = border[i];
                Vector3 b = border[i + 1];
                Vector3 c = GetClosestPointOnLineSegment(ball.pos, a, b);

                //d = ball.pos - c;

                float testDist = (ball.pos - c).magnitude;

                //Always run this if the first time so we get data to compare with
                //Which is maye more efficient than setting some large values in the beginning 
                if (i == 0 || testDist < minDist)
                {
                    minDist = testDist;

                    closest = c;

                    ab = b - a;

                    normal = ab.Perp();
                }

            }

            //Push out
            Vector3 d = ball.pos - closest;

            float dist = d.magnitude;

            //Special case if we end up exactly on the border 
            if (dist == 0f)
            {
                d = normal;
                dist = normal.magnitude;
            }

            d = d.normalized;

            //If they point in the same direction 
            if (Vector3.Dot(d, normal) >= 0f)
            {
                if (dist > ball.radius)
                {
                    return;
                }

                ball.pos += d * (ball.radius - dist);
            }
            //Push in the opposite direction
            else
            {
                ball.pos += d * -(ball.radius + dist);
            }

            //Update vel

            //Collisions can only change velocity components along the penetration direction
            float v = Vector3.Dot(ball.vel, d);

            float vNew = Mathf.Abs(v) * restitution;

            ball.vel += d * (vNew - v);
        }
    }
}