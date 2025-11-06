using System.Collections;
using System.Collections.Generic;
using UnityEngine;



namespace XPBD
{
    public class MyRigidBody
    {
        //Types of rbs we can simulate
        public enum Types
        {
            Box,
            Sphere
        }

        //For debugging
        private readonly Types type;

        //A rigid body has the following properties:
        //Position
        public Vector3 pos;
        //Velocity
        private Vector3 vel;
        //Rotation
        public Quaternion rot;
        //Inverse rot q^-1 = q* / |q|^2
        private Quaternion invRot;
        //Angular velocity
        //omega.magnitude -> speed of rotation
        private Vector3 omega;
        //Mass m
        //We are going to use inverse mass m^-1
        public float invMass;
        //Moment of inertia (resistance to torque)
        //Is a 3x3 matrix
        //if the body is aligned witht the x,y,z axis we can treat it as a 3d vector
        //I = |I_xx 0    0   | -> I = (I_xx, I_yy, I_zz)
        //    |0    I_yy 0   |
        //    |0    0    I_zz|
        //and do calculations in local space by transforming everything needed to local space
        //We are going to use inverse moment of inertia I^-1
        public Vector3 invInertia;

        //For XPBD simulation we cache these to fix velocities
        private Vector3 prevPos;
        private Quaternion prevRot;

        //Multiply velocity with this damping 
        public float damping;

        //The gameobjects showing how this rigid body looks
        //Is also the collider when raycasting
        public MyRigidBodyVisuals visualObjects;

        //Font size determines if we should display rb data on the screen
        private readonly int fontSize;

        //Was added in joints, we cache dt in Integrate()
        private float dt;


        //If fontSize = 0 we wont display any text
        //size - radius if we have a sphere, length of side if we have a box
        public MyRigidBody(Types type, Vector3 size, float density, Vector3 pos, Vector3 angles, int fontSize = 0)
        {
            this.type = type;

            this.fontSize = fontSize;

            this.damping = 0f;

            this.pos = pos;

            this.rot = new Quaternion
            {
                eulerAngles = angles
            };

            //Inverts this quaternion
            this.invRot = Quaternion.Inverse(this.rot);

            //Init these with some values
            this.vel = Vector3.zero;
            this.omega = Vector3.zero;

            this.prevPos = this.pos;
            this.prevRot = this.rot;

            //Data that depends on the rbs geometry
            if (type == Types.Box)
            {
                MyRigidBodyData.InitBox(this, size, density);
            }
            else if (type == Types.Sphere)
            {
                MyRigidBodyData.InitSphere(this, size, density);
            }

            UpdateMeshes();
        }



        //Move the visual meshes to the simulated position and rotation
        public void UpdateMeshes()
        {
            this.visualObjects.UpdateVisualObjects(this.pos, this.rot);
        }



        //Switch between showing the basic mesh or the more detailed mesh
        public void ShowSimulationView(bool show)
        {
            this.visualObjects.showVisualObj = show;
            this.UpdateMeshes();
        }



        //Display mass in kg next to object using OnGUI 
        public void DisplayData()
        {
            if (fontSize == 0)
            {
                return;
            }

            string displayText = Mathf.RoundToInt(1f / this.invMass) + "kg";

            BasicRBGUI.DisplayDataNextToRB(displayText, this.fontSize, this.pos);
        }



        //
        // Local pos to world pos and world pos to local pos
        //

        // a is a point on the rb in local space
        // a' is the same point but in world space
        // q is the quaternion
        // x is the position of rb (center of mass)

        //a' = x + q * a 
        public Vector3 LocalToWorld(Vector3 localPos) => this.pos + this.rot * localPos;

        //a = q^-1 * (a' - x)
        public Vector3 WorldToLocal(Vector3 worldPos) => this.invRot * (worldPos - this.pos);



        //
        // The velocity of a point on the body
        //

        public Vector3 GetVelocityAt(Vector3 pos)
        {
            if (this.invMass == 0f)
            {
                return Vector3.zero;
            }

            Vector3 r = pos - this.pos;

            //In the ref code, hes multiplying Vector3.Cross(this.omega, r) by -1
            //But I think its because his r is inverted???
            Vector3 pointVel = this.vel + Vector3.Cross(this.omega, r);

            return pointVel;
        }



        //
        // Update position, velocity, angular velocity, and rotation by using semi-implicit Euler
        //

        //From "Detailed rigid body simulation with xpbd"
        //h = dt / numSubsteps
        //Update pos:
        //x_prev = x
        //v = v + h * f_ext / m
        //x = x + h * v
        //Update rot:
        //q_prev = q
        //omega = omega + h * I^-1 * (tau_ext - (omega x (I * omega)))
        //q = q + h * 0.5 * [omega_x, omega_y, omega_z, 0] * q
        //Normailize: q = q / |q|
        
        //From YT tutorial
        //omega = omega + h * I^-1 * tau_ext (Where did (omega x (I * omega)) go??
        //q = q + 0.5 * dt * v[omega_x, omega_y, omega_z, 0] * q (I think the v is a misprint)
        
        //Derivation of the above equations:
        
        //Angular velocity
        //The angular equation of motion:
        //Takes into account both external influences and the body's own rotational behavior.
        //Sum(M_cg) = dH_cg / dt = I * (domega/dt) + (omega x (I * omega))
        //-> domega/dt = I^-1 * [Sum(M) - (omega x (I * omega))]
        //Which is the equation from the paper:
        //omega = omega + dt * I^-1 * (tau_ext - (omega x (I * omega))
        //omega x (I * omega) represents the effect of the body's current rotation on its angular momentum, contributing to the overall torque.
        //I^-1 Scales the effect of the torques based on the body's resistance to changes in its rotational motion.
        
        //Rotation
        //How a quaternion changes over time due to the angular velocity:
        //dRot = dq/dt = 0.5 * omega * q -> q_next = q + 0.5 * omega * q * dt
        //The factor of 0.5 arises from the mathematical properties of quaternions (they use half-angles)
        //and ensures that the rate of change of the quaternion correctly represents the physical rotation
        //(If omega is in body coordinates, you use dq/dt = 0.5 * q * omega)
        public void Integrate(float dt, Vector3 gravity)
        {
            this.dt = dt;
        
            if (this.invMass == 0f)
            {
                return;
            }


            //Linear motion

            //Cache the pos as we need it later
            this.prevPos = this.pos;

            //Calculate the new positon and velocity
            this.vel += gravity * dt;
           
            this.pos += this.vel * dt;


            //Angular motion

            //Cache the rot as we need it later
            this.prevRot = this.rot;

            //Update angular velocity:
            //omega = omega + h * I^-1 * tau_ext = omega (because we have no external torque)
            //The tutorial is not taking into account the body's own rotational behavior (omega x (I * omega)...

            //Update rotation:
            //q = q + dt * 0.5 * [omega_x, omega_y, omega_z, 0] * q

            //Put the angular velocity in quaternion form so we can multiply it by a quaternion
            //This is known as a pure quaternion (a quaternion with a real part of zero: w = 0)
            //Sometimes you see [0, omega_x, omega_y, omega_z], but it depends on the quaternion class
            Quaternion omegaAsQuaternion = new(this.omega.x, this.omega.y, this.omega.z, 0f);

            //dRot before multiplying by 0.5 (which we cant just do with a quaternion: 0.5 * q is not allowed)
            Quaternion dRot = omegaAsQuaternion * this.rot;

            //q = q + dt * 0.5 * dRot
            this.rot.x += 0.5f * dt * dRot.x;
            this.rot.y += 0.5f * dt * dRot.y;
            this.rot.z += 0.5f * dt * dRot.z;
            this.rot.w += 0.5f * dt * dRot.w;

            //The orientation quaternion must be a unit quaternion: q = q / |q|
            this.rot.Normalize();

            //Update the inverse rot with the new values
            this.invRot = Quaternion.Inverse(this.rot);
        }



        //
        // Fix velocity and angular velocity
        //

        //The velocities calculated in Integrate() are not the velocities we want
        //because they make the simulation unstable
        //Also add damping
        //From "Detailed rigid body simulation with xpbd"
        //v = (x - x_prev) / h
        //delta_q = q * q_prev^-1
        //omega = 2 * [delta_q_x, delta_q_y, delta_q_z] / h
        //omega = (delta_q_w >= 0) ? omega : -omega
        public void FixVelocities(float dt)
        {
            if (this.invMass == 0f)
            {
                return;
            }


            //Linear motion
            //v = (x - x_prev) / h
            //Cant be (x_prev - x)!
            this.vel = (this.pos - this.prevPos) / dt;

            //Damping (should maybe be in its own method)
            this.vel *= Mathf.Max(1f - this.damping * dt, 0f);


            //Angular motion

            //Compute the relative rotation between the two quaternions
            //This is the transformation that transforms the body from the frame before the solve into the frame after the solve
            //This operation is often used to determine the difference or change in orientation between two rotations
            //delta_q = q * q_prev^-1
            Quaternion delta_q = this.rot * Quaternion.Inverse(this.prevRot);

            //Turn the transformation into an angular velocity
            //Quaternions represent rotations using half-angles, to convert back to full angles, you multiply by 2
            //and then divide by dt to get the angular velocity
            //omega = 2 * [delta_q_x, delta_q_y, delta_q_z] / h
            this.omega = new Vector3(delta_q.x, delta_q.y, delta_q.z) * 2f / dt;

            //If delta_q.w is negative, the resulting angular velocity points in the opposite direction of the intended rotation
            if (delta_q.w < 0f)
            {
                this.omega *= -1f;
            }
        }



        //
        // Calculate generalized inverse mass
        //

        //We have to constraints: positional (distance) and angular
        //For positional the generalized inverse mass is:
        //w = m^-1 + (r x n)^T* I^-1 * (r x n)
        //For angular the generalized inverse mass is:
        //w = n^T * I^-1 * n
        //The code on github combines them into one method
        //But I shall use two methods because we cant set Vector3 to null so it becomes messy to combine


        //Positional constraints
        
        //The inverse mass is just 1/m 
        //But In order to keep energy conservation when transferring positional kinetic energy to rotational kinetic energy
        //we need a different value for inverse mass: the generalized inverse mass
        //Generalized inverse masses w = m^-1 + (r x n)^T * I^-1 * (r x n)
        //m - mass
        //n - correction direction
        //r - vector from center of mass to contact point
        //Derivation at the end of the paper "Detailed rb simulation with xpbd"

        //normal - direction between constraints
        //pos - where the constraint attaches to this body in world space
        private float GetGeneralizedInverseMass(Vector3 normal, Vector3 pos)
        {
            if (this.invMass == 0f)
            {
                return 0f;
            }

            Vector3 r = pos - this.pos;

            //rn = r x n
            Vector3 rn = Vector3.Cross(r, normal);

            //Global -> local because we gonna use the Inertia
            rn = this.invRot * rn;

            //(r x n)^T * I^-1 * (r x n) = rn^T * I^-1 * rn
            //3x3 * 3x1 = 3x1
            //|invI.x 0      0     | * |rn.x| = |rn.x * invI.x|
            //|0      invI.y 0     |   |rn.y|   |rn.y * invI.y|
            //|0      0      invI.z|   |rn.z|   |rn.z * invI.z|
            //1x3 * 3x1 = 1x1
            //|rn.x rn.y rn.z| * |rn.x * invI.x|
            //                   |rn.y * invI.y|
            //                   |rn.z * invI.z|
            float rnT_IInv_rn =
                rn.x * rn.x * this.invInertia.x +
                rn.y * rn.y * this.invInertia.y +
                rn.z * rn.z * this.invInertia.z;

            //w = m^-1 + rn^T * I^-1 * rn
            float w = this.invMass + rnT_IInv_rn;

            return w;
        }



        //Angular constraints

        //w = n^T * I^-1 * n
        //normal - direction between constraints???
        private float GetGeneralizedInverseMass(Vector3 normal)
        {
            if (this.invMass == 0f)
            {
                return 0f;
            }

            Vector3 n = normal;

            //Global -> local because we gonna use the Inertia
            n = this.invRot * n;

            //w = n^T * I^-1 * n
            float w =
                n.x * n.x * this.invInertia.x +
                n.y * n.y * this.invInertia.y +
                n.z * n.z * this.invInertia.z;

            return w;
        }



        //
        // Fix the constraint
        //

        //This doesnt have to be a distance constrain as there are many other constraints...
        //compliance: inverse of physical stiffness (alpha in equations)
        //corr: n * C = n * (l - l_0) (direction times elongation)
        //pos: where the constraint attaches to this body in world space
        //otherBody: the connected rb (if any, might be a fixed object for collision)
        //otherPos: where the constraint attaches to the other rb in world space
        //velocityLevel: Was dt but is replaced by a bool in joints, and determines if we should use dt in some calculations
        //Returns the force on this constraint
        public float ApplyCorrection(float compliance, Vector3 corr, Vector3 pos, MyRigidBody otherBody, Vector3 otherPos, bool velocityLevel = false)
        {
            //In the paper you see
            //delta_lambda = (-c - (alpha_tilde * lambda)) / (w_1 + w_2 + alpha_tilde)
            //alpha_tilde = alpha / dt^2
            //lambda = lambda + delta_lambda
            //and instead of lambda * normal they use delta_lambda * normal
            //BUT according to the YT video "09 Getting ready to simulate the world with XPBD" (7:15)
            //we dont need to keep track of the lagrange multiplier per constraint
            //if we iterate over the constraints just once
            //Notice that iteration and substeps are not the same
            //Some are iterating over the constraints multiple times each substep
            //But we are doing it just once because it generates a better result

            //If no elongation
            if (corr.sqrMagnitude == 0f)
            {
                return 0f;
            }

            //Find C and n from corr which is C * n
            float C = corr.magnitude;

            Vector3 normal = corr.normalized;

            //Compute generalized inverse mass for each rb
            // w = m^-1 * (r x n)^T * I^-1 * (r x n)
            float w_tot = this.GetGeneralizedInverseMass(normal, pos);

            if (otherBody != null)
            {
                w_tot += otherBody.GetGeneralizedInverseMass(normal, otherPos);
            }

            if (w_tot == 0f)
            {
                return 0f;
            }

            //Compute Lagrange multiplier
            // lambda = -C * (w_1 + w_2 + alpha / dt^2)^-1
            float lambda = -C / (w_tot + (compliance / (this.dt * this.dt)));

            //Update pos and rot
            //x_i = x_i +- w_i * lambda * n
            //q_i = q_i + 0.5 * lambda * [I_i^-1 * (r_i x n), 0] * q_i
            Vector3 lambda_normal = normal * -lambda;

            UpdatePosAndRot(lambda_normal, pos);

            if (otherBody != null)
            {
                lambda_normal *= -1f;
                otherBody.UpdatePosAndRot(lambda_normal, otherPos);
            }

            //Constraint force
            //F = (lambda * n) / dt^2
            //We dont need direction so ignore n
            float constraintForce = lambda / (this.dt * this.dt);

            return constraintForce;
        }



        //Update pos and rot to enforce distance constraints
        //Equations are from "Detailed rigid body simulation with xpbd"
        // x = x +- p / m
        // q = q +- 0.5 * (I^-1 * (r x p), 0) * q
        //where the positional impulse p = lambda * n 
        //because we are using lambda and not delta_lambda!
        public void UpdatePosAndRot(Vector3 p, Vector3 pos)
        {
            if (this.invMass == 0f)
            {
                return;
            }


            //Linear correction
            // x = x +- p / m
            // +- Because we move in different directions because we have two rb
            // p already has the +- in it
            this.pos += this.invMass * p;


            //Angular correction
            // q = q +- 0.5 * (I^-1 * (r x p), 0) * q

            //r
            Vector3 r = pos - this.pos;

            //r x p
            Vector3 r_cross_p = Vector3.Cross(r, p);

            //Transform dOmega to local space so we can use the simplifed moment of inertia
            r_cross_p = this.invRot * r_cross_p;

            //Scales dOmega by the inverse inertia
            //Why is it called dOmega?
            //I^-1 * (r x p)
            Vector3 dOmega = Vector3.zero;

            dOmega.x = r_cross_p.x * this.invInertia.x;
            dOmega.y = r_cross_p.y * this.invInertia.y;
            dOmega.z = r_cross_p.z * this.invInertia.z;

            //Transforms dOmega back into the original coordinate frame after the inverse rotation was applied earlier
            dOmega = this.rot * dOmega;

            //Same as during Integrate() except for the dt which is now 1
            //dRot <- dOmega * q_i
            //[I^-1 * (r x p), 0]
            Quaternion dRot = new(dOmega.x, dOmega.y, dOmega.z, 0f);

            //[I^-1 * (r x p), 0] * q
            dRot *= this.rot;

            //q_i = q_i + 0.5 * dRot
            this.rot.x += 0.5f * dRot.x;
            this.rot.y += 0.5f * dRot.y;
            this.rot.z += 0.5f * dRot.z;
            this.rot.w += 0.5f * dRot.w;

            //Always normalize when we update rotation
            this.rot.Normalize();

            //Cache the inverse
            this.invRot = Quaternion.Inverse(this.rot);
        }



        //
        // End simulation methods
        //

        public void Dispose()
        {
            this.visualObjects.Dispose();
        }

    }
}