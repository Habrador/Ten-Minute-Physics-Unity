using FLIPFluidSimulator;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


//Simulate a fluid where we also include air by using the FLIP method
//Based on: "How to write a FLIP Water Simulator" https://matthias-research.github.io/pages/tenMinutePhysics/
//TODO:
//- What is drift?
public class FLIPFluidSimController : MonoBehaviour
{
    //Public
    public Material fluidMaterial;

    //Private
    private FluidScene scene;

    //private FluidUI fluidUI;



    private void SetupScene()
    {
        //scene.sceneNr = sceneNr;
        scene.obstacleRadius = 0.15f;
        scene.overRelaxation = 1.9f;

        scene.SetTimeStep(1f / 60f);
        scene.numPressureIters = 40;

        //How detailed the simulation is in height (y) direction
        //Default was 100 in the source code but it's slow as molasses in Unity
        int res = 50;

        //The height of the simulation (the plane might be smaller but it doesnt matter because we can pretend its 3m and no one knows)
        float simHeight = 3f;

        //The size of a cell
        float h = simHeight / res;

        //How many cells do we have
        //y is up
        int numY = res;
        //The plane we use here is twice as wide as high
        int numX = 2 * numY;

        //Density of the fluid (water)
        float density = 1000f;

        //Particles

        //Fill a rectangle with size 0.8 * height and 0.60 * width with particles
        float relWaterHeight = 0.8f;
        float relWaterWidth = 0.6f;

        //Particle radius wrt cell size
        float r = 0.3f * h;    
        float dx = 2f * r;
        float dy = Mathf.Sqrt(3f) / 2f * dx;

        float tankWidth = numX * h;
        float tankHeight = numY * h;

        int numParticlesX = Mathf.FloorToInt((relWaterWidth * tankWidth - 2f * h - 2f * r) / dx);
        int numParticlesY = Mathf.FloorToInt((relWaterHeight * tankHeight - 2f * h - 2f * r) / dy);
        
        int maxParticles = numParticlesX * numParticlesY;


        //Create a new fluid simulator
        FLIPFluidSim f = scene.fluid = new FLIPFluidSim(density, numX, numY, h, r, maxParticles);


        //Create particles
        f.numParticles = numParticlesX * numParticlesY;

        int p = 0;
        
        for (int i = 0; i < numX; i++)
        {
            for (int j = 0; j < numY; j++)
            {
                //(x,y)
                f.particlePos[p++] = h + r + dx * i + (j % 2 == 0 ? 0f : r);
                f.particlePos[p++] = h + r + dy * j;
            }
        }


        //Setup grid cells for tank
        int n = f.numY;

        for (int i = 0; i < f.numX; i++)
        {
            for (int j = 0; j < f.numY; j++)
            {
                //Fluid
                float s = 1f;

                //Solid walls at left-right-bottom border
                if (i == 0 || i == f.numX - 1 || j == 0)
                {
                    s = 0f;
                }

                f.s[i * n + j] = s;
            }
        }


        //Add the circle we move with mouse
        //SetObstacle(3f, 2f, true);
    }
}
