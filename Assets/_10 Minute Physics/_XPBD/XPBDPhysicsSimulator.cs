using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace XPBD
{
    //Simulate rigid bodies, constraints, joints using XPBD: Extended Position Based Dynamics
    //"Position Based" means you manipulate particle positions (and rotations) directly,
    //unlike other methods which use forces or velocities
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
        //Algorithm 2 in the XPBD paper
        private void Simulate(float dt)
        {
            //Step 1. Update pos (x), vel (v), rot (q), angular vel (omega)
            foreach (MyRigidBody rb in allRigidBodies)
            {
                rb.Integrate(dt, this.gravity);
            }


            //Step 2. Handle constraints

            //Iterate over the constraints just once
            //Some are iterating over them multiple times each step
            //but then you have to cache a lagrange multiplier which is currently ignored
            //Its better to use substeps and iterate over the constraints (and all other things) just once each substep

            //Update pos and rot of the constraints
            foreach (DistanceConstraint dc in allDistanceConstraints)
            {
                dc.Solve(dt);
            }

            //Update pos and rot of the constraint we use when moving rbs with mouse
            this.dragConstraint?.Solve(dt);

            //Update joints which uses positional and angular constraints
            foreach (MyJoint joint in allJoints)
            {
                joint.Solve(dt);
            }


            //Step 3. Fix velocities
            //The velocities we calculated in integrate() make the simulation unstable
            //Update vel and angular vel by using pos and rot before and after integrate() and solve()
            foreach (MyRigidBody rb in allRigidBodies)
            {
                rb.FixVelocities(dt);
            }


            //Step 4. Velocity level
            //In the paper you see SolveVelocities(v_1,...,v_n, omega_1,...,omega_n), which 
            //is used to handle dynamic friction, restitution (collisions), and joint damping
            
            //Update joint damping
            foreach (MyJoint joint in allJoints)
            {
                joint.ApplyLinearDamping(dt);
                joint.ApplyAngularDamping(dt);
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