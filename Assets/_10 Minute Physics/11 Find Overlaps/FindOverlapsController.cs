using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Billiard;

//Find overlaps among thousands of objects blazing fast

//Implementation of the Spatial Partition Design Pattern where you split the scene into a grid
//Basically same as "03 Billiard" so reusing code from that project
//Based on https://www.youtube.com/watch?v=D2M8jTtKi44
public class FindOverlapsController : MonoBehaviour
{
    //Public
    public GameObject ballPrefabGO;


    //Private

    //The data structure we will use to improve the performance of ball-ball collision
    private SpatialHashing spatialHashing;

    //Grid
    private PlayArea grid;
    //Grid settings
    private readonly float cellSize = 0.2f;
    private readonly int numberOfCells = 20;

    //Simulation properties
    private readonly int subSteps = 5;

    //How much velocity is lost after collision between balls [0, 1]
    //Is usually called e
    //Elastic: e = 1 means same velocity after collision (if the objects have the same size and same speed)
    //Inelastic: e = 0 means no velocity after collions (if the objects have the same size and same speed) and energy is lost
    private readonly float restitution = 1f;

    //Balls
    private readonly int numberOfBalls = 100;

    private List<BilliardBall> allBalls;

    //The position of each ball
    private Vector3[] ballPositions;

    //To see the difference between the collision algorithms
    private int collisionChecks = 0;
    private int actualCollisions = 0;

    //Which collision algorithm are we going to use?
    private enum CollisionAlgorithm
    {
        BruteForce,
        SpatialHashing
    }

    private CollisionAlgorithm activeCollisionAlgorithm = CollisionAlgorithm.SpatialHashing;



    private void Start()
    {
        spatialHashing = new SpatialHashing(cellSize, numberOfBalls);

        grid = new PlayArea(numberOfCells, cellSize);

        ballPositions = new Vector3[numberOfBalls];


        //Center camera on grid
        Camera thisCamera = Camera.main;

        Vector3 cameraPos = grid.GridCenter;

        cameraPos.y = thisCamera.transform.position.y;

        thisCamera.transform.position = cameraPos;


        ResetSimulation();
    }



    private void ResetSimulation()
    {
        allBalls = new List<BilliardBall>();

        Vector2 mapSize = new(grid.GridWidth, grid.GridWidth);

        //Add balls within an area
        SetupBalls.AddRandomBallsWithinRectangle(ballPrefabGO, numberOfBalls, allBalls, cellSize, cellSize, mapSize, grid.GridCenter);

        BilliardMaterials.GiveBallsRandomColor(ballPrefabGO, allBalls);

        //Give each ball a velocity
        foreach (BilliardBall b in allBalls)
        {
            float maxVel = 1f;

            float randomVelX = Random.Range(-maxVel, maxVel);
            float randomVelZ = Random.Range(-maxVel, maxVel);

            Vector3 randomVel = new (randomVelX, 0f, randomVelZ);

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
        collisionChecks = 0;
        actualCollisions = 0;
    
        float sdt = Time.fixedDeltaTime / (float)subSteps;

        if (activeCollisionAlgorithm == CollisionAlgorithm.BruteForce)
        {
            UpdateBallsBruteForce(sdt);
        }
        else
        {
            UpdateBallsSpatialHashing(sdt);
        }
    }



    private void LateUpdate()
    {
        grid.DisplayGrid();
    }



    private void OnGUI()
    {
        MyOnGUI();
    }



    //The old slow way of moving balls and doing ball-ball collision
    private void UpdateBallsBruteForce(float sdt)
    {
        for (int i = 0; i < allBalls.Count; i++)
        {
            BilliardBall thisBall = allBalls[i];

            thisBall.SimulateBall(subSteps, sdt);

            //Check collision with the other balls after this ball in the list of all balls
            for (int j = i + 1; j < allBalls.Count; j++)
            {
                BilliardBall ballOther = allBalls[j];

                bool areColliding = BallCollisionHandling.HandleBallBallCollision(thisBall, ballOther, restitution);

                collisionChecks++;

                if (areColliding)
                {
                    actualCollisions++;
                }
            }

            grid.HandleBallEnvironmentCollision(thisBall);
        }
    }



    //The new faster way of moving balls and doing ball-ball collision
    private void UpdateBallsSpatialHashing(float sdt)
    {
        //Step 1. Move all balls and handle environment collisions
        foreach (BilliardBall thisBall in allBalls)
        {
            thisBall.SimulateBall(subSteps, sdt); 
            
            grid.HandleBallEnvironmentCollision(thisBall);
        }


        //Step 2. Add all balls to the grid data structure
        for (int i = 0; i < allBalls.Count; i++)
        {
            ballPositions[i] = allBalls[i].pos;
        }

        spatialHashing.AddParticlesToGrid(ballPositions);

        //For debugging
        //grid.DisplayDataStructures();


        //Step 3. Handle collision with this ball and other balls by using the grid data structure
        for (int i = 0; i < allBalls.Count; i++)
        {
            BilliardBall thisBall = allBalls[i];

            Vector2Int ballCellPos = spatialHashing.ConvertFromWorldToCell(thisBall.pos);

            //Check this cell and 8 surrounding cells for other balls
            //We are in an unbounded grid so we dont need to check if a surroundig cell is within the grid 
            foreach (Vector2Int cell in SpatialHashing.cellCoordinates)
            {
                Vector2Int cellPos = ballCellPos + cell;

                int arrayIndex = spatialHashing.Get1DArrayIndex(cellPos);

                //The index of the first ball in the allParticlesArray
                //AllParticlesArray references the allBalls array, so we have 3 arrays coordinating with each other
                int firstBallIndex = spatialHashing.table[arrayIndex];

                //How many balls in this cell?
                int numberOfBalls = spatialHashing.table[arrayIndex + 1] - firstBallIndex;
                
                //Loop through all balls in this cell and check for collision
                for (int j = firstBallIndex; j < firstBallIndex + numberOfBalls; j++)
                {
                    Ball otherBall = allBalls[spatialHashing.sortedParticles[j]];

                    bool areColliding = BallCollisionHandling.HandleBallBallCollision(thisBall, otherBall, restitution);

                    collisionChecks++;

                    if (areColliding)
                    {
                        actualCollisions++;
                    }
                }
            }
        }
    }



    //Buttons to select which collision algorithm to use
    //Text to display info to see the difference between the algorithms
    public void MyOnGUI()
    {
        GUILayout.BeginHorizontal("box");

        int fontSize = 20;

        RectOffset offset = new(5, 5, 5, 5);


        //Buttons
        GUIStyle buttonStyle = new(GUI.skin.button)
        {
            //buttonStyle.fontSize = 0; //To reset because fontSize is cached after you set it once 

            fontSize = fontSize,
            margin = offset
        };

        if (GUILayout.Button($"Brute Force", buttonStyle))
        {
            activeCollisionAlgorithm = CollisionAlgorithm.BruteForce;
        }
        if (GUILayout.Button("Spatial Hashing", buttonStyle))
        {
            activeCollisionAlgorithm = CollisionAlgorithm.SpatialHashing;
        }



        //Text
        string infoText = $"Spheres: {numberOfBalls} | Collision checks / frame: {collisionChecks} | Actual collisions / frame: {actualCollisions}";

        GUIStyle textStyle = GUI.skin.GetStyle("Label");

        textStyle.fontSize = fontSize;
        textStyle.margin = offset;

        GUILayout.Label(infoText, textStyle);


        GUILayout.EndHorizontal();
    }
}
