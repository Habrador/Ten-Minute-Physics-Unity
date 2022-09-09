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

    //Gravitational constant G
    //private readonly float G = 6.674f * Mathf.Pow(10f, -11f); 
    //Our planets masses are small, so we need a much larger G, or no movement will happen
    private readonly float G = 1f;



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

        //We first have to calculate all accelerations
        //We cant add the acceleration at once because it will change position of the planet, which is needed for the other planets
        List<Vector3> accelerations = new ();

        //TODO: Optimize this so we don't need to make the same calculation twice because F acts in the opposite direction as well
        for (int i = 0; i < allPlanets.Count; i++)
        {
            Planet thisPlanet = allPlanets[i];

            Vector3 accelerationVector = Vector3.zero;

            //Check all other planets
            for (int j = 0; j < allPlanets.Count; j++)
            {
                //Dont check the planet itself
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

                //Planets can intersect so rSqr will go to infinity, making F really big, so we need to clamp
                //We also need to clamp if they are too far apart or F will be really small and the planet will never return
                rSqr = Mathf.Clamp(rSqr, 0.1f, 100f);

                float F = G * ((m1 * m2) / rSqr);

                //F = m * a
                float a = F / m1;

                accelerationVector += a * thisOtherVec.normalized;
            }

            //Debug.Log(accelerationVector.magnitude);

            accelerations.Add(accelerationVector);
        }


        //Simulate each planet
        for (int i = 0; i < allPlanets.Count; i++)
        {
            Planet thisPlanet = allPlanets[i];

            thisPlanet.SimulatePlanet(subSteps, sdt, accelerations[i]);

            //Debug.Log(accelerations[i].magnitude);

            Debug.DrawRay(thisPlanet.pos, accelerations[i].normalized);
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
