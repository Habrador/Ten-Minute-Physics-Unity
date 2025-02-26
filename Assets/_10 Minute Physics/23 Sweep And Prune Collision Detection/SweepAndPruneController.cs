using EulerianFluidSimulator;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Unity.Burst.Intrinsics.X86;
using static UnityEditor.ShaderGraph.Internal.KeywordDependentCollection;

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

    private CollisionAlgorithm activeCollisionAlgorithm = CollisionAlgorithm.BruteForce;

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

    //Spheres
    //Simulation data belonging to each sphere
    private List<Sphere> spheres;
    //The gameobject belonging to each sphere
    private List<Transform> visualSpheres;
    //How mmany spheres we simulate
    private const int totalSpheres = 10;

    //Simulation settings

    //To get the same simulation every time
    private const int seed = 0;

    //Run multiple physics steps per frame
    private const int numSubsteps = 5;

    //Restitution coefficient = how bouncy the spheres are if they collide
    private const float e = 1f;



    private void Start()
    {
        Random.InitState(seed);

        spheres = new();
        visualSpheres = new();

        //Add random spheres
        for (int i = 0; i < totalSpheres; i++)
        {
            float radius = Mathf.Floor(Mathf.Pow(Random.value, 10f) * 20f + 2f);
            
            float x = Random.value * (borderSizeX - radius * 2) + radius;
            float y = Random.value * (borderSizeY - radius * 2) + radius;
            
            float vx = Random.value * 400 - 200;
            float vy = Random.value * 400 - 200;
            
            spheres.Add(new Sphere(x, y, vx, vy, radius));

            //Initialize the gameobject sphere which we can actually see
            //this.color = `hsl(${ Math.random() * 360}, 70 %, 50 %)`;
        }
    }



    private void Update()
    {
        return;
    
        //Update the visual position of the sphere
        for (int i = 0; i < visualSpheres.Count; i++)
        {
            Sphere sphereData = spheres[i];

            Vector3 spherePos = new(sphereData.x, sphereData.y, 0f);

            visualSpheres[i].position = spherePos;
        }

        DisplayBorder();
    }



    private void FixedUpdate()
    {
        return;
    
        //Reset the counters
        collisionChecks = 0;
        actualCollisions = 0;

        float dt = Time.fixedDeltaTime;

        float subDt = dt / (float)numSubsteps;

        for (int step = 0; step < numSubsteps; step++)
        {
            //Move each sphere and make sure the sphere is not outside of the map
            foreach (Sphere sphere in spheres)
            {
                sphere.Update(subDt, borderSizeX, borderSizeY);
            }

            //Check for collisions with other spheres
            if (activeCollisionAlgorithm == CollisionAlgorithm.BruteForce)
            {
                BruteForceCollisions(spheres);
            }
            else
            {
                SweepAndPruneCollisions(spheres);
            }
        }
    }



    private void OnGUI()
    {
        MyOnGUI();
    }



    //What happens if two spheres are colliding?
    private void CalculateCollision(Sphere sphere1, Sphere sphere2, float e)
    {
        float dx = sphere2.x - sphere1.x;
        float dy = sphere2.y - sphere1.y;

        float dist = Mathf.Sqrt(dx * dx + dy * dy);

        //The spheres are not colliding
        if (dist > sphere1.radius + sphere2.radius)
        {            
            return;
        }

        float nx = dx / dist;
        float ny = dy / dist;

        float vrx = sphere2.vx - sphere1.vx;  // Changed direction of relative velocity
        float vry = sphere2.vy - sphere1.vy;

        float vn = vrx * nx + vry * ny;

        if (vn > 0)
        {
            return;
        }

        float j = -(1f + e) * vn / (1f / sphere1.mass + 1 / sphere2.mass);

        sphere1.vx = sphere1.vx - (j * nx) / sphere1.mass;  // Changed sign
        sphere1.vy = sphere1.vy - (j * ny) / sphere1.mass;  // Changed sign
        sphere2.vx = sphere2.vx + (j * nx) / sphere2.mass;  // Changed sign
        sphere2.vy = sphere2.vy + (j * ny) / sphere2.mass;  // Changed sign
    }



    //Check if two spheres are colliding
    private void SolveCollision(Sphere sphere1, Sphere sphere2)
    {
        collisionChecks++;

        float dx = sphere2.x - sphere1.x;
        float dy = sphere2.y - sphere1.y;
        
        float distance = Mathf.Sqrt(dx * dx + dy * dy);

        //The spheres are colliding
        if (distance < sphere1.radius + sphere2.radius)
        {
            actualCollisions++;

            //Calculate new velocities using provided function
            CalculateCollision(sphere1, sphere2, e);

            //Resolve overlap
            float overlap = (sphere1.radius + sphere2.radius - distance);
            float nx = dx / distance;
            float ny = dy / distance;

            float cx = overlap * nx / 2f;
            float cy = overlap * ny / 2f;

            sphere1.x -= cx;
            sphere1.y -= cy;
            sphere2.x += cx;
            sphere2.y += cy;
        }
    }



    //Slow collision detection where we check each speher against all other spheres
    private void BruteForceCollisions(List<Sphere> spheres)
    {
        for (int i = 0; i < spheres.Count; i++)
        {
            for (int j = i + 1; j < spheres.Count; j++)
            {
                SolveCollision(spheres[i], spheres[j]);
            }
        }
    }



    //Fast collision detection where we first sort all objects by their left-border x coordinate
    private void SweepAndPruneCollisions(List<Sphere> spheres)
    {
        //const sortedSpheres = spheres.sort((a, b) => a.Left - b.Left);

        //TEMP
        List<Sphere> sortedSpheres = spheres;

        for (int i = 0; i < sortedSpheres.Count; i++)
        {
            Sphere sphere1 = sortedSpheres[i];

            for (int j = i + 1; j < sortedSpheres.Count; j++)
            {
                Sphere sphere2 = sortedSpheres[j];

                if (sphere2.Left > sphere1.Right)
                {
                    break;
                }

                if (Mathf.Abs(sphere1.y - sphere2.y) <= sphere1.radius + sphere2.radius)
                {
                    SolveCollision(sphere1, sphere2);
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



    //Buttons, checkboxes, show min/max pressure
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
        string infoText = $"Spheres: {totalSpheres} | Collision checks / frame: {collisionChecks} | Actual collisions / frame: {actualCollisions}";

        GUIStyle textStyle = GUI.skin.GetStyle("Label");

        textStyle.fontSize = fontSize;
        textStyle.margin = offset;

        GUILayout.Label(infoText, textStyle);


        GUILayout.EndHorizontal();
    }
}
