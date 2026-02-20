using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEditor.PlayerSettings;

namespace XPBD
{
    public static class AngularVelocityCorrection
    {
        //From YT
        //ApplyAngularVelocityCorrection(delta_omega)
        //
        // delta_omega_italics = |delta_omega|
        // n = delta_omega / |delta_omega|
        //
        // w = n^T * I^-1 * n
        //
        // lambda = -delta_omega_italics * (w1 + w2)^-1
        //
        // omega = omega +- lambda * I^-1 * n

        public static void Apply(MyRigidBody rb1, MyRigidBody rb2, Vector3 corr)
        {
            //If no elongation
            if (corr.sqrMagnitude == 0f)
            {
                return;
            }

            //Find C and n from corr which is C * n
            float C = corr.magnitude;

            //This can be optimized as we alread have the magnitude: normal = corr / C
            Vector3 normal = corr.normalized;

            //Compute generalized inverse mass for each rb
            // w = m^-1 * (r x n)^T * I^-1 * (r x n)
            float w_tot = GeneralizedInverseMass.Angular(rb1, normal);

            if (rb2 != null)
            {
                w_tot += GeneralizedInverseMass.Angular(rb2, normal);
            }

            if (w_tot == 0f)
            {
                return;
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

            UpdatePosAndRot(rb1, lambda_normal);

            if (rb2 != null)
            {
                lambda_normal *= -1f;
                UpdatePosAndRot(rb2, lambda_normal);
            }
        }


        public static void UpdatePosAndRot(MyRigidBody rb, Vector3 p)
        {
            if (rb.invMass == 0f)
            {
                return;
            }

            //Angular correction
            Vector3 dOmega = p;

            dOmega = rb.invRot * dOmega;

            dOmega.x = rb.invInertia.x * dOmega.x;
            dOmega.y = rb.invInertia.y * dOmega.y;
            dOmega.z = rb.invInertia.z * dOmega.z;

            dOmega = rb.rot * dOmega;

            rb.omega += dOmega;
        }
    }
}
