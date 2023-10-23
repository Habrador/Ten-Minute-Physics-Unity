using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FLIPFluidSimulator
{
    //This one is using a very similar data structure as the fluid an is thius a great source of confusion, so should be in its own class
    public class PushParticlesApart
    {
        private readonly float particleRadius;
        //To save computations: invSpacing = 1.0 / (2.2 * particleRadius);
        //We use it because we might use some other cell structure to handle particle-particle collision more efficient
        //To handle particle-particle collision we check the cell the particle is in and surrounding cells. To make sure that works, we may use another spacing... 
        private readonly float invSpacing;
        //These may differ from the fluid simulation's 
        private readonly int numX;
        private readonly int numY;
        private readonly int numCells;
        //For particle-particle collision
        //How many particles are in each cell?
        private readonly int[] numCellParticles;
        //Partial sums, store the cummulative number of particles in each cell
        private readonly int[] firstCellParticle;
        private readonly int[] cellParticleIds;

        //Convert between 2d and 1d array
        public int To1D(int i, int j) => i + (numX * j);



        public PushParticlesApart(float particleRadius, float simWidth, float simHeight, int maxParticles)
        {
            this.particleRadius = particleRadius;
        
            //To optimize particle-particle collision
            //Which is why we need another grid than the fluid grid
            this.invSpacing = 1f / (2.2f * particleRadius);

            this.numX = Mathf.FloorToInt(simWidth * this.invSpacing) + 1;
            this.numY = Mathf.FloorToInt(simHeight * this.invSpacing) + 1;

            this.numCells = this.numX * this.numY;

            this.numCellParticles = new int[this.numCells];
            //Should be one bigger to be on the safe side
            this.firstCellParticle = new int[this.numCells + 1];
            this.cellParticleIds = new int[maxParticles];
        }



        //Handle particle-particle collision
        //Same idea as in tutorial 11 "Finding overlaps among thousands of objects blazing fast"
        public void Push(int numIters, int numParticles, float[] particlePos, float[] particleColor)
        {
            //Count particles per cell
            System.Array.Fill(this.numCellParticles, 0);

            //For each particle
            for (int i = 0; i < numParticles; i++)
            {
                float x = particlePos[2 * i];
                float y = particlePos[2 * i + 1];

                //Which cell is this particle in?  
                int xi = Mathf.Clamp(Mathf.FloorToInt(x * this.invSpacing), 0, this.numX - 1);
                int yi = Mathf.Clamp(Mathf.FloorToInt(y * this.invSpacing), 0, this.numY - 1);

                //2d array to 1d
                int cellNr = xi * this.numY + yi;
                //int cellNr = To1D()

                //Add 1 to this cell because a particle is in it
                this.numCellParticles[cellNr]++;
            }


            //Partial sums
            int first = 0;

            //For each cell
            for (int i = 0; i < this.numCells; i++)
            {
                first += this.numCellParticles[i];

                this.firstCellParticle[i] = first;
            }

            //Guard
            this.firstCellParticle[this.numCells] = first;


            //Fill particles into cells
            for (int i = 0; i < numParticles; i++)
            {
                float x = particlePos[2 * i];
                float y = particlePos[2 * i + 1];

                //Which cell is this particle in?  
                int xi = Mathf.Clamp(Mathf.FloorToInt(x * this.invSpacing), 0, this.numX - 1);
                int yi = Mathf.Clamp(Mathf.FloorToInt(y * this.invSpacing), 0, this.numY - 1);

                //2d array to 1d
                int cellNr = xi * this.numY + yi;

                this.firstCellParticle[cellNr]--;

                this.cellParticleIds[this.firstCellParticle[cellNr]] = i;
            }


            //Push particles apart

            //The more iterations the fewer the particles are still colliding with each other
            for (int iter = 0; iter < numIters; iter++)
            {
                //For each particle
                for (int i = 0; i < numParticles; i++)
                {
                    MoveParticleApart(i, particlePos, particleColor);
                }
            }
        }



        //Move a single particle apart from the particle it is colliding with
        //The colliding particles will also move
        private void MoveParticleApart(int particleIndex, float[] particlePos, float[] particleColor)
        {
            float px = particlePos[2 * particleIndex];
            float py = particlePos[2 * particleIndex + 1];

            //Which cell is this particle in?
            int pxi = Mathf.FloorToInt(px * this.invSpacing);
            int pyi = Mathf.FloorToInt(py * this.invSpacing);

            //Check this cell and surrounding cells for particles that can collide
            int x0 = Mathf.Max(pxi - 1, 0);
            int y0 = Mathf.Max(pyi - 1, 0);

            int x1 = Mathf.Min(pxi + 1, this.numX - 1);
            int y1 = Mathf.Min(pyi + 1, this.numY - 1);

            for (int xi = x0; xi <= x1; xi++)
            {
                for (int yi = y0; yi <= y1; yi++)
                {
                    //2d array to 1d 
                    int cellNr = xi * this.numY + yi;

                    int firstIndex = this.firstCellParticle[cellNr];
                    int lastIndex = this.firstCellParticle[cellNr + 1];

                    for (int j = firstIndex; j < lastIndex; j++)
                    {
                        int particleIndexOther = this.cellParticleIds[j];

                        //Dont check the particle itself
                        if (particleIndexOther == particleIndex)
                        {
                            continue;
                        }

                        //The pos of the other particle we want to check collision against
                        float qx = particlePos[2 * particleIndexOther];
                        float qy = particlePos[2 * particleIndexOther + 1];

                        //The distance square to this other particle
                        float dx = qx - px;
                        float dy = qy - py;

                        float dSquare = dx * dx + dy * dy;

                        //The min distance for the particle not to collide
                        float minDist = 2f * particleRadius;

                        float minDistSquare = minDist * minDist;

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
                        particlePos[2 * particleIndex] -= dx;
                        particlePos[2 * particleIndex + 1] -= dy;

                        particlePos[2 * particleIndexOther] += dx;
                        particlePos[2 * particleIndexOther + 1] += dy;


                        //Update particle colors
                        UpdateColor(particleColor, particleIndex, particleIndexOther);
                    }
                }
            }
        }



        private void UpdateColor(float[] particleColor, int particle_index, int particle_index_other)
        {
            //Diffuse colors of colliding particles
            float colorDiffusionCoeff = 0.001f;

            //r, g, b 
            for (int k = 0; k < 3; k++)
            {
                float color0 = particleColor[3 * particle_index + k];
                float color1 = particleColor[3 * particle_index_other + k];

                float color = (color0 + color1) * 0.5f;

                particleColor[3 * particle_index + k] = color0 + (color - color0) * colorDiffusionCoeff;
                particleColor[3 * particle_index_other + k] = color1 + (color - color1) * colorDiffusionCoeff;
            }
        }
    }
}