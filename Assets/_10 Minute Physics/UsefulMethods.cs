using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class UsefulMethods
{
    //https://gdbooks.gitbooks.io/3dcollisions/content/Chapter1/closest_point_on_ray.html
    public static Vector3 GetClosestPointOnRay(Vector3 p, Ray ray)
    {
        Vector3 a = ray.origin;
        Vector3 b = ray.origin + ray.direction;
    
        //Special case when a = b, meaning that the the denominator is 0 and we get an error
        Vector3 ab = b - a;

        float denominator = Vector3.Dot(ab, ab);

        //If a = b, then return just one of the points
        if (denominator == 0f)
        {
            return a;
        }

        //Find the closest point from p to the line segment a-b
        float t = Vector3.Dot(p - a, ab) / denominator;

        //Clamp t to not be behind the ray 
        t = Mathf.Max(0f, t);

        //Find the coordinate of this point
        Vector3 c = a + t * ab;

        return c;
    }


    
    //Similar to ray but we clamp t to be on the line segment
    public static Vector3 GetClosestPointOnLineSegment(Vector3 p, Vector3 a, Vector3 b)
    {
        //Special case when a = b, meaning that the the denominator is 0 and we get an error
        Vector3 ab = b - a;

        float denominator = Vector3.Dot(ab, ab);

        //If a = b, then return just one of the points
        if (denominator == 0f)
        {
            return a;
        }

        //Find the closest point from p to the line segment a-b
        float t = Vector3.Dot(p - a, ab) / denominator;

        //Clamp so we always get a point on the line segment
        t = Mathf.Clamp01(t);

        //Find the coordinate of this point
        Vector3 c = a + t * ab;

        return c;
    }



    //Ray-sphere collision detection
    //https://www.lighthouse3d.com/tutorials/maths/ray-sphere-intersection/
    public static bool IsRayHittingSphere(Ray ray, Vector3 sphereCenter, float radius, out float hitDistance)
    {
        Vector3 p = ray.origin;
        Vector3 dir = ray.direction;

        Vector3 c = sphereCenter;
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
            hitDistance = 0f;

            return false;
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

            hitDistance = dist_i1;

            return true;
        }
    }
}
