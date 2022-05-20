using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConstraintsController : MonoBehaviour
{
    //How to simulate constraints?
    // - Spring: demands a large stiffness parameter which can cause numerical problems
    // - Generalized coordiates: can be mathematically very complicated
    // - Solve for constraint forces: can also be very complicated
    // - Just move it to the constraint: simple and physically accurate if we use enough substeps

    public GameObject ballGO;

    //The constraint
    private Vector3 wireCenter = Vector3.zero;
    private float wireRadius = 5f;

    private Vector3 gravity = new Vector3(0f, -9.81f, 0f);

    private Bead bead;

    //Important to use sub steps or the bead will lose momentum
    //The more time steps the better, but also slower and may lead floating point precision issues
    private int subSteps = 5;


    private void Start()
    {
        this.bead = new Bead(1f, ballGO.transform.position);    
    }


    private void Update()
    {
        //Update the visual position of the bead
        ballGO.transform.position = bead.pos;
    }


    private void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;

        float sdt = dt / (float)subSteps;

        for (int i = 0; i < subSteps; i++)
        {
            bead.StartStep(sdt, gravity);

            bead.KeepOnWire(wireCenter, wireRadius);

            bead.EndStep(sdt);
        }
    }




    private void LateUpdate()
    {    
        DisplayShapes.DrawCircle(wireCenter, wireRadius, Color.white);
    }
}
