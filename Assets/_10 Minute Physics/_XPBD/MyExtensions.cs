using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace XPBD
{
    public static class MyExtensions
    {
        public static Quaternion Conjugate(this Quaternion q)
        {
            //Quaternion q = new Quaternion(b, c, d, a); 
            //Quaternion conjugate = new Quaternion(-b, -c, -d, a);
            Quaternion conjugated = new Quaternion(-q.x, -q.y, -q.z, q.w);

            return conjugated;
        }
    }
}


