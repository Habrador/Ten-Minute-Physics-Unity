using System.Collections;
using System.Collections.Generic;
using UnityEngine;



namespace XPBD
{
    //Aka Linear constraint
    public static class PositionalConstraint
    {
        //From YT video
        //p1,p2: locations of attachment points
        //delta_p: correction vector
        //alpha: compliance
        //ApplyLinearCorrection(p1, p2, delta_p, alpha)
        //{
        //  C = |delta_p|
        //  n = delta_p / |delta_p|
        // 
        //  w = m^-1 * ((p - x) x n)^T * I^-1 * ((p - x) x n)
        //
        //  lambda = -C * (w_1 + w_2 + alpha / dt^2)^-1
        //
        //  x = x +- m^-1 * lambda * n
        //  q = q +- 0.5 * lambda * [I^-1 * ((p - x) x n), 0] * q
        //
        //  F = (lambda * n) / dt^2
        //}


        //In the paper you see
        //
        // To apply a positional constraint delta_x at position r1 and r2, we first split
        // it into its direction n and its magnitude c
        //
        // w = m^-1 * (r x n)^T * I^-1 * (r x n) 
        // where r = p - x
        //
        // delta_lambda = (-c - (alpha_tilde * lambda)) / (w_1 + w_2 + alpha_tilde)
        // where alpha_tilde = alpha / dt^2
        // lambda = lambda + delta_lambda
        //
        // x = x +- p / m
        // where p = delta_lambda * n (which is the positional impulse)
        // q = q +- 0.5 * [I^-1 * (r x p), 0] * q
        //
        // Force f = (lambda * n) / dt^2

        //BUT according to the YT video "09 Getting ready to simulate the world with XPBD" (7:15)
        //we dont need to keep track of the lagrange multiplier per constraint
        //if we iterate over the constraints just once
        //Notice that iteration and substeps are not the same
        //Some are iterating over the constraints multiple times each substep
        //But we are doing it just once because it generates a better result


        //This doesnt have to be a distance constrain as there are many other constraints...
        //alpha: inverse of physical stiffness (sometimes called compliance)
        //corr: n * C = n * (l - l_0) (direction times elongation)
        //p1,p2: where the constraint attaches to this body in world space
        //rb1, rb2: connected rigid bodies
        //velocityLevel: Was dt but is replaced by a bool in joints, and determines if we should use dt in some calculations (dt is cached each FixedUpdate). From paper: velocityLevel is used to handle dynamic friction, restitution, and joint damping
        //Returns the force on this constraint. Calculations for velocityLevel should be in its own class to make it less confusing
        //Was removed. In the YT video the guy combined Position and rotational constraints and velocity level...
        public static float ApplyCorrection(float alpha, Vector3 corr, MyRigidBody rb1, Vector3 p1, MyRigidBody rb2, Vector3 p2)
        {
            //If no elongation
            if (corr.sqrMagnitude == 0f)
            {
                return 0f;
            }

            //Find C and n from corr which is C * n
            float C = corr.magnitude;

            //This can be optimized as we alread have the magnitude: normal = corr / C
            Vector3 normal = corr.normalized;

            //Compute generalized inverse mass for each rb
            // w = m^-1 * (r x n)^T * I^-1 * (r x n)
            float w_tot = GeneralizedInverseMass.Calculate(rb1, normal, p1);

            if (rb2 != null)
            {
                w_tot += GeneralizedInverseMass.Calculate(rb2, normal, p2);
            }

            if (w_tot == 0f)
            {
                return 0f;
            }

            //Compute Lagrange multiplier
            // lambda = -C * (w_1 + w_2 + alpha / dt^2)^-1
            float lambda = -C / (w_tot + (alpha / (rb1.dt * rb1.dt)));

            //Update pos and rot
            //x = x +- 1/m * lambda * n
            //q = q +- 0.5 * lambda * [I^-1 * (r x n), 0] * q
            Vector3 lambda_normal = normal * -lambda;

            UpdatePosAndRot(rb1, lambda_normal, p1);

            if (rb2 != null)
            {
                lambda_normal *= -1f;
                UpdatePosAndRot(rb2, lambda_normal, p2);
            }

            //Constraint force [N]
            //F = (lambda * n) / dt^2
            //We dont need direction so ignore n
            float constraintForce = lambda / (rb1.dt * rb1.dt);

            return constraintForce;
        }



        //Update pos and rot to enforce distance constraints
        //Equations are from "Detailed rigid body simulation with xpbd"
        // x = x +- p / m
        // q = q +- 0.5 * (I^-1 * (r x p), 0) * q
        //where the positional impulse p = lambda * n 
        //because we are using lambda and not delta_lambda!
        public static void UpdatePosAndRot(MyRigidBody rb, Vector3 p, Vector3 pos)
        {
            if (rb.invMass == 0f)
            {
                return;
            }


            //Linear correction
            // x = x +- p / m
            // +- Because we move in different directions because we have two rb
            // p already has the +- in it
            rb.pos += rb.invMass * p;


            //Angular correction
            // q = q +- 0.5 * (I^-1 * (r x p), 0) * q

            //r
            Vector3 r = pos - rb.pos;

            //r x p
            Vector3 r_cross_p = Vector3.Cross(r, p);

            //Transform dOmega to local space so we can use the simplifed moment of inertia
            r_cross_p = rb.invRot * r_cross_p;

            //Scales dOmega by the inverse inertia
            //Why is it called dOmega?
            //I^-1 * (r x p)
            Vector3 dOmega = Vector3.zero;

            dOmega.x = r_cross_p.x * rb.invInertia.x;
            dOmega.y = r_cross_p.y * rb.invInertia.y;
            dOmega.z = r_cross_p.z * rb.invInertia.z;

            //Transforms dOmega back into the original coordinate frame after the inverse rotation was applied earlier
            dOmega = rb.rot * dOmega;

            //Same as during Integrate() except for the dt which is now 1
            //dRot <- dOmega * q_i
            //[I^-1 * (r x p), 0]
            Quaternion dRot = new(dOmega.x, dOmega.y, dOmega.z, 0f);

            //[I^-1 * (r x p), 0] * q
            dRot *= rb.rot;

            //q_i = q_i + 0.5 * dRot
            rb.rot.x += 0.5f * dRot.x;
            rb.rot.y += 0.5f * dRot.y;
            rb.rot.z += 0.5f * dRot.z;
            rb.rot.w += 0.5f * dRot.w;

            //Always normalize when we update rotation
            rb.rot.Normalize();

            //Cache the inverse
            rb.invRot = Quaternion.Inverse(rb.rot);
        }
    }
}