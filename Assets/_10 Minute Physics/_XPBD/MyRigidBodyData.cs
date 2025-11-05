using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace XPBD
{
    //Init rigid bodies
    public class MyRigidBodyData
    {
        public static void InitBox(MyRigidBody rb, Vector3 size, float density)
        {
            //Create the obj we can see
            GameObject newBoxObj = GameObject.CreatePrimitive(PrimitiveType.Cube);

            newBoxObj.transform.localScale = size;

            rb.visualObjects = new MyRigidBodyVisuals(newBoxObj);

            if (density > 0f)
            {
                //mass = volume * density
                float mass = density * size.x * size.y * size.z;

                rb.invMass = 1f / mass;

                //I (solid rectangular cuboid)
                //https://en.wikipedia.org/wiki/List_of_moments_of_inertia
                //h,w,d = height,width,depth
                //I_h = 1/12 * m * (w^2 + d^2)
                //I_w = 1/12 * m * (h^2 + d^2)
                //I_d = 1/12 * m * (h^2 + w^2)
                float Ix = 1f / 12f * mass * (size.y * size.y + size.z * size.z);
                float Iy = 1f / 12f * mass * (size.x * size.x + size.z * size.z);
                float Iz = 1f / 12f * mass * (size.x * size.x + size.y * size.y);

                rb.invInertia = new Vector3(1f / Ix, 1f / Iy, 1f / Iz);
            }
        }



        public static void InitSphere(MyRigidBody rb, Vector3 size, float density)
        {
            //Create the obj we can see
            //The tutorial is using two half-spheres where one is white and other is red
            //to easier see the rotations, we can maybe use a texture instead???
            GameObject newSphereObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);

            newSphereObj.transform.localScale = size.x * Vector3.one;

            rb.visualObjects = new MyRigidBodyVisuals(newSphereObj);

            if (density > 0f)
            {
                float r = size.x;

                //mass = volume * density = 4/3 * pi * r^3 * density
                float mass = 4f / 3f * Mathf.PI * r * r * r * density;

                rb.invMass = 1f / mass;

                //I (solid sphere)
                //https://en.wikipedia.org/wiki/List_of_moments_of_inertia
                //I = 2/5 * m * r^2
                float I = 2f / 5f * mass * r * r;

                rb.invInertia = new Vector3(1f / I, 1f / I, 1f / I);
            }
        }

    }
}