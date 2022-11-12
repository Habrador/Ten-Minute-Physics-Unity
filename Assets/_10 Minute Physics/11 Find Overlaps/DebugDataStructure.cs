using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Billiard;

//Only used to find bugs in the grid class
public class DebugDataStructure : MonoBehaviour
{
    public List<Transform> allBallTransforms;

    private SpatialHashing spatialHashing;

    //Grid
    private PlayArea grid;
    //Grid settings
    private readonly float cellSize = 0.2f;
    private readonly int numberOfCells = 20;

    private readonly float ballRadius = 0.2f;

    private List<BilliardBall> allBalls;


    private bool isColliding;



    private void Start()
    {
        foreach (Transform t in allBallTransforms)
        {
            t.localScale = Vector3.one * ballRadius;
        }


        allBalls = new List<BilliardBall>();

        foreach (Transform t in allBallTransforms)
        {
            allBalls.Add(new BilliardBall(Vector3.zero, t));
        }

        spatialHashing = new SpatialHashing(cellSize, allBalls.Count);

        grid = new PlayArea(numberOfCells, cellSize);
    }


    private void Update()
    {
        //Update balls list
        allBalls.Clear();

        foreach (Transform t in allBallTransforms)
        {
            allBalls.Add(new BilliardBall(Vector3.zero, t));
        }

        List<Vector3> ballPositions = new ();

        for (int i = 0; i < allBalls.Count; i++)
        {
            ballPositions.Add(allBalls[i].pos);
        }

        spatialHashing.AddParticlesToGrid(ballPositions);


        spatialHashing.DisplayDataStructures();


        isColliding = false;


        for (int i = 0; i < allBalls.Count; i++)
        {
            BilliardBall thisBall = allBalls[i];

            Vector2Int ballCellPos = spatialHashing.ConvertFromWorldToCell(thisBall.pos);

            int ballArrayPos = spatialHashing.Get1DArrayIndex(ballCellPos);

            //Check this cell and 8 surrounding cells for other balls
            //We are using spatial hashing so we dont need to check if a surroundig cell is within the grid because the grid is infinite 
            foreach (Vector2Int cell in SpatialHashing.cellCoordinates)
            {
                Vector2Int cellPos = ballCellPos + cell;

                int arrayIndex = spatialHashing.Get1DArrayIndex(cellPos);

                ///Debug.Log(ballArrayPos + " " + arrayIndex);

                int particlePos = spatialHashing.tableArray[arrayIndex];

                //How many balls in this cell?
                int numberOfParticles = spatialHashing.tableArray[arrayIndex + 1] - particlePos;

                //if (arrayIndex == 7)
                //{
                //    Debug.Log(numberOfParticles);
                //}
                    

                for (int j = particlePos; j < particlePos + numberOfParticles; j++)
                {
                    Ball otherBall = allBalls[spatialHashing.allParticles[j]];

                    float dist = (thisBall.pos - otherBall.pos).magnitude;

                    if (arrayIndex == 7)
                    {
                        Debug.Log(thisBall.pos + " " + otherBall.pos);
                    }

                    if (dist == 0f)
                    {
                        Debug.Log("Found the same ball");
                    
                        continue;
                    }

                    if (dist < ballRadius)
                    {
                        //Debug.Log("Collision");

                        isColliding = true;
                    }
                }
            }

            break;
        }
    }



    private void LateUpdate()
    {
        grid.DisplayGrid();
    }



    private void OnGUI()
    {
        GUI.Label(new Rect(10, 10, 100, 20), isColliding.ToString());
    }
}
