using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements.Experimental;
using static UnityEngine.Rendering.HableCurve;

//A collection of standardized methods
public static class UsefulMethods
{
    //The value we use to avoid floating point precision issues
    //http://sandervanrossen.blogspot.com/2009/12/realtime-csg-part-1.html
    //Unity has a built-in Mathf.Epsilon;
    //But it's better to use our own so we can test different values
    public const float EPSILON = 0.00001f;

    //Useful to define which space 2d is in 3d
    public enum Space { XZ, XY }


    //
    // Clamp list indices
    //

    //Will even work if index is larger/smaller than listSize, so can loop multiple times
    public static int ClampListIndex(int index, int listSize)
    {
        index = ((index % listSize) + listSize) % listSize;

        return index;
    }



    //
    // Generate a small random value 
    //

    //Is used to avoid vertices to have for example the exact same x coordinate which may cause bugs
    public static float RandEps()
    {
        float eps = 0.0001f;

        //Generate a random value between [-eps, eps]
        float randomValue = -eps + 2f * Random.Range(0f, 1f) * eps;

        return randomValue;
    }



    //
    // Generate coordinates on the edge of a circle
    //

    //The circle is 2d but coordinates are in 3d x,z so y is 0
    //Is adding the start position twice, so if segments is 10 you get 11 vertices
    public static List<Vector3> GetCircleSegments_XZ(Vector3 circleCenter, float radius, int segments)
    {
        List<Vector3> vertices = GetCircleSegments(circleCenter, radius, segments, Space.XZ);

        return vertices;
    }

    //Generate vertices on the circle circumference in xy space
    public static List<Vector3> GetCircleSegments_XY(Vector3 circleCenter, float radius, int segments)
    {
        List<Vector3> vertices = GetCircleSegments(circleCenter, radius, segments, Space.XY);

        return vertices;
    }


    private static List<Vector3> GetCircleSegments(Vector3 circleCenter, float radius, int segments, Space space_2d)
    {
        List<Vector3> vertices = new();

        float angleStep = 360f / (float)segments;
        float angle = 0f;

        for (int i = 0; i < segments + 1; i++)
        {
            float x = radius * Mathf.Cos(angle * Mathf.Deg2Rad);
            float y = radius * Mathf.Sin(angle * Mathf.Deg2Rad);

            Vector3 vertex = Vector3.zero;

            if (space_2d == Space.XY)
            {
                vertex = new Vector3(x, y, 0f) + circleCenter;
            }
            else if (space_2d == Space.XZ)
            {
                vertex = new Vector3(x, 0f, y) + circleCenter;
            }

            vertices.Add(vertex);

            angle += angleStep;
        }

        return vertices;
    }



    //
    // The closest point on a ray from a vertex
    //

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



    //
    // The closest point on a line segment from a point
    //
    
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



    // 
    // Calculate the center of a sphere given 4 points on the surface of the sphere
    //

    //http://rodolphe-vaillant.fr/entry/127/find-a-tetrahedron-circumcenter
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
}
