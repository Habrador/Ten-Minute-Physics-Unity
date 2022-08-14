using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PinballMachine
{
    public static class PinballCollisions
    {
        //Assume the all doesnt affect the flipper and that the restitution is zero 
        public static void HandleBallFlipperCollision(Ball ball, Flipper flipper)
        {
            //First check if they collide
            Vector3 closest = UsefulMethods.GetClosestPointOnLineSegment(ball.pos, flipper.pos, flipper.GetTip());

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



        //Similar to ball-ball collision but obstacles don't move, and the obstacle gives the ball an extra bounce velocity
        public static void HandleBallJetBumperCollision(Ball ball, JetBumper obs)
        {
            //Check if the balls are colliding (obs is assumed to be a ball as well)
            bool areColliding = BallCollisionHandling.AreBallsColliding(ball.pos, obs.pos, ball.radius, obs.radius);

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
        }
    }
}