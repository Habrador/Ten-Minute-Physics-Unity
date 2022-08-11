using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//A node in a pendulum with double precision
public class NodeDouble
{
    public Vector3Double vel;
    public Vector3Double pos;
    public Vector3Double prevPos;

    public double mass;

    //Is this node attached to a wall? 
    public bool isFixed;


    public NodeDouble(Vector3Double pos, double mass, bool isFixed = false)
    {
        this.pos = pos;
        this.mass = mass;
    
        this.isFixed = isFixed;
    }


    public void StartStep(double dt, Vector3Double gravity)
    {
        vel += gravity * dt;

        prevPos = pos;

        pos += vel * dt;
    }


    //Calculate new velocity because the velocity we calculate during integration will explode due to gravity
    public void EndStep(double dt)
    {
        //v = distance / t [m/s]
        vel = (pos - prevPos) / dt;
    }
}
