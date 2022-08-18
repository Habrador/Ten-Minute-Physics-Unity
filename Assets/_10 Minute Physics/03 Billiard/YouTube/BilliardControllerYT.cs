using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Billiard;

namespace Billiard
{
    //Simulate billiard balls with different size
    //Based on: https://matthias-research.github.io/pages/tenMinutePhysics/
    //Issues:
    //- Sometimes when we bounce many balls against a circle, the balls cluster after running the simulation for some time, which maybe be because of floating point precision issues??? After some research I think they cluster because of collision detection with the outer circle. If you lower speed and add fast-forward, they no longer cluster
    public class BilliardControllerYT : MonoBehaviour
    {
        //Public
        public GameObject ballPrefabGO;

        public BilliardTable table;

        //Where the balls will start
        public Transform startPosTrans;


        //Private

        //Simulation properties
        private readonly int subSteps = 1;

        private int fastForwardSpeed = 10;

        private readonly int numberOfBalls = 1;

        private readonly float startVel = 0.25f;

        //How much velocity is lost after collision between balls [0, 1]
        //Is usually called e
        //Elastic: e = 1 means same velocity after collision (if the objects have the same size and same speed)
        //Inelastic: e = 0 means no velocity after collions (if the objects have the same size and same speed) and energy is lost
        private readonly float restitution = 1.00f;

        //To get the same simulation every time
        private readonly int seed = 0;

        private List<BilliardBall> allBalls;

        //How long before the simulation starts to make it easier for people to see the initial conditions
        private readonly float pauseTimer = 2f;

        private bool canSimulate = false;

        private List<Queue<Vector3>> historialPositions = new();

        private bool displayHistory = true;



        private void Start()
        {
            Random.InitState(seed);

            allBalls = new List<BilliardBall>();

            float ballRadius = 0.35f;


            Vector3 startPos = Vector3.zero;

            SetupBalls.AddBall(ballPrefabGO, allBalls, startPos, ballRadius);

            //SetupBalls.AddRandomBallsWithinCircle(ballPrefabGO, numberOfBalls, allBalls, ballRadius, ballRadius + 1.8f, 5f);

            //Vector2 rectangleSize = new Vector2(10f, 14f);

            //SetupBalls.AddRandomBallsWithinRectangle(ballPrefabGO, numberOfBalls, allBalls, ballRadius, ballRadius + 1.8f, rectangleSize);

            //SetupBalls.AddBallsOnCircle(ballPrefabGO, numberOfBalls, allBalls, 0.2f, 0.4f);

            //AddBallsWithinArea();


            //Offset the balls to the startPos
            if (startPosTrans != null)
            {
                foreach (BilliardBall ball in allBalls)
                {
                    ball.pos += startPosTrans.position;
                }
            }


            //Give each ball a color
            BilliardMaterials.GiveBallsRandomColor(ballPrefabGO, allBalls);
            //BilliardMaterials.GiveBallsGradientColor(ballPrefabGO, allBalls);


            //Give each ball a velocity
            foreach (BilliardBall ball in allBalls)
            {
                float velAngle = 0f;

                float randomVelY = Random.Range(-velAngle, velAngle);

                Vector3 ballVel = Quaternion.Euler(0f, 0f, 0f) * Vector3.right * startVel;

                ball.vel = ballVel;
            }


            //The problem now is that some balls may intersect with other balls
            //So we need to run an algorithm that moves them apart while still making sure they are within the play area
            //SetupBalls.MoveAllBallsApart(allBalls, table);

            if (displayHistory)
            {
                for (int i = 0; i < numberOfBalls; i++)
                {
                    historialPositions.Add(new Queue<Vector3>());
                }
            }

            table.Init();

            StartCoroutine(WaitForSimulationToStart(pauseTimer));  
        }



        private IEnumerator WaitForSimulationToStart(float pauseTimer)
        {
            yield return new WaitForSeconds(pauseTimer);

            canSimulate = true;
        }


        
        private void Update()
        {
            //Update the transform with the position we simulate in FixedUpdate
            foreach (BilliardBall ball in allBalls)
            {
                ball.UpdateVisualPosition();

                //Debug.Log(ball.vel.magnitude);
            }

            if (displayHistory)
            {
                for (int i = 0; i < allBalls.Count; i++)
                {
                    historialPositions[i].Enqueue(allBalls[i].pos);
                }
            }
        }




        private void FixedUpdate()
        {
            if (!canSimulate)
            {
                return;
            }


            for (int scale = 0; scale < fastForwardSpeed; scale++)
            {
                float sdt = Time.fixedDeltaTime / (float)subSteps;

                for (int i = 0; i < allBalls.Count; i++)
                {
                    BilliardBall thisBall = allBalls[i];

                    thisBall.SimulateBall(subSteps, sdt);

                    /*
                    //Check collision with the other balls after this ball in the list of all balls
                    for (int j = i + 1; j < allBalls.Count; j++)
                    {
                        BilliardBall otherBall = allBalls[j];

                        //HandleBallCollision(ball, ballOther, restitution);
                        BallCollisionHandling.HandleBallBallCollision(thisBall, otherBall, restitution);
                    }
                    */

                    bool isColliding = table.HandleBallCollision(thisBall, restitution);

                    //We only need to save history if the ball is colliding, otherwise its just moving in the same direction
                    //if (displayHistory && isColliding)
                    //{
                    //    historialPositions[i].Enqueue(allBalls[i].pos);
                    //}
                }


                //Add some friction
                //for (int i = 0; i < allBalls.Count; i++)
                //{
                //    BilliardBall thisBall = allBalls[i];

                //    thisBall.vel *= 0.99f;
                //}
            }
        }



        private void LateUpdate()
        {
            if (displayHistory)
            {
                foreach (Queue<Vector3> historicalPosition in historialPositions)
                {
                    List<Vector3> verts = new List<Vector3>(historicalPosition);

                    DisplayShapes.DrawLine(verts, DisplayShapes.ColorOptions.Gray);
                }
            }
        }
    }
}