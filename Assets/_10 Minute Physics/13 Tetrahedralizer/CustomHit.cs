using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CustomHit
{
    //The distance along the ray to where the ray hit the object
    public float distance;

    public Vector3 location;
    public Vector3 normal; 

    //What is index? Start pos of triangle? 
    public int index;

    public CustomHit(float distance, Vector3 location, Vector3 normal, int index = -1)
    {
        this.distance = distance;
        this.location = location;
        this.normal = normal;
        this.index = index;
    }
}
