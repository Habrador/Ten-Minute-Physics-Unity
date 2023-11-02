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
        public Vector2 WorldToSim(Vector2 pos)
        {
            //The plane is assumed to be centered around world space origo
            //Origo of the simulation space is in bottom-left of the plane, so start by moving the point to simulation space (0,0)
            Vector2 offset = new(simPlaneWidth * 0.5f, simPlaneHeight * 0.5f);
            
            //Scale the coordinates to match simulation space
            Vector2 scale = new(fluid.SimWidth / simPlaneWidth, fluid.SimHeight / simPlaneHeight);

            pos += offset;

            pos *= scale;

            return pos;
        }



        //Convert from simulation space to world space
        public Vector2 SimToWorld(Vector2 pos)
        {
            //Scale
            Vector2 scale = new(fluid.SimWidth / simPlaneWidth, fluid.SimHeight / simPlaneHeight);

            //Compensate for where origo starts  
            Vector2 offset = new(simPlaneWidth * 0.5f, simPlaneHeight * 0.5f);

            pos /= scale;

            pos -= offset;

            return pos;
        }



        //Convert from simulation space to cell space = in which cell is a certain coordinate
        public Vector2Int SimToCell(Vector2 pos)
        {
            float cellSize = fluid.h;
        
            int xCell = Mathf.FloorToInt(pos.x / cellSize);
            int yCell = Mathf.FloorToInt(pos.y / cellSize);

            Vector2Int cell = new(xCell, yCell);

            return cell;
        }
    }
}