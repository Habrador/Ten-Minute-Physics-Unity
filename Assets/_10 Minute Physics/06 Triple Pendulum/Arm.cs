using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Arm : MonoBehaviour
{
    public Transform cylinder1;
    public Transform cylinder2;

    public Transform rectangle;

    public Transform hinge1;
    public Transform hinge2;

    private float armDepth = 0.1f;


    public void Init(float radius)
    {
        cylinder1.localScale = new Vector3(radius, armDepth, radius);
        cylinder2.localScale = new Vector3(radius, armDepth, radius);

        rectangle.localScale = new Vector3(armDepth * 2f, radius, 1f);

        float hingeRadius = radius * 0.6f;

        hinge1.localScale = new Vector3(hingeRadius, armDepth + 0.01f, hingeRadius);
        hinge2.localScale = new Vector3(hingeRadius, armDepth + 0.01f, hingeRadius);
    }



    public void UpdateSection(Vector3 p1, Vector3 p2, bool isOffset)
    {
        cylinder1.position = p1;
        cylinder2.position = p2;

        hinge1.position = p1;
        hinge2.position = p2;

        Vector3 center = (p1 + p2) * 0.5f;

        float length = (p1 - p2).magnitude;

        rectangle.position = center;

        Vector3 scale = rectangle.localScale;

        rectangle.localScale = new Vector3(scale.x, scale.y, length);

        rectangle.LookAt(p2);

        //So the arms don't intersect 
        if (isOffset)
        {
            cylinder1.position += Vector3.forward * armDepth * 2f;
            cylinder2.position += Vector3.forward * armDepth * 2f;

            hinge1.position += Vector3.forward * armDepth * 2f;
            hinge2.position += Vector3.forward * armDepth * 2f;

            rectangle.position += Vector3.forward * armDepth * 2f;
        }
    }

}
