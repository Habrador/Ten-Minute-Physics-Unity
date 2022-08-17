using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Billiard
{
    //Add billiard balls with different configurations
    public static class SetupBalls
    {
        //Add balls within an area, such as circle, rectangle
        public static void AddBallsWithinArea(GameObject ballPrefabGO, int numberOfBalls, List<BilliardBall> allBalls, float ballRadius)
        {
            Material ballBaseMaterial = ballPrefabGO.GetComponent<MeshRenderer>().sharedMaterial;

            //Create random balls
            for (int i = 0; i < numberOfBalls; i++)
            {
                GameObject newBallGO = GameObject.Instantiate(ballPrefabGO);


                //Random color
                Material randomBallMaterial = BilliardMaterials.GetRandomBilliardBallMaterial(ballBaseMaterial);

                newBallGO.GetComponent<MeshRenderer>().material = randomBallMaterial;


                //Scale
                newBallGO.transform.localScale = Vector3.one * ballRadius;


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

                BilliardBall newBall = new(newBallGO.transform);

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
        public static void AddBallsOnMiniCircle(GameObject ballPrefabGO, int numberOfBalls, List<BilliardBall> allBalls, float ballRadius, float miniCircleRadius)
        {
            Material ballBaseMaterial = ballPrefabGO.GetComponent<MeshRenderer>().sharedMaterial;

            Vector3 ballsCenter = Vector3.zero;

            List<Vector3> ballPositons = UsefulMethods.GetCircleSegments_XZ(ballsCenter, miniCircleRadius, numberOfBalls);

            //Debug.Log(ballPositons.Count);

            for (int i = 0; i < ballPositons.Count - 1; i++)
            {
                GameObject newBallGO = GameObject.Instantiate(ballPrefabGO);


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
                newBallGO.transform.localScale = Vector3.one * ballRadius;


                //Add the actual ball
                BilliardBall newBall = new(newBallGO.transform);

                allBalls.Add(newBall);
            }
        }



        //Add balls within the entire map and move them apart from each other so they dont collide
        public static void AddRandomBallsWithinRectangle(GameObject ballPrefabGO, int numberOfBalls, List<BilliardBall> allBalls, float minBallRadius, float maxBallRadius, Vector2 mapSize)
        {
            Material ballBaseMaterial = ballPrefabGO.GetComponent<MeshRenderer>().sharedMaterial;

            //Create random balls
            for (int i = 0; i < numberOfBalls; i++)
            {
                GameObject newBallGO = GameObject.Instantiate(ballPrefabGO);


                //Random color
                Material randomBallMaterial = BilliardMaterials.GetRandomBilliardBallMaterial(ballBaseMaterial);

                newBallGO.GetComponent<MeshRenderer>().material = randomBallMaterial;


                //Random size
                //Size has to be before pos so we can take the radius into account
                float randomSize = Random.Range(minBallRadius, maxBallRadius);

                newBallGO.transform.localScale = Vector3.one * randomSize;


                //Random pos within rectangle
                float randomPosX = Random.Range(-mapSize.x * 0.5f, mapSize.x * 0.5f);
                float randomPosZ = Random.Range(-mapSize.y * 0.5f, mapSize.y * 0.5f);

                Vector3 randomPos = new(randomPosX, 0f, randomPosZ);

                //Random pos within circle
                //Vector2 randomPos2D = Random.insideUnitCircle * (5f - (randomSize * 0.5f));

                //Vector3 randomPos = new(randomPos2D.x, 0f, randomPos2D.y);

                newBallGO.transform.position = randomPos;


                BilliardBall newBall = new(newBallGO.transform);

                allBalls.Add(newBall);
            }
        }



        //Iterate through all balls and make sure they dont collide
        public static void MoveAllBallsApart(List<BilliardBall> allBalls, BilliardTable billiardTable)
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

                        TryMoveTwoBallsApart(thisBall, otherBall, billiardTable);
                    }
                }
            }
        }



        //Given two balls, test if they intersect, if so move them apart if they dont end up outside of the circle
        private static void TryMoveTwoBallsApart(Ball b1, Ball b2, BilliardTable billiardTable)
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

                if (!billiardTable.IsBallOutsideOfTable(b1_NewPos, b1.radius))
                {
                    b1.pos = b1_NewPos;
                }
                if (!billiardTable.IsBallOutsideOfTable(b2_NewPos, b2.radius))
                {
                    b2.pos = b2_NewPos;
                }
            }
        }
    }
}