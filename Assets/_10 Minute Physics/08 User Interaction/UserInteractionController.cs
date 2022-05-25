using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Based on "Providing user interaction with a 3d scene"
//https://matthias-research.github.io/pages/tenMinutePhysics/
public class UserInteractionController : MonoBehaviour
{
    public Transform ballTransform;

    public Camera mainCamera;

    private InteractiveBall ball;

    private int subSteps = 5;

    private Vector3 gravity = new Vector3(0f, -9.81f, 0f);



    private void Start()
    {
        //Init the ball
        ball = new InteractiveBall(ballTransform);

        ball.vel = new Vector3(3f, 5f, 2f);
    }



    private void Update()
    {
        //Update the visual position of the ball
        ball.UpdateVisualPosition();
    }



    //User interactions should be in LateUpdate
    private void LateUpdate()
    {
        if (Input.GetMouseButtonDown(0))
        {
            //Debug.Log("Ray fired");
        
            //A ray from the mouse into the scene
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

            //Find if the ray hit a sphere
            CustomPhysicsRaycast(ray, out CustomHit hit);

            if (hit != null)
            {
                Debug.Log("Ray hit");
            }
            else
            {
                Debug.Log("Ray missed");
            }

        }

    }



    private void FixedUpdate()
    {
        float sdt = Time.fixedDeltaTime / (float)subSteps;

        for (int step = 0; step < subSteps; step++)
        {
            ball.SimulateBall(sdt, gravity);
        }

        //Collision detection
        ball.HandleWallCollision();
    }



    //Cant use Physics.Raycast because we are not using Unitys physics system, so we have to make our own
    private void CustomPhysicsRaycast(Ray ray, out CustomHit hit)
    {
        //hit = null;

        //Assumer we have just spheres, then we need to do ray-sphere collision detection
        ball.IsRayHitting(ray, out hit);
    }
    
}
