using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Constraints
{
    public class Bead
    {
        public float radius;

        public float mass;

        public Vector3 pos;

        public Vector3 prevPos;

        public Vector3 vel;

        public Transform transform;


        public Bead(Transform transform)
        {
            this.mass = transform.localScale.x * 5f;
            this.pos = transform.position;
            this.transform = transform;
            this.radius = transform.localScale.x * 0.5f;

            //DONT change scale, we are already doing that when we create the bead
            //Change scale depending on mass
            //float newScale = trans.localScale.x * mass;

            //this.trans.localScale = Vector3.one * newScale;
        }


        public void StartStep(float dt, Vector3 gravity)
        {
            this.vel += gravity * dt;

            this.prevPos = pos;

            this.pos += this.vel * dt;
        }


        //Move the bead to the closest point on the wire
        public void KeepOnWire(Vector3 center, float radius)
        {
            //Direction from center to the bead
            Vector3 dir = this.pos - center;

            float length = dir.magnitude;

            if (length == 0f)
            {
                return;
            }

            dir = dir.normalized;

            //Constraint error: How far should we move the bead?
            float lambda = radius - length;

            this.pos += dir * lambda;
        }


        //Calculate new velocity because the velocity we calculate during integration will explode due to gravity
        public void EndStep(float dt)
        {
            //v = s / t [m/s]
            this.vel = (this.pos - this.prevPos) / dt;
        }
    }
}