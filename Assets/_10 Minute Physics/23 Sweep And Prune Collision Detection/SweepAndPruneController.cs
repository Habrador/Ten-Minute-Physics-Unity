using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

//Based on "23 Broad phase collision detection with Sweep and Prune"
//from https://matthias-research.github.io/pages/tenMinutePhysics/index.html
//Is similar to "11 Finding overlaps among thousands of objects blazing fast"
//but can handle object of different sizes
//Broad phase means it's only doing collision detection of each object's AABB
//If you need more detailed collision detection you how do add it afterwards
//Simulation is in 2d space: x,y
public class SweepAndPruneController : MonoBehaviour
{
    public GameObject spherePrefabObj;

    //Which collision algorithm are we going to use?
    private enum CollisionAlgorithm 
    {
        BruteForce,
        SweepPrune
    }

    private CollisionAlgorithm activeCollisionAlgorithm = CollisionAlgorithm.SweepPrune;

    //To see the difference between the collision algorithms
    private int collisionChecks = 0;
    private int actualCollisions = 0;

    //Size of border
    //Collision detection assumes bottom left of map is at 0,0
    private const float borderSizeX = 20f;
    private const float borderSizeY = 10f;
    //To display the border
    private Material borderMaterial;
    private Mesh borderMesh;

    //Discs
    //Simulation data belonging to each disc
    private List<Disc> allDiscs;
    //The gameobject belonging to each disc so we can see it
    private List<Transform> visualDiscs;
    //How mmany disc we simulate
    private const int totalDiscs = 200;

    //Simulation settings

    //To get the same simulation every time
    private const int seed = 0;

    //Run multiple physics steps per frame
    private const int numSubsteps = 1;

    //Restitution coefficient = how bouncy the spheres are if they collide
    private const float e = 1f;



    private void Start()
    {
        Random.InitState(seed);

        allDiscs = new();
        visualDiscs = new();

        //Add random disc
        for (int i = 0; i < totalDiscs; i++)
        {
            //The data needed to simulate each sphere

            //float radius = Mathf.Floor(Mathf.Pow(Random.value, 10f) * 20f + 2f)
            //The pow operation skews the distribution of the random number towards smaller values
            float random_01_skewed = Mathf.Pow(Random.value, 10f);
            //Make the discs because now some of them are close to 0, and make sure they have some min size
            float radius = random_01_skewed * 10f + 2f;
            //Make them equal size
            radius = Mathf.Floor(radius);
            //Scale (we cant scale before floor because then they will be zero)
            radius *= 0.1f;
            
            //Random pos within the border 
            float x = Random.value * (borderSizeX - radius * 2f) + radius;
            float y = Random.value * (borderSizeY - radius * 2f) + radius;

            //Random velocity between -1 and 1 multiplied by some scale
            float velocityScale = 2f;

            float vx = (Random.value * 2f - 1f) * velocityScale;
            float vy = (Random.value * 2f - 1f) * velocityScale;
            
            allDiscs.Add(new Disc(x, y, vx, vy, radius));


            //Initialize the gameobject sphere which we can actually see
            Vector3 spherePos = new(x, y, 0f);

            //Scale is diameter
            Vector3 sphereScale = radius * 2f * Vector3.one;

            GameObject newVisualSphereObj = Instantiate(spherePrefabObj, spherePos, Quaternion.identity);

            newVisualSphereObj.transform.localScale = sphereScale;

            //Random color
            //The tutorial uses //this.color = `hsl(${ Math.random() * 360}, 70 %, 50 %)`; but hsl is not easily available in Unity
            newVisualSphereObj.GetComponent<MeshRenderer>().material.color = Random.ColorHSV();
            
            visualDiscs.Add(newVisualSphereObj.transform);
        }
    }



    private void Update()
    {
        //return;

        //Update the visual position of the discs
        for (int i = 0; i < visualDiscs.Count; i++)
        {
            Disc discData = allDiscs[i];

            Vector3 spherePos = new(discData.x, discData.y, 0f);

            visualDiscs[i].position = spherePos;
        }
    }



    private void LateUpdate()
    {
        DisplayBorder();
    }



    private void FixedUpdate()
    {
        //return;
    
        //Reset the counters
        collisionChecks = 0;
        actualCollisions = 0;
        
        //Disc simulation
        float dt = Time.fixedDeltaTime;

        float subDt = dt / (float)numSubsteps;
        
        for (int step = 0; step < numSubsteps; step++)
        {
            //Move each disc and make sure the disc is not outside of the border
            foreach (Disc disc in allDiscs)
            {
                disc.UpdatePos(subDt);

                SolveDiscBorderCollision(disc);
            }

            //Check for collisions with other discs
            if (activeCollisionAlgorithm == CollisionAlgorithm.BruteForce)
            {
                BruteForceCollisions(allDiscs);
            }
            else
            {
                SweepAndPruneCollisions(allDiscs);
            }
        }
    }



    private void OnGUI()
    {
        MyOnGUI();
    }



    private void SolveDiscBorderCollision(Disc disc)
    {
        //Check if the ball ended outside of the map
        //If so move it inside and invert the vel component
        //This assumes maps bottom-left corner is at 0,0
        if (disc.x - disc.radius < 0f)
        {
            disc.x = disc.radius;
            disc.vx *= -1f;
        }
        if (disc.x + disc.radius > borderSizeX)
        {
            disc.x = borderSizeX - disc.radius;
            disc.vx *= -1f;
        }
        if (disc.y + disc.radius > borderSizeY)
        {
            disc.y = borderSizeY - disc.radius;
            disc.vy *= -1f;
        }
        if (disc.y - disc.radius < 0f)
        {
            disc.y = disc.radius;
            disc.vy *= -1f;
        }
    }



    //Check if two discs are colliding
    //If so push them apart and update velocity
    private void SolveCollision(Disc disc_1, Disc disc_2)
    {
        collisionChecks++;

        bool areColliding = BallCollisionHandling.HandleDiscDiscCollision(disc_1, disc_2, e);

        if (areColliding)
        {
            actualCollisions++;
        }
    }



    //Slow collision detection where we check each disc against all other discs
    private void BruteForceCollisions(List<Disc> discs)
    {
        for (int i = 0; i < discs.Count; i++)
        {
            for (int j = i + 1; j < discs.Count; j++)
            {
                SolveCollision(discs[i], discs[j]);
            }
        }
    }



    //Fast collision detection where we first sort all discs by their left-border x coordinate
    private void SweepAndPruneCollisions(List<Disc> discs)
    {
        //Sort all discs AABB by their left edge
        List<Disc> sortedDiscs = discs.OrderBy(disc => disc.Left).ToList();

        for (int i = 0; i < sortedDiscs.Count; i++)
        {
            Disc disc_1 = sortedDiscs[i];

            for (int j = i + 1; j < sortedDiscs.Count; j++)
            {
                Disc disc_2 = sortedDiscs[j];

                //If the left side of the sphere to the right is on the right side of the sphere to the left we know they cant collide
                if (disc_2.Left > disc_1.Right)
                {
                    break;
                }

                //disc_2 is not above or below disc_1 so can collide
                if (Mathf.Abs(disc_1.y - disc_2.y) <= disc_1.radius + disc_2.radius)
                {
                    SolveCollision(disc_1, disc_2);
                }
            }
        }
    }



    //Display the border with line segments
    public void DisplayBorder()
    {
        //Display the grid with lines
        if (borderMaterial == null)
        {
            borderMaterial = new Material(Shader.Find("Unlit/Color"));

            borderMaterial.color = Color.black;
        }

        if (borderMesh == null)
        {
            //The 4 corners of the border
            Vector3 BL = new(0f, 0f, 0f);
            Vector3 BR = new(borderSizeX, 0f, 0f);
            Vector3 TL = new(0f, borderSizeY, 0f);
            Vector3 TR = new(borderSizeX, borderSizeY, 0f);

            //Generate line segments
            List<Vector3> lineVertices = new();

            lineVertices.Add(BL);
            lineVertices.Add(BR);
            lineVertices.Add(TR);
            lineVertices.Add(TL);
            lineVertices.Add(BL);


            //Generate the indices
            List<int> indices = new();

            for (int i = 0; i < lineVertices.Count; i++)
            {
                indices.Add(i);
            }


            //Generate the mesh
            borderMesh = new();

            borderMesh.SetVertices(lineVertices);
            borderMesh.SetIndices(indices, MeshTopology.LineStrip, 0);
        }

        //Display the mesh
        Graphics.DrawMesh(borderMesh, Vector3.zero, Quaternion.identity, borderMaterial, 0, Camera.main, 0);
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
        if (GUILayout.Button("Sweep and Prune", buttonStyle))
        {
            activeCollisionAlgorithm = CollisionAlgorithm.SweepPrune;
        }

        

        //Text
        string infoText = $"Spheres: {totalDiscs} | Collision checks / frame: {collisionChecks} | Actual collisions / frame: {actualCollisions}";

        GUIStyle textStyle = GUI.skin.GetStyle("Label");

        textStyle.fontSize = fontSize;
        textStyle.margin = offset;

        GUILayout.Label(infoText, textStyle);


        GUILayout.EndHorizontal();
    }
}
