using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Node
{
    public float mass;

    public Vector3 pos;

    public Vector3 prevPos;

    public Vector3 vel;

    public Transform trans;


    public Node(float mass, Vector3 pos, Transform trans)
    {
        this.mass = mass;
        this.pos = pos;
        this.trans = trans;

        //Change scale of transform depending on mass
        if (mass > 0f)
        {
            //This is the scale when mass is 1
            float newScale = trans.localScale.x * mass;

            this.trans.localScale = Vector3.one * newScale;
        }
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
