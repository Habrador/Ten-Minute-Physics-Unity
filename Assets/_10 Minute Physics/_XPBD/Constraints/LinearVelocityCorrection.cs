using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace XPBD
{
    public class LinearVelocityCorrection
    {
        //From YT:
        //ApplyLinearVelocityCorrection(p1, p2, delta_v)
        //
        // delta_v_italics = |delta_v|
        // n = delta_v / |delta_v|
        //
        // w = m^-1 + (r x n)^T* I^-1 * (r x n)
        //
        // lambda = -delta_v_italics * (w1 + w2)^-1
        //
        // v = v +- lambda * n * 1/m
        // omega = omega +- lambda * I^-1 * (r x n)

        public static float Apply(MyRigidBody rb1, Vector3 p1, MyRigidBody rb2, Vector3 p2, Vector3 corr)
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
            float w_tot = GeneralizedInverseMass.Positional(rb1, normal, p1);

            if (rb2 != null)
            {
                w_tot += GeneralizedInverseMass.Positional(rb2, normal, p2);
            }

            if (w_tot == 0f)
            {
                return 0f;
            }

            //Compute Lagrange multiplier
            float compliance = 0f;

            float alpha = compliance / (rb1.dt * rb1.dt);

            float lambda = -C / (w_tot + alpha);

            normal *= -lambda;

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
            rb.vel += rb.invMass * p;


            //Angular correction
            Vector3 dOmega = pos - rb.pos;

            dOmega = Vector3.Cross(dOmega, p);

            dOmega = rb.invRot * dOmega;

            dOmega.x = rb.invInertia.x * dOmega.x;
            dOmega.y = rb.invInertia.y * dOmega.y;
            dOmega.z = rb.invInertia.z * dOmega.z;

            dOmega = rb.rot * dOmega;

            rb.omega += dOmega;
        }

    }
}