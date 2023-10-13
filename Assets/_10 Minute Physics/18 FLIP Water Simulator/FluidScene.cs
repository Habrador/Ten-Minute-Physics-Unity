using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FLIPFluidSimulator
{
    //Settings for the fluid simulation and a ref to the fluid simulation itself    
    public class FluidScene
    {
        public FLIPFluidSim fluid = null;

        //Display settings
        public bool showGrid = false;

        //Simulation settings
        //Is not in the tutorial but needs to be there to make Unity's toggle work
        public bool useOverRelaxation = true;

        //Relaxation
        //https://www.sanfoundry.com/computational-fluid-dynamics-questions-answers-under-relaxation/
        //Use a relaxation factor (coefficient) to increase the convergence of the solution by changing the values of the variables during the iterative process.
        // - Over-relaxation (coefficient > 1). Will lead to a higher rate of convergence and to a faster convergence. The disadvantage is that stability will decrease.
        // - Under-relaxation (coefficient < 1). The stability will increase, but convergence will be slower   
        //Here we will use a coefficient in the range [1, 2]
        public float overRelaxation = 1.9f;

        //Get the time step
        //Set this in a specific method because if we change dt we also have to change Time.fixedDeltaTime
        public float dt { get; private set; }

        //Need several iterations each update to make the fluid incompressible
        //Default is 40 and we set it in SetupScene
        public int numIters = 100;

        //Is sometimes 0 
        public float gravity = -9.81f;

        //Is used in the "paint" scene to add smoke in some sinus curve, so we can paint with different colors
        public int frameNr = 0;

        //Is the simulation paused?
        public bool isPaused = false;

        //Obstacles
        public bool showObstacle = false;

        //Local space
        public float obstacleX = 0f;
        public float obstacleY = 0f;

        public float obstacleRadius = 0.15f;

        //The plane we simulate the fluid on
        //The plane is assumed to be centered around world space origo
        public float simPlaneWidth;
        public float simPlaneHeight;

        //To which we attach the texture
        public Material fluidMaterial;

        //The texture used to display fluid data 
        public Texture2D fluidTexture;



        public FluidScene(Material fluidMaterial)
        {
            this.fluidMaterial = fluidMaterial;

            SetTimeStep(1f / 120f);
        }



        //Set the time step
        //Default is 1/120=0.008 in the source code while Unity default is 1/50=0.02
        //We could run the Simulate() method multiple times to get a smaller time step or update Time.fixedDeltaTime
        //It's important that the dt is small enough so that the maximum motion of the velocity field is less than the width of a grid cell: dt < h/u_max. But dt can sometimes be larger if theres a buffer around the cells, so you should use a constant you can experiment with: dt = k * (h/u_max)
        public void SetTimeStep(float timeStep)
        {
            this.dt = timeStep;
            Time.fixedDeltaTime = timeStep;
        }



        //Convert from world space to simulation space
        public Vector2 WorldToSim(float x, float y)
        {
            //The plane is assumed to be centered around world space origo
            //Origo of the simulation space is in bottom-left of the plane, so start by moving origo
            x += simPlaneWidth * 0.5f;
            y += simPlaneHeight * 0.5f;

            //Scale the coordinates to match simulation space

            //For testing
            //int cellsX = 4;
            //int cellsY = 2;

            //float h = 3f;

            //float simWidth = cellsX * h;
            //float simHeight = cellsY * h;

            //For actual simulation
            float simWidth = fluid.SimWidth;
            float simHeight = fluid.SimHeight;

            x *= simWidth / simPlaneWidth;
            y *= simHeight / simPlaneHeight;

            Vector2 simSpaceCoordinates = new(x, y);

            return simSpaceCoordinates;
        }



        //Convert from simulation space to world space
        public Vector2 SimToWorld(float x, float y)
        {
            //For testing
            //int cellsX = 4;
            //int cellsY = 2;

            //float h = 3f;

            //float simWidth = cellsX * h;
            //float simHeight = cellsY * h;

            //For actual simulation
            float simWidth = fluid.SimWidth;
            float simHeight = fluid.SimHeight;

            x /= simWidth / simPlaneWidth;
            y /= simHeight / simPlaneHeight;

            x -= simPlaneWidth * 0.5f;
            y -= simPlaneHeight * 0.5f;

            Vector2 worldSpaceCoordinates = new(x, y);

            return worldSpaceCoordinates;
        }



        //Convert from simulation space to cell space = in which cell is a certain coordinate
        public Vector2Int SimToCell(float x, float y)
        {
            float cellSize = fluid.h;

            int cellX = Mathf.FloorToInt(x / cellSize);
            int cellY = Mathf.FloorToInt(y / cellSize);

            Vector2Int cellPos = new(cellX, cellY);

            return cellPos;
        }
    }
}
