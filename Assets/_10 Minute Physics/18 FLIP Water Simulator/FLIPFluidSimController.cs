using EulerianFluidSimulator;
using FLIPFluidSimulator;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


//Simulate a fluid where we also include air by using the FLIP method
//Based on: "How to write a FLIP Water Simulator" https://matthias-research.github.io/pages/tenMinutePhysics/
//The Eulerian fluid simulation had problem with numerical viscosity because of the advection step. This could sort-of be solved by using cubic interpolation, but a better way is to replace velocity advection with particle advection. We are still using a grid with fluid, so we combine grid simulation and particle simulation: PIC (Particle In Cell)
//A problem with PIC is that when we transfer velocities from the grid to the particles (and vice versa), the velocities are smoothed. To solve this, people came up with FLIP (Fluid Implicit Particle) where we interpolate the change in velocity from the grid to the particles, and then we increment the velocity of the particles based on this change. This means FLIP will be free of numerical diffusion!
//BUT the problem with FLIP is that it may develop noise. PIC doesnt have this problem so lets combine PIC and FLIP. 
//TODO:
//- What is drift? Balls getting clumped together? 
//- Optimize particle-particle intersection which is the current bottleneck
//- Why do we need both an array for solid/non-solid and solid/fluid/air array? 
public class FLIPFluidSimController : MonoBehaviour
{
    //Public
    public Material fluidMaterial;

    public GameObject particlePrefabGO;


    //Private
    private FLIPFluidScene scene;

    private FLIPFluidUI fluidUI;



    private void Start()
    {
        scene = new FLIPFluidScene(fluidMaterial);

        fluidUI = new FLIPFluidUI(this);

        //The size of the plane we run the simulation on so we can convert from world space to simulation space
        scene.simPlaneWidth = 2f;
        scene.simPlaneHeight = 1f;

        SetupScene();
    }



    private void Update()
    {
        //Display the fluid
        //DisplayFluid.TestDraw(scene);

        DisplayFLIPFluid.Draw(scene, particlePrefabGO);
    }



    private void LateUpdate()
    {
        //Interactions such as moving obstacles with mouse and pause the simulation
        fluidUI.Interaction(scene);
    }



    private void FixedUpdate()
    {
        //Simulate the fluid
        Simulate();
    }



    private void OnGUI()
    {
        //Display the UI
        fluidUI.MyOnGUI(scene);
    }



    //Simulate the fluid
    //Needs to be accessed from the UI so we can simulate step by step by pressing a key
    public void Simulate()
    {
        if (scene.isPaused)
        {
            return;
        }

        scene.fluid.Simulate(
                scene.dt,
                scene.gravity,
                scene.flipRatio,
                scene.numPressureIters,
                scene.numParticleIters,
                scene.overRelaxation,
                scene.compensateDrift,
                scene.separateParticles,
                scene.obstacleX,
                scene.obstacleY,
                scene.obstacleRadius,
                scene.obstacleVelX,
                scene.obstacleVelY);

        scene.frameNr++;
    }



    //
    // Init the fluid sim
    //

    private void SetupScene()
    {
        scene.obstacleRadius = 0.05f; //Was 0.15 but his simulation is 3x bigger in world space
        scene.overRelaxation = 1.9f;

        //float

        scene.SetTimeStep(1f / 60f);
        scene.numPressureIters = 40;
        scene.numParticleIters = 2;
        scene.isPaused = false;

        //scene.isPaused = false;

        //How detailed the simulation is in height (y) direction
        int res = 100;

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
        //This will generate 28860 particles which is slow as molasses
        //3000 particles is going relatively fast
        float relWaterHeight = 0.2f;
        float relWaterWidth = 0.3f;

        //Particle radius wrt cell size
        float r = 0.3f * h;
        //We want to init the particles not like a chessboard but like this:
        //o o o o
        // o o o
        //o o o o
        float dx = 2f * r;
        float dy = Mathf.Sqrt(3f) / 2f * dx;

        float tankWidth = numX * h;
        float tankHeight = numY * h;

        //Have to compensate for the border and 0.5 particle on each side to make sure they fit
        float borderAndOneParticleCompensation = 2f * h + 2f * r;

        //And then we divide the allowed distance with the distance between each particle to figure out how many particles fit
        int numParticlesX = Mathf.FloorToInt((relWaterWidth * tankWidth - borderAndOneParticleCompensation) / dx);
        int numParticlesY = Mathf.FloorToInt((relWaterHeight * tankHeight - borderAndOneParticleCompensation) / dy);

        int maxParticles = numParticlesX * numParticlesY;

        //Debug.Log(maxParticles);


        //Create a new fluid simulator
        FLIPFluidSim f = scene.fluid = new FLIPFluidSim(density, numX, numY, h, r, maxParticles);


        //Create particles
        f.numParticles = numParticlesX * numParticlesY;

        int p = 0;

        for (int i = 0; i < numParticlesX; i++)
        {
            for (int j = 0; j < numParticlesY; j++)
            {
                //o o o o
                // o o o
                //o o o o
                //To get every other particle to offset a little in x dir:
                float xOffset = j % 2 == 0 ? 0f : r;

                //x
                f.particlePos[p++] = h + r + dx * i + xOffset;
                //y
                f.particlePos[p++] = h + r + dy * j;
            }
        }


        //Setup walls for tank
        for (int cellX = 0; cellX < f.NumX; cellX++)
        {
            for (int cellY = 0; cellY < f.NumY; cellY++)
            {
                //1 = fluid
                float s = 1f;

                //Solid walls at left-right-bottom border
                if (cellX == 0 || cellX == f.NumX - 1 || cellY == 0)
                {
                    s = 0f;
                }

                f.s[f.To1D(cellX, cellY)] = s;
            }
        }


        //Add the circle we move with mouse
        SetObstacle(3f, 2f, true);
    }



    //
    // Position an obstacle in the fluid and make it interact with the fluid if it has a velocity
    //

    //x,y are in simulation space - NOT world space
    public void SetObstacle(float x, float y, bool reset)
    {
        //Give the fluid a velocity if we have dragged the obstacle
        float vx = 0f;
        float vy = 0f;

        if (!reset)
        {
            //Calculate the velocity the obstacle has
            //Should be Time.deltaTime and not scene.dt because we move the object in LateUpdate()
            vx = (x - scene.obstacleX) / Time.deltaTime;
            vy = (y - scene.obstacleY) / Time.deltaTime;
        }

        //Save the position of the obsstacle so we can later display it
        scene.obstacleX = x;
        scene.obstacleY = y;

        //Mark which cells are covered by the obstacle
        float r = scene.obstacleRadius;

        float rSquare = r * r;

        FLIPFluidSim f = scene.fluid;

        //Ignore border
        for (int cellX = 1; cellX < f.NumX - 2; cellX++)
        {
            for (int cellY = 1; cellY < f.NumY - 2; cellY++)
            {
                //Start by setting all cells to fluids (= 1)
                //Cant do an System.Array.Fill because then the border will also be fluid
                f.s[f.To1D(cellX, cellY)] = 1f;

                //Distance from circle center to cell center
                float dx = (cellX + 0.5f) * f.Spacing - x;
                float dy = (cellY + 0.5f) * f.Spacing - y;

                //Is the cell within the obstacle?
                //Using the square is faster than actual distance but gives the same result 
                float distSqr = dx * dx + dy * dy;

                if (distSqr < rSquare)
                {
                    //Mark this cell as obstacle 
                    f.s[f.To1D(cellX, cellY)] = 0f;

                    //Give the fluid a velocity if we have moved it
                    //These are the 4 velocities belonging to this cell
                    f.u[f.To1D(cellX, cellY)] = vx; //Left
                    f.u[f.To1D(cellX + 1, cellY)] = vx; //Right
                    f.v[f.To1D(cellX, cellY)] = vy; //Bottom
                    f.v[f.To1D(cellX, cellY + 1)] = vy; //Top
                }
            }
        }

        scene.showObstacle = true;
        scene.obstacleVelX = vx;
        scene.obstacleVelY = vy;
    }
}
