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

        public static void Apply()
        {

        }

    }
}