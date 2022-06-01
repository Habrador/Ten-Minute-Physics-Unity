using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Based on https://github.com/matthias-research/pages/blob/master/tenMinutePhysics/BlenderTetPlugin.py
public static class Tetrahedralizer
{
    private static readonly int[,] tetFaces = new int[,] { { 2, 1, 0 }, { 0, 1, 3 }, { 1, 2, 3 }, { 2, 0, 3 } };


    //Called CreateTets in python code
    //- resolution - Interior resolution [0, 100] - used when we add extra vertices inside of the mesh. 0 means no points added 
    //- minQualityExp - Min Tet Quality Exp [-4, 0]
    //- oneFacePerTet - One Face Per Tet, making it easier to export?????
    //- tetScale - Tet Scale [0.1, 1]
    public static void CreateTetrahedralization(CustomMesh originalMesh, int resolution = 10, int minQualityExp = -3, bool oneFacePerTet = true, float tetScale = 0.8f, List<Vector3> debugPoints = null)
    {
        float minQuality = Mathf.Pow(10f, minQualityExp);

        //The mesh where the tetrahedrons will live
        CustomMesh tetMesh = new CustomMesh();

        tetMesh.name = "Tets";

        //Create vertices from input mesh (which is called tree in the original code)
        //The guy in the code is adding a very small random value to each coordinate for some reason...
        //Maybe to avoid issues when the vertices are perpendicular to each other like in the 2d voronoi diagram 
        //Hes calling it distortion later on 
        List<Vector3> tetVerts = new List<Vector3>();

        foreach (Vector3 v in originalMesh.vertices)
        {        
            Vector3 randomizedVertex = new Vector3(v.x + UsefulMethods.RandEps(), v.y + UsefulMethods.RandEps(), v.z + UsefulMethods.RandEps());

            tetVerts.Add(randomizedVertex);
        }


        //Measure vertices
        MeasureVertices(tetVerts, out Vector3 bMin, out Vector3 bMax, out float radius);
        

        //Interior sampling = add new vertices inside of the mesh
        AddInteriorPoints(resolution, originalMesh, tetVerts, bMin, bMax);

        debugPoints.Clear();
        debugPoints.AddRange(tetVerts);

        //Big tet to start with        
        float s = 5f * radius;

        tetVerts.Add(new Vector3(-s, 0f, -s));
        tetVerts.Add(new Vector3(s, 0f, -s));
        tetVerts.Add(new Vector3(0f,  s,  s));
        tetVerts.Add(new Vector3(0f, -s,  s));


        //Generate tet ids

        //Returns number of faces = number of triangles (4 per tetra)
        CreateTetIds(tetVerts, originalMesh, minQuality);


        //Finalize stuff
        
        //Generate either
        //- one face per triangle in each tetrahedron
        //- one non-planar quad-face per tetrahedron (which is better for exporting). So you get two triangles per tetrahedron, and you can figure out the other triangles by using the other two triangles???? If each face in each terahedron has a triangle it makes it difficult to identify a tetrahedron after improting it???
    }



    private static void MeasureVertices(List<Vector3> tetVerts, out Vector3 bMin, out Vector3 bMax, out float radius)
    {
        //Center of the mesh
        Vector3 center = Vector3.zero;

        //Bounds of the mesh
        float inf = float.MaxValue;

        bMin = new Vector3(inf, inf, inf);
        bMax = new Vector3(-inf, -inf, -inf);

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


        //The radius of the mesh: the distance from the center to the vertex the furthest away
        radius = 0f;

        foreach (Vector3 p in tetVerts)
        {
            float d = (p - center).magnitude;

            radius = Mathf.Max(radius, d);
        }
    }



    //Add points inside of the mesh
    private static void AddInteriorPoints(int resolution, CustomMesh originalMesh, List<Vector3> tetVerts, Vector3 bMin, Vector3 bMax)
    {
        if (resolution <= 0)
        {
            return;
        }


        Vector3 dims = bMax - bMin;

        float dim = Mathf.Max(dims.x, Mathf.Max(dims.y, dims.z));

        //The distance between each new vertex
        float h = dim / resolution;

        for (int xi = 0; xi < (int)(dims.x / h) + 1; xi++)
        {
            float x = bMin.x + xi * h + UsefulMethods.RandEps();

            for (int yi = 0; yi < (int)(dims.y / h) + 1; yi++)
            {
                float y = bMin.y + yi * h + UsefulMethods.RandEps();

                for (int zi = 0; zi < (int)(dims.z / h) + 1; zi++)
                {
                    float z = bMin.z + zi * h + UsefulMethods.RandEps();

                    Vector3 p = new Vector3(x, y, z);

                    //Only add the point if it is within the mesh
                    if (UsefulMethods.IsPointInsideMesh(originalMesh.vertices, originalMesh.triangles, p, 0.5f * h))
                    {
                        tetVerts.Add(p);
                    }
                }
            }
        }
    }



    private static void CreateTetIds(List<Vector3> verts, CustomMesh inputMesh, float minQuality)
    {
        //pos in verts list (4 per tetra in the list?) (4 * tetNr + 0) to get the first vertex 
        List<int> tetIds = new List<int>();
        List<int> tetMarks = new List<int>();
        List<int> neighbors = new List<int>();

        //The normals to each tetra triangle
        List<Vector3> planesN = new List<Vector3>();
        //What is D??? You calculate it by using Dot(p0, n) where n is the normal and p0 a triangle vertex
        List<float> planesD = new List<float>();

        int tetMark = 0;
        int firstFreeTet = -1;

        //How many vertices we have excluding the big tetra we added as first tetra
        int firstBig = verts.Count - 4;

        //Start with the first big tetra
        tetIds.Add(firstBig + 0);
        tetIds.Add(firstBig + 1);
        tetIds.Add(firstBig + 2);
        tetIds.Add(firstBig + 3);

        tetMarks.Add(0);

        for (int i = 0; i < 4; i++)
        {
            neighbors.Add(-1);

            //What is going on here???? 
            //tetFaces are { { 2, 1, 0 }, { 0, 1, 3 }, { 1, 2, 3 }, { 2, 0, 3 } }
            //These correspons to the tetras 4 triangles and their orientation, similar to a mesh's triangle indices
            //So we can get all 4 triangle from the list of all vertices if we have the tetId 
            Vector3 p0 = verts[firstBig + tetFaces[i, 0]];
            Vector3 p1 = verts[firstBig + tetFaces[i, 1]];
            Vector3 p2 = verts[firstBig + tetFaces[i, 2]];

            Vector3 n = Vector3.Cross(p1 - p0, p2 - p0).normalized;

            planesN.Add(n);
            planesD.Add(Vector3.Dot(p0, n));
        }

        //Vector3 center = Vector3.zero;


        //
        // Main tetrahedralization algorithm starts here
        //


        //Add all input points one-by-one
        for (int i = 0; i < firstBig; i++)
        {
            Vector3 p = verts[i];

            if (i % 100 == 0)
            {
                Debug.Log($"Inserting vert { i + 1 } of { firstBig }");
            }


            //Find non-deleted tetra
            int tetNr = 0;

            int safety = 0;
            
            while (tetIds[4 * tetNr] < 0)
            {
                tetNr += 1;

                safety += 1;

                if (safety > 1000000)
                {
                    Debug.Log("Stuck in infinite loop when finding non-deleted tet");

                    break;
                }
            }


            //Search for the tetra the point is in
            tetMark += 1;

            bool found = false;

            safety = 0;

            while (!found)
            {
                safety += 1;

                if (safety > 1000000)
                {
                    Debug.Log("Stuck in infinite loop when searchig for the tetra the point is in");

                    break;
                }

                if (tetNr < 0 || tetMarks[tetNr] == tetMark)
                {
                    break;
                }

                tetMarks[tetNr] = tetMark;

                int id0 = tetIds[4 * tetNr + 0];
                int id1 = tetIds[4 * tetNr + 1];
                int id2 = tetIds[4 * tetNr + 2];
                int id3 = tetIds[4 * tetNr + 3];

                Vector3 center = (verts[id0] + verts[id1] + verts[id2] + verts[id3]) * 0.25f;

                float minT = float.MaxValue;

                int minFaceNr = -1;

                for (int j = 0; j < 4; j++)
                {
                    Vector3 n = planesN[4 * tetNr + j];
                    float d = planesD[4 * tetNr + j];

                    float hp = Vector3.Dot(n, p) - d;
                    float hc = Vector3.Dot(n, center) - d;

                    float t = hp - hc;

                    if (t == 0f)
                    {
                        continue;
                    }

                    //Time when c -> p hits the face
                    t = -hc / t;

                    if (t >= 0f && t < minT)
                    {
                        minT = t;
                        minFaceNr = j;
                    }
                }
                
                if (minT >= 1f)
                {
                    found = true;
                }
                else
                {
                    tetNr = neighbors[4 * tetNr + minFaceNr];
                }
            }

            if (!found)
            {
                Debug.Log("Failed to insert vertex");
                
                continue;
            }


            //Find violatig tets
            //Flood-fill from that tetra to find the tetras that should be removed

            //Remove the tetras

            //Add a tetra-fan at the new point

        }


        //Remove the tetras we dont want in the result
        // - the tetras with low quality
        // - the tetras that are not part of the original mesh (the center if the tetra is outside of the mesh)
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
