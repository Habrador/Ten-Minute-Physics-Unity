using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Node : Ball
{
    public Vector3 prevPos;

    //Is this node attached to a wall? 
    public bool isFixed;


    public Node(Transform ballTransform, bool isFixed = false) : base(ballTransform)
    {
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
