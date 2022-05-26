using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Grabber
{
    //Data needed 
    private Camera mainCamera;

    //Ball grabbing data
    private float distanceToBall;

    private InteractiveBall grabbedBall = null;

    //Need so we can give the ball a velocity when we release it
    private Vector3 lastBallPos;



    public Grabber(Camera mainCamera)
    {
        this.mainCamera = mainCamera;
    }



    public void StartGrab(InteractiveBall ball)
    {
        if (grabbedBall != null)
        {
            return;
        }
        
        //A ray from the mouse into the scene
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        //Find if the ray hit a sphere
        CustomPhysicsRaycast(ray, out CustomHit hit, ball);

        if (hit != null)
        {
            //Debug.Log("Ray hit");

            grabbedBall = hit.ball;

            //Move the ball to the ray because the ray may not have hit at the center of the ball
            Vector3 ballPosOnRay = UsefulMethods.GetClosestPointOnRay(grabbedBall.pos, ray);

            grabbedBall.StartGrab(ballPosOnRay);

            lastBallPos = ballPosOnRay;

            distanceToBall = (ray.origin - grabbedBall.pos).magnitude;
        }
        else
        {
            //Debug.Log("Ray missed");
        }
    }




    public void MoveGrab()
    {
        if (grabbedBall == null)
        {
            return;
        }

        //A ray from the mouse into the scene
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        Vector3 ballPos = ray.origin + ray.direction * distanceToBall;

        lastBallPos = grabbedBall.pos;

        grabbedBall.MoveGrabbed(ballPos);
    }



    public void EndGrab()
    {
        if (grabbedBall == null)
        {
            return;
        }

        //Add a velocity to the ball
        float vel = (grabbedBall.pos - lastBallPos).magnitude / Time.deltaTime;

        Vector3 dir = (grabbedBall.pos - lastBallPos).normalized;

        grabbedBall.EndGrab(grabbedBall.pos, dir * vel);

        grabbedBall = null;
    }



    //Cant use Physics.Raycast because we are not using Unitys physics system, so we have to make our own
    private void CustomPhysicsRaycast(Ray ray, out CustomHit hit, InteractiveBall ball)
    {
        //hit = null;

        //Assumer we have just spheres, then we need to do ray-sphere collision detection
        ball.IsRayHittingThisBall(ray, out hit);
    }
}
