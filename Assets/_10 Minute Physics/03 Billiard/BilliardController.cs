using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Billiard;

//Simulate billiard balls with different size
//Based on: https://matthias-research.github.io/pages/tenMinutePhysics/
public class BilliardController : MonoBehaviour
{
    public GameObject ballPrefabGO;

    //Simulation properties
    private readonly int subSteps = 5;

    //How much velocity is lost after collision between balls [0, 1]
    //Is usually called e
    //Elastic: e = 1 means same velocity after collision (if the objects have the same size and same speed)
    //Inelastic: e = 0 means no velocity after collions (if the objects have the same size and same speed) and energy is lost
    private readonly float restitution = 0.8f;

    private List<BilliardBall> allBalls;

    //The simulation area is a square with side length
    private readonly float wallLength = 5f;


    private void Start()
    {
        ResetSimulation();
    }



    private void ResetSimulation()
    {
        allBalls = new List<BilliardBall>();

        //Create random balls
        for (int i = 0; i < 20; i++)
        {
            GameObject newBallGO = Instantiate(ballPrefabGO);

            //Random pos
            float randomPosX = Random.Range(-5f, 5f);
            float randomPosZ = Random.Range(-5f, 5f);

            Vector3 randomPos = new Vector3(randomPosX, 0f, randomPosZ);

            //Random size
            float randomSize = Random.Range(0.1f, 1f);

            newBallGO.transform.position = randomPos;
            newBallGO.transform.localScale = Vector3.one * randomSize;

            //Random vel
            float maxVel = 4f;

            float randomVelX = Random.Range(-maxVel, maxVel);
            float randomVelZ = Random.Range(-maxVel, maxVel);

            Vector3 randomVel = new Vector3(randomVelX, 0f, randomVelZ);

            BilliardBall newBall = new BilliardBall(randomVel, newBallGO.transform);

            allBalls.Add(newBall);
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
        for (int i = 0; i < allBalls.Count; i++)
        {
            BilliardBall thisBall = allBalls[i];

            thisBall.SimulateBall(subSteps);

            //Check collision with the other balls after this ball in the list of all balls
            for (int j = i + 1; j < allBalls.Count; j++)
            {
                BilliardBall ballOther = allBalls[j];

                //HandleBallCollision(ball, ballOther, restitution);
                BallCollisionHandling.HandleBallBallCollision(thisBall, ballOther, restitution);
            }

            thisBall.HandleSquareCollision(wallLength);
        }
    }
}
