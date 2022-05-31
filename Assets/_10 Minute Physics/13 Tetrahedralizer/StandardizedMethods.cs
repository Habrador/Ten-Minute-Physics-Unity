using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class StandardizedMethods
{
    //The value we use to avoid floating point precision issues
    //http://sandervanrossen.blogspot.com/2009/12/realtime-csg-part-1.html
    //Unity has a built-in Mathf.Epsilon;
    //But it's better to use our own so we can test different values
    public const float EPSILON = 0.00001f;


    // 
    // Calculate the center of a sphere given 4 points on the surface of the sphere
    //

    //http://rodolphe-vaillant.fr/entry/127/find-a-tetrahedron-circumcenter
    public static Vector3 GetCircumcenter(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
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

    //Is needed because we have to fill the mesh with extra vertices so it's no longer hollow

    private static Vector3Int[] dirs = {
        new Vector3Int(1, 0, 0),
        new Vector3Int(-1, 0, 0), //Changed -1 from y to x because I think thats a bug in the original code
        new Vector3Int(0, 1, 0),
        new Vector3Int(0, -1, 0),
        new Vector3Int(0, 0, 1),
        new Vector3Int(0, 0, -1),
    };

    //minDist - we are using this method to add extra vertices to inside of the mesh. But the new vertices shouldnt be too close to old faces, so if a new vertex is closer than minDist the its ignored
    public static bool IsPointInsideMesh(CustomMesh tree, Vector3 p, float minDist = 0f)
    {
        //Cast a ray in several directions and use a majority vote
        int numIn = 0;

        for (int i = 0; i < dirs.Length; i++)
        {
            Ray ray = new Ray(p, dirs[i]);
        
            if (IsRayHittingMesh(ray, tree, out CustomHit hit))
            {
                if (Vector3.Dot(hit.normal, dirs[i]) > 0f)
                {
                    numIn += 1;
                }
                
                //If the new vertex is too close to a mesh triangle, then we dont want it
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

    public static bool IsRayHittingMesh(Ray ray, CustomMesh tree, out CustomHit bestHit)
    {
        bestHit = null;

        bool isHittingMesh = false;

        //Should return location, normal, index, distance

        //The normal of the triangle p1-p2-p3 (oriented counter-clockwise) is:
        //Vector3.Cross(p2-p1, p3-p1).normalized

        //if (UsefulMethods.IsRayHittingSphere(ray, pos, radius, out float hitDistance))
        //{
        //    hit = new CustomHit(hitDistance, this);
        //}

        //We dont care about the normal of the triangle, just if the ray is hitting a triangle from either side

        Vector3[] verts = tree.verts;
        int[] tris = tree.tris;

        float smallestDistance = float.MaxValue;

        //Foreach triangle
        for (int i = 0; i < tris.Length; i += 3)
        {
            Vector3 a = verts[i + 0];
            Vector3 b = verts[i + 1];
            Vector3 c = verts[i + 2];

            if (IsRayHittingTriangle(a, b, c, ray, out CustomHit hit))
            {
                hit.index = i;

                if (hit.distance < smallestDistance)
                {
                    smallestDistance = hit.distance;

                    bestHit = hit;
                }
            }
        }



        return isHittingMesh;
    }



    //
    // Ray triangle intersection
    //

    //https://www.scratchapixel.com/lessons/3d-basic-rendering/ray-tracing-rendering-a-triangle/ray-triangle-intersection-geometric-solution
    public static bool IsRayHittingTriangle(Vector3 v0, Vector3 v1, Vector3 v2, Ray ray, out CustomHit hit)
    {
        hit = null;
    
        //Compute plane's normal
        Vector3 v0v1 = v1 - v0;
        Vector3 v0v2 = v2 - v0;
        
        //No need to normalize
        Vector3 N = Vector3.Cross(v0v1, v0v2); 
        
        //float area2 = N.magnitude;


        //
        // Step 1: Finding P (the intersection point) by turning the triangle into a plane
        //

        float NdotRayDirection = Vector3.Dot(N, ray.direction);

        //If the dot product is almost 0 the ray is parallell to the triangle
        if (Mathf.Abs(NdotRayDirection) < StandardizedMethods.EPSILON)
        {
            return false;  
        }
            
        //Compute d parameter using equation 2 by picking any point on the triangle, such as v0
        float d = -Vector3.Dot(N, v0); //Not sure if - should be before N or outside. It is outside in the example, but also missing completely in the same example

        //Compute t (equation 3)
        float t = -(Vector3.Dot(N, ray.origin) + d) / NdotRayDirection;

        //Check if the triangle is behind the ray
        if (t < 0)
        {
            return false;  
        }

        //Compute the intersection point using equation 1
        Vector3 P = ray.origin + t * ray.direction;


        //
        // Step 2: inside-outside test
        //

        //Vector perpendicular to triangle's plane
        Vector3 C;   

        //Edge 0
        Vector3 edge0 = v1 - v0;
        Vector3 vp0 = P - v0;
        
        C = Vector3.Cross(edge0, vp0);

        //P is on the right side 
        if (Vector3.Dot(N, C) < 0f)
        {
            return false;
        }

        //Edge 1
        Vector3 edge1 = v2 - v1;
        Vector3 vp1 = P - v1;
        
        C = Vector3.Cross(edge1, vp1);

        //P is on the right side 
        if (Vector3.Dot(N, C) < 0f)
        {
            return false;
        }

        //Edge 2
        Vector3 edge2 = v0 - v2;
        Vector3 vp2 = P - v2;
        
        C = Vector3.Cross(edge2, vp2);

        //P is on the right side 
        if (Vector3.Dot(N, C) < 0f)
        { 
            return false;
        }

        //This ray hits the triangle

        //Calculate the custom data we need
        hit = new CustomHit(t, P, N.normalized);

        return true;  
    }
}
