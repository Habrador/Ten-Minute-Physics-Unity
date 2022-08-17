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

        public Table table;



        //Private

        //Simulation properties
        private readonly int subSteps = 1;

        private int fastForwardSpeed = 10;

        private readonly int numberOfBalls = 10;

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

        private bool displayHistory = false;



        private void Start()
        {
            Random.InitState(seed);
        
            ResetSimulation();

            for (int i = 0; i < numberOfBalls; i++)
            {
                historialPositions.Add(new Queue<Vector3>());
            }

            table.Init();

            StartCoroutine(WaitForSimulationToStart(pauseTimer));    
        }



        private IEnumerator WaitForSimulationToStart(float pauseTimer)
        {
            yield return new WaitForSeconds(pauseTimer);

            canSimulate = true;
        }



        private void ResetSimulation()
        {
            allBalls = new List<BilliardBall>();

            //AddRandomBallsWithinMap();

            AddBallsOnMiniCircle();   

            //AddBallsWithinArea();
        }



        private void AddBallsWithinArea()
        {
            Material ballBaseMaterial = ballPrefabGO.GetComponent<MeshRenderer>().sharedMaterial;

            //Create random balls
            for (int i = 0; i < numberOfBalls; i++)
            {
                GameObject newBallGO = Instantiate(ballPrefabGO);


                //Random color
                Material randomBallMaterial = BilliardMaterials.GetRandomBilliardBallMaterial(ballBaseMaterial);

                newBallGO.GetComponent<MeshRenderer>().material = randomBallMaterial;


                //Scale
                newBallGO.transform.localScale = Vector3.one * 0.2f;


                //Pos

                //Random pos within rectangle
                float rectHalfSize = 0.2f;

                float randomPosX = Random.Range(-rectHalfSize, rectHalfSize);
                float randomPosZ = Random.Range(-rectHalfSize, rectHalfSize);

                Vector3 randomPos = new(randomPosX, 0f, randomPosZ);

                //Random pos within circle
                //Vector2 randomPos2D = Random.insideUnitCircle * rectSize * 2f;

                //Vector3 randomPosCircle = new(randomPos2D.x, 0f, randomPos2D.y);

                //randomPos += randomPosCircle;

                //Move it down
                randomPos += Vector3.right * 3f;

                newBallGO.transform.position = randomPos;


                //Vel
                float velAngle = 0f;

                float randomVelY = Random.Range(-velAngle, velAngle);

                Vector3 ballVel = Quaternion.Euler(0f, randomVelY, 0f) * Vector3.forward * startVel;

                BilliardBall newBall = new(ballVel, newBallGO.transform);

                allBalls.Add(newBall);
            }

            //Create balls with fixed with between them
            //float side = 0.6f;

            //int balls = 10;

            //Vector3 pos = new Vector3(-0.3f, 0f, 0.3f);

            //for (int x = 0; x < balls; x++)
            //{
            //    pos.z = side * 0.5f;

            //    for (int z = 0; z < balls; z++)
            //    {
            //        GameObject newBallGO = Instantiate(ballPrefabGO);


            //        //Random color
            //        Material randomBallMaterial = BilliardMaterials.GetRandomBilliardBallMaterial(ballBaseMaterial);

            //        newBallGO.GetComponent<MeshRenderer>().material = randomBallMaterial;

            //        //Scale
            //        newBallGO.transform.localScale = Vector3.one * 0.2f;

            //        //Pos 
            //        newBallGO.transform.position = pos;

            //        //Vell
            //        Vector3 startVel = Quaternion.Euler(0f, 0f, 0f) * Vector3.forward * 5f;

            //        //Add the ball
            //        BilliardBall newBall = new(startVel, newBallGO.transform);

            //        allBalls.Add(newBall);

            //        pos.z -= side / balls;
            //    }

            //    pos.x += side / balls;
            //}
        }



        //Add balls on the circumference of a circle
        private void AddBallsOnMiniCircle()
        {
            Material ballBaseMaterial = ballPrefabGO.GetComponent<MeshRenderer>().sharedMaterial;

            Vector3 ballsCenter = Vector3.zero;

            float miniCircleRadius = 0.2f;

            List<Vector3> ballPositons = UsefulMethods.GetCircleSegments_XZ(ballsCenter, miniCircleRadius, numberOfBalls);

            //Debug.Log(ballPositons.Count);

            for (int i = 0; i < ballPositons.Count - 1; i++)
            {
                GameObject newBallGO = Instantiate(ballPrefabGO);


                //Mat
                //Material randomBallMaterial = BilliardMaterials.GetRandomBilliardBallMaterial(ballBaseMaterial);

                //newBallGO.GetComponent<MeshRenderer>().material = randomBallMaterial;

                Material lerpedMaterial = BilliardMaterials.GetLerpedMaterial(ballBaseMaterial, i, ballPositons.Count - 2);

                newBallGO.GetComponent<MeshRenderer>().material = lerpedMaterial;


                //Pos
                newBallGO.transform.position = ballPositons[i];

                //Move it down
                newBallGO.transform.position += Vector3.right * 0f;


                //Scale
                newBallGO.transform.localScale = Vector3.one * 0.2f;


                //Vel
                Vector3 ballVel = Quaternion.Euler(0f, 20f, 0f) * Vector3.forward * startVel;


                //Add the actual ball
                BilliardBall newBall = new(ballVel, newBallGO.transform);

                allBalls.Add(newBall);
            }
        }



        //Add balls within the entire map and move them apart from each other so they dont collide
        private void AddRandomBallsWithinMap()
        {
            Material ballBaseMaterial = ballPrefabGO.GetComponent<MeshRenderer>().sharedMaterial;

            //Create random balls
            for (int i = 0; i < numberOfBalls; i++)
            {
                GameObject newBallGO = Instantiate(ballPrefabGO);


                //Random color
                Material randomBallMaterial = BilliardMaterials.GetRandomBilliardBallMaterial(ballBaseMaterial);

                newBallGO.GetComponent<MeshRenderer>().material = randomBallMaterial;


                //Random size
                //Size has to be before pos so we can take the radius into account
                float randomSize = Random.Range(0.1f, 1f);

                newBallGO.transform.localScale = Vector3.one * randomSize;


                //Random pos within rectangle
                //float randomPosX = Random.Range(-halfWallLength, halfWallLength);
                //float randomPosZ = Random.Range(-halfWallLength, halfWallLength);

                //Vector3 randomPos = new(randomPosX, 0f, randomPosZ);

                //Random pos within circle
                Vector2 randomPos2D = Random.insideUnitCircle * (5f - (randomSize * 0.5f));

                Vector3 randomPos = new(randomPos2D.x, 0f, randomPos2D.y);

                newBallGO.transform.position = randomPos;


                //Random vel
                float maxVel = startVel;

                float randomVelX = Random.Range(-maxVel, maxVel);
                float randomVelZ = Random.Range(-maxVel, maxVel);

                Vector3 randomVel = new Vector3(randomVelX, 0f, randomVelZ);


                BilliardBall newBall = new(randomVel, newBallGO.transform);

                allBalls.Add(newBall);
            }


            //The problem now is that some balls may intersect with other balls
            //So we need to run an algorithm that moves them apart while still making sure they are within the play area
            MoveAllBallsApart();
        }



        //Iterate through all balls and make sure they dont collide
        private void MoveAllBallsApart()
        {
            int iterations = 10;

            for (int k = 0; k < iterations; k++)
            {
                for (int i = 0; i < allBalls.Count; i++)
                {
                    BilliardBall thisBall = allBalls[i];

                    //Check collision with the other balls after this ball in the list of all balls
                    for (int j = i + 1; j < allBalls.Count; j++)
                    {
                        BilliardBall otherBall = allBalls[j];

                        TryMoveTwoBallsApart(thisBall, otherBall);
                    }
                }
            }
        }


        //Given two balls, test if they intersect, if so move them apart if they dont end up outside of the circle
        private void TryMoveTwoBallsApart(Ball b1, Ball b2)
        {
            float distBetweenBallsSqr = (b1.pos - b2.pos).sqrMagnitude;

            float allowedDist = b1.radius + b2.radius;

            if (distBetweenBallsSqr < allowedDist * allowedDist)
            {
                //Direction from b1 to b2
                Vector3 dir = b2.pos - b1.pos;

                //The distance between the balls
                float d = dir.magnitude;

                dir = dir.normalized;

                //The distace each ball should move so they no longer intersect 
                float corr = (b1.radius + b2.radius - d) * 0.5f;

                //Check if they can move to their new positions
                Vector3 b1_NewPos = b1.pos + dir * -corr;
                Vector3 b2_NewPos = b2.pos + dir * corr;

                if (!table.IsBallOutsideOfTable(b1_NewPos, b1.radius))
                {
                    b1.pos = b1_NewPos;
                }
                if (!table.IsBallOutsideOfTable(b2_NewPos, b2.radius))
                {
                    b2.pos = b2_NewPos;
                }
            }
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
                    //thisBall.HandleSquareCollision(wallLength);

                    table.HandleBallCollision(thisBall, restitution);
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

                    DisplayShapes.DrawLine(verts, DisplayShapes.ColorOptions.White);
                }
            }
        }
    }
}