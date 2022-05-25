using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CustomHit
{
    //The distance along the ray to where the ray hit the object
    public float distance;

    //The transform the ray hit
    public Transform transform;

    public CustomHit(float distance, Transform transform)
    {
        this.distance = distance;
        this.transform = transform;
    }
}
