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
        private readonly int numX;
        private readonly int numY;
        private readonly int numCells;
        //Cell height and width
        private readonly float h;
        //1/h
        private readonly float one_over_h;

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
        public readonly int FLUID_CELL = 0;
        public readonly int AIR_CELL = 1;
        public readonly int SOLID_CELL = 2;

        public readonly int[] cellType;

        //Particles
        //How many particles?
        public int numParticles;
        //How many particles allowed?
        private int maxParticles;
        //The pos of each particle (x,y) after each other
        public readonly float[] particlePos;
        //The color of each particle (r,g,b) after each other. Color values are in the range [0,1]
        //We have to update this color array in FixedUpdate because we do it when we push particles apart
        public readonly float[] particleColor;
        //The vel of each particle (x,y) after each other
        private readonly float[] particleVel;
        //The density of particles in a cell. Its not 1 particle in cell = +1 density. It depends on how close a particle is to the center if a cell
        public readonly float[] particleDensity;
        //The average particle density before the simulation starts
        public float particleRestDensity;
        //Particle radius
        public readonly float particleRadius;
        //To push particles apart so they are not intersecting
        private readonly PushParticlesApart pushParticlesApart;


        //Convert between 2d and 1d array
        //The conversion can cause great confusion, so we better do it in one place throughout all code
        //Was (i * numY) + j in tutorial but should be i + (numX * j) if we want them row-by-row after each other in the flat array
        //Otherwise we get them column by column which is maybe how js prefers them when displaying???
        //https://softwareengineering.stackexchange.com/questions/212808/treating-a-1d-data-structure-as-2d-grid
        public int To1D(int xi, int yi) => xi + (numX * yi);

        //Is a coordinate in local space within the simulation area?
        public bool IsWithinArea(float x, float y) => (x > 0 && x < SimWidth && y > 0 && y < SimHeight);


        //Getters

        //These are not the same as the height we set at start because of the two border cells
        public float SimWidth => numX * h;
        public float SimHeight => numY * h;

        public int NumX => numX;
        public int NumY => numY;

        public float Spacing => h;

       

        public FLIPFluidSim(float density, int numX, int numY, float h, float particleRadius, int maxParticles)
        {
            this.density = density;

            //Add 2 extra cells because we need a border, or are we adding two u's on each side???
            //Because we use a staggered grid, then there will be no u on the right side of the cells in the last column if we add new cells... The p's are in the middle and of the same size, so we add two new cells while ignoring there's no u's on the right side of the last column. The book "Fluid Simulation for Computer Graphics" says that the velocity arrays should be one larger than the pressure array because we have 1 extra velocity on the right side of the last column. 
            //He says border cells in the video
            this.numX = numX + 2;
            this.numY = numY + 2;

            //Cellspacing
            this.h = h;
            this.one_over_h = 1f / this.h;

            int numCells = this.numX * this.numY;

            this.numCells = numCells;

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


            //Particles
            this.maxParticles = maxParticles;

            this.particlePos = new float[2 * this.maxParticles];
            this.particleVel = new float[2 * this.maxParticles];

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
                
            this.particleDensity = new float[numCells];
            this.particleRestDensity = 0f;

            this.particleRadius = particleRadius;

            this.pushParticlesApart = new(particleRadius, SimWidth, SimHeight, maxParticles);

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
            //Update particle vel and pos by adding gravity
            IntegrateParticles(dt, gravity);

            if (separateParticles)
            {
                //Handle particle-particle collisions
                //PushParticlesApart(numParticleIters);
                pushParticlesApart.Push(numParticleIters, this.numParticles, this.particlePos, this.particleColor);
            }

            //Handle particle-world collisions
            HandleParticleCollisions(obstacleX, obstacleY, obstacleRadius, obstacleVelX, obstacleVelY);

            //Velocity transfer: Particles -> Grid
            //TransferVelocities(true);

            //Update the density of particles in a cell and calculate rest density at the start of the simulation
            //UpdateParticleDensity();

            //Make the grid velocities incompressible
            //SolveIncompressibility(numPressureIters, dt, overRelaxation, compensateDrift);

            //Velocity transfer: Grid -> Particles
            //TransferVelocities(false, flipRatio);
        }



        //
        // Simulate particles
        //

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
                this.particlePos[2 * i    ] += this.particleVel[2 * i    ] * dt;
                //y dir
                this.particlePos[2 * i + 1] += this.particleVel[2 * i + 1] * dt;
            }
        }



        //
        // Handle particle-world collisions
        //

        private void HandleParticleCollisions(float obstacleX, float obstacleY, float obstacleRadius, float obstacleVelX, float obstacleVelY)
        {
            //Why are we using 1f / this.fInvSpacing and not just h?
            float h = 1f / this.one_over_h;

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


        
        //
        // Update the density of particles
        //

        //The density is defined at the center of each cell
        private void UpdateParticleDensity()
        {   
            float h = this.h;
            float one_over_h = this.one_over_h;
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
                //Density is defined at the center of each cell
                //If the particle is to the left of center in the first cell we push it to be on the border between the first cell and second cell  
                x = Mathf.Clamp(x, h, (this.numX - 1) * h);
                y = Mathf.Clamp(y, h, (this.numY - 1) * h);

                //The cells to interpolate between
                //We have already made sure the particle is at least on the border between the first and second cell
                //If the particle is left of the second p, we have to subtract 0.5 from its position to get the index of the left cell to interpolate from  
                //+-----+-----+
                //|     |     |
                //|  p  |  p  |
                //|     |     |
                //+-----+-----+
                int x0 = Mathf.FloorToInt((x - half_h) * one_over_h);
                int x1 = Mathf.Min(x0 + 1, this.numX - 2);         

                int y0 = Mathf.FloorToInt((y - half_h) * one_over_h);
                int y1 = Math.Min(y0 + 1, this.numY - 2);

                //t is a parameter in the range [0, 1]. If tx = 0 we get A or if tx = 1 we get B if we interpolate between A and B where A has coordinate x0 and B has coordinate x1 -> x1-x0 = h
                //tx = (xp - x0) / (x1 - x0) = (xp - x0) / h = deltaX / h
                //Original code said (x - half_h) - x0 * h but its confusing why you should subtract half_h from x because we want the coordinate of x0
                float deltaX = x - ((x0 * h) + half_h);
                float deltaY = y - ((y0 * h) + half_h);

                float tx = deltaX * one_over_h;
                float ty = deltaY * one_over_h;

                //From FluidSim class we know how to interpolate between A,B,C,D 
                // C-----D
                // |     |
                // |___P |
                // |   | |
                // A-----B
                //P = (1 - tx) * (1 - ty) * A + tx * (1 - ty) * B + (1 - tx) * ty * C + tx * ty * D

                //To simplify:
                float sx = 1f - tx;
                float sy = 1f - ty;

                //We get: P = sx * sy * A + tx * sy * B + sx * ty * C + tx * ty * D 
                //The weighted density of a particle in each cell becomes:
                if (x0 < this.numX && y0 < this.numY) d[To1D(x0, y0)] += sx * sy; //A
                if (x1 < this.numX && y0 < this.numY) d[To1D(x1, y0)] += tx * sy; //B
                if (x0 < this.numX && y1 < this.numY) d[To1D(x0, y1)] += sx * ty; //C
                if (x1 < this.numX && y1 < this.numY) d[To1D(x1, y1)] += tx * ty; //D 
            }


            //Calculate the average density of water cells before the simulation starts
            //We set particleRestDensity = 0 in the start so this will update once  
            if (this.particleRestDensity == 0f)
            {
                float sum = 0f;
                
                int numFluidCells = 0;

                //Calculate the total fluid density and how many cells have fluids in them 
                for (int i = 0; i < this.numCells; i++)
                {
                    if (this.cellType[i] == FLUID_CELL)
                    {
                        sum += d[i];
                        numFluidCells += 1;
                    }
                }

                if (numFluidCells > 0)
                {
                    //The average density
                    this.particleRestDensity = sum / numFluidCells;
                }
            }
        }



        //
        // Transfer velocities to the grid from particles or from the particles to the grid
        //

        private void TransferVelocities(bool toGrid, float flipRatio = 0.8f)
        {
            int n = this.numY;
            
            float h = this.h;
            float one_over_h = this.one_over_h;
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
                for (int i = 0; i < this.numCells; i++)
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
                    int xi = Mathf.Clamp(Mathf.FloorToInt(x * one_over_h), 0, this.numX - 1);
                    int yi = Mathf.Clamp(Mathf.FloorToInt(y * one_over_h), 0, this.numY - 1);
                    
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
                    x = Mathf.Clamp(x, h, (this.numX - 1) * h);
                    y = Mathf.Clamp(y, h, (this.numY - 1) * h);

                    //Which cells are we interpolating from
                    int x0 = Mathf.Min(Mathf.FloorToInt((x - dx) * one_over_h), this.numX - 2);
                    float tx = ((x - dx) - x0 * h) * one_over_h;
                    int x1 = Mathf.Min(x0 + 1, this.numX - 2);

                    int y0 = Mathf.Min(Mathf.FloorToInt((y - dy) * one_over_h), this.numY - 2);
                    float ty = ((y - dy) - y0 * h) * one_over_h;
                    int y1 = Mathf.Min(y0 + 1, this.numY - 2);


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
                    for (int i = 0; i < this.numX; i++)
                    {
                        for (int j = 0; j < this.numY; j++)
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
        // Make the fluid incompressible and calculate the pressure at the same time. Also compensate for drift 
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


            //int n = this.numY;

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
                for (int i = 1; i < this.numX - 1; i++)
                {
                    for (int j = 1; j < this.numY - 1; j++)
                    {
                        //If this cell is not a fluid, meaning its air or obstacle
                        if (this.cellType[To1D(i, j)] != FLUID_CELL)
                        {
                            continue;
                        }   
                            
                        int center = To1D(i, j);
                        int left = To1D(i - 1, j);
                        int right = To1D(i + 1, j);
                        int bottom = To1D(i, j - 1);
                        int top = To1D(i, j + 1);

                        //Cache how many of the surrounding cells are obstacles
                        //float s = this.s[center];
                        float sLeft = this.s[left];
                        float sRight = this.s[right];
                        float sBottom = this.s[bottom];
                        float sTop = this.s[top];

                        float sTot = sLeft + sRight + sBottom + sTop;
                        
                        //Continue if all surrounding cells are obstacles
                        //We have already made sure this cell is a fluid cell
                        if (sTot == 0f)
                        {
                            continue;
                        }

                        //Divergence = total amount of fluid velocity the leaves the cell 
                        float divergence = this.u[right] - this.u[center] + this.v[top] - this.v[center];

                        //Reduce divergence in dense regions to compensare for drift
                        //This will cause more outward push
                        if (this.particleRestDensity > 0f && compensateDrift)
                        {
                            //PSstiffness coefficient parameter
                            float k = 1f;

                            float compression = this.particleDensity[To1D(i, j)] - this.particleRestDensity;

                            if (compression > 0f)
                            {
                                divergence -= k * compression;
                            }
                        }

                        float divergence_Over_sTot = -divergence / sTot;

                        //Multiply by the overrelaxation coefficient to speed up the convergence of Gauss-Seidel relaxation
                        divergence_Over_sTot *= overRelaxation;

                        //Calculate pressure
                        this.p[center] += cp * divergence_Over_sTot;

                        //Update velocities to ensure incompressibility
                        this.u[center] -= sLeft * divergence_Over_sTot;
                        this.u[right] += sRight * divergence_Over_sTot;
                        this.v[center] -= sBottom * divergence_Over_sTot;
                        this.v[top] += sTop * divergence_Over_sTot;
                    }
                }
            }
        }
    }
}