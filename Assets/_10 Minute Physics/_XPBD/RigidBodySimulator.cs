using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace XPBD
{
    public class RigidBodySimulator
    {
        private readonly Vector3 gravity;

        //So we can delete all gameobjects from the scene if we change scene
        public List<MyRigidBody> allRigidBodies;
        public List<DistanceConstraint> allDistanceConstraints;

        //Dragconstraint, meaning if we drag with mouse to interact we add a temp distance constraint
        public DistanceConstraint dragConstraint;
        private float dragCompliance;



        public RigidBodySimulator(Vector3 gravity)
        {
            this.gravity = gravity;

            this.allRigidBodies = new();
            this.allDistanceConstraints = new();

            //Move stuff with mouse
            this.dragConstraint = null;
            this.dragCompliance = 0.001f;
        }



        public void AddRigidBody(MyRigidBody rigidBody)
        {
            allRigidBodies.Add(rigidBody);
        }



        public void AddDistanceConstraint(DistanceConstraint distanceConstraint)
        {
            allDistanceConstraints.Add(distanceConstraint);
        }



        //Called from FixedUpdate
        public void MyFixedUpdate(float dt, int numSubSteps)
        {
            float sdt = dt / (float)numSubSteps;

            for (int subStep = 0; subStep < numSubSteps; subStep++)
            {
                Simulate(sdt);
            }
        }



        //The XPBD simulation loop
        private void Simulate(float dt)
        {
            //Update pos, vel, rot, angular vel
            for (int i = 0; i < allRigidBodies.Count; i++)
            {
                allRigidBodies[i].Integrate(dt, this.gravity);
            }

            //Constraints
            //Update pos and rot
            for (int i = 0; i < allDistanceConstraints.Count; i++)
            {
                allDistanceConstraints[i].Solve(dt);
            }

            //Move stuff with mouse
            if (this.dragConstraint != null)
            {
                this.dragConstraint.Solve(dt);
            }

            //The velocities we calculated in integrate() make the simulation unstable
            //Update vel and angular vel by using pos and rot before and after integrate() and solve()
            for (int i = 0; i < allRigidBodies.Count; i++)
            {
                allRigidBodies[i].FixVelocities(dt);
            }
        }



        //Called from Update
        public void MyUpdate()
        {
            //Update meshes so they have correct orientation and position
            for (int i = 0; i < allRigidBodies.Count; i++)
            {
                allRigidBodies[i].UpdateMeshes();
            }

            for (int i = 0; i < allDistanceConstraints.Count; i++)
            {
                allDistanceConstraints[i].UpdateMesh();
            }

            //Move stuff with mouse
            if (this.dragConstraint != null)
            {
                this.dragConstraint.UpdateMesh();
            }
        }



        //
        // Mouse interactions
        //

        //pos is in world coordinates
        public void StartDrag(MyRigidBody body, Vector3 pos)
        {
            //TODO: this is some default parameter in the original code and doesnt say what it is in this section, so might be true or false
            bool unilateral = false;

            this.dragConstraint = new DistanceConstraint(body, null, pos, pos, 0f, this.dragCompliance, unilateral);
        }

        public void Drag(Vector3 pos)
        {
            if (this.dragConstraint != null)
            {
                this.dragConstraint.worldPos1 = pos;
            }
        }

        public void EndDrag()
        {
            if (this.dragConstraint != null)
            {
                this.dragConstraint.Dispose();
                this.dragConstraint = null;
            }
        }



        //Cleanup to easily switch between scenes with rbs
        public void Dispose()
        {
            for (int i = 0; i < this.allRigidBodies.Count; i++)
            {
                this.allRigidBodies[i].Dispose();
            }

            for (int i = 0; i < this.allDistanceConstraints.Count; i++)
            {
                this.allDistanceConstraints[i].Dispose();
            }

            if (this.dragConstraint != null)
            {
                this.dragConstraint.Dispose();
            }
        }
    }
}