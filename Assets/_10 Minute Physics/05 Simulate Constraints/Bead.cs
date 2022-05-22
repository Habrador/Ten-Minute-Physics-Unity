using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Constraints
{
    public class Bead
    {
        public Vector3 prevPos;

        public Ball ball;


        public Bead(Transform ballTransform)
        {
            this.ball = new Ball(ballTransform); 
        }


        public void UpdateVisualPosition()
        {
            this.ball.UpdateVisualPosition();
        }


        public void StartStep(float dt, Vector3 gravity)
        {
            this.ball.vel += gravity * dt;

            this.prevPos = this.ball.pos;

            this.ball.pos += this.ball.vel * dt;
        }


        //Move the bead to the closest point on the wire
        public void KeepOnWire(Vector3 center, float radius)
        {
            //Direction from center to the bead
            Vector3 dir = this.ball.pos - center;

            float length = dir.magnitude;

            if (length == 0f)
            {
                return;
            }

            dir = dir.normalized;

            //Constraint error: How far should we move the bead?
            float lambda = radius - length;

            this.ball.pos += dir * lambda;
        }


        //Calculate new velocity because the velocity we calculate during integration will explode due to gravity
        public void EndStep(float dt)
        {
            //v = s / t [m/s]
            this.ball.vel = (this.ball.pos - this.prevPos) / dt;
        }
    }
}