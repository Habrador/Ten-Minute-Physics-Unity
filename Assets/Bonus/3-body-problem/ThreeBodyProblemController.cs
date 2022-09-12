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

    private Camera thisCamera;

    private readonly List<Planet> allPlanets = new();

    private readonly bool displayHistory = false;

    private readonly List<List<Vector3>> historicalPositions = new ();

    private readonly List<DisplayShapes.ColorOptions> historicalPositionsColor = new ();


    //Planet settings
    private const int NUMBER_OF_PLANETS = 200;

    private readonly MinMax minMaxPlanetRadius = new (0.1f, 0.4f);


    //Simulation settings
    private const int SUB_STEPS = 1;

    //Add planets within this area
    private readonly Vector2 mapSize = new(14f, 10f);

    //Newton's law of universal gravitation

    //Gravitational constant G
    //private readonly float G = 6.674f * Mathf.Pow(10f, -11f); 
    //Our planets masses are small, so we need a much larger G, or no movement will happen
    private readonly float G = 1f;

    //The square distance at which the equation is valid
    //The planets can intersect so Force will go to infinity if we don't clamp it
    //Also planets far away from the action will never come back if we don't clamp the max value
    private readonly MinMax minMaxRSqr = new(0.3f, 25f);



    private void Start()
    {
        Random.InitState(SEED);

        //Generate the planets
        AddPlanets();

        GivePlanetsRandomColor();

        /*
        if (displayHistory && NUMBER_OF_PLANETS == 3)
        {
            //Assume we have 3 planets which is the 3-body-problem
            historicalPositions.Add(new List<Vector3>());
            historicalPositions.Add(new List<Vector3>());
            historicalPositions.Add(new List<Vector3>());

            historicalPositionsColor.Add(DisplayShapes.ColorOptions.Red);
            historicalPositionsColor.Add(DisplayShapes.ColorOptions.Blue);
            historicalPositionsColor.Add(DisplayShapes.ColorOptions.Yellow);

            allPlanets[0].ballTransform.GetComponent<MeshRenderer>().material.color = Color.red;
            allPlanets[1].ballTransform.GetComponent<MeshRenderer>().material.color = Color.blue;
            allPlanets[2].ballTransform.GetComponent<MeshRenderer>().material.color = Color.yellow;
        }
        */

        //Give each planet a velocity
        //foreach (Planet p in allPlanets)
        //{
        //    float maxVel = 0.5f;

        //    float randomVelX = Random.Range(-maxVel, maxVel);
        //    float randomVelZ = Random.Range(-maxVel, maxVel);

        //    Vector3 randomVel = new Vector3(randomVelX, 0f, randomVelZ);

        //    p.vel = randomVel;
        //}


        //Center the camera
        thisCamera = Camera.main;

        CenterCamera();
    }



    private void Update()
    {
        foreach (Planet p in allPlanets)
        {
            p.UpdateVisualPosition();
        }

        //Make sure all objects are visible on screen
        ZoomCamera();

        //Save history
        if (displayHistory && NUMBER_OF_PLANETS == 3)
        {
            for (int i = 0; i < allPlanets.Count; i++)
            {
                historicalPositions[i].Add(allPlanets[i].pos);
            }
        }
    }



    private void LateUpdate()
    {
        /*
        //Draw the lines connected to the center of mass from each planet
        Vector3 center = GetCenterOfMass();

        List<Vector3> lineSegments = new();

        foreach (Planet p in allPlanets)
        {
            lineSegments.Add(p.pos);
            lineSegments.Add(center);
        }

        DisplayShapes.DrawLineSegments(lineSegments, DisplayShapes.ColorOptions.White);
        */

        //Display the historical positions
        if (displayHistory && NUMBER_OF_PLANETS == 3)
        {
            for (int i = 0; i < historicalPositions.Count; i++)
            {
                DisplayShapes.DrawLine(historicalPositions[i], historicalPositionsColor[i]);
            }
        }
    }



    //Calculate the center of mass of all planets, which should be constant throughout the simulation
    private Vector3 GetCenterOfMass()
    {
        Vector3 centerOfMass = Vector3.zero;

        float totalMass = 0f;

        foreach (Planet p in allPlanets)
        {
            centerOfMass += p.pos * p.mass;

            totalMass += p.mass;
        }

        centerOfMass /= totalMass;

        return centerOfMass;
    }



    //Focus the camera on the center of mass of all planets 
    private void CenterCamera()
    {
        Vector3 center = GetCenterOfMass();

        Vector3 cameraPos = center;

        cameraPos.y = thisCamera.transform.position.y;

        thisCamera.transform.position = cameraPos;
    }



    //Change camera size so all planets are visible on the screen
    private void ZoomCamera()
    {
        //Check if at least one planet is not visible on screen
        bool isVisible = true;

        foreach (Planet p in allPlanets)
        {
            //IMPORTANT: Turn off shadows on the object to make this work
            //Otherwise the object is still considered to be in the screen even though its outside to Unity can calculate its shadows
            if (!p.ballTransform.GetComponent<Renderer>().isVisible)
            {
                isVisible = false;

                break;
            }
        }

        //if (!isVisible)
        //{
        //    Debug.Log("Zoom out");
        //}

        if (isVisible)
        {
            return;
        }

        //Zoom camera
        float zoomSpeed = 0.5f;

        if (isVisible)
        {
            zoomSpeed *= -1f;
        }

        float size = thisCamera.orthographicSize;

        size += zoomSpeed * Time.deltaTime;

        size = Mathf.Clamp(size, 5f, 100f);

        Camera.main.orthographicSize = size;
    }



    private void FixedUpdate()
    {
        //We first have to calculate all accelerations
        //We cant add the acceleration at once to each planet because it will change position of the planet, which is needed for the other planets
        Vector3[] accelerations = CalculateAccelerations();


        //Simulate each planet
        float sdt = Time.fixedDeltaTime / (float)SUB_STEPS;

        for (int i = 0; i < allPlanets.Count; i++)
        {
            Planet thisPlanet = allPlanets[i];

            thisPlanet.SimulatePlanet(SUB_STEPS, sdt, accelerations[i]);

            //Debug.Log(accelerations[i].magnitude);

            //Debug.DrawRay(thisPlanet.pos, accelerations[i].normalized);
        }
    }



    //Calculate the acceleration each planet should have this time step
    private Vector3[] CalculateAccelerations()
    {
        Vector3[] accelerations = new Vector3[allPlanets.Count];

        for (int i = 0; i < allPlanets.Count; i++)
        {
            Planet thisPlanet = allPlanets[i];

            //Check all other planets coming after this planet
            for (int j = i + 1; j < allPlanets.Count; j++)
            {
                Planet otherPlanet = allPlanets[j];

                //Use Newton's law of universal gravitation to simulate the planets
                //F = G * (m1 * m2) / r^2

                float m1 = thisPlanet.mass;
                float m2 = otherPlanet.mass;

                Vector3 thisOtherVec = otherPlanet.pos - thisPlanet.pos;

                float rSqr = thisOtherVec.sqrMagnitude;

                //Planets can intersect so rSqr will go to infinity, making F really big, so we need to clamp
                //We also need to clamp if they are too far apart or F will be really small and the planet will never return
                rSqr = Mathf.Clamp(rSqr, minMaxRSqr.min, minMaxRSqr.max);

                float F = G * ((m1 * m2) / rSqr);

                //F = m * a
                float aThisPlanet = F / m1;
                float aOtherPlanet = F / m2;

                //F with direction
                accelerations[i] += aThisPlanet * thisOtherVec.normalized;
                accelerations[j] += aOtherPlanet * -thisOtherVec.normalized;
            }
        }

        return accelerations;
    }



    private void AddPlanets()
    {
        for (int i = 0; i < NUMBER_OF_PLANETS; i++)
        {
            GameObject newBallGO = GameObject.Instantiate(planetPrefabGO);

            //Random size
            float randomSize = Random.Range(minMaxPlanetRadius.min, minMaxPlanetRadius.max);

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
    }



    //Re-use the colors from billiards
    private void GivePlanetsRandomColor()
    {
        //Color them
        Material ballBaseMaterial = planetPrefabGO.GetComponent<MeshRenderer>().sharedMaterial;

        foreach (Planet p in allPlanets)
        {
            Material randomBallMaterial = Billiard.BilliardMaterials.GetRandomBilliardBallMaterial(ballBaseMaterial);

            p.ballTransform.GetComponent<MeshRenderer>().material = randomBallMaterial;
        }
    }
}
