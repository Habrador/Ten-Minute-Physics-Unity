using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Intersections
{
    //
    // Ray-sphere intersection
    //

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



    //
    // Point-mesh intersection
    //

    //The 6 directions we will fire the ray
    private static readonly Vector3Int[] rayDirections = {
        new Vector3Int(1, 0, 0),
        new Vector3Int(-1, 0, 0),
        new Vector3Int(0, 1, 0),
        new Vector3Int(0, -1, 0),
        new Vector3Int(0, 0, 1),
        new Vector3Int(0, 0, -1),
    };

    //minDist - we are using this method to add extra vertices to inside of the mesh. But the new vertices shouldnt be too close to old faces, so if a new vertex is closer than minDist the its ignored
    public static bool IsPointInsideMesh(Vector3[] vertices, int[] triangles, Vector3 p, float minDist = 0f)
    {
        //Cast a ray in several directions and use a majority vote to determine if the point is inside of the mesh
        int numIn = 0;

        foreach (Vector3Int rayDir in rayDirections)
        {
            Ray ray = new Ray(p, rayDir);

            if (IsRayHittingMesh(ray, vertices, triangles, out CustomHit hit))
            {
                //Is the ray hitting the triangle from the inside?
                if (Vector3.Dot(hit.normal, rayDir) > 0f)
                {
                    numIn += 1;
                }

                //If the new point is too close to a triangle, then we dont want the point
                //This is useful if we add points within the mesh and want them distributed equally
                if (minDist > 0f && hit.distance < minDist)
                {
                    return false;
                }
            }
        }

        //If at least 3 rays hits a triangle, then we assume the point is within the mesh
        return numIn > 3;
    }



    //
    // Ray-mesh intersection
    //

    //Should return location, normal, index, distance
    //We dont care about the direction the ray hits - just if the ray is hitting a triangle from either side
    public static bool IsRayHittingMesh(Ray ray, Vector3[] vertices, int[] triangles, out CustomHit bestHit)
    {
        bestHit = null;

        float smallestDistance = float.MaxValue;

        //Loop through all triangles and find the one thats the closest
        for (int i = 0; i < triangles.Length; i += 3)
        {
            Vector3 a = vertices[triangles[i + 0]];
            Vector3 b = vertices[triangles[i + 1]];
            Vector3 c = vertices[triangles[i + 2]];

            if (IsRayHittingTriangle(a, b, c, ray, out CustomHit hit))
            {
                if (hit.distance < smallestDistance)
                {
                    smallestDistance = hit.distance;

                    bestHit = hit;

                    bestHit.index = i;
                }
            }
        }

        //If at least a triangle was hit
        bool hitMesh = false;

        if (bestHit != null)
        {
            hitMesh = true;
        }

        return hitMesh;
    }



    //
    // Ray-plane intersection
    //

    //https://www.scratchapixel.com/lessons/3d-basic-rendering/ray-tracing-rendering-a-triangle/ray-triangle-intersection-geometric-solution
    public static bool IsRayHittingPlane(Ray ray, Vector3 planeNormal, Vector3 planePos, out float t)
    {
        //Add default because have to
        t = 0f;

        //First check if the plane and the ray are perpendicular
        float NdotRayDirection = Vector3.Dot(planeNormal, ray.direction);

        //If the dot product is almost 0 then ray is perpendicular to the triangle, so no itersection is possible
        if (Mathf.Abs(NdotRayDirection) < UsefulMethods.EPSILON)
        {
            return false;
        }

        //Compute d parameter using equation 2 by picking any point on the plane
        float d = -Vector3.Dot(planeNormal, planePos);

        //Compute t (equation 3)
        t = -(Vector3.Dot(planeNormal, ray.origin) + d) / NdotRayDirection;

        //Check if the plane is behind the ray
        if (t < 0)
        {
            return false;
        }

        return true;
    }



    //
    // Ray-triangle intersection
    //

    //First do ray-plane itersection and then check if the itersection point is within the triangle
    //https://www.scratchapixel.com/lessons/3d-basic-rendering/ray-tracing-rendering-a-triangle/ray-triangle-intersection-geometric-solution
    public static bool IsRayHittingTriangle(Vector3 v0, Vector3 v1, Vector3 v2, Ray ray, out CustomHit hit)
    {
        hit = null;

        //Compute plane's normal
        Vector3 v0v1 = v1 - v0;
        Vector3 v0v2 = v2 - v0;

        //No need to normalize
        Vector3 planeNormal = Vector3.Cross(v0v1, v0v2);


        //
        // Step 1: Finding P (the intersection point) by turning the triangle into a plane
        //

        if (!IsRayHittingPlane(ray, planeNormal, v0, out float t))
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
        if (Vector3.Dot(planeNormal, C) < 0f)
        {
            return false;
        }

        //Edge 1
        Vector3 edge1 = v2 - v1;
        Vector3 vp1 = P - v1;

        C = Vector3.Cross(edge1, vp1);

        //P is on the right side 
        if (Vector3.Dot(planeNormal, C) < 0f)
        {
            return false;
        }

        //Edge 2
        Vector3 edge2 = v0 - v2;
        Vector3 vp2 = P - v2;

        C = Vector3.Cross(edge2, vp2);

        //P is on the right side 
        if (Vector3.Dot(planeNormal, C) < 0f)
        {
            return false;
        }

        //This ray hits the triangle

        //Calculate the custom data we need
        hit = new CustomHit(t, P, planeNormal.normalized);

        return true;
    }
}
