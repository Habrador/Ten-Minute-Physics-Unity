using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CustomHit
{
    //The distance along the ray to where the ray hit the object
    public float distance;

    //The ball the ray hit, which is better than transform because we can get the transform from the ball script
    public InteractiveBall ball;

    public CustomHit(float distance, InteractiveBall ball)
    {
        this.distance = distance;
        this.ball = ball;
    }
}
