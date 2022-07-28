using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//A node in a pendulum
public class Node
{
    public Vector3 vel;
    public Vector3 pos;
    public Vector3 prevPos;

    public float mass;

    //Is this node attached to a wall? 
    public bool isFixed;


    public Node(Vector3 pos, float mass, bool isFixed = false)
    {
        this.pos = pos;
        this.mass = mass;
    
        this.isFixed = isFixed;
    }


    public void StartStep(float dt, Vector3 gravity)
    {
        vel += gravity * dt;

        prevPos = pos;

        pos += vel * dt;
    }


    //Calculate new velocity because the velocity we calculate during integration will explode due to gravity
    public void EndStep(float dt)
    {
        //v = distance / t [m/s]
        vel = (pos - prevPos) / dt;
    }
}
