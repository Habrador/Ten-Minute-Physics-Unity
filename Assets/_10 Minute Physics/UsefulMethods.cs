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



}
