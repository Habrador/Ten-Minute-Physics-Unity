using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Based on https://github.com/matthias-research/pages/blob/master/tenMinutePhysics/BlenderTetPlugin.py
public static class Tetrahedralizer
{
    private static int[,] tetFaces = new int[,] { { 2, 1, 0 }, { 0, 1, 3 }, { 1, 2, 3 }, { 2, 0, 3 } };


    //Called CreateTets in python code
    //- resolution - Interior resolution [0, 100]
    //- minQualityExp - Min Tet Quality Exp [-4, 0]
    //- oneFacePerTet - One Face Per Tet, making it easier to export?????
    //- tetScale - Tet Scale [0.1, 1]
    public static void CreateTetrahedralization(int resolution = 10, int minQualityExp = -3, bool oneFacePerTet = true, float tetScale = 0.8f)
    {
        float minQuality = Mathf.Pow(10f, minQualityExp);


    }



    //
    // Measure the quality of a tetrahedron
    //

    //https://www2.mps.mpg.de/homes/daly/CSDS/t4h/tetra.htm
    //1 is best quality, 0 is worst
    public static float TetQuality(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
    {
        //The ideal volume is calculated for a regular tetrahedron with a side length equal to the average of the 6 distances between the 4 points
        //This ideal tetrahedron has volume L^3 * sqrt(2) / 12

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

        //ms is mean square?
        float ms = (s0 * s0 + s1 * s1 + s2 * s2 + s3 * s3 + s4 * s4 + s5 * s5) / 6f;
        
        float rms = Mathf.Sqrt(ms);

        float vol_ideal = ((rms * rms * rms) * Mathf.Sqrt(2f)) / 12f;

        //The actual volume
        float vol_actual = Vector3.Dot(d0, Vector3.Cross(d1, d2)) / 6f;

        //Compare the actual volume with the ideal volume
        float quality = vol_actual / vol_ideal;

        return quality;
    }



    //
    // Compare edges
    //

    private static int CompareEdges()
    {
        Debug.Log("Implement this method!");
    
        return -1;
    }

    private static bool EqualEdges()
    {
        Debug.Log("Implement this method!");

        return false;
    }
}
