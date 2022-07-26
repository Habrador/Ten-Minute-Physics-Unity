using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Node : Ball
{
    public Vector3 prevPos;

    //Is this node attached to a wall? 
    public bool isFixed;

    private Arm arm;


    public Node(Transform ballTransform, Arm arm = null, bool isFixed = false) : base(ballTransform)
    {
        this.isFixed = isFixed;

        this.arm = arm;
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


    public void UpdateArmPosition(Vector3 p1, Vector3 p2, bool isOffset)
    {
        if (arm == null)
        {
            return;
        }
    
        arm.UpdateSection(p1, p2, isOffset);
    }
}
