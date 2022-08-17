using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Billiard
{
    //Add billiard balls with different configurations
    public static class SetupBalls
    {
        //Add balls on the circumference of a circle
        public static void AddBallsOnCircle(GameObject ballPrefabGO, int numberOfBalls, List<BilliardBall> allBalls, float ballRadius, float circleRadius)
        {
            List<Vector3> ballPositons = UsefulMethods.GetCircleSegments_XZ(Vector3.zero, circleRadius, numberOfBalls);

            //Debug.Log(ballPositons.Count);

            for (int i = 0; i < ballPositons.Count - 1; i++)
            {
                GameObject newBallGO = GameObject.Instantiate(ballPrefabGO);

                //Pos
                newBallGO.transform.position = ballPositons[i];

                //Scale
                newBallGO.transform.localScale = Vector3.one * ballRadius;

                //Add the actual ball
                BilliardBall newBall = new(newBallGO.transform);

                allBalls.Add(newBall);
            }
        }



        //Add balls randomly within a rectangle
        public static void AddRandomBallsWithinRectangle(GameObject ballPrefabGO, int numberOfBalls, List<BilliardBall> allBalls, float minBallRadius, float maxBallRadius, Vector2 rectangleSize)
        {            
            for (int i = 0; i < numberOfBalls; i++)
            {
                GameObject newBallGO = GameObject.Instantiate(ballPrefabGO);

                //Random size
                float randomSize = Random.Range(minBallRadius, maxBallRadius);

                newBallGO.transform.localScale = Vector3.one * randomSize;

                //Random pos within rectangle
                float halfBallSize = randomSize * 0.5f;

                float halfWidthX = rectangleSize.x * 0.5f;
                float halfWidthY = rectangleSize.y * 0.5f;

                float randomPosX = Random.Range(-halfWidthX + halfBallSize, halfWidthX - halfBallSize);
                float randomPosZ = Random.Range(-halfWidthY + halfBallSize, halfWidthY - halfBallSize);

                Vector3 randomPos = new(randomPosX, 0f, randomPosZ);

                newBallGO.transform.position = randomPos;

                //Add the actual ball
                BilliardBall newBall = new(newBallGO.transform);

                allBalls.Add(newBall);
            }
        }



        //Add balls randomly within a circle
        public static void AddRandomBallsWithinCircle(GameObject ballPrefabGO, int numberOfBalls, List<BilliardBall> allBalls, float minBallRadius, float maxBallRadius, float circleRadius)
        { 
            for (int i = 0; i < numberOfBalls; i++)
            {
                GameObject newBallGO = GameObject.Instantiate(ballPrefabGO);

                //Random size
                float randomSize = Random.Range(minBallRadius, maxBallRadius);

                newBallGO.transform.localScale = Vector3.one * randomSize;

                //Random pos within circle
                Vector2 randomPos2D = Random.insideUnitCircle * (circleRadius - (randomSize * 0.5f));

                Vector3 randomPos = new(randomPos2D.x, 0f, randomPos2D.y);

                newBallGO.transform.position = randomPos;

                //Add the actual ball
                BilliardBall newBall = new(newBallGO.transform);

                allBalls.Add(newBall);
            }
        }



        //When we add random balls they might intersect with each other
        //Iterate through all balls and try to make sure they dont collide
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