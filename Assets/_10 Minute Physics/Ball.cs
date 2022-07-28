using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//General ball class
public class Ball
{
    public Vector3 vel;
    public Vector3 pos;

    public Transform ballTransform;

    public readonly float radius;

    public readonly float mass;


    public Ball(Transform ballTransform, float density = 1f)
    {
        this.ballTransform = ballTransform;

        pos = ballTransform.position;

        radius = ballTransform.localScale.x * 0.5f;

        mass = (4f / 3f) * Mathf.PI * Mathf.Pow(radius, 3f) * density;
    }


    public Ball(Vector3 pos, float mass)
    {
        this.pos = pos;
        this.mass = mass;
    }


    public virtual void UpdateVisualPosition()
    {
        ballTransform.position = pos;
    }

}
