using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UserInteraction;

//Based on "Providing user interaction with a 3d scene"
//https://matthias-research.github.io/pages/tenMinutePhysics/
public class UserInteractionController : MonoBehaviour
{
    public Transform ballTransform;

    public Texture2D cursorTexture;

    private InteractiveBall ball;

    private int subSteps = 5;

    private Vector3 gravity = new Vector3(0f, -9.81f, 0f);

    //What we use to grab the balls
    private Grabber grabber;

    


    private void Start()
    {
        //Init the ball
        ball = new InteractiveBall(ballTransform);

        ball.vel = new Vector3(3f, 5f, 2f);

        //Init the grabber
        grabber = new Grabber(Camera.main);

        Cursor.visible = true;

        Cursor.SetCursor(cursorTexture, Vector2.zero, CursorMode.ForceSoftware);

//#if UNITY_EDITOR
        //Cursor.SetCursor(UnityEditor.PlayerSettings.defaultCursor, Vector2.zero, CursorMode.ForceSoftware);
//#endif
    }



    private void Update()
    {
        //Update the visual position of the ball
        ball.UpdateVisualPosition();

        grabber.MoveGrab();
    }



    //User interactions should be in LateUpdate
    private void LateUpdate()
    {
        if (Input.GetMouseButtonDown(0))
        {
            List<IGrabbable> temp = new List<IGrabbable>();

            temp.Add(ball);
        
            grabber.StartGrab(temp);
        }

        if (Input.GetMouseButtonUp(0))
        {
            grabber.EndGrab();
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
}
