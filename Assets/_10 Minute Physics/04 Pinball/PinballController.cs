using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PinballMachine;

//Simulate a pinball machine
//Based on: https://matthias-research.github.io/pages/tenMinutePhysics/
public class PinballController : MonoBehaviour
{
    //Drags
    public GameObject ballPrefabGO;

    public GameObject flipper_L_GO;
    public GameObject flipper_R_GO;

    //Should be ordered counter-clockwise
    public Transform borderTransformsParent;

    //The round obstacles making the ball bounce 
    public Transform jetBumpersParent;


    //Settings
    //private float flipperRadius = 0.5f;

    private Vector3 gravity = new Vector3(0f, -3f, 0f);

    private float restitution = 0.3f;


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
        for (int i = 0; i < 20; i++)
        {
            GameObject newBallGO = Instantiate(ballPrefabGO);

            //Random pos
            float randomPosX = Random.Range(-4f, 4f);
            float randomPosZ = Random.Range(0f, 5.5f);

            Vector3 randomPos = new Vector3(randomPosX, randomPosZ, 0f);

            //Random size
            float randomSize = Random.Range(0.3f, 1f);

            newBallGO.transform.position = randomPos;
            newBallGO.transform.localScale = Vector3.one * randomSize;

            PinballBall newBall = new PinballBall(Vector3.zero, newBallGO.transform, restitution);

            balls.Add(newBall);
        }


        //Add the obstales = jet bumpers
        foreach (Transform t in jetBumpersParent)
        {
            float pushVel = 5f;

            float bumperRadius = t.localScale.x * 0.5f;

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
        flipper_R = new Flipper(flipperRadius, pos_R, flipperLength, Mathf.PI + restAngle, -maxRotation, angularVel, flipperRestitution);
    }



    private void Update()
    {
        is_L_FlipperActivated = Input.GetKey(KeyCode.LeftControl) ? true : false;

        is_R_FlipperActivated = Input.GetKey(KeyCode.LeftArrow) ? true : false;

        foreach (PinballBall ball in balls)
        {
            ball.UpdateVisualPostion();
        }
    }



    private void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;

        //Flippers
        flipper_L.touchIdentifier = is_L_FlipperActivated ? 1f : -1f;
        flipper_R.touchIdentifier = is_R_FlipperActivated ? 1f : -1f;

        flipper_L.Simulate(dt);
        flipper_R.Simulate(dt);

        for (int i = 0; i < balls.Count; i++)
        {
            PinballBall thisBall = balls[i];
        
            //Move the ball
            thisBall.SimulateBall(dt, gravity);

            //Collision with other balls
            for (int j = i + 1; j < balls.Count; j++)
            {
                PinballBall otherBall = balls[j];

                CustomPhysics.HandleBallBallCollision(thisBall.ball, otherBall.ball, restitution);
            }

            //Collision with obstacles
            foreach (Obstacle obs in obstacles)
            {
                PinballCollisions.HandleBallObstacleCollision(thisBall.ball, obs);
            }

            //Collision with flippers
            PinballCollisions.HandleBallFlipperCollision(thisBall.ball, flipper_L);
            PinballCollisions.HandleBallFlipperCollision(thisBall.ball, flipper_R);

            //Collision with walls
            PinballCollisions.HandleBallBorderCollision(thisBall.ball, border, restitution);

        }
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
        //Vector3 p = ballTrans_1.position;
        //Vector3 a = flipper_L_GO.transform.position;
        //Vector3 b = flipper_R_GO.transform.position;

        //float ballRadius = ballTrans_1.localScale.x * 0.5f;

        //Color flipperColor = Color.blue;

        //if (IsBallCapsuleColliding(p, ballRadius, a, b, flipperRadius))
        //{
        //    flipperColor = Color.red;
        //}

        //DisplayShapes.DrawCapsule(flipper_L_GO.transform.position, flipper_R_GO.transform.position, flipperRadius, flipperColor);
    }



}
