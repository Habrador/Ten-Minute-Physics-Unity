using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PinballMachine;

//Simulate a pinball machine
//Based on: https://matthias-research.github.io/pages/tenMinutePhysics/
public class PinballController : MonoBehaviour
{
    //Drags
    public Transform ballTrans_1;
    public Transform ballTrans_2;

    public GameObject flipper_L_GO;
    public GameObject flipper_R_GO;

    //Should be ordered counter-clockwise
    public Transform borderTransformsParent;

    //The round obstacles making the ball bounce 
    public Transform jetBumpersParent;


    //Settings
    private float flipperRadius = 0.5f;

    private Vector3 gravity = new Vector3(0f, -9.81f, 0f);

    private float restitution = 1f;


    //Flipper parts
    private List<PinballBall> balls = new List<PinballBall>();

    private List<Obstacle> obstacles = new List<Obstacle>();

    private Flipper flipper_L;
    private Flipper flipper_R;

    private List<Vector3> border = new List<Vector3>();


    //Flipper controls
    //Needed because we change these in Update, while using them in FixedUpdate
    private bool is_L_FlipperActivated = false;
    private bool is_R_FlipperActivated = false;



    private void Start()
    {
        //Add the borders
        foreach (Transform t in borderTransformsParent)
        {
            border.Add(t.position);
        }

        //To close the border
        border.Add(border[0]);

        borderTransformsParent.gameObject.SetActive(false);


        //Add the balls
        PinballBall ball_1 = new PinballBall(Vector3.zero, ballTrans_1, restitution);
        PinballBall ball_2 = new PinballBall(Vector3.zero, ballTrans_2, restitution);

        balls.Add(ball_1);
        balls.Add(ball_2);


        //Add the obstales = jet bumpers
        foreach (Transform t in jetBumpersParent)
        {
            float pushVel = 2f;

            float bumperRadius = t.localScale.x;

            Vector3 pos = t.position;

            Obstacle obstacle = new Obstacle(bumperRadius, pos, pushVel);
        
            obstacles.Add(obstacle);
        }


        //Add the flippers
        float flipperRadius = flipper_L_GO.transform.localScale.x * 0.5f;

        float flipperLength = 1.6f;

        float maxRotation = 1f;

        float restAngle = 0.5f;

        float angularVel = 10f;

        float flipperRestitution = 0f;

        Vector3 pos_L = flipper_L_GO.transform.position;
        Vector3 pos_R = flipper_R_GO.transform.position;

        flipper_L = new Flipper(flipperRadius, pos_L, flipperLength, -restAngle, maxRotation, angularVel, flipperRestitution);
        flipper_R = new Flipper(flipperRadius, pos_R, flipperLength, Mathf.PI + restAngle, maxRotation, angularVel, flipperRestitution);
    }



    private void Update()
    {
        if (Input.GetKey(KeyCode.LeftControl))
        {
            is_L_FlipperActivated = true;
        }
        else
        {
            is_L_FlipperActivated = false;
        }

        if (Input.GetKey(KeyCode.LeftArrow))
        {
            is_R_FlipperActivated = true;
        }
        else
        {
            is_R_FlipperActivated = false;
        }
    }



    private void FixedUpdate()
    {
        
    }



    private void LateUpdate()
    {
        //ball = new Ball(Vector3.zero, ballGO.transform, 0.2f, 0.5f);
        //DisplayPinballObjects();

        DisplayShapes.DrawLineSegments(border, Color.white);

        DisplayFlipper(flipper_L);
        DisplayFlipper(flipper_R);
    }



    private void DisplayFlipper(Flipper f)
    {
        Vector3 a = f.pos;
        Vector3 b = f.GetTip();

        DisplayShapes.DrawCapsule(a, b, f.radius, Color.red);
    }



    private void TestCollision()
    {
        Vector3 p = ballTrans_1.position;
        Vector3 a = flipper_L_GO.transform.position;
        Vector3 b = flipper_R_GO.transform.position;

        float ballRadius = ballTrans_1.localScale.x * 0.5f;

        Color flipperColor = Color.blue;

        if (IsBallCapsuleColliding(p, ballRadius, a, b, flipperRadius))
        {
            flipperColor = Color.red;
        }

        DisplayShapes.DrawCapsule(flipper_L_GO.transform.position, flipper_R_GO.transform.position, flipperRadius, flipperColor);
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
