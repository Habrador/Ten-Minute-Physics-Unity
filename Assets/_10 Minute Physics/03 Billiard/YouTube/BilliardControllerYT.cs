using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Billiard;

namespace Billiard
{
    //Simulate billiard balls with different size
    //Based on: https://matthias-research.github.io/pages/tenMinutePhysics/
    public class BilliardControllerYT : MonoBehaviour
    {
        public GameObject ballPrefabGO;

        public GameObject floorGO;

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

        private readonly float floorRadius = 5f;


        private void Start()
        {
            ResetSimulation();

            GenerateCircleMesh(floorGO, Vector3.zero, floorRadius, 10);
        }



        private void ResetSimulation()
        {
            allBalls = new List<BilliardBall>();

            Material ballBaseMaterial = ballPrefabGO.GetComponent<MeshRenderer>().sharedMaterial;

            //Create random balls
            for (int i = 0; i < 20; i++)
            {
                GameObject newBallGO = Instantiate(ballPrefabGO);


                //Random color
                Material randomBallMaterial = BilliardMaterials.GetRandomBilliardBallMaterial(ballBaseMaterial);

                newBallGO.GetComponent<MeshRenderer>().material = randomBallMaterial;


                //Random pos
                float randomPosX = Random.Range(-5f, 5f);
                float randomPosZ = Random.Range(-5f, 5f);

                Vector3 randomPos = new(randomPosX, 0f, randomPosZ);


                //Random size
                float randomSize = Random.Range(0.1f, 1f);

                newBallGO.transform.position = randomPos;
                newBallGO.transform.localScale = Vector3.one * randomSize;


                //Random vel
                float maxVel = 4f;

                float randomVelX = Random.Range(-maxVel, maxVel);
                float randomVelZ = Random.Range(-maxVel, maxVel);

                Vector3 randomVel = new Vector3(randomVelX, 0f, randomVelZ);


                BilliardBall newBall = new(randomVel, newBallGO.transform);

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

                HandleBallCircleCollision(thisBall, Vector3.zero, floorRadius);
            }
        }



        private void LateUpdate()
        {
            //Draw the circle the beads are attached to
            //DisplayShapes.DrawCircle(Vector3.zero, floorRadius, DisplayShapes.ColorOptions.White, DisplayShapes.Space2D.XZ);
        }



        private void HandleBallCircleCollision(Ball ball, Vector3 circleCenter, float circleRadius, float restitution = 1f)
        {        
            //The distance between the center and the ball's center
            float distCenterToBallSqr = (ball.pos - circleCenter).sqrMagnitude;

            //If that distance is greater than this, the ball is outside
            float maxAllowedDist = circleRadius - ball.radius;

            if (distCenterToBallSqr > maxAllowedDist * maxAllowedDist)
            {
                Vector3 wallNormal = (circleCenter - ball.pos).normalized;


                //Move the ball so it's no longer colliding
                ball.pos = (circleRadius - ball.radius) * -1f * wallNormal;


                //Update velocity 

                //Collisions can only change velocity components along the penetration direction
                float v = Vector3.Dot(ball.vel, wallNormal);

                float vNew = Mathf.Abs(v) * restitution;

                //Remove the old velocity and add the new velocity
                ball.vel += wallNormal * (vNew - v);
            }
        }



        private void GenerateCircleMesh(GameObject floorGO, Vector3 circleCenter, float radius, int segments)
        {
            //Generate the vertices and the indices
            int circleResolution = 100;

            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();

            vertices.Add(circleCenter);

            float angleStep = 360f / circleResolution;

            float angle = 0f;

            for (int i = 0; i < circleResolution + 1; i++)
            {
                float x = radius * Mathf.Cos(angle * Mathf.Deg2Rad);
                float y = radius * Mathf.Sin(angle * Mathf.Deg2Rad);

                Vector3 vertex = new Vector3(x, 0f, y) + circleCenter;

                vertices.Add(vertex);

                angle += angleStep;
            }


            //Generate the indices
            for (int i = 2; i < vertices.Count; i++)
            {
                triangles.Add(0);
                triangles.Add(i);
                triangles.Add(i - 1);
            }

            //Generate the mesh
            Mesh m = new Mesh();

            m.SetVertices(vertices);
            m.SetTriangles(triangles, 0);

            m.RecalculateNormals();

            floorGO.GetComponent<MeshFilter>().sharedMesh = m;
        }
    }
}