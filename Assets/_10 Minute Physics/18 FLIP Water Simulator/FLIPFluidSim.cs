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
        //Same as 1/h - not sure why hes using it... Theres an invSpacing above...
        //To save computations: this.pInvSpacing = 1.0 / (2.2 * particleRadius);
        //We use it because we might use some other cell structure to handle particle-particle collision more efficient
        //To handle particle-particle collision we check the cell the particle is in and surrounding cells. To make sure that works, we may use another spacing... 
        private readonly float pInvSpacing;
        //See above why these may differ from numX and numCells, etc
        private readonly int pNumX;
        private readonly int pNumY;
        private readonly int pNumCells;
        //For particle-particle collision
        //How many particles are in each cell?
        private readonly int[] numCellParticles;
        //Partial sums, store the cummulative number of particles in each cell
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
            //Should be one bigger to be on the safe side
            this.firstCellParticle = new int[this.pNumCells + 1];
            this.cellParticleIds = new int[maxParticles];

            this.numParticles = 0;
        }



        //
        // Simulation loop for the fluid
        //

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
            float obstacleY, 
            float obstacleRadius, 
            float obstacleVelX, 
            float obstacleVelY)
        {
            int numSubSteps = 1;

            float sdt = dt / (float)numSubSteps;

            for (int step = 0; step < numSubSteps; step++)
            {
                //Simulate particles
            
                //Update particle vel and pos by adding gravity
                IntegrateParticles(sdt, gravity);
                
                if (separateParticles)
                {
                    PushParticlesApart(numParticleIters);
                }

                //Handle particle-world collisions
                HandleParticleCollisions(obstacleX, obstacleY, obstacleRadius, obstacleVelX, obstacleVelY);


                //Velocity transfer
                //TransferVelocities(true);


                //UpdateParticleDensity();


                //Make the grid velocities incompressible
                //SolveIncompressibility(numPressureIters, sdt, overRelaxation, compensateDrift);


                //Velocity transfer: Grid -> Particles
                //TransferVelocities(false, flipRatio);
            }

            //UpdateParticleColors();

            //UpdateCellColors();
        }



        //
        // Simulate particles
        //

        //Move particles
        private void IntegrateParticles(float dt, float gravity)
        {
            //For each particle
            for (int i = 0; i < this.numParticles; i++)
            {
                //Update vel: v = v + a * dt
                //y dir with gravity
                this.particleVel[2 * i + 1] += dt * gravity;

                //Update pos: s = s + v * dt
                //x dir
                this.particlePos[2 * i] += this.particleVel[2 * i] * dt;
                //y dir
                this.particlePos[2 * i + 1] += this.particleVel[2 * i + 1] * dt;
            }
        }


        //Handle particle-world collisions
        private void HandleParticleCollisions(float obstacleX, float obstacleY, float obstacleRadius, float obstacleVelX, float obstacleVelY)
        {
            //this.pInvSpacing = 1.0 / (2.2 * particleRadius);
            //-> h = 2.2 * particleRadius  
            //Why are we just not using this.h??? They seem to have the same value...
            float h = 1f / this.invSpacing;
            //float r = this.particleRadius;

            //Debug.Log(h);
            //Debug.Log(this.h);

            //For collision with moving cirlce obtacle
            //The minimum distance allowed between a particle and the obstacle 
            float minDist = obstacleRadius + this.particleRadius;
            float minDistSquare = minDist * minDist;

            //For collision with walls
            //First cell has width h
            float minX = h + this.particleRadius;
            float maxX = (this.numX - 1) * h - this.particleRadius;
            float minY = h + this.particleRadius;
            float maxY = (this.numY - 1) * h - this.particleRadius;

            //For each particle
            for (int i = 0; i < this.numParticles; i++)
            {
                float x = this.particlePos[2 * i];
                float y = this.particlePos[2 * i + 1];


                //Obstacle collision
                //The distance square between the particle and the obstacle
                float dx = x - obstacleX;
                float dy = y - obstacleY;
                float distSquare = dx * dx + dy * dy;

                //If a particle is colliding with the moving obstalcle, set their velocity to the velocity of the obstacle
                if (distSquare < minDistSquare)
                {
                    this.particleVel[2 * i] = obstacleVelX;
                    this.particleVel[2 * i + 1] = obstacleVelY;
                }


                //Wall collisions
                //If a particle is outside, move it in again and set its velocity to zero
                //x
                if (x < minX)
                {
                    x = minX;
                    this.particleVel[2 * i] = 0f;
                }
                if (x > maxX)
                {
                    x = maxX;
                    this.particleVel[2 * i] = 0f;
                }
                //y
                if (y < minY)
                {
                    y = minY;
                    this.particleVel[2 * i + 1] = 0f;
                }
                if (y > maxY)
                {
                    y = maxY;
                    this.particleVel[2 * i + 1] = 0f;
                }

                //Update position
                this.particlePos[2 * i] = x;
                this.particlePos[2 * i + 1] = y;
            }
        }


        //Handle particle-particle collision
        //Same idea as in tutorial 11 "Finding overlaps among thousands of objects blazing fast"
        private void PushParticlesApart(int numIters)
        {
            float colorDiffusionCoeff = 0.001f;

            //Count particles per cell
            System.Array.Fill(this.numCellParticles, 0);
            //this.numCellParticles.fill(0);

            //For each particle
            for (int i = 0; i < this.numParticles; i++)
            {
                float x = this.particlePos[2 * i];
                float y = this.particlePos[2 * i + 1];

                //Which cell is this particle in?  
                int xi = Mathf.Clamp(Mathf.FloorToInt(x * this.pInvSpacing), 0, this.pNumX - 1);
                int yi = Mathf.Clamp(Mathf.FloorToInt(y * this.pInvSpacing), 0, this.pNumY - 1);
                
                //2d array to 1d
                int cellNr = xi * this.pNumY + yi;
                
                //Add 1 to this cell because a particle is in it
                this.numCellParticles[cellNr]++;
            }

            
            //Partial sums
            int first = 0;

            //For each cell
            for (int i = 0; i < this.pNumCells; i++)
            {
                first += this.numCellParticles[i];

                this.firstCellParticle[i] = first;
            }

            //Guard
            this.firstCellParticle[this.pNumCells] = first;


            //Fill particles into cells
            for (int i = 0; i < this.numParticles; i++)
            {
                float x = this.particlePos[2 * i];
                float y = this.particlePos[2 * i + 1];

                //Which cell is this particle in?  
                int xi = Mathf.Clamp(Mathf.FloorToInt(x * this.pInvSpacing), 0, this.pNumX - 1);
                int yi = Mathf.Clamp(Mathf.FloorToInt(y * this.pInvSpacing), 0, this.pNumY - 1);

                //2d array to 1d
                int cellNr = xi * this.pNumY + yi;
                
                this.firstCellParticle[cellNr]--;
                
                this.cellParticleIds[this.firstCellParticle[cellNr]] = i;
            }


            //Push particles apart

            float minDist = 2f * this.particleRadius;
            float minDist2 = minDist * minDist;

            //The more iterations the fewer the particles are still colliding with each other
            for (int iter = 0; iter < numIters; iter++)
            {
                //For each particle
                for (int i = 0; i < this.numParticles; i++)
                {
                    float px = this.particlePos[2 * i];
                    float py = this.particlePos[2 * i + 1];

                    //Which cell is this particle in?
                    int pxi = Mathf.FloorToInt(px * this.pInvSpacing);
                    int pyi = Mathf.FloorToInt(py * this.pInvSpacing);

                    //Check this cell and surrounding cells for particles that can collide
                    int x0 = Mathf.Max(pxi - 1, 0);
                    int y0 = Mathf.Max(pyi - 1, 0);

                    int x1 = Mathf.Min(pxi + 1, this.pNumX - 1);
                    int y1 = Mathf.Min(pyi + 1, this.pNumY - 1);

                    for (int xi = x0; xi <= x1; xi++)
                    {
                        for (int yi = y0; yi <= y1; yi++)
                        {
                            //2d array to 1d 
                            int cellNr = xi * this.pNumY + yi;

                            int firstIndex = this.firstCellParticle[cellNr];
                            int lastIndex = this.firstCellParticle[cellNr + 1];
                            
                            for (int j = firstIndex; j < lastIndex; j++)
                            {
                                int id = this.cellParticleIds[j];

                                //Dont check the particle itself
                                if (id == i)
                                {
                                    continue;
                                }
                                
                                //The pos of the other particle we want to check collision against
                                float qx = this.particlePos[2 * id];
                                float qy = this.particlePos[2 * id + 1];

                                //The distance square to this other particle
                                float dx = qx - px;
                                float dy = qy - py;

                                float d2 = dx * dx + dy * dy;

                                //If outside or exactly at the same position
                                if (d2 > minDist2 || d2 == 0f)
                                {
                                    continue;
                                }

                                //The actual distance to the other particle
                                float d = Mathf.Sqrt(d2);

                                //Push each particle half the distance needed to make them no longer collide
                                float s = 0.5f * (minDist - d) / d;
                                
                                dx *= s;
                                dy *= s;
                                
                                //Update their positions
                                this.particlePos[2 * i] -= dx;
                                this.particlePos[2 * i + 1] -= dy;

                                this.particlePos[2 * id] += dx;
                                this.particlePos[2 * id + 1] += dy;

                                //Diffuse colors
                                //Why are we updating colors here??? 
                                /*
                                for (int k = 0; k < 3; k++)
                                {
                                    Color color0 = this.particleColor[3 * i + k];
                                    Color color1 = this.particleColor[3 * id + k];
                                    
                                    Color color = (color0 + color1) * 0.5;

                                    this.particleColor[3 * i + k] = color0 + (color - color0) * colorDiffusionCoeff;
                                    this.particleColor[3 * id + k] = color1 + (color - color1) * colorDiffusionCoeff;
                                }
                                */
                            }
                        }
                    }
                }
            }
        }
    }

}