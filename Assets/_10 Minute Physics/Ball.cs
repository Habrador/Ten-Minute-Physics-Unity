using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ball : MonoBehaviour
{
    public Vector3 vel;
    public Vector3 pos;

    public Transform ballTransform;

    public readonly float radius;

    public readonly float mass;

    public Ball(Transform ballTransform, float density = 1f)
    {
        this.pos = ballTransform.position;
        this.ballTransform = ballTransform;
        this.radius = ballTransform.localScale.x * 0.5f;
        this.mass = (4f / 3f) * Mathf.PI * Mathf.Pow(this.radius, 3f) * density;
    }
}
