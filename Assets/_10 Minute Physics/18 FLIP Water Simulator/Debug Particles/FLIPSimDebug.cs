using EulerianFluidSimulator;
using FLIPFluidSimulator;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static UnityEngine.ParticleSystem;

//Observations:
// - If you remove all fluid methods except those that belong to the particles, the particles are falling to the bottom showing some erratic behaviour, in the original code as well. As they are all being crushed to the bottom, the more collisions checks we have to make and the slower it gets. 
public class FLIPSimDebug : MonoBehaviour
{
    //Public
    public Material fluidMaterial;

    public Material particlesMaterial;

    public GameObject particlesPlane;

    public GameObject particlePrefabObj;


    //Private
    private FLIPFluidScene scene;

    private FLIPFluidUI fluidUI;

    private FLIPFluidSim sim;

    private DisplayParticlesAsShader displayParticlesAsShader;



    private void Start()
    {
        scene = new FLIPFluidScene(fluidMaterial);

        //fluidUI = new FLIPFluidUI(this);

        //The size of the plane we run the simulation on so we can convert from world space to simulation space
        scene.simPlaneWidth = 2f;
        scene.simPlaneHeight = 1f;

        SetupScene();

        sim = scene.fluid;

        displayParticlesAsShader = new DisplayParticlesAsShader(particlesPlane, particlesMaterial);
    }



    private void Update()
    {
        //Display the fluid
        //DisplayFluid.TestDraw(scene);

        //DisplayFLIPFluid.Draw(scene);
        DisplayParticles(scene);
    }



    private void LateUpdate()
    {
        //Interactions such as moving obstacles with mouse and pause the simulation
        //fluidUI.Interaction(scene);
    }



    private void FixedUpdate()
    {
        //Simulate the fluid
        Simulate();
    }



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

        //How detailed the simulation is in height (y) direction (how many cells)
        int res = 20;

        //The height of the simulation (the plane might be smaller but it doesnt matter because we can pretend its 3m and no one knows)
        float simHeight = 3f;

        //The size of a cell
        float h = simHeight / res;

        //Debug.Log(h);

        //How many cells do we have
        //y is up
        int numY = res;
        //The plane we use here is twice as wide as high
        int numX = 2 * numY;

        //Density of the fluid (water)
        float density = 1000f;

        //Particles

        //Fill a rectangle with size 0.8 * height and 0.60 * width with particles
        //Tutorial is generating 32116 particles
        float relWaterHeight = 0.4f; //Was 0.8
        float relWaterWidth = 0.3f; //Was 0.6

        //Particle radius wrt cell size
        float r = 0.3f * h; //0.009

        //Debug.Log(r); //0.045

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
        //Debug.Log(tankWidth);
        //Debug.Log(tankHeight);


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
        //SetObstacle(3f, 2f, true);
    }



    private void Simulate()
    {
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
    }



    private void DisplayParticles(FLIPFluidScene scene)
    {
        //First update their colors
        //UpdateParticleColors(scene);

        displayParticlesAsShader.UpdateParticles(scene);
    }



    void OnDestroy()
    {
        displayParticlesAsShader.MyOnDestroy();
    }
}
