using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FLIPFluidSimulator
{
    public class FLIPFluidSim
    {
        //How it works:
        //- Treat air as nothing because has much smaller density than water. Dont process or access velocities between air cells
        //- Use particles with position and velocity. A water cell is a cell with particles in it -> PIC (Particle in Cell)

        //PIC:
        //Simulate Particles
        //Velocity transfer: Particles -> Grid
        //Make the grid velocities incompressible
        //Velocity transfer: Grid -> Particles
        //(Particles carry velocity so no grid advection step is needed!!!)
        //Introduces viscocity

        //FLIP:
        //Simulate Particles
        //Velocity transfer: Particles -> Grid and make a copy of the grid velocities
        //Make the grid velocities incompressible
        //Velocity transfer: Add velcity changes to the particles: incompressible vel - copy of the grid velocities
        //Introduces noise

        //Combine PIC with FLIP to minimize viscocity and noise: 90% FLIP + 10% PIC = success!

        //Make the solver aware of drift (fluid sinks to the bottom disappearing)
        //Decrease time step or increase iteration count = slow simulation
        //Better to push particles apart
        //...and compute particle density in each cell to reduce divergence in dense regions -> more outward push in dense regions


        //Simulation parameters
        private readonly float density;

        //Simulation grid settings
        public int numX;
        public int numY;
        //Cell height and width
        public float h;
        //1/h
        private float invSpacing;

        //Simulation data structures
        //Orientation of the grid:
        // i + 1 means right, j + 1 means up
        // (0,0) is bottom-left
        //Velocity field (u, v, w) 
        //A staggered grid is improving the numerical results with less artificial dissipation  
        //u component stored in the middle of the left vertical line of each cell
        //v component stored in the middle of the bottom horizontal line of each cell
        public readonly float[] u;
        public readonly float[] v;
        private readonly float[] uNew;
        private readonly float[] vNew;
        private readonly float[] du;
        private readonly float[] dv;
        //Pressure field
        public float[] p;
        //If obstacle (0) or fluid (1)
        //Should use float instead of bool because it makes some calculations simpler
        public float[] s;

        //In this simulation a cell can also be air
        private int FLUID_CELL = 0;
        private int AIR_CELL = 1;
        private int SOLID_CELL = 2;

        private readonly int[] cellType;

        private readonly Color[] cellColor;

        //Particles
        //How many particles?
        public int numParticles;
        //How many particles allowed?
        private int maxParticles;
        //The pos of each particle (x,y) after each other
        public readonly float[] particlePos;
        //The color of each particle
        private readonly Color[] particleColor;
        //The vel of each particle (x,y) after each other
        private readonly float[] particleVel;
        //The density of particles in a cell
        private readonly float[] particleDensity;
        //Rest
        private readonly float particleRestDensity;
        //Radius
        public readonly float particleRadius;
        //To save computations: this.pInvSpacing = 1.0 / (2.2 * particleRadius);
        private readonly float pInvSpacing;
        //???
        private readonly int pNumX;
        private readonly int pNumY;
        private readonly int pNumCells;
        //???
        private readonly int[] numCellParticles;
        private readonly int[] firstCellParticle;
        private readonly int[] cellParticleIds;


        //Convert between 2d and 1d array
        //The conversion can cause great confusion, so we better do it in one place throughout all code
        //Was (i * numY) + j in tutorial but should be i + (numX * j) if we want them row-by-row after each other in the flat array
        //Otherwise we get them column by column which is maybe how js prefers them when displaying???
        //https://softwareengineering.stackexchange.com/questions/212808/treating-a-1d-data-structure-as-2d-grid
        public int To1D(int i, int j) => i + (numX * j);

        //These are not the same as the height we set at start because of the two border cells
        public float SimWidth => numX * h;
        public float SimHeight => numY * h;

        //Is a coordinate in local space within the simulation area?
        public bool IsWithinArea(float x, float y) => (x > 0 && x < SimWidth && y > 0 && y < SimHeight);



        public FLIPFluidSim(float density, int numX, int numY, float h, float particleRadius, int maxParticles)
        {
            this.density = density;

            //Add 2 extra cells because we need a border, or are we adding two u's on each side???
            //Because we use a staggered grid, then there will be no u on the right side of the cells in the last column if we add new cells... The p's are in the middle and of the same size, so we add two new cells while ignoring there's no u's on the right side of the last column. The book "Fluid Simulation for Computer Graphics" says that the velocity arrays should be one larger than the pressure array because we have 1 extra velocity on the right side of the last column. 
            //He says border cells in the video
            this.numX = numX + 2;
            this.numY = numY + 2;
            this.h = h;
            this.invSpacing = 1f / this.h;

            int numCells = this.numX * this.numY;

            this.u = new float[numCells];
            this.v = new float[numCells];
            this.uNew = new float[numCells];
            this.vNew = new float[numCells];
            this.du = new float[numCells];
            this.dv = new float[numCells];

            this.p = new float[numCells];
            //Will init all cells to walls (0)
            this.s = new float[numCells];

            this.cellType = new int[numCells];
            this.cellColor = new Color[numCells];


            //Particles
            this.maxParticles = maxParticles;

            this.particlePos = new float[2 * this.maxParticles];
            this.particleColor = new Color[this.maxParticles];
            //Init the color
            //for (var i = 0; i < this.maxParticles; i++)
            //this.particleColor[3 * i + 2] = 1.0;

            this.particleVel = new float[2 * this.maxParticles];
            this.particleDensity = new float[numCells];
            this.particleRestDensity = 0f;

            this.particleRadius = particleRadius;
            this.pInvSpacing = 1f / (2.2f * particleRadius);

            this.pNumX = Mathf.FloorToInt(SimWidth * this.pInvSpacing) + 1;
            this.pNumY = Mathf.FloorToInt(SimHeight * this.pInvSpacing) + 1;

            this.pNumCells = this.pNumX * this.pNumY;

            this.numCellParticles = new int[this.pNumCells];
            this.firstCellParticle = new int[this.pNumCells + 1];
            this.cellParticleIds = new int[maxParticles];

            this.numParticles = 0;

        }



        //Simulation loop for the fluid
        public void Simulate(
            float dt, 
            float gravity, 
            float flipRatio, 
            int numPressureIters, 
            int numParticleIters, 
            float overRelaxation, 
            bool compensateDrift, 
            bool separateParticles, 
            float obstacleX, 
            float abstacleY, 
            float obstacleRadius)
        {
            int numSubSteps = 1;

            float sdt = dt / (float)numSubSteps;

            for (int step = 0; step < numSubSteps; step++)
            {
                //IntegrateParticles(sdt, gravity);
                
                if (separateParticles)
                {
                    //PushParticlesApart(numParticleIters);
                }
                
                //HandleParticleCollisions(obstacleX, abstacleY, obstacleRadius)

                //TransferVelocities(true);
                
                //UpdateParticleDensity();
                
                //SolveIncompressibility(numPressureIters, sdt, overRelaxation, compensateDrift);
                
                //TransferVelocities(false, flipRatio);
            }

            //UpdateParticleColors();
            
            //UpdateCellColors();
        }
    }

}