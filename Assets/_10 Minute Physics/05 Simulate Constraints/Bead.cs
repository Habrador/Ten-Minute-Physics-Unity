using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Constraints
{
    public class Bead : Ball
    {
        public Vector3 prevPos;


        public Bead(Transform ballTransform) : base(ballTransform)
        {
                
        }


        public void StartStep(float dt, Vector3 gravity)
        {
            vel += gravity * dt;

            prevPos = pos;

            pos += vel * dt;
        }


        //Move the bead to the closest point on the wire
        public void KeepOnWire(Vector3 center, float radius)
        {
            //Direction from center to the bead
            Vector3 dir = pos - center;

            float length = dir.magnitude;

            if (length == 0f)
            {
                return;
            }

            dir = dir.normalized;

            //Constraint error: How far should we move the bead?
            float lambda = radius - length;

            pos += dir * lambda;
        }


        //Calculate new velocity because the velocity we calculate during integration will explode due to gravity
        public void EndStep(float dt)
        {
            //v = s / t [m/s]
            vel = (pos - prevPos) / dt;
        }
    }
}