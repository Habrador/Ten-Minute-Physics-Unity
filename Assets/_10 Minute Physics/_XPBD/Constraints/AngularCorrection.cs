using System.Collections;
using System.Collections.Generic;
using UnityEngine;



namespace XPBD
{
    public static class AngularCorrection
    {
        //From YT video
        //delta_phi: orientation correction vector
        //alpha: compliance
        //ApplyAngularCorrection(delta_phi, alpha)
        //{
        //  C = |delta_phi|
        //  n = delta_phi / |delta_phi|
        // 
        //  w = n^T * I^-1 * n
        //
        //  lambda = -C * (w_1 + w_2 + alpha / dt^2)^-1
        //
        //  q = q +- 0.5 * lambda * [I^-1 * n, 0] * q
        //
        //  Torque = (lambda * n) / dt^2
        //}

        //In the paper you see
        //
        // We have a rotation vector delta_q which we split into:
        // n: direction n which is the rotation axis
        // theta: magnitude which is the which is the rotation angle
        //
        // w = n^T * I^-1 * n
        //
        // delta_lambda = (-theta - (alpha_tilde * lambda)) / (w_1 + w_2 + alpha_tilde)
        // where alpha_tilde = alpha / dt^2
        // lambda = lambda + delta_lambda
        //
        // q = q +- 0.5 * [I^-1 * p, 0] * q
        //
        // Project the quantities n, r, p into the rest state of the bodies before evaluating the expression
        // For joints, the attachment points r are typically defined in rest state
        //
        // Torque: tau = (lambda * n) / dt^2

        //See PositionalConstraint class why we dont have to use delta_lambda!


        //For rotational constraints
        //alpha: inverse of physical stiffness (sometimes called compliance)
        //delta_phi: rotation vector
        //rb1, rb2: connected rigid bodies
        //velocityLevel was removed and is in its own class to make it less confusing
        //In the YT video the guy combined Position and rotational constraints and velocity level...
        public static float Apply(float alpha, Vector3 delta_phi, MyRigidBody rb1, MyRigidBody rb2)
        {
            //If no correction
            if (delta_phi.sqrMagnitude == 0f)
            {
                return 0f;
            }

            //Rotation angle
            float theta = delta_phi.magnitude;

            //Rotation axis
            Vector3 normal = delta_phi.normalized;

            //This is different for rational constraint,
            //it uses no pos when calculating generalized inverse mass
            //Compute generalized inverse mass for each rb
            // w = n^T * I^-1 * n
            float w_tot = GeneralizedInverseMass.Angular(rb1, normal);

            if (rb2 != null)
            {
                w_tot += GeneralizedInverseMass.Angular(rb2, normal);
            }

            if (w_tot == 0f)
            {
                return 0f;
            }

            //Compute Lagrange multiplier
            // lambda = -theta * (w_1 + w_2 + alpha / dt^2)^-1
            float lambda = -theta / (w_tot + (alpha / (rb1.dt * rb1.dt)));


            //Update rot
            //q = q +- 0.5 * lambda * [I^-1 * n, 0] * q
            Vector3 lambda_normal = normal * -lambda;

            UpdateRot(rb1, lambda_normal);

            if (rb2 != null)
            {
                lambda_normal *= -1f;
                UpdateRot(rb2, lambda_normal);
            }

            //Constraint torque [Nm]
            //tau = (lambda * n) / dt^2
            //We dont need direction so ignore n
            float constraintTorque = lambda / (rb1.dt * rb1.dt);

            return constraintTorque;
        }


        //Update rot
        //q = q +- 0.5 * lambda * [I^-1 * n, 0] * q = q +- 0.5 * [I^-1 * p, 0] * q
        //p is lambda_normal
        //velocityLevel was removed and is in its own class
        public static void UpdateRot(MyRigidBody body, Vector3 p)
        {
            if (body.invMass == 0f)
            {
                return;
            }

            //Transform p to local space so we can use the simplifed moment of inertia
            p = body.invRot * p;

            //Scales dOmega by the inverse inertia
            //Why is it called dOmega?
            //I^-1 * p
            Vector3 dOmega = Vector3.zero;

            dOmega.x = p.x * body.invInertia.x;
            dOmega.y = p.y * body.invInertia.y;
            dOmega.z = p.z * body.invInertia.z;

            //Transforms dOmega back into the original coordinate frame after the inverse rotation was applied earlier
            dOmega = body.rot * dOmega;

            //In the code onn github:
            // stabilize rotation
            //dOmega.multiplyScalar(0.5);
            //and then he multiplies it again by 0.5 (body.rot.x += 0.5f * dRot.x;)
            //Why 2 times???

            //Same as during Integrate() except for the dt which is now 1
            //dRot <- dOmega * q_i
            //[I^-1 * p, 0]
            Quaternion dRot = new(dOmega.x, dOmega.y, dOmega.z, 0f);

            //[I^-1 * p, 0] * q
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