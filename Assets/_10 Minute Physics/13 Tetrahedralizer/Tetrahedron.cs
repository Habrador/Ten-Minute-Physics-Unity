using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Standardized Tetrahedron methods
public static class Tetrahedron
{
    //
    // The volume of a tetrahedron
    //
    public static float Volume(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
    {
        Vector3 d0 = p1 - p0;
        Vector3 d1 = p2 - p0;
        Vector3 d2 = p3 - p0;

        float volume = Vector3.Dot(Vector3.Cross(d1, d2), d0) / 6f;

        return volume;
    }



    //
    // The quality of a tetrahedron
    //

    //https://www2.mps.mpg.de/homes/daly/CSDS/t4h/tetra.htm
    //1 is best quality, 0 is worst
    public static float TetQuality(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
    {
        //The ideal volume is calculated for a regular tetrahedron with a side length equal to the average of the 6 distances between the 4 points

        //The 6 distances between the 4 points
        Vector3 d0 = p1 - p0;
        Vector3 d1 = p2 - p0;
        Vector3 d2 = p3 - p0;
        Vector3 d3 = p2 - p1;
        Vector3 d4 = p3 - p2;
        Vector3 d5 = p1 - p3;

        float s0 = d0.magnitude;
        float s1 = d1.magnitude;
        float s2 = d2.magnitude;
        float s3 = d3.magnitude;
        float s4 = d4.magnitude;
        float s5 = d5.magnitude;

        //Mean square
        float ms = (s0 * s0 + s1 * s1 + s2 * s2 + s3 * s3 + s4 * s4 + s5 * s5) / 6f;

        //Root mean square to get the average length of all sides
        float rms = Mathf.Sqrt(ms);

        //The ideal tetrahedron has volume L^3 * sqrt(2) / 12
        float vol_ideal = ((rms * rms * rms) * Mathf.Sqrt(2f)) / 12f;

        //The actual volume
        float vol_actual = Tetrahedron.Volume(p0, p1, p2, p3);

        //Compare the actual volume with the ideal volume
        float quality = vol_actual / vol_ideal;

        return quality;
    }
}
