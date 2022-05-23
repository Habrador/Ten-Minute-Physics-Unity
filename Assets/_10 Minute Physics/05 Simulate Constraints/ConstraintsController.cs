using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Constraints;

//Simulate beads attached to a circular wire
//Based on: https://matthias-research.github.io/pages/tenMinutePhysics/
public class ConstraintsController : MonoBehaviour
{
    //How to simulate constraints?
    // - Spring: demands a large stiffness parameter which can cause numerical problems
    // - Generalized coordiates: can be mathematically very complicated
    // - Solve for constraint forces: can also be very complicated
    // - Just move it to the constraint: simple and physically accurate if we use enough substeps

    public GameObject beadGO;

    public List<Material> materials;

    //The constraint
    private Vector3 wireCenter = Vector3.zero;
    
    private float wireRadius = 5f;

    //Simulation settings
    private Vector3 gravity = new Vector3(0f, -9.81f, 0f);

    private float restitution = 1f;

    //Important to use sub steps or the bead will lose momentum
    //The more time steps the better, but also slower and may lead floating point precision issues
    private int subSteps = 20;

    //All beads on the constraint
    private List<Bead> allBeads;



    private void Start()
    {
        //this.bead = new Bead(1f, ballGO.transform.position);    

        ResetSimulation();
    }


    private void ResetSimulation()
    {
        allBeads = new List<Bead>();

        //Create random balls
        for (int i = 0; i < 6; i++)
        {
            GameObject newBallGO = Instantiate(beadGO);

            //Random pos on the circle
            Vector2 posOnCircle = Random.insideUnitCircle.normalized * 5f;

            Vector3 randomPos = new Vector3(posOnCircle.x, posOnCircle.y, 0f);

            //Random size (and thus mass)
            float randomSize = Random.Range(0.5f, 2f);

            newBallGO.transform.position = randomPos;
            newBallGO.transform.localScale = Vector3.one * randomSize;

            Bead newBead = new Bead(newBallGO.transform);

            //Random material
            Material randomMat = materials[Random.Range(0, materials.Count)];

            newBallGO.GetComponent<MeshRenderer>().sharedMaterial = randomMat;

            allBeads.Add(newBead);
        }
    }



    private void Update()
    {
        //Update the visual position of the beads
        foreach (Bead b in allBeads)
        {
            b.UpdateVisualPosition();
        }
    }



    private void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;

        float sdt = dt / (float)subSteps;  
                    
        for (int step = 0; step < subSteps; step++)
        {
            for (int i = 0; i < allBeads.Count; i++)
            {
                allBeads[i].StartStep(sdt, gravity);
            }

            for (int i = 0; i < allBeads.Count; i++)
            {
                allBeads[i].KeepOnWire(wireCenter, wireRadius);
            }

            for (int i = 0; i < allBeads.Count; i++)
            {
                allBeads[i].EndStep(sdt);
            }

            for (int i = 0; i < allBeads.Count; i++)
            {
                for (int j = i + 1; j < allBeads.Count; j++)
                {
                    CustomPhysics.HandleBallBallCollision(allBeads[i], allBeads[j], restitution);
                }
            }
        }
    }



    private void LateUpdate()
    {    
        //Draw the circle the beads are attached to
        DisplayShapes.DrawCircle(wireCenter, wireRadius, Color.white);
    }
}
