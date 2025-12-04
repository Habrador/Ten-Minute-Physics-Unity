using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Color = UnityEngine.Color;

//Display the Julia set and Madelbrot fractals
//Based on "19 Differential equations and calculus from scratch"
//From: https://matthias-research.github.io/pages/tenMinutePhysics/index.html
public class FractalsController : MonoBehaviour
{
    public GameObject planeObj;

    //Gradient color used to display the fractals
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

    private int maxIters = 100;
    //Center of the plane
    private float centerX = 0.0f;
    private float centerY = 0.0f;
    //Zoom level
    private float scale = 0.01f;
    //Parameters
    private float juliaX = -0.62580000000000f;
    private float juliaY = 0.40250000000000f;

    //Which fractal to draw?
    bool drawMandelbrot = false;

    //Should we use the sci-color or just plain mono orange color
    bool drawMono = true;

    //How many pixels does our texture (canvas) have
    //Our canvas is twice as long as it is high
    private readonly int height = 200;
    private int width;



    private void Start()
    {
        width = 2 * height;

        DisplayFractals();
    }



    private void DisplayFractals()
    {
        //Generate the fractal colors
        Color32[,] colors = GenerateColors(width, height);

        //Display the colors on the plane
        Texture2D texture = GenerateTexture(colors);

        planeObj.GetComponent<MeshRenderer>().material.mainTexture = texture;
    }



    private Color32[,] GenerateColors(int width, int height)
    {
        Color32[,] colors = new Color32[width, height];

        //The start y coordinate in world space
        float y = (centerY - height / 2f) * scale;

        for (int j = 0; j < height; j++)
        {
            //The start x coordinate in world space
            float x = (centerX - width / 2f) * scale;

            for (int i = 0; i < width; i++)
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



    //The magic function that generates the fractals
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



    //Get a gradient color 0->255
    private Color32 GetGradientColor(int nr, int steps)
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

        Color32 finalColor = new((byte)color[0], (byte)color[1], (byte)color[2], (byte)255);

        return finalColor;
    }



    //Generate a texture with colors
    private Texture2D GenerateTexture(Color32[,] colors)
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
        Color32[] colors1D = new Color32[xRes * yRes];

        for (int x = 0; x < xRes; x++)
        {
            for (int y = 0; y < yRes; y++)
            {
                colors1D[x + (xRes * y)] = colors[x,y];
            }
        }

        fractalsTexture.SetPixels32(colors1D);

        //Copies changes you've made in a CPU texture to the GPU
        fractalsTexture.Apply(false);

        return fractalsTexture;
    }



    private void OnGUI()
    {
        GUILayout.BeginHorizontal("box");

        //Settings
        int fontSize = 20;

        RectOffset offset = new(5, 5, 5, 5);

        GUIStyle buttonStyle = new(GUI.skin.button)
        {
            //buttonStyle.fontSize = 0; //To reset because fontSize is cached after you set it once 

            fontSize = fontSize,
            margin = offset
        };

        GUIStyle textStyle = new(GUI.skin.label)
        {
            fontSize = fontSize,
            margin = offset
        };

        //Buttons and sliders
        if (GUILayout.Button(!drawMandelbrot ? "Julia" : "Mandelbrot", buttonStyle))
        {
            drawMandelbrot = !drawMandelbrot;

            DisplayFractals();
        }
        if (GUILayout.Button(drawMono ? "Mono" : "Gradient", buttonStyle))
        {
            drawMono = !drawMono;

            DisplayFractals();
        }

        GUILayout.Label("Iterations:", textStyle);

        int newMaxIters = (int)EditorGUILayout.Slider(maxIters, 1, 500);

        if (newMaxIters != maxIters)
        {
            maxIters = newMaxIters;

            DisplayFractals();
        }

        GUILayout.EndHorizontal();
    }
}
