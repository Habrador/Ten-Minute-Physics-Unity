using System.Collections;
using System.Collections.Generic;
using UnityEditor.Search;
using UnityEngine;



namespace EulerianFluidSimulator
{
    //Settings for the fluid simulation and a ref to the fluid simulation itself    
    public class FluidScene
    {
        public FluidSim fluid = null;

        //The tutorial is using an int: tank (0), wind tunnel (1), paint (2), highres wind tunnel (3)
        //public int sceneNr = 0;
        //...but an enum is less confusing!
        public enum SceneNr
        {
            Tank, WindTunnel, Paint, HighResWindTunnel
        }

        public SceneNr sceneNr;

        //Display settings
        public bool showStreamlines = false;
        public bool showVelocities = false;
        public bool showPressure = false;
        public bool showSmoke = true;

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
        public void WorldToSim(float x, float y, out float xLocal, out float yLocal)
        {
            //The plane is assumed to be centered around world space origo
            //Origo of the simulation space is in bottom-left of the plane, so start by moving the point to simulation space (0,0)
            float origoOffsetX = simPlaneWidth * 0.5f;
            float origoOffsetY = simPlaneHeight * 0.5f;

            x += origoOffsetX;
            y += origoOffsetY;

            //Scale the coordinates to match simulation space

            //For testing
            //int cellsX = 4;
            //int cellsY = 2;

            //float h = 3f;

            //float simWidth = cellsX * h;
            //float simHeight = cellsY * h;

            //For actual simulation
            float xScale = fluid.SimWidth / simPlaneWidth;
            float yScale = fluid.SimHeight / simPlaneHeight;

            xLocal = x * xScale;
            yLocal = y * yScale;
        }



        //Convert from simulation space to world space
        public void SimToWorld(float x, float y, out float xGlobal, out float yGlobal)
        {
            //For testing
            //int cellsX = 4;
            //int cellsY = 2;

            //float h = 3f;

            //float simWidth = cellsX * h;
            //float simHeight = cellsY * h;

            //For actual simulation
            float xScale = fluid.SimWidth / simPlaneWidth;
            float yScale = fluid.SimHeight / simPlaneHeight;

            x /= xScale;
            y /= yScale;

            //Compensate for where origo starts  
            float origoOffsetX = simPlaneWidth * 0.5f;
            float origoOffsetY = simPlaneHeight * 0.5f;

            xGlobal = x - origoOffsetX;
            yGlobal = y - origoOffsetY;
        }



        //Convert from simulation space to cell space = in which cell is a certain coordinate
        public void SimToCell(float x, float y, out int xCell, out int yCell)
        {
            float cellSize = fluid.h;
        
            xCell = Mathf.FloorToInt(x / cellSize);
            yCell = Mathf.FloorToInt(y / cellSize);
        }
    }
}