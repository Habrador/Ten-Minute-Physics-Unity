using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Node : Ball
{
    public Vector3 prevPos;

    public bool isWall;


    public Node(Transform ballTransform, bool isWall = false) : base(ballTransform)
    {
        this.isWall = isWall;
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
        //v = s / t [m/s]
        vel = (pos - prevPos) / dt;
    }
}
