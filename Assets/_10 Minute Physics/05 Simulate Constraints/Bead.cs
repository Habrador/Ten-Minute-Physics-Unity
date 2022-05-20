using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bead
{
    //public float radius;

    public float mass;

    public Vector3 pos;

    public Vector3 prevPos;

    public Vector3 vel;

    public Transform trans;


    public Bead(float mass, Vector3 pos, Transform trans = null)
    {
        this.mass = mass;
        this.pos = pos;
        this.trans = null;
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

        //Constraint error: How far shiuld we move the bead?
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
