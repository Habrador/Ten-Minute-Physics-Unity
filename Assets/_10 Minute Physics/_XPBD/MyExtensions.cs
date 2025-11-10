using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace XPBD
{
    public static class MyExtensions
    {
        //Whats the difference between conjugate and inverse?
        //q = a + bi + cj + dk 
        //Conjugate: q* = a − bi − cj − dk 
        //Inverse: q^-1 = q* / ||q||^2
        //The Conjugate is the inverse if the quaternion is normalized
        public static Quaternion Conjugate(this Quaternion q) => new(-q.x, -q.y, -q.z, q.w);
    }
}


