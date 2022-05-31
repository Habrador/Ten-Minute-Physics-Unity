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
    public static void CreateTetrahedralization(CustomMesh inputMesh, int resolution = 10, int minQualityExp = -3, bool oneFacePerTet = true, float tetScale = 0.8f)
    {
        float minQuality = Mathf.Pow(10f, minQualityExp);

        Mesh tetMesh = new Mesh();

        tetMesh.name = "Tets";


        //Create vertices from input mesh (which is called tree in the original code)
        //The guy in the code is adding a very small random value to each coordinate for some reason...
        //Maybe to avoid issues when the vertices are perpendicular to each other like in the 2d voronoi diagram 
        //Hes calling it distortion later on 
        List<Vector3> tetVerts = new List<Vector3>();

        foreach (Vector3 v in inputMesh.vertices)
        {        
            Vector3 randomizedVertex = new Vector3(v.x + RandEps(), v.y + RandEps(), v.z + RandEps());

            tetVerts.Add(randomizedVertex);
        }

        List<Triangle> triangles = inputMesh.GetTriangles();


        //Measure vertices
        float inf = float.MaxValue;

        Vector3 center = Vector3.zero;
        
        //Bounds
        Vector3 bMin = new Vector3(inf, inf, inf);
        Vector3 bMax = new Vector3(-inf, -inf, -inf);

        foreach (Vector3 p in tetVerts)
        {
            center += p;

            bMin.x = Mathf.Min(p.x, bMin.x);
            bMin.y = Mathf.Min(p.y, bMin.y);
            bMin.z = Mathf.Min(p.z, bMin.z);

            bMax.x = Mathf.Max(p.x, bMax.x);
            bMax.y = Mathf.Max(p.y, bMax.y);
            bMax.z = Mathf.Max(p.z, bMax.z);
        }

        center /= tetVerts.Count;

        //The distance from the center to the vertex the furthest away
        float radius = 0f;

        foreach (Vector3 p in tetVerts)
        {
            float d = (p - center).magnitude;

            radius = Mathf.Max(radius, d);
        }
        
        
        //Interior sampling = add new vertices inside of the mesh, which is why we needed the dimensions of the mesh
        if (resolution > 0)
        {
            Vector3 dims = bMax - bMin;

            float dim = Mathf.Max(dims.x, Mathf.Max(dims.y, dims.z));

            float h = dim / resolution;

            for (int xi = 0; xi < (int)(dims.x / h) + 1; xi++)
            {
                float x = bMin.x + xi * h + RandEps();

                for (int yi = 0; yi < (int)(dims.y / h) + 1; yi++)
                {
                    float y = bMin.y + yi * h + RandEps();

                    for (int zi = 0; zi < (int)(dims.z / h) + 1; zi++)
                    {
                        float z = bMin.z + zi * h + RandEps();

                        Vector3 p = new Vector3(x, y, z);

                        if (UsefulMethods.IsPointInsideMesh(triangles, p, 0.5f * h))
                        {
                            tetVerts.Add(p);
                        }
                    }
                }
            }
        }


        //Big tet to start with
        float s = 5f * radius;

        tetVerts.Add(new Vector3(-s, 0f, -s));
        tetVerts.Add(new Vector3(s, 0f, -s));
        tetVerts.Add(new Vector3(0f,  s,  s));
        tetVerts.Add(new Vector3(0f, -s,  s));


        //Generate tet ids

        //Returns number of faces = number of triangles (4 per tetra)
        CreateTetIds(tetVerts, inputMesh, minQuality);
    }



    private static void CreateTetIds(List<Vector3> tetVerts, CustomMesh inputMesh, float minQuality)
    {   
        
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



    //
    // Generate a small random value to avoid vertices that are lined up which may cause bugs
    //
    private static float RandEps()
    {
        float eps = 0.0001f;

        float randomValue = -eps + 2f * Random.Range(0f, 1f) * eps;

        return randomValue;
    }
    
}
