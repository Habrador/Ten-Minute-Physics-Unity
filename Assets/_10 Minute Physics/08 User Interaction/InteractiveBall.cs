using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UserInteraction
{
    public class InteractiveBall : Ball, IGrabbable
    {
        //Has the user grabbed this ball with the mouse?
        //So the ball is no longer updating with physics
        public bool isGrabbed = false;



        public InteractiveBall(Transform ballTransform) : base(ballTransform)
        {

        }


        //
        // Methods related to user interaction with the ball
        //

        public void StartGrab(Vector3 pos)
        {
            isGrabbed = true;

            this.pos = pos;

            vel = Vector3.zero;
        }


        public void MoveGrabbed(Vector3 pos)
        {
            this.pos = pos;
        }


        public void EndGrab(Vector3 pos, Vector3 vel)
        {
            isGrabbed = false;

            this.pos = pos;
            this.vel = vel;
        }

       
        public void IsRayHittingBody(Ray ray, out CustomHit hit)
        {
            hit = null;

            if (Intersections.IsRayHittingSphere(ray, pos, radius, out float hitDistance))
            {
                hit = new CustomHit(hitDistance, Vector3.zero, Vector3.zero);
            }
        }



        public Vector3 GetGrabbedPos()
        {
            return this.pos;
        }



        //
        // Methods related to the simulation of the ball physics
        //

        public void SimulateBall(float dt, Vector3 gravity)
        {
            if (isGrabbed)
            {
                return;
            }

            vel += gravity * dt;
            pos += vel * dt;
        }



        public void HandleWallCollision()
        {
            if (isGrabbed)
            {
                return;
            }

            //Make sure the ball is within the area, which is 5 m in all directions (except y)
            //If outside, reset ball and mirror velocity
            float halfSimSize = 5f - radius;

            //So the ball loses some velocity each time it hits a wall or it will continue forever
            float bounceVel = -1f * 0.8f;

            //x
            if (pos.x < -halfSimSize)
            {
                pos.x = -halfSimSize;
                vel.x *= bounceVel;
            }
            else if (pos.x > halfSimSize)
            {
                pos.x = halfSimSize;
                vel.x *= bounceVel;
            }

            //y
            if (pos.y < 0f + radius)
            {
                pos.y = 0f + radius;
                vel.y *= bounceVel;
            }
            //Sky is the limit, so no collision detection in y-positive direction

            //z
            if (pos.z < -halfSimSize)
            {
                pos.z = -halfSimSize;
                vel.z *= bounceVel;
            }
            else if (pos.z > halfSimSize)
            {
                pos.z = halfSimSize;
                vel.z *= bounceVel;
            }
        }
    }
}