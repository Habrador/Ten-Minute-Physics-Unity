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
public class SweepAndPruneController : MonoBehaviour
{
    //Which collision algorithm are we going to use?
    private enum CollisionAlgorithm 
    {
        BruteForce,
        SweepPrune
    }

    private CollisionAlgorithm activeCollisionAlgorithm = CollisionAlgorithm.SweepPrune;

    //Size of map
    //Collision detection assumes bottom left of map is at 0,0
    private const float mapSizeX = 20f;
    private const float mapSizeY = 10f;

    private int collisionChecks = 0;
    private int actualCollisions = 0;

    //All spheres
    private List<Sphere> spheres;

    private const int totalSpheres = 10;

    private const int seed = 0;



    private void Start()
    {
        Random.InitState(seed);
    
        //Add random spheres
        for (int i = 0; i < totalSpheres; i++)
        {
            float radius = Mathf.Floor(Mathf.Pow(Random.value, 10f) * 20f + 2f);
            
            float x = Random.value * (mapSizeX - radius * 2) + radius;
            float y = Random.value * (mapSizeY - radius * 2) + radius;
            
            float vx = Random.value * 400 - 200;
            float vy = Random.value * 400 - 200;
            
            spheres.Add(new Sphere(x, y, vx, vy, radius));
        }
    }



    private void Update()
    {
        //Draw all spheres
        //spheres.forEach(sphere => sphere.draw());
    }



    private void FixedUpdate()
    {
        //Reset the counters
        collisionChecks = 0;
        actualCollisions = 0;

        //Run multiple physics steps per frame
        int numSubsteps = 5;
        float subDt = Time.fixedDeltaTime / (float)numSubsteps;

        for (int step = 0; step < numSubsteps; step++)
        {
            //Move each sphere and make sure the sphere is not outside of the map
            foreach (Sphere sphere in spheres)
            {
                sphere.Update(subDt, mapSizeX, mapSizeY);
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

            //Restitution coefficient
            float e = 1f;

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



    private void SweepAndPruneCollisions(List<Sphere> spheres)
    {
        //const sortedSpheres = spheres.sort((a, b) => a.left - b.left);

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

}
