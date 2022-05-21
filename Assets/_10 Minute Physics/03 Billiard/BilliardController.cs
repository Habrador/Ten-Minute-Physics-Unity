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
    private int subSteps = 5;

    //How much velocity is lost after collision between balls [0, 1]
    //Is usually called e
    //Elastic: e = 1 means same velocity after collision (if the objects have the same size and same speed)
    //Inelastic: e = 0 means no velocity after collions (if the objects have the same size and same speed) and energy is lost
    private float restitution = 0.8f;

    private List<BilliardBall> allBalls;




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
            ball.UpdateVisualPostion();
        }
    }




    private void FixedUpdate()
    {
        for (int i = 0; i < allBalls.Count; i++)
        {
            BilliardBall ball = allBalls[i];

            ball.SimulateBall(subSteps);

            //Check collision with the other balls after this ball in the list of all balls
            for (int j = i + 1; j < allBalls.Count; j++)
            {
                BilliardBall ballOther = allBalls[j];

                HandleBallCollision(ball, ballOther, restitution);
            }

            ball.HandleWallCollision();
        }
    }



    private void HandleBallCollision(BilliardBall b1, BilliardBall b2, float restitution)
    {
        //Direction from b1 to b2
        Vector3 dir = b2.pos - b1.pos;

        //The distance between the balls
        float d = dir.magnitude;

        //The balls are not colliding
        if (d == 0f || d > b1.radius + b2.radius)
        {
            return;
        }

        //Normalized direction
        dir = dir.normalized;


        //Update positions

        //The distace each ball should move so they no longer intersect 
        float corr = (b1.radius + b2.radius - d) * 0.5f;

        //Move the balls apart along the dir vector
        b1.pos += dir * -corr; //-corr because dir goes from b1 to b2
        b2.pos += dir *  corr;


        //Update velocities

        //The part of each balls velocity along dir
        float v1 = Vector3.Dot(b1.vel, dir);
        float v2 = Vector3.Dot(b2.vel, dir);

        float m1 = b1.radius;
        float m2 = b2.radius;

        //Assume the objects are stiff
        float new_v1 = (m1 * v1 + m2 * v2 - m2 * (v1 - v2) * restitution) / (m1 + m2);
        float new_v2 = (m1 * v1 + m2 * v2 - m1 * (v2 - v1) * restitution) / (m1 + m2);

        //Change velocity components along dir
        b1.vel += dir * (new_v1 - v1);
        b2.vel += dir * (new_v2 - v2);
    }
}
