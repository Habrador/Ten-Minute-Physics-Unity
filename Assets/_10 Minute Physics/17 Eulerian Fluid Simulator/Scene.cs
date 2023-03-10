using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Settings for the fluid simulation and a ref to the fluid simulation itself
namespace FluidSimulator
{
    public class Scene
    {
        public FluidSim fluid = null;

        //The tutorial is using an int: tank (0), wind tunnel (1), paint (2), highres wind tunnel (3)
        //public int sceneNr = 0;

        //...but an enum is better
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
        public bool useOverRelaxation = true; //Is not in the tutorial but needs to be there to make Unity's toggles work

        //Trick to get a stable simulation by speeding up convergence [1, 2]
        public float overRelaxation = 1.9f;

        public float dt;

        //Need several íterations each update to make the fluid incompressible
        public int numIters = 100;

        //Is sometimes 0 for some reason...
        public float gravity = -9.81f;

        //Is used for some reason when we are in the "paint" scene to add smoke in some sinus curve
        public int frameNr = 0;

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
            float simWidth = fluid.GetWidth();
            float simHeight = fluid.GetHeight();

            x *= simWidth / simPlaneWidth;
            y *= simHeight / simPlaneHeight;

            Vector2 simSpaceCoordinates = new (x, y);

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
            float simWidth = fluid.GetWidth();
            float simHeight = fluid.GetHeight();

            x /= simWidth / simPlaneWidth;
            y /= simHeight / simPlaneHeight;

            x -= simPlaneWidth * 0.5f;
            y -= simPlaneHeight * 0.5f;

            Vector2 worldSpaceCoordinates = new(x, y);

            return worldSpaceCoordinates;
        }
    }
}