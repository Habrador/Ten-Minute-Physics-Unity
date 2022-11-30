using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Based on https://catlikecoding.com/unity/tutorials/procedural-grid/
public class ClothDataProcedural : ClothData
{
    private readonly float[] vertices;
    private readonly int[] triangles;

    public override float[] GetVerts => vertices;

    public override int[] GetFaceTriIds => triangles;



    public ClothDataProcedural()
    {
        //The cloth from the tutorial has the following dimensions
        //x [-0.2, 0.2]
        //y [0.345859, 1.145859]

        Vector3 posStart = new Vector3(-0.2f, 1.145859f, 0f);

        float xRange = 0.2f - (-0.2f);
        float yRange = 1.145859f - 0.345859f;

        //Meaning how many cells, so vertices are +1
        int resolutionX = 20;

        //We want the grid cells to be squares so Y resolution has to be different
        float cellSize = xRange / resolutionX;

        int resolutionY = Mathf.FloorToInt(yRange / cellSize);

        
        //Add the vertices
        Vector3[] vertices = new Vector3[(resolutionX + 1) * (resolutionY + 1)];

        Vector3 pos = posStart;

        for (int i = 0, y = 0; y <= resolutionY; y++)
        {        
            for (int x = 0; x <= resolutionX; x++, i++)
            {
                vertices[i] = pos;

                pos.x += cellSize;
            }

            pos.x = posStart.x;
            pos.y -= cellSize;
        }

        //Convert from Vector3 to flat array
        this.vertices = new float[vertices.Length * 3];
        
        for (int i = 0; i < vertices.Length; i++)
        {
            this.vertices[3 * i + 0] = vertices[i].x;
            this.vertices[3 * i + 1] = vertices[i].y;
            this.vertices[3 * i + 2] = vertices[i].z;
        }


        //Add the triangles
        this.triangles = new int[resolutionX * resolutionY * 6];

        for (int ti = 0, vi = 0, y = 0; y < resolutionY; y++, vi++)
        {
            for (int x = 0; x < resolutionX; x++, ti += 6, vi++)
            {
                triangles[ti] = vi;
                triangles[ti + 3] = triangles[ti + 2] = vi + 1;
                triangles[ti + 4] = triangles[ti + 1] = vi + resolutionX + 1;
                triangles[ti + 5] = vi + resolutionX + 2;
            }
        }
    }
}
