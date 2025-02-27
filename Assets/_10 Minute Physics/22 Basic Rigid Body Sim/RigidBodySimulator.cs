using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class RigidBodySimulator
{
    private Vector3 gravity;

    private List<MyRigidBody> rigidBodies;

    private List<DistanceConstraint> distanceConstraints;

    //Add dragconstraint, meaning if we drag with mouse to interact we add a temp constraint



    //Removed scene, timeStepSize, parameters
    public RigidBodySimulator(Vector3 gravity)
    {
        //this.scene = scene;
        this.gravity = gravity;
        //this.dt = timeStepSize;
        //this.numSubSteps = 10;
        //this.numIterations = 1;
        this.rigidBodies = new();
        this.distanceConstraints = new();

        //this.dragConstraint = null;
        //this.dragCompliance = 0.001;
    }



    public void AddRigidBody(MyRigidBody rigidBody)
    {
        rigidBodies.Add(rigidBody);
    }



    public void AddDistanceConstraint(DistanceConstraint distanceConstraint)
    {
        distanceConstraints.Add(distanceConstraint);
    }



    public void Simulate(float dt, int numSubSteps)
    {
        float sdt = dt / (float)numSubSteps;

        for (int subStep = 0; subStep < numSubSteps; subStep++)
        {
            for (int i = 0; i < rigidBodies.Count; i++)
            {
                rigidBodies[i].Integrate(sdt, this.gravity);
            }
                
            for (int i = 0; i < distanceConstraints.Count; i++)
            {
                distanceConstraints[i].Solve();
            }

            //Move stuff with mouse
            //if (this.dragConstraint)
            //{
            //    this.dragConstraint.solve();
            //}

            for (int i = 0; i < rigidBodies.Count; i++)
            {
                rigidBodies[i].UpdateVelocities(sdt);
            }
        }

        //Update meshes, move to Update
        for (int i = 0; i < rigidBodies.Count; i++)
        {
            rigidBodies[i].UpdateMeshes();
        }
            
        for (int i = 0; i < distanceConstraints.Count; i++)
        {
            distanceConstraints[i].UpdateMesh();
        }
        
        //Move stuff with mouse
        //if (this.dragConstraint)
        //    this.dragConstraint.updateMesh();
    }



    //
    // Mouse interactions
    //

    //startDrag(body, pos)
    //{
    //    this.dragConstraint = new DistanceConstraint(this.scene, body, null, pos, pos, 0.0, this.dragCompliance);
    //}

    //drag(pos)
    //{
    //    if (this.dragConstraint)
    //        this.dragConstraint.worldPos1.copy(pos);
    //}

    //endDrag(pos)
    //{
    //    if (this.dragConstraint)
    //    {
    //        this.dragConstraint.dispose();
    //        this.dragConstraint = null;
    //    }
    //}



    //Cleanup
    //dispose()
    //{
    //    for (let i = 0; i < this.rigidBodies.length; i++)
    //        this.rigidBodies[i].dispose();

    //    for (let i = 0; i < this.distanceConstraints.length; i++)
    //        this.distanceConstraints[i].dispose();

    //    if (this.dragConstraint)
    //        this.dragConstraint.dispose();
    //}

}
