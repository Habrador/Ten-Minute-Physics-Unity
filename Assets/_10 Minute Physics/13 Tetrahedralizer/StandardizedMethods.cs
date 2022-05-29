using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class StandardizedMethods
{
    // 
    // Calculate the center of a circumsphere of a tetrahedron given 4 points on the surface
    //
    public static Vector3 GetCircumCenter(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
    {
        Vector3 b = p1 - p0;
        Vector3 c = p2 - p0;
        Vector3 d = p3 - p0;

        float det = 2f * (b.x * (c.y * d.z - c.z * d.y) - b.y * (c.x * d.z - c.z * d.x) + b.z * (c.x * d.y - c.y * d.x));

        if (det == 0f)
        {
            return p0;
        }
        else
        {
            Vector3 v = Vector3.zero;

            v += Vector3.Cross(c, d) * Vector3.Dot(b, b);
            v += Vector3.Cross(d, b) * Vector3.Dot(c, c);
            v += Vector3.Cross(b, c) * Vector3.Dot(d, d);

            v /= det;

            return p0 + v;
        }
    }



    //
    // Is a point inside of a mesh?
    //

    private static Vector3Int[] dirs = {
        new Vector3Int(1, 0, 0),
        new Vector3Int(-1, 0, 0), //Changed -1 from y to x because I think thats a bug in the original code
        new Vector3Int(0, 1, 0),
        new Vector3Int(0, -1, 0),
        new Vector3Int(0, 0, 1),
        new Vector3Int(0, 0, -1),
    };

    public static bool IsInside(Mesh tree, Vector3 p, float minDist = 0f)
    {
        //Cast a ray in several directions and use a majority vote
        int numIn = 0;

        for (int i = 0; i < dirs.Length; i++)
        {
            Ray ray = new Ray(p, dirs[i]);
        
            if (IsRayHittingMesh(ray, out CustomHit hit))
            {
                if (Vector3.Dot(hit.normal, dirs[i]) > 0f)
                {
                    numIn += 1;
                }
                
                //What is going on here? Maybe related to floating point precision issues???  
                if (minDist > 0f && hit.distance < minDist)
                {
                    return false;
                }
            }
        }

        return numIn > 3;
    }



    //
    // Custom raycast
    //

    public static bool IsRayHittingMesh(Ray ray, out CustomHit hit)
    {
        hit = null;

        return false;

        //Should return location, normal, index, distance

        //if (UsefulMethods.IsRayHittingSphere(ray, pos, radius, out float hitDistance))
        //{
        //    hit = new CustomHit(hitDistance, this);
        //}
    }
}
