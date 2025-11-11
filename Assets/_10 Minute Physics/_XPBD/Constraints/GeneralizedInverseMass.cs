using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace XPBD
{
    public static class GeneralizedInverseMass 
    {
        //
        // Calculate generalized inverse mass
        //

        //We have two constraints: positional (distance) and angular
        //For positional the generalized inverse mass is:
        //w = m^-1 + (r x n)^T* I^-1 * (r x n)
        //For angular the generalized inverse mass is:
        //w = n^T * I^-1 * n
        //The code on github combines them into one method
        //But we shall use two methods because we cant set Vector3 to null so it becomes messy to combine


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
        public static float Calculate(MyRigidBody rb, Vector3 normal, Vector3 pos)
        {
            if (rb.invMass == 0f)
            {
                return 0f;
            }

            Vector3 r = pos - rb.pos;

            //rn = r x n
            Vector3 rn = Vector3.Cross(r, normal);

            //Global -> local because we gonna use the Inertia
            rn = rb.invRot * rn;

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
                rn.x * rn.x * rb.invInertia.x +
                rn.y * rn.y * rb.invInertia.y +
                rn.z * rn.z * rb.invInertia.z;

            //w = m^-1 + rn^T * I^-1 * rn
            float w = rb.invMass + rnT_IInv_rn;

            return w;
        }



        //Angular constraints

        //w = n^T * I^-1 * n
        //normal - direction between constraints???
        public static float Calculate(MyRigidBody rb, Vector3 normal)
        {
            if (rb.invMass == 0f)
            {
                return 0f;
            }

            Vector3 n = normal;

            //Global -> local because we gonna use the Inertia
            n = rb.invRot * n;

            //w = n^T * I^-1 * n
            float w =
                n.x * n.x * rb.invInertia.x +
                n.y * n.y * rb.invInertia.y +
                n.z * n.z * rb.invInertia.z;

            return w;
        }
    }
}