using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PinballMachine;

public class PinballController : MonoBehaviour
{
    public GameObject ballGO;

    public GameObject flipperL_a_GO;
    public GameObject flipperL_b_GO;

    private float flipperRadius = 0.5f;

    //Reuse the ball class from last video
    private Ball ball;
    


    private void Update()
    {
        ball = new Ball(Vector3.zero, ballGO.transform, 0.2f, 0.5f); 
    
        
    }



    private void TestCollision()
    {
        Vector3 p = ballGO.transform.position;
        Vector3 a = flipperL_a_GO.transform.position;
        Vector3 b = flipperL_b_GO.transform.position;

        float ballRadius = ballGO.transform.localScale.x * 0.5f;

        Color flipperColor = Color.blue;

        if (IsBallCapsuleColliding(p, ballRadius, a, b, flipperRadius))
        {
            flipperColor = Color.red;
        }

        DisplayShapes.DrawCapsule(flipperL_a_GO.transform.position, flipperL_b_GO.transform.position, flipperRadius, flipperColor);
    }



    private bool IsBallCapsuleColliding(Vector3 p, float ballRadius, Vector3 a, Vector3 b, float capsuleRadius)
    {
        bool isColliding = false;

        //Find the closest point from the ball to the line segment a-b
        Vector3 c = GetClosestPointOnLineSegment(p, a, b);

        //Add a fake ball at this point and do circle-circle collision
        float distancePointLine = (p - c).sqrMagnitude;

        float allowedDistance = ballRadius + capsuleRadius;

        if (distancePointLine < allowedDistance * allowedDistance)
        {
            isColliding = true;
        }

        return isColliding;
    }



    private Vector3 GetClosestPointOnLineSegment(Vector3 p, Vector3 a, Vector3 b)
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
}
