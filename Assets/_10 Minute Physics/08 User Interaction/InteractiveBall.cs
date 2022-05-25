using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractiveBall : Ball
{
    //Has the user grabbed this ball with the mouse?
    public bool isGrabbed = false;



    public InteractiveBall(Transform ballTransform) : base(ballTransform)
    {

    }


    //
    // Methods related to user interaction with the ball
    //

    public void StartGrab(Vector3 pos)
    {
        isGrabbed = true;

        this.pos = pos;
    }


    public void MoveGrabbed(Vector3 pos)
    {
        this.pos = pos;
    }


    public void EndGrab(Vector3 pos, Vector3 vel)
    {
        isGrabbed = false;

        this.pos = pos;
        this.vel = vel;
    }


    //Ray-sphere collision detection
    //https://www.lighthouse3d.com/tutorials/maths/ray-sphere-intersection/
    public void IsRayHitting(Ray ray, out CustomHit hit)
    {
        hit = null;

        Vector3 p = ray.origin;
        Vector3 dir = ray.direction;

        Vector3 c = pos;
        float r = radius;

        //This is the vector from p to c
        Vector3 vpc = c - p;  

        //Assume the ray starts outside of the sphere

        //The closest point on the ray from the sphere center
        Vector3 pc = UsefulMethods.GetClosestPointOnRay(c, ray);

        //Debug.DrawRay(pc, Vector3.up * 5f, Color.white, 20f);

        //There is no intersection if the distance between the center of the sphere and the closest point on the ray is larger than the radius of the sphere  
        if ((pc - c).sqrMagnitude > r * r)
        {
            //Debug.Log("No intersection from within algorithm");
        
            return;
        }
        else
        {
            //Distance from pc to i1 (itersection point 1) by using the triangle pc - c - i1
            float dist_i1_pc = Mathf.Sqrt(Mathf.Pow(radius, 2f) - Mathf.Pow((pc - c).magnitude, 2f));

            //The distance to the first intersection point (there are two because the ray is also exiting the sphere) from the start of the ray
            //But we don't care about exiting the sphere becase that intersection point is further away 
            float dist_i1 = 0f;

            //Ray start is outside sphere	
            if (vpc.sqrMagnitude > r * r) 
            {
                dist_i1 = (pc - p).magnitude - dist_i1_pc;
            }
            //Ray start is inside sphere
            else
            {
                dist_i1 = (pc - p).magnitude + dist_i1_pc;
            }

            //Vector3 intersection = p + dir * dist_i1;

            //Debug.DrawRay(intersection, Vector3.up * 5f, Color.white, 20f);

            //float distance = (ray.origin - intersection).magnitude;

            hit = new CustomHit(dist_i1, ballTransform);
        }
    }



    //
    // Methods related to the simulation of the ball physics
    //

    public void SimulateBall(float dt, Vector3 gravity)
    {
        if (isGrabbed)
        {
            return;
        }
    
        vel += gravity * dt;
        pos += vel * dt;
    }



    public void HandleWallCollision()
    {
        if (isGrabbed)
        {
            return;
        }

        //Make sure the ball is within the area, which is 5 m in all directions (except y)
        //If outside, reset ball and mirror velocity
        float halfSimSize = 5f - radius;

        //x
        if (pos.x < -halfSimSize)
        {
            pos.x = -halfSimSize;
            vel.x *= -1f;
        }
        else if (pos.x > halfSimSize)
        {
            pos.x = halfSimSize;
            vel.x *= -1f;
        }

        //y
        if (pos.y < 0f + radius)
        {
            pos.y = 0f + radius;
            vel.y *= -1f;
        }
        //Sky is the limit, so no collision detection in y-positive direction

        //z
        if (pos.z < -halfSimSize)
        {
            pos.z = -halfSimSize;
            vel.z *= -1f;
        }
        else if (pos.z > halfSimSize)
        {
            pos.z = halfSimSize;
            vel.z *= -1f;
        }
    }
}
