using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Billiard;

public class DebugDataStructure : MonoBehaviour
{
    public List<Transform> allBallTransforms;

    //Grid
    private GridMap grid;
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

        grid = new GridMap(numberOfCells, cellSize, allBalls.Count);
    }


    private void Update()
    {
        //Update balls list
        allBalls.Clear();

        foreach (Transform t in allBallTransforms)
        {
            allBalls.Add(new BilliardBall(Vector3.zero, t));
        }


        grid.AddParticlesToGrid(allBalls);


        grid.DisplayDataStructures();


        isColliding = false;


        for (int i = 0; i < allBalls.Count; i++)
        {
            BilliardBall thisBall = allBalls[i];

            Vector2Int ballCellPos = grid.ConvertFromWorldToCell(thisBall.pos);

            int ballArrayPos = grid.Get1DArrayIndex(ballCellPos);

            //Check this cell and 8 surrounding cells for other balls
            //We are using spatial hashing so we dont need to check if a surroundig cell is within the grid because the grid is infinite 
            foreach (Vector2Int cell in GridMap.cellCoordinates)
            {
                Vector2Int cellPos = ballCellPos + cell;

                int arrayIndex = grid.Get1DArrayIndex(cellPos);

                ///Debug.Log(ballArrayPos + " " + arrayIndex);

                int particlePos = grid.particlesInCells[arrayIndex];

                //How many balls in this cell?
                int numberOfParticles = grid.particlesInCells[arrayIndex + 1] - particlePos;

                //if (arrayIndex == 7)
                //{
                //    Debug.Log(numberOfParticles);
                //}
                    

                for (int j = particlePos; j < particlePos + numberOfParticles; j++)
                {
                    Ball otherBall = allBalls[grid.particles[j]];

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
