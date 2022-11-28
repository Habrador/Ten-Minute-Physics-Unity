using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Custom Vector3 with double precision instead of float
public struct Vector3Double
{
    public double x, y, z;

    public Vector3Double(double x, double y, double z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }



    //Vector operations
    public Vector3Double Normalized => this / Magnitude;
    
    public double Magnitude => System.Math.Sqrt(x * x + y * y + z * z);

    public Vector3 ToVector3 => new ((float)x, (float)y, (float)z);



    //Operator overloads
    public static Vector3Double operator *(Vector3Double vec, double a)
    {
        return new (vec.x * a, vec.y * a, vec.z * a);
    }

    public static Vector3Double operator *(double a, Vector3Double vec)
    {
        return new(vec.x * a, vec.y * a, vec.z * a);
    }

    public static Vector3Double operator /(Vector3Double vec, double a)
    {
        return new(vec.x / a, vec.y / a, vec.z / a);
    }

    public static Vector3Double operator +(Vector3Double vecA, Vector3Double vecB)
    {
        return new (vecA.x + vecB.x, vecA.y + vecB.y, vecA.z + vecB.z);
    }

    public static Vector3Double operator -(Vector3Double vecA, Vector3Double vecB)
    {
        return new(vecA.x - vecB.x, vecA.y - vecB.y, vecA.z - vecB.z);
    }
}



