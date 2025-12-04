using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Color = UnityEngine.Color;

public class FractalsController : MonoBehaviour
{
    public GameObject planeObj;

    private readonly int[][] gradientColors = 
    {
        new int[] {15, 2, 66},
        new int[] {191, 41, 12},
        new int[] {222, 99, 11},
        new int[] {229, 208, 14},
        new int[] {255, 255, 255},
        new int[] {102, 173, 183},
        new int[] {14, 29, 104}
    };

    int maxIters = 100;
    bool drawMandelbrot = false;
    float centerX = 0.0f;
    float centerY = 0.0f;
    float scale = 0.0035f;

    float juliaX = -0.62580000000000f;
    float juliaY = 0.40250000000000f;

    //Should we use the sci-color or just plain color
    bool drawMono = true;


    void Start()
    {
        //Our canvas is twice as long as it is high
        int height = 100;
        int width = 2 * height;

        //Generate the fractal colors
        Color[,] colors = GenerateColors(width, height);

        //Display the colors on the plane
        Texture2D texture = GenerateTexture(colors);

        planeObj.GetComponent<MeshRenderer>().material.mainTexture = texture;
    }



    private Color[,] GenerateColors(int width, int height)
    {
        Color[,] colors = new Color[width, height];

        //The start y coordinate in world space
        float y = centerY - height / 2f * scale;

        for (int j = 0; j < height; j++)
        {
            //The start x coordinate in world space
            float x = centerX - width / 2f * scale;

            for (int i = 0; i < height; i++)
            {
                //Compute the color for this pixel
                int numIters = GetNumIters(x, y, drawMandelbrot ? x : juliaX, drawMandelbrot ? y : juliaY, maxIters);

                if (numIters < maxIters)
                {
                    colors[i, j] = drawMono ? Color.black : GetGradientColor(numIters, 20);
                }
                else
                {
                    colors[i, j] = drawMono ? new Color(255, 192, 0) : Color.black;
                }

                x += scale;
            }

            y += scale;
        }

        return colors;
    }



    private int GetNumIters(float x1, float x2, float c1, float c2, int maxIters)
    {
        for (int iters = 0; iters < maxIters; iters++)
        {
            if (x1 * x1 + x2 * x2 > 4.0f)
            {
                return iters;
            }

            float x = x1;
            x1 = x1 * x1 - x2 * x2;
            x2 = 2.0f * x * x2;

            x1 += c1;
            x2 += c2;
        }
        return maxIters;
    }



    //Get a color 0->255
    private Color GetGradientColor(int nr, int steps)
    {
        int numCols = gradientColors.Length;

        int col0 = Mathf.FloorToInt(nr / steps) % numCols;
        int col1 = (col0 + 1) % numCols;
        
        int step = nr % steps;

        int[] color = { 0, 0, 0 };

        for (int i = 0; i < 3; i++)
        {
            int c0 = gradientColors[col0][i];
            int c1 = gradientColors[col1][i];
            
            color[i] = Mathf.FloorToInt(c0 + (c1 - c0) / steps * step);
        }

        return new Color(color[0], color[1], color[2]);
    }



    private Texture2D GenerateTexture(Color[,] colors)
    {
        int xRes = colors.GetLength(0);
        int yRes = colors.GetLength(1);

        Texture2D fractalsTexture = new(xRes, yRes);

        //Texture settings
        //Dont blend the pixels
        fractalsTexture.filterMode = FilterMode.Point;

        //Blend the pixels 
        //fractalsTexture.filterMode = FilterMode.Bilinear;

        //Don't wrap the border with the border on the opposite side of the texture
        fractalsTexture.wrapMode = TextureWrapMode.Clamp;

        //Add all colors to the texture
        //2d array -> 1d array
        Color[] colors1D = new Color[xRes * yRes];

        for (int x = 0; x < xRes; x++)
        {
            for (int y = 0; y < yRes; y++)
            {
                colors1D[x + (xRes * y)] = colors[x,y];
            }
        }

        fractalsTexture.SetPixels(colors1D);

        //Copies changes you've made in a CPU texture to the GPU
        fractalsTexture.Apply(false);

        return fractalsTexture;
    }
}
