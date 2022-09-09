using System.Collections;
using System.Collections.Generic;
using UnityEngine;


//The 3-body-problems with planets interacting with each other
//https://en.wikipedia.org/wiki/Three-body_problem
//You can add how many bodies you want, so its more like N-Body Problem
public class ThreeBodyProblemController : MonoBehaviour
{
    //
    // Public
    //

    public GameObject planetPrefabGO;


    //
    // Private
    //
    
    private const int SEED = 0;

    //Planets settings
    private readonly List<Planet> allPlanets = new();

    private const int NUMBER_OF_PLANETS = 3;

    private readonly Vector2 minMaxplanetRadius = new (0.1f, 2f);

    //Simulation settings
    private readonly int subSteps = 1;

    //Gravitational constant 6.674×10−11
    private readonly float G = 6.674f * Mathf.Pow(10f, -11f); 



    private void Start()
    {
        Random.InitState(SEED);


        //Generate the planets
        AddPlanets();

        //Give each ball a velocity
        //foreach (Planet p in allPlanets)
        //{
        //    float maxVel = 4f;

        //    float randomVelX = Random.Range(-maxVel, maxVel);
        //    float randomVelZ = Random.Range(-maxVel, maxVel);

        //    Vector3 randomVel = new Vector3(randomVelX, 0f, randomVelZ);

        //    p.vel = randomVel;
        //}


        //Debug.Log(G);
    }



    private void Update()
    {
        foreach (Planet p in allPlanets)
        {
            p.UpdateVisualPosition();
        }
    }



    private void FixedUpdate()
    {
        float sdt = Time.fixedDeltaTime / (float)subSteps;

        for (int i = 0; i < allPlanets.Count; i++)
        {
            Planet thisPlanet = allPlanets[i];

            Vector3 accelerationVector = Vector3.zero;

            //Check all other planets
            for (int j = 0; j < allPlanets.Count; j++)
            {
                if (i == j)
                {
                    continue;
                }
            
                Planet otherPlanet = allPlanets[j];

                //Use Newton's law of universal gravitation to simulate the planets
                //F = G * (m1 * m2) / r^2

                float m1 = thisPlanet.mass;
                float m2 = otherPlanet.mass;

                Vector3 thisOtherVec = otherPlanet.pos - thisPlanet.pos;

                float rSqr = thisOtherVec.sqrMagnitude;

                float F = G * ((m1 * m2) / rSqr);

                //F = m * a
                float a = F / m1;

                accelerationVector += a * thisOtherVec.normalized;
            }

            accelerationVector *= 3000000000f;

            //Debug.Log(accelerationVector.magnitude);

            thisPlanet.SimulatePlanet(subSteps, sdt, accelerationVector);
        }
    }



    private void AddPlanets()
    {
        Vector2 mapSize = new(10f, 14f);

        for (int i = 0; i < NUMBER_OF_PLANETS; i++)
        {
            GameObject newBallGO = GameObject.Instantiate(planetPrefabGO);

            //Random size
            float randomSize = Random.Range(minMaxplanetRadius.x, minMaxplanetRadius.y);

            newBallGO.transform.localScale = Vector3.one * randomSize;

            //Random pos within rectangle
            float halfBallSize = randomSize * 0.5f;

            float halfWidthX = mapSize.x * 0.5f;
            float halfWidthY = mapSize.y * 0.5f;

            float randomPosX = Random.Range(-halfWidthX + halfBallSize, halfWidthX - halfBallSize);
            float randomPosZ = Random.Range(-halfWidthY + halfBallSize, halfWidthY - halfBallSize);

            Vector3 randomPos = new(randomPosX, 0f, randomPosZ);

            newBallGO.transform.position = randomPos;

            //Add the actual planet
            Planet newPlanet = new(newBallGO.transform);

            allPlanets.Add(newPlanet);
        }


        //Color them
        Material ballBaseMaterial = planetPrefabGO.GetComponent<MeshRenderer>().sharedMaterial;

        foreach (Planet p in allPlanets)
        {
            Material randomBallMaterial = Billiard.BilliardMaterials.GetRandomBilliardBallMaterial(ballBaseMaterial);

            p.ballTransform.GetComponent<MeshRenderer>().material = randomBallMaterial;
        }
    }
}
