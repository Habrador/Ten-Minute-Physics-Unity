using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Billiard;

namespace Billiard
{
    //Simulate billiard balls with different size
    //Based on: https://matthias-research.github.io/pages/tenMinutePhysics/
    public class BilliardController : MonoBehaviour
    {
        public GameObject ballPrefabGO;

        public BilliardTable billiardTable;

        //Simulation properties
        private readonly int subSteps = 5;

        private readonly int numberOfBalls = 20;

        //How much velocity is lost after collision between balls [0, 1]
        //Is usually called e
        //Elastic: e = 1 means same velocity after collision (if the objects have the same size and same speed)
        //Inelastic: e = 0 means no velocity after collions (if the objects have the same size and same speed) and energy is lost
        private readonly float restitution = 0.8f;

        private List<BilliardBall> allBalls;



        private void Start()
        {
            ResetSimulation();

            billiardTable.Init();
        }



        private void ResetSimulation()
        {
            allBalls = new List<BilliardBall>();

            Vector2 mapSize = new (10f, 14f);

            SetupBalls.AddRandomBallsWithinRectangle(ballPrefabGO, numberOfBalls, allBalls, 0.1f, 1f, mapSize, Vector3.zero);

            BilliardMaterials.GiveBallsRandomColor(ballPrefabGO, allBalls);

            //Give each ball a velocity
            foreach (BilliardBall b in allBalls)
            {
                float maxVel = 4f;

                float randomVelX = Random.Range(-maxVel, maxVel);
                float randomVelZ = Random.Range(-maxVel, maxVel);

                Vector3 randomVel = new Vector3(randomVelX, 0f, randomVelZ);

                b.vel = randomVel;
            }
        }



        private void Update()
        {
            //Update the transform with the position we simulate in FixedUpdate
            foreach (BilliardBall ball in allBalls)
            {
                ball.UpdateVisualPosition();
            }
        }



        private void FixedUpdate()
        {
            float sdt = Time.fixedDeltaTime / (float)subSteps;

            for (int i = 0; i < allBalls.Count; i++)
            {
                BilliardBall thisBall = allBalls[i];

                thisBall.SimulateBall(subSteps, sdt);

                //Check collision with the other balls after this ball in the list of all balls
                for (int j = i + 1; j < allBalls.Count; j++)
                {
                    BilliardBall ballOther = allBalls[j];

                    //HandleBallCollision(ball, ballOther, restitution);
                    BallCollisionHandling.HandleBallBallCollision(thisBall, ballOther, restitution);
                }

                //thisBall.HandleSquareCollision(wallLength);
                billiardTable.HandleBallEnvironmentCollision(thisBall);
            }
        }
    }
}