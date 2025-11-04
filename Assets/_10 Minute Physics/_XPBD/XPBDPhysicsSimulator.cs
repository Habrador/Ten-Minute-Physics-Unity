using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace XPBD
{
    //Simulate rigid bodies, constraints, joints using XPBD: Extended Position Based Dynamics
    public class XPBDPhysicsSimulator
    {
        private readonly Vector3 gravity;

        //So we can delete all gameobjects from the scene if we change scene
        public List<MyRigidBody> allRigidBodies = new();
        public List<DistanceConstraint> allDistanceConstraints = new();
        public List<MyJoint> allJoints = new();

        //If we drag with mouse to interact we add a temp distance constraint
        public DistanceConstraint dragConstraint;



        public XPBDPhysicsSimulator(Vector3 gravity)
        {
            this.gravity = gravity;
        }



        //Add stuff to the physics simulation
        public void AddRigidBody(MyRigidBody rb) => allRigidBodies.Add(rb);

        public void AddDistanceConstraint(DistanceConstraint dc) => allDistanceConstraints.Add(dc);

        public void AddJoint(MyJoint joint) => allJoints.Add(joint);



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
            foreach (MyRigidBody rb in allRigidBodies)
            {
                rb.Integrate(dt, this.gravity);
            }

            //Constraints
            //Update pos and rot of the constraints that connects rbs
            foreach (DistanceConstraint dc in allDistanceConstraints)
            {
                dc.Solve(dt);
            }

            //Update pos and rot of the constraint we use when moving rbs with mouse
            this.dragConstraint?.Solve(dt);

            //The velocities we calculated in integrate() make the simulation unstable
            //Update vel and angular vel by using pos and rot before and after integrate() and solve()
            foreach (MyRigidBody rb in allRigidBodies)
            {
                rb.FixVelocities(dt);
            }
        }



        //Called from Update
        public void MyUpdate()
        {
            //Update meshes so they have correct orientation and position
            foreach (MyRigidBody rb in allRigidBodies)
            {
                rb.UpdateMeshes();
            }

            foreach (DistanceConstraint dc in allDistanceConstraints)
            {
                dc.UpdateMesh();
            }

            this.dragConstraint?.UpdateMesh();
        }



        //Cleanup when simulation is over
        public void Dispose()
        {
            foreach (MyRigidBody rb in allRigidBodies)
            {
                rb.Dispose();
            }
        
            foreach (DistanceConstraint dc in allDistanceConstraints)
            {
                dc.Dispose();
            }

            this.dragConstraint?.Dispose();
        }
    }
}