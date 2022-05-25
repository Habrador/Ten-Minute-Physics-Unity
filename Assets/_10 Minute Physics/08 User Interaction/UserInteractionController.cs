using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Based on "Providing user interaction with a 3d scene"
//https://matthias-research.github.io/pages/tenMinutePhysics/
public class UserInteractionController : MonoBehaviour
{
    public Transform ballTransform;

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
    
}
