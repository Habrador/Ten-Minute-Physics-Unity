using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Circular billiard table
public class Circle : BilliardTable
{
    private readonly int segments = 100;

    private readonly float radius = 5f;



    public override void Init()
    {
        MeshFilter mf = this.gameObject.GetComponent<MeshFilter>();

        mf.sharedMesh = DisplayShapes.GenerateCircleMesh_XZ(transform.position, radius, segments);
    }



    public override bool HandleBallEnvironmentCollision(Ball ball, float restitution)
    {
        bool isColliding = false;
    
        Vector3 circleCenter = transform.position;

        if (IsBallOutsideOfCircle(ball.pos, ball.radius, circleCenter, radius))
        {
            isColliding = true;
        
            Vector3 wallNormal = (circleCenter - ball.pos).normalized;


            //Move the ball so it's no longer colliding
            ball.pos = (radius - ball.radius) * -wallNormal;


            //Update velocity 

            //Collisions can only change velocity components along the penetration direction
            float v = Vector3.Dot(ball.vel, wallNormal);

            float vNew = Mathf.Abs(v) * restitution;

            //Remove the old velocity and add the new velocity
            ball.vel += wallNormal * (vNew - v);

            //Same result
            //ball.vel = Vector3.Reflect(ball.vel, -wallNormal);
        }

        return isColliding;
    }



    //Is a ball outside of the circle border?
    public override bool IsBallOutsideOfTable(Vector3 ballPos, float ballRadius)
    {
        return IsBallOutsideOfCircle(ballPos, ballRadius, transform.position, radius);
    }



    private bool IsBallOutsideOfCircle(Vector3 ballPos, float ballRadius, Vector3 circleCenter, float circleRadius)
    {
        bool isOutside = false;

        //The distance between the center and the ball's center
        float distCenterToBallSqr = (ballPos - circleCenter).sqrMagnitude;

        //If that distance is greater than this, the ball is outside
        float maxAllowedDist = circleRadius - ballRadius;

        if (distCenterToBallSqr > maxAllowedDist * maxAllowedDist)
        {
            isOutside = true;
        }

        return isOutside;
    }



    public override bool IsBallInHole(Ball ball)
    {
        return false;
    }

    public override void MyUpdate()
    {
        //throw new System.NotImplementedException();
    }
}
