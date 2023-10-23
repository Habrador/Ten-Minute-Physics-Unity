using System;
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
        //These are called fNumX (f = fluid) to make them differ from pNumX (p = particle)
        public int fNumX;
        public int fNumY;
        private int fNumCells;
        //Cell height and width
        public float h;
        //1/h
        private float fInvSpacing;

        //Simulation data structures
        //Orientation of the grid:
        // i + 1 means right, j + 1 means up
        // (0,0) is bottom-left

        //Velocity field (u, v, w) 
        //A staggered grid is improving the numerical results with less artificial dissipation  
        //u component stored in the middle of the left vertical line of each cell
        //v component stored in the middle of the bottom horizontal line of each cell
        public float[] u;
        public float[] v;
        private readonly float[] uPrev;
        private readonly float[] vPrev;
        private readonly float[] du;
        private readonly float[] dv;
        //Pressure field
        public float[] p;
        //If obstacle (0) or fluid (1)
        //Should use float instead of bool because it makes some calculations simpler
        public float[] s;

        //The different cell types we can have
        //In this simulation a cell can also be air
        private readonly int FLUID_CELL = 0;
        private readonly int AIR_CELL = 1;
        private readonly int SOLID_CELL = 2;

        private readonly int[] cellType;

        //Color of each cell (r, g, b) after each other. Color values are in the rannge [0,1]
        private readonly float[] cellColor;

        //Particles
        //How many particles?
        public int numParticles;
        //How many particles allowed?
        private int maxParticles;
        //The pos of each particle (x,y) after each other
        public readonly float[] particlePos;
        //The color of each particle (r,g,b) after each other. Color values are in the rannge [0,1]
        private readonly float[] particleColor;
        //The vel of each particle (x,y) after each other
        private readonly float[] particleVel;
        //The density of particles in a cell
        private readonly float[] particleDensity;
        //Rest
        private float particleRestDensity;
        //Particle radius
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
        public int To1D(int i, int j) => i + (fNumX * j);

        //These are not the same as the height we set at start because of the two border cells
        public float SimWidth => fNumX * h;
        public float SimHeight => fNumY * h;

        //Is a coordinate in local space within the simulation area?
        public bool IsWithinArea(float x, float y) => (x > 0 && x < SimWidth && y > 0 && y < SimHeight);



        public FLIPFluidSim(float density, int numX, int numY, float h, float particleRadius, int maxParticles)
        {
            this.density = density;

            //Add 2 extra cells because we need a border, or are we adding two u's on each side???
            //Because we use a staggered grid, then there will be no u on the right side of the cells in the last column if we add new cells... The p's are in the middle and of the same size, so we add two new cells while ignoring there's no u's on the right side of the last column. The book "Fluid Simulation for Computer Graphics" says that the velocity arrays should be one larger than the pressure array because we have 1 extra velocity on the right side of the last column. 
            //He says border cells in the video
            this.fNumX = numX + 2;
            this.fNumY = numY + 2;

            //Cellspacing
            this.h = h;
            this.fInvSpacing = 1f / this.h;

            int numCells = this.fNumX * this.fNumY;

            this.fNumCells = numCells;

            this.u = new float[numCells];
            this.v = new float[numCells];
            this.uPrev = new float[numCells];
            this.vPrev = new float[numCells];
            this.du = new float[numCells];
            this.dv = new float[numCells];

            this.p = new float[numCells];
            //Will init all cells to walls (0)
            this.s = new float[numCells];

            this.cellType = new int[numCells];

            //(r,g,b) after each other so all cells are 0 = black
            this.cellColor = new float[numCells * 3];


            //Particles
            this.maxParticles = maxParticles;

            this.particlePos = new float[2 * this.maxParticles];
            
            this.particleColor = new float[this.maxParticles * 3];
            
            //Init the colors to blue
            for (int i = 0; i < this.maxParticles; i++)
            {
                //r = 0
                //g = 0
                //b = 1
                //(r,g,b) after each other so index 2 = blue 
                this.particleColor[3 * i + 2] = 1f;
            }
                

            this.particleVel = new float[2 * this.maxParticles];
            this.particleDensity = new float[numCells];
            this.particleRestDensity = 0f;

            this.particleRadius = particleRadius;

            //To optimize particle-particle collision
            //Which is why we need another grid than the fluid grid
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
                    //Handle particle-particle collisions
                    PushParticlesApart(numParticleIters);
                }

                //Handle particle-world collisions
                HandleParticleCollisions(obstacleX, obstacleY, obstacleRadius, obstacleVelX, obstacleVelY);


                //Velocity transfer: Particles -> Grid
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
            //Why are we using 1f / this.fInvSpacing and not just h?
            float h = 1f / this.fInvSpacing;

            //For collision with moving cirlce obtacle
            //The minimum distance allowed between a particle and the obstacle 
            float minDist = obstacleRadius + this.particleRadius;
            float minDistSquare = minDist * minDist;

            //For collision with walls
            //First cell has width h
            float minX = h + this.particleRadius;
            float maxX = (this.fNumX - 1) * h - this.particleRadius;
            float minY = h + this.particleRadius;
            float maxY = (this.fNumY - 1) * h - this.particleRadius;

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
            float minDistSquare = minDist * minDist;

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

                                float dSquare = dx * dx + dy * dy;
                               
                                //If outside or exactly at the same position
                                if (dSquare > minDistSquare || dSquare == 0f)
                                {
                                    continue;
                                }

                                //The actual distance to the other particle
                                float d = Mathf.Sqrt(dSquare);
                                
                                //Push each particle half the distance needed to make them no longer collide
                                float s = 0.5f * (minDist - d) / d;
                                
                                dx *= s;
                                dy *= s;
                                
                                //Update their positions
                                this.particlePos[2 * i] -= dx;
                                this.particlePos[2 * i + 1] -= dy;

                                this.particlePos[2 * id] += dx;
                                this.particlePos[2 * id + 1] += dy;

                                //Diffuse colors of colliding particles
                                //r, g, b 
                                for (int k = 0; k < 3; k++)
                                {
                                    float color0 = this.particleColor[3 * i + k];
                                    float color1 = this.particleColor[3 * id + k];

                                    float color = (color0 + color1) * 0.5f;

                                    this.particleColor[3 * i + k] = color0 + (color - color0) * colorDiffusionCoeff;
                                    this.particleColor[3 * id + k] = color1 + (color - color1) * colorDiffusionCoeff;
                                }

                            }
                        }
                    }
                }
            }
        }


        private void UpdateParticleDensity()
        {
            int n = this.fNumY;
            
            float h = this.h;
            float one_over_h = this.fInvSpacing;
            float half_h = 0.5f * h;

            float[] d = particleDensity;

            //Reset
            System.Array.Fill(d, 0f);

            //For each particle
            for (int i = 0; i < this.numParticles; i++)
            {
                //Particle pos
                float x = this.particlePos[2 * i];
                float y = this.particlePos[2 * i + 1];

                //Make sure the particle is within the grid
                x = Mathf.Clamp(x, h, (this.fNumX - 1) * h);
                y = Mathf.Clamp(y, h, (this.fNumY - 1) * h);

                //The cells to interpolate between
                int x0 = Mathf.FloorToInt((x - half_h) * one_over_h);
                float tx = ((x - half_h) - x0 * h) * one_over_h;
                int x1 = Mathf.Min(x0 + 1, this.fNumX - 2);

                int y0 = Mathf.FloorToInt((y - half_h) * one_over_h);
                float ty = ((y - half_h) - y0 * h) * one_over_h;
                int y1 = Math.Min(y0 + 1, this.fNumY - 2);

                float sx = 1f - tx;
                float sy = 1f - ty;

                if (x0 < this.fNumX && y0 < this.fNumY) d[x0 * n + y0] += sx * sy;
                if (x1 < this.fNumX && y0 < this.fNumY) d[x1 * n + y0] += tx * sy;
                if (x1 < this.fNumX && y1 < this.fNumY) d[x1 * n + y1] += tx * ty;
                if (x0 < this.fNumX && y1 < this.fNumY) d[x0 * n + y1] += sx * ty;
            }

            if (this.particleRestDensity == 0f)
            {
                float sum = 0f;
                
                int numFluidCells = 0;

                for (int i = 0; i < this.fNumCells; i++)
                {
                    if (this.cellType[i] == FLUID_CELL)
                    {
                        sum += d[i];
                        numFluidCells++;
                    }
                }

                if (numFluidCells > 0)
                {
                    this.particleRestDensity = sum / numFluidCells;
                }
            }
        }



        //
        // Transfer velocities to the grid from particles or from the particles to the grid
        //

        private void TransferVelocities(bool toGrid, float flipRatio)
        {
            int n = this.fNumY;
            
            float h = this.h;
            float one_over_h = this.fInvSpacing;
            float half_h = 0.5f * h;

            if (toGrid)
            {
                //Fill previous velocities arrays before we update velocities
                //u -> uPrev
                System.Array.Copy(u, uPrev, u.Length);
                //v -> vPrev
                System.Array.Copy(v, vPrev, v.Length);

                //Original code which is u -> uPrev???
                //this.prevU.set(this.u);
                //this.prevV.set(this.v);

                //Here we reset u, so above should be correct
                System.Array.Fill(du, 0f);
                System.Array.Fill(dv, 0f);

                System.Array.Fill(u, 0f);
                System.Array.Fill(v, 0f);
                

                //Set cell types

                //First set all celltypes to solid or air depending on if a cell is an obstacle
                //So ignore water for now...
                //For each cell in the simulation
                for (int i = 0; i < this.fNumCells; i++)
                {
                    this.cellType[i] = this.s[i] == 0f ? SOLID_CELL : AIR_CELL;
                }
                    
                //Check if an air cell is filled with water
                //For each particle
                for (int i = 0; i < this.numParticles; i++)
                {
                    //Position of the particle
                    float x = this.particlePos[2 * i];
                    float y = this.particlePos[2 * i + 1];

                    //The cell the particle is in
                    int xi = Mathf.Clamp(Mathf.FloorToInt(x * one_over_h), 0, this.fNumX - 1);
                    int yi = Mathf.Clamp(Mathf.FloorToInt(y * one_over_h), 0, this.fNumY - 1);
                    
                    //2d to 1d
                    int cellNr = xi * n + yi;

                    //If the particle is in an air cell, then make it a fluid cell
                    if (this.cellType[cellNr] == AIR_CELL)
                    {
                        this.cellType[cellNr] = FLUID_CELL;
                    }
                }
            }

            //This loop will run twice
            //First we update u velocities, then we update v velocities 
            for (int component = 0; component < 2; component++)
            {
                //Staggered grid...
                float dx = component == 0 ? 0f : half_h;
                float dy = component == 0 ? half_h : 0f;

                //Do we modify u or v velocities?
                float[] f = component == 0 ? this.u : this.v;
                float[] prevF = component == 0 ? this.uPrev : this.vPrev;
                float[] d = component == 0 ? this.du : this.dv;

                //For each particle
                for (int i = 0; i < this.numParticles; i++)
                {
                    float x = this.particlePos[2 * i];
                    float y = this.particlePos[2 * i + 1];

                    //Make sure the position is within the grid
                    x = Mathf.Clamp(x, h, (this.fNumX - 1) * h);
                    y = Mathf.Clamp(y, h, (this.fNumY - 1) * h);

                    //Which cells are we interpolating from
                    int x0 = Mathf.Min(Mathf.FloorToInt((x - dx) * one_over_h), this.fNumX - 2);
                    float tx = ((x - dx) - x0 * h) * one_over_h;
                    int x1 = Mathf.Min(x0 + 1, this.fNumX - 2);

                    int y0 = Mathf.Min(Mathf.FloorToInt((y - dy) * one_over_h), this.fNumY - 2);
                    float ty = ((y - dy) - y0 * h) * one_over_h;
                    int y1 = Mathf.Min(y0 + 1, this.fNumY - 2);


                    float sx = 1f - tx;
                    float sy = 1f - ty;

                    float d0 = sx * sy;
                    float d1 = tx * sy;
                    float d2 = tx * ty;
                    float d3 = sx * ty;

                    //2d array to 1d array 
                    int nr0 = x0 * n + y0;
                    int nr1 = x1 * n + y0;
                    int nr2 = x1 * n + y1;
                    int nr3 = x0 * n + y1;

                    //Transfer this particle's velocity to the grid
                    if (toGrid)
                    {
                        float pv = this.particleVel[2 * i + component];

                        f[nr0] += pv * d0; d[nr0] += d0;
                        f[nr1] += pv * d1; d[nr1] += d1;
                        f[nr2] += pv * d2; d[nr2] += d2;
                        f[nr3] += pv * d3; d[nr3] += d3;
                    }
                    //Transfer velocities from the grid to the particle
                    else
                    {
                        int offset = component == 0 ? n : 1;
                        
                        float valid0 = this.cellType[nr0] != AIR_CELL || this.cellType[nr0 - offset] != AIR_CELL ? 1f : 0f;
                        float valid1 = this.cellType[nr1] != AIR_CELL || this.cellType[nr1 - offset] != AIR_CELL ? 1f : 0f;
                        float valid2 = this.cellType[nr2] != AIR_CELL || this.cellType[nr2 - offset] != AIR_CELL ? 1f : 0f;
                        float valid3 = this.cellType[nr3] != AIR_CELL || this.cellType[nr3 - offset] != AIR_CELL ? 1f : 0f;

                        float v = this.particleVel[2 * i + component];

                        //The source code calls this d, but we also say that d = du or dv array above
                        float this_d = valid0 * d0 + valid1 * d1 + valid2 * d2 + valid3 * d3;

                        if (this_d > 0f)
                        {
                            float picV = (valid0 * d0 * f[nr0] + valid1 * d1 * f[nr1] + valid2 * d2 * f[nr2] + valid3 * d3 * f[nr3]) / this_d;
                            
                            float corr = (valid0 * d0 * (f[nr0] - prevF[nr0]) + valid1 * d1 * (f[nr1] - prevF[nr1])
                                + valid2 * d2 * (f[nr2] - prevF[nr2]) + valid3 * d3 * (f[nr3] - prevF[nr3])) / this_d;
                            
                            float flipV = v + corr;

                            this.particleVel[2 * i + component] = (1f - flipRatio) * picV + flipRatio * flipV;
                        }
                    }
                }


                if (toGrid)
                {
                    for (int i = 0; i < f.Length; i++)
                    {
                        if (d[i] > 0f)
                        {
                            f[i] /= d[i];
                        }
                    }

                    //Restore solid cells
                    for (int i = 0; i < this.fNumX; i++)
                    {
                        for (int j = 0; j < this.fNumY; j++)
                        {
                            bool solid = this.cellType[i * n + j] == SOLID_CELL;
                            
                            if (solid || (i > 0 && this.cellType[(i - 1) * n + j] == SOLID_CELL))
                            {
                                this.u[i * n + j] = this.uPrev[i * n + j];
                            }
                                
                            if (solid || (j > 0 && this.cellType[i * n + j - 1] == SOLID_CELL))
                            {
                                this.v[i * n + j] = this.vPrev[i * n + j];
                            }
                        }
                    }
                }
            }
        }



        //
        // Make the fluid incompressible and calculate the pressure at the same time
        //

        private void SolveIncompressibility(int numIters, float dt, float overRelaxation, bool compensateDrift = true)
        {
            //Reset pressure 
            System.Array.Fill(p, 0f);

            //Fill previous velocities arrays before we update velocities
            //u -> uPrev
            System.Array.Copy(u, uPrev, u.Length);
            //v -> vPrev
            System.Array.Copy(v, vPrev, v.Length);

            //Original code which is u -> uPrev which is confirmed in TransferVelocities()
            //this.prevU.set(this.u);
            //this.prevV.set(this.v);


            int n = this.fNumY;

            float cp = this.density * this.h / dt;

            /*
            //What is this piece of code doing???
            for (var i = 0; i < this.numCells; i++)
            {
                float u = this.u[i];
                float v = this.v[i];
            }
            */

            //Make the fluid incompressible by looping the cells multiple times
            for (int iter = 0; iter < numIters; iter++)
            {
                //For each cell except the border
                for (int i = 1; i < this.fNumX - 1; i++)
                {
                    for (int j = 1; j < this.fNumY - 1; j++)
                    {
                        //If this cell is not a fluid, meaning its air or obbstacle
                        if (this.cellType[i * n + j] != FLUID_CELL)
                        {
                            continue;
                        }   
                            

                        int center = i * n + j;
                        int left = (i - 1) * n + j;
                        int right = (i + 1) * n + j;
                        int bottom = i * n + j - 1;
                        int top = i * n + j + 1;

                        //Cache how many of the surrounding cells are obstacles
                        float s = this.s[center];
                        float sx0 = this.s[left];
                        float sx1 = this.s[right];
                        float sy0 = this.s[bottom];
                        float sy1 = this.s[top];

                        float sTot = sx0 + sx1 + sy0 + sy1;
                        
                        if (s == 0f || sTot == 0f)
                        {
                            continue;
                        }

                        //Divergence = total amount of fluid velocity the leaves the cell 
                        float div = this.u[right] - this.u[center] + this.v[top] - this.v[center];

                        //???
                        if (this.particleRestDensity > 0f && compensateDrift)
                        {
                            float k = 1f;

                            float compression = this.particleDensity[i * n + j] - this.particleRestDensity;

                            if (compression > 0f)
                            {
                                div -= k * compression;
                            }
                        }

                        float p = -div / s;

                        p *= overRelaxation;

                        //Calculate pressure
                        this.p[center] += cp * p;

                        //Update velocities to ensure incompressibility
                        this.u[center] -= sx0 * p;
                        this.u[right] += sx1 * p;
                        this.v[center] -= sy0 * p;
                        this.v[top] += sy1 * p;
                    }
                }
            }
        }



        //
        // Coloring
        // 

        private void UpdateParticleColors()
        {
            float one_over_h = this.fInvSpacing;

            //For each particle
            for (int i = 0; i < this.numParticles; i++)
            {
                float s = 0.01f;

                this.particleColor[3 * i + 0] = Mathf.Clamp(this.particleColor[3 * i + 0] - s, 0f, 1f);
                this.particleColor[3 * i + 1] = Mathf.Clamp(this.particleColor[3 * i + 1] - s, 0f, 1f);
                this.particleColor[3 * i + 2] = Mathf.Clamp(this.particleColor[3 * i + 2] + s, 0f, 1f);

                //Particle pos
                float x = this.particlePos[2 * i + 0];
                float y = this.particlePos[2 * i + 1];

                //The cell the particle is in
                int xi = Mathf.Clamp(Mathf.FloorToInt(x * one_over_h), 1, this.fNumX - 1);
                int yi = Mathf.Clamp(Mathf.FloorToInt(y * one_over_h), 1, this.fNumY - 1);

                //2d to 1d array
                int cellNr = xi * this.fNumY + yi;

                float d0 = this.particleRestDensity;

                if (d0 > 0f)
                {
                    float relDensity = this.particleDensity[cellNr] / d0;
                    
                    if (relDensity < 0.7f)
                    {
                        //Theres another s abover so this is s2
                        float s2 = 0.8f;

                        this.particleColor[3 * i + 0] = s2;
                        this.particleColor[3 * i + 1] = s2;
                        this.particleColor[3 * i + 2] = 1f;
                    }
                }
            }
        }



        private void UpdateCellColors()
        {
            //Reset
            System.Array.Fill(this.cellColor, 0f);

            //For each cell
            for (int i = 0; i < this.fNumCells; i++)
            {
                //Solid
                if (this.cellType[i] == SOLID_CELL)
                {
                    //Gray
                    this.cellColor[3 * i + 0] = 0.5f;
                    this.cellColor[3 * i + 1] = 0.5f;
                    this.cellColor[3 * i + 2] = 0.5f;
                }
                //Fluid
                else if (this.cellType[i] == FLUID_CELL)
                {
                    float d = this.particleDensity[i];
                    
                    if (this.particleRestDensity > 0f)
                    {
                        d /= this.particleRestDensity;
                    }
                        
                    SetSciColor(i, d, 0f, 2f);
                }
                //Air
                //Becomes black because we reset colors to 0 at the start
            }
        }



        private void SetSciColor(int cellNr, float val, float minVal, float maxVal)
        {
            val = Mathf.Min(Mathf.Max(val, minVal), maxVal - 0.0001f);

            float d = maxVal - minVal;

            val = (d == 0f) ? 0.5f : (val - minVal) / d;
            
            float m = 0.25f;
            
            var num = Mathf.Floor(val / m);

            var s = (val - num * m) / m;

            float r = 0f;
            float g = 0f; 
            float b = 0f;

            switch (num)
            {
                case 0: r = 0.0f; g = s; b = 1.0f; break;
                case 1: r = 0.0f; g = 1.0f; b = 1.0f - s; break;
                case 2: r = s; g = 1.0f; b = 0.0f; break;
                case 3: r = 1.0f; g = 1.0f - s; b = 0.0f; break;
            }

            this.cellColor[3 * cellNr] = r;
            this.cellColor[3 * cellNr + 1] = g;
            this.cellColor[3 * cellNr + 2] = b;
        }
    }

}