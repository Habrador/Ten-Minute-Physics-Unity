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

    //Planets settings
    private readonly List<Planet> allPlanets = new();

    private const int NUMBER_OF_PLANETS = 5;

    private readonly MinMax minMaxPlanetRadius = new (0.5f, 1.0f);

    //Simulation settings
    private readonly int subSteps = 1;

    //Newton's law of universal gravitation
    
    //Gravitational constant G
    //private readonly float G = 6.674f * Mathf.Pow(10f, -11f); 
    //Our planets masses are small, so we need a much larger G, or no movement will happen
    private readonly float G = 50f;

    //The square distance at which the equation is valid
    //The planets can intersect so Force will go to infinity if we don't clamp it
    //Also planets far away from the action will never come back if we don't clamp the max value
    private readonly MinMax minMaxRSqr = new(0.3f, 25f);



    private void Start()
    {
        Random.InitState(SEED);

        //Generate the planets
        AddPlanets();

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
    }



    private void LateUpdate()
    {
        //Draw the lines connected to the center of mass from each planet
        Vector3 center = GetCenterOfMass();

        List<Vector3> lineSegments = new ();

        foreach (Planet p in allPlanets)
        {
            lineSegments.Add(p.pos);
            lineSegments.Add(center);
        }

        DisplayShapes.DrawLineSegments(lineSegments, DisplayShapes.ColorOptions.White);
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
            if (!p.ballTransform.GetComponent<Renderer>().isVisible)
            {
                isVisible = false;

                break;
            }
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
        float sdt = Time.fixedDeltaTime / (float)subSteps;

        //We first have to calculate all accelerations
        //We cant add the acceleration at once because it will change position of the planet, which is needed for the other planets
        List<Vector3> accelerations = new ();

        foreach (Planet p in allPlanets)
        {
            accelerations.Add(Vector3.zero);
        }

        
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


        //Simulate each planet
        for (int i = 0; i < allPlanets.Count; i++)
        {
            Planet thisPlanet = allPlanets[i];

            thisPlanet.SimulatePlanet(subSteps, sdt, accelerations[i]);

            //Debug.Log(accelerations[i].magnitude);

            //Debug.DrawRay(thisPlanet.pos, accelerations[i].normalized);
        }
    }



    private void AddPlanets()
    {
        Vector2 mapSize = new(10f, 14f);

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


        //Color them
        Material ballBaseMaterial = planetPrefabGO.GetComponent<MeshRenderer>().sharedMaterial;

        foreach (Planet p in allPlanets)
        {
            Material randomBallMaterial = Billiard.BilliardMaterials.GetRandomBilliardBallMaterial(ballBaseMaterial);

            p.ballTransform.GetComponent<MeshRenderer>().material = randomBallMaterial;
        }
    }
}
