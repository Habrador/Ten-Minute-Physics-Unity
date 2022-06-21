using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Useful for:
//- Soft-body physics
//- Procedural destruction
//- When generating 3d voronoi diagram 
//- To calculate the volume of a mesh
//Based on https://github.com/matthias-research/pages/blob/master/tenMinutePhysics/BlenderTetPlugin.py
//This video is also useful https://www.youtube.com/watch?v=hRz3sh7QQ6w for some of the math
public static class Tetrahedralizer
{
    //The orientation of each triangle in the tetra which has vertices 0-1-2-3
    private static readonly int[,] tetFaces = new int[,] { { 2, 1, 0 }, { 0, 1, 3 }, { 1, 2, 3 }, { 2, 0, 3 } };


    //Called CreateTets in python code
    //- resolution - Interior resolution [0, 100] - used when we add extra vertices inside of the mesh. 0 means no points added 
    //- minQualityExp - Min Tet Quality Exp [-4, 0]
    //- oneFacePerTet - One Face Per Tet, making it easier to export if each tetra is defined by a quad
    //- tetScale - Tet Scale, is used when we show all individual tetras to make them smaller to better see them [0.1, 1]
    public static CustomMesh CreateTetrahedralization(CustomMesh originalMesh, int resolution = 10, int minQualityExp = -3, bool oneFacePerTet = true, float tetScale = 0.8f, List<Vector3> debugPoints = null)
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
        tetVerts.Add(new Vector3(0f, s, s));
        tetVerts.Add(new Vector3(0f, -s, s));


        //Generate tet ids

        //Returns number of faces = number of triangles (4 per tetra)
        List<int> faces = CreateTetIds(tetVerts, originalMesh, minQuality);


        //Finalize stuff

        //Generate either
        //- One triangle per face in each tetrahedron
        //- One quad-face per tetrahedron (which is better for exporting). So you get two triangles per tetrahedron, which includes the 4 vertices of each tetrahedron  

        int numTets = (int)(faces.Count / 4);

        if (oneFacePerTet)
        {
            List<Vector3> vertices = originalMesh.vertices;

            int numSrcPoints = vertices.Count;

            int numPoints = tetVerts.Count - 4;

            //Copy vertices in the original mesh without distortion
            for (int i = 0; i < numSrcPoints; i++)
            {
                Vector3 co = vertices[i];
                tetMesh.vertices.Add(co);
            }
            //Copy the other vertices
            for (int i = numSrcPoints; i < numPoints; i++)
            {
                Vector3 p = tetVerts[i];
                tetMesh.vertices.Add(p);
            }
        }
        else
        {
            for (int i = 0; i < numTets; i++)
            {
                Vector3 v0 = tetVerts[faces[4 * i + 0]];
                Vector3 v1 = tetVerts[faces[4 * i + 1]];
                Vector3 v2 = tetVerts[faces[4 * i + 2]];
                Vector3 v3 = tetVerts[faces[4 * i + 3]];

                //Need a center so we can scale to tetra to better see all tetras
                Vector3 center = (v0 + v1 + v2 + v3) / 4f;

                //4 triangles per tetra
                for (int j = 0; j < 4; j++)
                {
                    //3 vertices per triangle
                    for (int k = 0; k < 3; k++)
                    {
                        Vector3 p = tetVerts[faces[4 * i + tetFaces[j, k]]];

                        //Scale the tetra towards the center so we can see it easier
                        p = center + (p - center) * tetScale;

                        tetMesh.vertices.Add(p);
                    }
                }
            }
        }


        //Build the triangles

        //bm.verts.ensure_lookup_table()

        int nr = 0;

        for (int i = 0; i < numTets; i++)
        {
            if (oneFacePerTet)
            {
                int id0 = faces[4 * i + 0];
                int id1 = faces[4 * i + 1];
                int id2 = faces[4 * i + 2];
                int id3 = faces[4 * i + 3];

                tetMesh.AddTriangle(id0, id1, id2);
                tetMesh.AddTriangle(id0, id2, id3);
            }
            else
            {
                List<Vector3> verts = tetMesh.vertices;
            
                for (int j = 0; j < 4; i++)
                {
                    //int id0 = verts[nr + 0];
                    //int id1 = verts[nr + 1];
                    //int id2 = verts[nr + 2];

                    int id0 = nr + 0;
                    int id1 = nr + 1;
                    int id2 = nr + 2;

                    tetMesh.AddTriangle(id0, id1, id2);

                    nr += 3;
                }
            }
        }

        //bm.to_mesh(tetMesh)
        //tetMesh.update()

        return tetMesh;
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

        //Using square is faster
        float radiusSqr = 0f;

        foreach (Vector3 p in tetVerts)
        {
            float d = (p - center).sqrMagnitude;

            radiusSqr = Mathf.Max(radiusSqr, d);
        }

        radius = Mathf.Sqrt(radiusSqr);
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

        Vector3[] verts = originalMesh.vertices.ToArray();
        int[] tris = originalMesh.triangles.ToArray();

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
                    if (UsefulMethods.IsPointInsideMesh(verts, tris, p, 0.5f * h))
                    {
                        tetVerts.Add(p);
                    }
                }
            }
        }
    }



    private static List<int> CreateTetIds(List<Vector3> verts, CustomMesh inputMesh, float minQuality)
    {
        //pos in verts list (4 per tetra in the list?) (4 * tetNr + 0) to get the first vertex 
        //-1 means deletet tetra?
        List<int> tetIds = new List<int>();
        //???
        List<int> tetMarks = new List<int>();
        //Neighbor to each tetra face (-1 if no neighbor) - is a position in tetIds
        List<int> neighbors = new List<int>();

        //The normals to each tetra triangle which is the same as the planes normal
        List<Vector3> planesN = new List<Vector3>();
        //d in the equation of the plane ax + by + cz + d = 0 where n = (a, b, c)
        List<float> planesD = new List<float>();

        //The last tetra id we added, making it faster to figure out in which tetrahedra a new point is added???
        //So instead of doing point-tetra collision for all tetras, we start by using the last added tetra and fire a ray from its center 
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
            
            //Deletet tras are marked with -1 and each tetra has 4 positions in the array (4 triangles), thus the 4 * tetNr
            while (tetIds[4 * tetNr] < 0)
            {
                if (IsStuck(ref safety, "Stuck in infinite loop when finding non-deleted tet"))
                {
                    break;
                }

                tetNr += 1;
            }


            //Search for the tetra the point is in
            tetMark += 1;

            bool found = false;

            safety = 0;

            while (!found)
            {
                if (IsStuck(ref safety, "Stuck in infinite loop when searchig for the tetra the point is in"))
                {
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

                //Create a ray from the tetra center to the new point
                //Find which face is intersecting this ray, and move across the face to the neighbor
                //Repeat until you are in the tetra the point p is in
                Vector3 center = (verts[id0] + verts[id1] + verts[id2] + verts[id3]) * 0.25f;

                //Best ray-intersection point length
                float minT = float.MaxValue;

                //The face of the tetra we should cross to move closer to the final tetra
                int minFaceNr = -1;

                //Loop through the tetras 4 faces
                for (int j = 0; j < 4; j++)
                {
                    //Ray-plane intersection
                    Vector3 n = planesN[4 * tetNr + j];
                    float d = planesD[4 * tetNr + j];

                    float hp = Vector3.Dot(n, p) - d;
                    float hc = Vector3.Dot(n, center) - d;

                    float t = hp - hc;

                    if (t == 0f)
                    {
                        continue;
                    }

                    //Time when center -> p hits the face
                    t = -hc / t;

                    //If the face is infront of the ray and the intersection point is better
                    if (t >= 0f && t < minT)
                    {
                        minT = t;
                        minFaceNr = j;
                    }
                }
                
                //We found the tetra the point is in
                if (minT >= 1f)
                {
                    found = true;
                }
                //Move across the face to the enighboring tetra
                else
                {
                    tetNr = neighbors[4 * tetNr + minFaceNr];
                }
            }

            //Something went wrong, perhaps the start big tetra is too small or some floating point precision issues???
            if (!found)
            {
                Debug.Log("Failed to insert vertex");
                
                continue;
            }


            //Find violating tets
            //Flood-fill from that tetra to find the tetras that should be removed
            //So we dont need to check all tetras - only the ones that appear during the flood-fill algorithm
            tetMark += 1;

            List<int> violatingTets = new();

            Queue<int> stack = new();

            //Add the first one to the queue because we know its violating
            stack.Enqueue(tetNr);

            safety = 0;

            while (stack.Count != 0)
            {
                if (IsStuck(ref safety, "Stuck in infinite loop when floodfilling violating tetras"))
                {
                    break;
                }
            
            
                tetNr = stack.Dequeue();

                if (tetMarks[tetNr] == tetMark)
                {
                    continue;
                }

                tetMarks[tetNr] = tetMark;

                violatingTets.Add(tetNr);

                //For each neighbor
                for (int j = 0; j < 4; j++)
                {
                    int n = neighbors[4 * tetNr + j];

                    //If there's no neighbor or we have already visited the neighbor
                    if (n < 0 || tetMarks[n] == tetMark)
                    {
                        continue;
                    }

                    //Delaunay condition test
                    int id0 = tetIds[4 * n + 0];
                    int id1 = tetIds[4 * n + 1];
                    int id2 = tetIds[4 * n + 2];
                    int id3 = tetIds[4 * n + 3];

                    Vector3 c = UsefulMethods.GetCircumCenter(verts[id0], verts[id1], verts[id2], verts[id3]);

                    float r = (verts[id0] - c).magnitude;

                    //This tetra is violating the delaunay conditional test
                    if ((p - c).magnitude < r)
                    {
                        stack.Enqueue(n);
                    }
                }
            }

            //Remove the tetras that are violating, and create new ones
            List<Edge> edges = new();

            for (int j = 0; j < violatingTets.Count; j++)
            {
                tetNr = violatingTets[j];

                //Copy info before we delete it
                int[] ids = new int[4];
                int[] ns = new int[4];

                for (int k = 0; k < 4; k++)
                {
                    ids[k] = tetIds[4 * tetNr + k];
                    ns[k] = neighbors[4 * tetNr + k];
                }

                //Delete the tet
                tetIds[4 * tetNr] = -1;
                tetIds[4 * tetNr + 1] = firstFreeTet;
                firstFreeTet = tetNr;

                //Visit neighbors
                for (int k = 0; k < 4; k++)
                {
                    int n = ns[k];

                    if (n >= 0 && tetMarks[n] == tetMark)
                    {
                        continue;
                    }

                    //No neighbor or neighbor is not-violating -> we are facing the border

                    //Create new tet
                    int newTetNr = firstFreeTet;

                    if (newTetNr >= 0)
                    {
                        firstFreeTet = tetIds[4 * firstFreeTet + 1];
                    }
                    else
                    {
                        newTetNr = (int)(tetIds.Count / 4);
                        
                        tetMarks.Add(0);

                        for (int l = 0; l < 4; l++)
                        {
                            tetIds.Add(-1);
                            neighbors.Add(-1);
                            planesN.Add(Vector3.zero);
                            planesD.Add(0f);
                        }
                    }

                    int id0 = ids[tetFaces[k, 2]];
                    int id1 = ids[tetFaces[k, 1]];
                    int id2 = ids[tetFaces[k, 0]];

                    tetIds[4 * newTetNr + 0] = id0;
                    tetIds[4 * newTetNr + 1] = id1;
                    tetIds[4 * newTetNr + 2] = id2;
                    tetIds[4 * newTetNr + 3] = i;

                    neighbors[4 * newTetNr] = n;

                    if (n >= 0)
                    {
                        for (int l = 0; l < 4; l++)
                        {
                            if (neighbors[4 * n + l] == tetNr)
                            {
                                neighbors[4 * n + l] = newTetNr;
                            }
                        }
                    }

                    //Will set the neighbors among the new tets later

                    neighbors[4 * newTetNr + 1] = -1;
                    neighbors[4 * newTetNr + 2] = -1;
                    neighbors[4 * newTetNr + 3] = -1;

                    for (int l = 0; l < 4; l++)
                    {
                        Vector3 p0 = verts[tetIds[4 * newTetNr + tetFaces[l, 0]]];
                        Vector3 p1 = verts[tetIds[4 * newTetNr + tetFaces[l, 1]]];
                        Vector3 p2 = verts[tetIds[4 * newTetNr + tetFaces[l, 2]]];

                        Vector3 newN = Vector3.Cross(p1 - p0, p2 - p0).normalized;

                        float newD = Vector3.Dot(newN, p0);

                        planesN[4 * newTetNr + l] = newN;
                        planesD[4 * newTetNr + l] = newD;
                    }

                    if (id0 < id1)
                    {
                        edges.Add(new Edge(id0, id1, newTetNr, 1));
                    }
                    else
                    {
                        edges.Add(new Edge(id1, id0, newTetNr, 1));
                    }

                    if (id1 < id2)
                    {
                        edges.Add(new Edge(id1, id2, newTetNr, 2));
                    }
                    else
                    {
                        edges.Add(new Edge(id2, id1, newTetNr, 2));
                    }

                    if (id2 < id0)
                    {
                        edges.Add(new Edge(id2, id0, newTetNr, 3));
                    }
                    else
                    {
                        edges.Add(new Edge(id0, id2, newTetNr, 3));
                    }
                }
            }


            //Fix neighbors

            //Sort the edges
            //https://stackoverflow.com/questions/3163922/sort-a-custom-class-listt

            //List<Edge> sortedEdges = sorted(edges, key = cmp_to_key(compareEdges))

            List<Edge> sortedEdges = new List<Edge>(edges);

            sortedEdges.Sort((e0, e1) =>
            {
                if (e0.idA < e1.idA || (e0.idA == e1.idA && e0.idB < e1.idB))
                {
                    return -1;
                }
                else
                {
                    return 1;
                }
            });

            int nr = 0;
            int numEdges = sortedEdges.Count;

            safety = 0;

            while (nr < numEdges)
            {
                if (IsStuck(ref safety, "Stuck in infinite loop when fixing edges"))
                {
                    break;
                }

                Edge e0 = sortedEdges[nr];
                nr += 1;

                if (nr < numEdges && EqualEdges(sortedEdges[nr], e0))
                {
                    Edge e1 = sortedEdges[nr];

                    int id0 = tetIds[4 * e0.newTetNr + 0];
                    int id1 = tetIds[4 * e0.newTetNr + 1];
                    int id2 = tetIds[4 * e0.newTetNr + 2];
                    int id3 = tetIds[4 * e0.newTetNr + 3];

                    int jd0 = tetIds[4 * e1.newTetNr + 0];
                    int jd1 = tetIds[4 * e1.newTetNr + 1];
                    int jd2 = tetIds[4 * e1.newTetNr + 2];
                    int jd3 = tetIds[4 * e1.newTetNr + 3];

                    neighbors[4 * e0.newTetNr + e0.unknown] = e1.newTetNr;
                    neighbors[4 * e1.newTetNr + e1.unknown] = e0.newTetNr;

                    nr += 1;
                }
            }

        }


        //Remove the tetras we dont want in the result
        // - the tetras with low quality
        // - the tetras that are not part of the original mesh (the center if the tetra is outside of the mesh)
        // - the tetras we have deleted
        // - the tetras that belong to the original big tetra we started with

        int numTets = (int)(tetIds.Count / 4);
        int num = 0;
        int numBad = 0;

        Vector3[] meshVerts = inputMesh.vertices.ToArray();
        int[] meshTris = inputMesh.triangles.ToArray();

        for (int i = 0; i < numTets; i++)
        {
            int id0 = tetIds[4 * i + 0];
            int id1 = tetIds[4 * i + 1];
            int id2 = tetIds[4 * i + 2];
            int id3 = tetIds[4 * i + 3];

            if (id0 < 0 || id0 >= firstBig || id1 >= firstBig || id2 >= firstBig || id3 >= firstBig)
            {
                continue;
            }

            //The tetrahedron is of low quality
            Vector3 p0 = verts[id0];
            Vector3 p1 = verts[id1];
            Vector3 p2 = verts[id2];
            Vector3 p3 = verts[id3];

            float quality = Tetrahedron.TetQuality(p0, p1, p2, p3);

            if (quality < minQuality)
            {
                numBad += 1;

                continue;
            }

            //The center of the tetrahedron is outside of the original mesh 
            Vector3 center = (p0 + p1 + p2 + p3) / 4f;

            if (!UsefulMethods.IsPointInsideMesh(meshVerts, meshTris, center))
            {
                continue;
            }

            tetIds[num] = id0;

            num += 1;

            tetIds[num] = id1;

            num += 1;

            tetIds[num] = id2;

            num += 1;

            tetIds[num] = id3;

            num += 1;
        }

        //Delete the tets startig at num
        //del tetIds[num:]
        tetIds.RemoveRange(num, tetIds.Count - num);

        Debug.Log($"Number of bad tets deleted: {numBad}");

        Debug.Log($"Number of tets created: {(int)(tetIds.Count / 4)}");

        return tetIds;

    }



    //
    // Compare edges
    //

    //Used when sorting
    private static int CompareEdges(Edge e0, Edge e1)
    {
        if (e0.idA < e1.idA || (e0.idA == e1.idA && e0.idB < e1.idB))
        {
            return -1;
        }
        else
        {
            return 1;
        }
    }

    //Are two edges the same?
    private static bool EqualEdges(Edge e0, Edge e1)
    {
        bool areEqual = (e0.idA == e1.idA && e0.idB == e1.idB);

        return areEqual;
    }



    //
    // Help method to avoid getting stuck in infinite loop
    //
    private static bool IsStuck(ref int safety, string message)
    {
        bool isStuck = false;

        safety += 1;

        if (safety > 1000000)
        {
            Debug.Log(message);

            isStuck = true;
        }

        return isStuck;
    }
}
