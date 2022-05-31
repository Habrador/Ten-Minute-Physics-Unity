using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//General triangle class used to display triangles on the screen
public class Triangle
{
    public Vector3 a, b, c;
    public Vector3 normal;
    public bool isIntersecting;


    public Triangle(Vector3 a, Vector3 b, Vector3 c, bool isIntersecting = false)
    {
        this.a = a;
        this.b = b;
        this.c = c;
        this.isIntersecting = isIntersecting;

        normal = CalculateNormal(a, b, c);
    }


    private Vector3 CalculateNormal(Vector3 a, Vector3 b, Vector3 c)
    {
        //The normal of the triangle a-b-c (oriented counter-clockwise) is:
        Vector3 normal = Vector3.Cross(b - a, c - a).normalized;

        return normal;
    }


    public Vector3 GetCenter => (a + b + c) / 3f;

}
