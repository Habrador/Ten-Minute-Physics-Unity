using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace XPBD
{
    public static class PositionalConstraint
    {
        //This doesnt have to be a distance constrain as there are many other constraints...
        //compliance: inverse of physical stiffness (alpha in equations)
        //corr: n * C = n * (l - l_0) (direction times elongation)
        //pos: where the constraint attaches to this body in world space
        //otherBody: the connected rb (if any, might be a fixed object for collision)
        //otherPos: where the constraint attaches to the other rb in world space
        //velocityLevel: Was dt but is replaced by a bool in joints, and determines if we should use dt in some calculations (dt is cached each FixedUpdate)
        //Returns the force on this constraint
        public static float ApplyCorrection(float compliance, Vector3 corr, MyRigidBody body, Vector3 pos, MyRigidBody otherBody, Vector3 otherPos, bool velocityLevel = false)
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
            float w_tot = GeneralizedInverseMass.Calculate(body, normal, pos);

            if (otherBody != null)
            {
                w_tot += GeneralizedInverseMass.Calculate(otherBody, normal, otherPos);
            }

            if (w_tot == 0f)
            {
                return 0f;
            }

            //Compute Lagrange multiplier
            // lambda = -C * (w_1 + w_2 + alpha / dt^2)^-1
            float lambda = -C / (w_tot + (compliance / (body.dt * body.dt)));

            //Update pos and rot
            //x_i = x_i +- w_i * lambda * n
            //q_i = q_i + 0.5 * lambda * [I_i^-1 * (r_i x n), 0] * q_i
            Vector3 lambda_normal = normal * -lambda;

            UpdatePosAndRot(body, lambda_normal, pos);

            if (otherBody != null)
            {
                lambda_normal *= -1f;
                UpdatePosAndRot(otherBody, lambda_normal, otherPos);
            }

            //Constraint force
            //F = (lambda * n) / dt^2
            //We dont need direction so ignore n
            float constraintForce = lambda / (body.dt * body.dt);

            return constraintForce;
        }



        //Update pos and rot to enforce distance constraints
        //Equations are from "Detailed rigid body simulation with xpbd"
        // x = x +- p / m
        // q = q +- 0.5 * (I^-1 * (r x p), 0) * q
        //where the positional impulse p = lambda * n 
        //because we are using lambda and not delta_lambda!
        public static void UpdatePosAndRot(MyRigidBody body, Vector3 p, Vector3 pos)
        {
            if (body.invMass == 0f)
            {
                return;
            }


            //Linear correction
            // x = x +- p / m
            // +- Because we move in different directions because we have two rb
            // p already has the +- in it
            body.pos += body.invMass * p;


            //Angular correction
            // q = q +- 0.5 * (I^-1 * (r x p), 0) * q

            //r
            Vector3 r = pos - body.pos;

            //r x p
            Vector3 r_cross_p = Vector3.Cross(r, p);

            //Transform dOmega to local space so we can use the simplifed moment of inertia
            r_cross_p = body.invRot * r_cross_p;

            //Scales dOmega by the inverse inertia
            //Why is it called dOmega?
            //I^-1 * (r x p)
            Vector3 dOmega = Vector3.zero;

            dOmega.x = r_cross_p.x * body.invInertia.x;
            dOmega.y = r_cross_p.y * body.invInertia.y;
            dOmega.z = r_cross_p.z * body.invInertia.z;

            //Transforms dOmega back into the original coordinate frame after the inverse rotation was applied earlier
            dOmega = body.rot * dOmega;

            //Same as during Integrate() except for the dt which is now 1
            //dRot <- dOmega * q_i
            //[I^-1 * (r x p), 0]
            Quaternion dRot = new(dOmega.x, dOmega.y, dOmega.z, 0f);

            //[I^-1 * (r x p), 0] * q
            dRot *= body.rot;

            //q_i = q_i + 0.5 * dRot
            body.rot.x += 0.5f * dRot.x;
            body.rot.y += 0.5f * dRot.y;
            body.rot.z += 0.5f * dRot.z;
            body.rot.w += 0.5f * dRot.w;

            //Always normalize when we update rotation
            body.rot.Normalize();

            //Cache the inverse
            body.invRot = Quaternion.Inverse(body.rot);
        }
    }
}