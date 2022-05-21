using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PinballMachine;

//Simulate a pinball machine
//Based on: https://matthias-research.github.io/pages/tenMinutePhysics/
public class PinballController : MonoBehaviour
{
    //Drags
    public GameObject ballGO;

    public GameObject flipper_L_GO;
    public GameObject flipper_R_GO;

    //Should be ordered counter-clockwise
    public Transform borderTransformsParent;


    //Settings
    private float flipperRadius = 0.5f;

    private Vector3 gravity = new Vector3(0f, -9.81f, 0f);


    //Flipper parts
    private Ball ball;

    //private List<Ball> balls;

    private List<Obstacle> obstacles = new List<Obstacle>();

    private List<Flipper> flippers = new List<Flipper>();

    private List<Vector3> border = new List<Vector3>();



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


        //Add the flippers

    }



    private void Update()
    {
        //ball = new Ball(Vector3.zero, ballGO.transform, 0.2f, 0.5f);
        //DisplayPinballObjects();

        DisplayShapes.DrawLineSegments(border, Color.white);
    }



    public void DisplayPinballObjectsInEditor()
    {
        ////Draw border
        //List<Vector3> borderVertices = new List<Vector3>();

        //foreach (Transform t in border)
        //{
        //    borderVertices.Add(t.position);
        //}

        //DisplayShapes.DrawLineSegments(borderVertices, Color.white);

        //Debug.DrawLine(Vector3.zero, Vector3.one * 5f, Color.white, 20f);
    }



    private void TestCollision()
    {
        Vector3 p = ballGO.transform.position;
        Vector3 a = flipper_L_GO.transform.position;
        Vector3 b = flipper_R_GO.transform.position;

        float ballRadius = ballGO.transform.localScale.x * 0.5f;

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
