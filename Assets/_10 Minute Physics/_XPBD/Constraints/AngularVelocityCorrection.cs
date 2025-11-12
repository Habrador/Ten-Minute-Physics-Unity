using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

        public static void Apply()
        {

        }

    }
}