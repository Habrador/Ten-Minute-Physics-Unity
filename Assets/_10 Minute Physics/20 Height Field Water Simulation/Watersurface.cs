using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using UnityEngine;
using UnityEngine.UIElements;

namespace HeightFieldWaterSim
{
    public class WaterSurface
    {
        //The speed of the waves
        //To get a stable simulation we have to make sure waves don't travel further than one grid cell
        //So the speed is limited later in the simulation to make sure this is true
        private float waveSpeed;
        //Damping coefficients used in the water simulation to damp the pos and vel
        private float posDamping;
        private float velDamping;
        //Parameter that defines the intensity of what happens to the the water when objects interact with them [0, 1]
        private float alpha;

        //Number of vertices = water columns
        private int numX;
        private int numZ;
        //numX * numZ
        private int numCells;
        //The distance between each water columns
        private float spacing;

        //The height of the water columns in up direction
        private float[] heights;
        //The velocity of the water columns in up direction
        private float[] velocities;
        //The water columnheight covered by an object 
        private float[] bodyHeights;
        //Need previous because we are interested in the difference
        private float[] prevBodyHeights;

        //Water mesh to show the water surface
        private Mesh waterMesh;
        //These are in the mesh we initialize in the start
        //It should be faster to also have a separate array so we not each update first have to get the array, then update it, and then add it to the mesh?
        private Vector3[] waterVertices;
        private Material waterMaterial;



        public WaterSurface(float sizeX, float sizeZ, float depth, float spacing, Material waterMaterial)
        {
            //Physics data
            this.waveSpeed = 2f;
            this.posDamping = 1f;
            this.velDamping = 0.3f;
            this.alpha = 0.5f;

            //The water columns are not between 4 vertices, they are ON the vertices of the water mesh
            this.numX = Mathf.FloorToInt(sizeX / spacing) + 1;
            this.numZ = Mathf.FloorToInt(sizeZ / spacing) + 1;

            this.spacing = spacing;
            this.numCells = this.numX * this.numZ;

            this.heights = new float[this.numCells];
            this.bodyHeights = new float[this.numCells];
            this.prevBodyHeights = new float[this.numCells];
            this.velocities = new float[this.numCells];

            System.Array.Fill(this.heights, depth);
            System.Array.Fill(this.velocities, 0f);


            //
            // Generate the visual mesh showing the water surface
            //

            //Generate the mesh's vertices and uvs
            Vector3[] positions = new Vector3[this.numCells];
            Vector2[] uvs = new Vector2[this.numCells];

            //Center of the mesh
            int cx = Mathf.FloorToInt(this.numX / 2f);
            int cz = Mathf.FloorToInt(this.numZ / 2f);

            for (int i = 0; i < this.numX; i++)
            {
                for (int j = 0; j < this.numZ; j++)
                {
                    float posX = (i - cx) * spacing;
                    float posY = 0f;
                    float posZ = (j - cz) * spacing;

                    positions[i * this.numZ + j] = new Vector3(posX, posY, posZ);

                    float u = i / (float)this.numX;
                    float v = j / (float)this.numZ;

                    uvs[i * this.numZ + j] = new Vector2(u, v);
                }
            }


            //Build triangles from the vertices
            //If the grid is 3x3 cells (16 vertices) we need a total of 18 triangles
            //-> 18*3 = 54 triangle indices are needed
            //numX is vertices: (4-3)*(4-3)*2*3 = 54
            int[] index = new int[(this.numX - 1) * (this.numZ - 1) * 2 * 3];
            
            int pos = 0;
            
            for (int i = 0; i < this.numX - 1; i++)
            {
                for (int j = 0; j < this.numZ - 1; j++)
                {
                    int id0 = i * this.numZ + j;
                    int id1 = i * this.numZ + j + 1;
                    int id2 = (i + 1) * this.numZ + j + 1;
                    int id3 = (i + 1) * this.numZ + j;

                    index[pos++] = id0;
                    index[pos++] = id1;
                    index[pos++] = id2;

                    index[pos++] = id0;
                    index[pos++] = id2;
                    index[pos++] = id3;
                }
            }


            //Generate the mesh itself
            Mesh newMesh = new()
            {
                vertices = positions,
                triangles = index,
                uv = uvs
            };

            //To make it faster to update the mesh often
            newMesh.MarkDynamic();

            this.waterMesh = newMesh;
            this.waterVertices = positions;
            this.waterMaterial = waterMaterial;

            UpdateVisMesh();



            //Test that the water waves are working
            //for (int i = 0; i < 100; i++)
            //{
            //    heights[i + 40] = 0.2f;
            //}
        }



        //Water-ball interaction
        private void SimulateCoupling(float dt)
        {
            //Swap buffers from last update
            (this.prevBodyHeights, this.bodyHeights) = (this.bodyHeights, this.prevBodyHeights);
            
            //Reset
            System.Array.Fill(this.bodyHeights, 0f);

            //Center
            int cx = Mathf.FloorToInt(this.numX / 2f);
            int cz = Mathf.FloorToInt(this.numZ / 2f);
            
            float oneOverSpacing = 1f / this.spacing;
            float hSquare = this.spacing * this.spacing;

            //For each ball
            for (int i = 0; i < MyPhysicsScene.objects.Count; i++)
            {
                HFBall ball = MyPhysicsScene.objects[i];

                Vector3 ballPos = ball.pos;
                float ballRadius = ball.radius;

                //Find a bounding box to the ball so we dont have to loop thorugh all cells and do the radius test
                //The map is centered around origo and because the balls are in global space we need to convert their position to cell space
                //int cellX = Mathf.FloorToInt(pos.x / CELL_SIZE);
                //But this assume the map starts at (0,0)
                //Our map is centered around (0,0) so we have to shift it 
                int xMin = Mathf.Max(0, cx + Mathf.FloorToInt((ballPos.x - ballRadius) * oneOverSpacing));
                int xMax = Mathf.Min(this.numX - 1, cx + Mathf.FloorToInt((ballPos.x + ballRadius) * oneOverSpacing));
                int zMin = Mathf.Max(0, cz + Mathf.FloorToInt((ballPos.z - ballRadius) * oneOverSpacing));
                int zMax = Mathf.Min(this.numZ - 1, cz + Mathf.FloorToInt((ballPos.z + ballRadius) * oneOverSpacing));

                for (int xi = xMin; xi <= xMax; xi++)
                {
                    for (int zi = zMin; zi <= zMax; zi++)
                    {
                        //Convert from cell space to global space
                        float x = (xi - cx) * this.spacing;
                        float z = (zi - cz) * this.spacing;
                        
                        //Distance square from the cell to the center of the ball in 2d space
                        float r2 = (ballPos.x - x) * (ballPos.x - x) + (ballPos.z - z) * (ballPos.z - z);
                        
                        //If this distance square is less than the radius square, the cell is within the ball
                        if (r2 < ballRadius * ballRadius)
                        {
                            //Pythagoras to get the half-height of the ball at this cell. Is difficult to draw a simple picture to show how it works so get a pen and paper
                            //ballRadius * ballRadius is the hypotenuse
                            //This height is independent of the y-coordinate
                            float bodyHalfHeight = Mathf.Sqrt(ballRadius * ballRadius - r2);
                            
                            float waterHeight = this.heights[xi * this.numZ + zi];

                            //0 is ground
                            float bodyMin = Mathf.Max(ballPos.y - bodyHalfHeight, 0f);

                            //If the ball sticks out from the water we have to clamp it to waterHeight 
                            float bodyMax = Mathf.Min(ballPos.y + bodyHalfHeight, waterHeight);

                            //Ex. Entire ball outside of water
                            //ballPos.y = 500
                            //bodyHalfHeight = 10
                            //waterHeight = 20
                            //-> bodyMin = Max(500 - 10, 0) = 490
                            //-> bodyMax = Min(500 + 10, 20) = 20
                            //-> bodyHeight = Max (20 - 490, 0) = 0
                            float bodyHeight = Mathf.Max(bodyMax - bodyMin, 0f);
                            
                            //If this section of the ball is submerged in water
                            if (bodyHeight > 0f)
                            {
                                //Add buoyancy force to the ball
                                //The force acting on the object is proprtional to the water displaced by the object
                                //F = mg = rho_water * volume * g
                                float volume = bodyHeight * hSquare;

                                ball.ApplyForce(-volume * MyPhysicsScene.gravity.y, dt);
                                
                                this.bodyHeights[xi * this.numZ + zi] += bodyHeight;
                            }
                        }
                    }
                }
            }


            //Smooth the bodyHeights to prevent spikes and instabilities
            for (int iter = 0; iter < 2; iter++)
            {
                //For each cell
                for (int x = 0; x < this.numX; x++)
                {
                    for (int z = 0; z < this.numZ; z++)
                    {
                        //2d -> 1d array
                        int id = x * this.numZ + z;

                        //Smooth by taking the average of the surrounding cells
                        int num = x > 0 && x < this.numX - 1 ? 2 : 1;
                        
                        num += z > 0 && z < this.numZ - 1 ? 2 : 1;
                        
                        float avg = 0f;
                        
                        if (x > 0) avg += this.bodyHeights[id - this.numZ];
                        if (x < this.numX - 1) avg += this.bodyHeights[id + this.numZ];
                        if (z > 0) avg += this.bodyHeights[id - 1];
                        if (z < this.numZ - 1) avg += this.bodyHeights[id + 1];
                        
                        avg /= num;
                        
                        //Shouldnt we do this in another loop after this one because now the average will use the average...or maybe it makes no difference?
                        this.bodyHeights[id] = avg;
                    }
                }
            }


            //Update the water heights 
            //h = h + alpha * (b - b_prev)
            for (int i = 0; i < this.numCells; i++)
            {
                float bodyChange = this.bodyHeights[i] - this.prevBodyHeights[i];

                this.heights[i] += this.alpha * bodyChange;
            }
        }



        //Water surface simulation
        private void SimulateSurface(float dt)
        {
            //To get a stable simulation we have to make sure waves don't travel further than one grid cell
            this.waveSpeed = Mathf.Min(this.waveSpeed, 0.5f * this.spacing / dt);

            //The constant of proportionality from the wave equation
            //k = c^2 / s^2
            float k = (this.waveSpeed * this.waveSpeed) / (this.spacing * this.spacing);

            //Damping
            float pd = Mathf.Min(this.posDamping * dt, 1f);
            float vd = Mathf.Max(0f, 1f - this.velDamping * dt);


            //Loop 1. Calculate new velocities
            for (int i = 0; i < this.numX; i++)
            {
                for (int j = 0; j < this.numZ; j++)
                {
                    //2d to 1d array conversion
                    int id = i * this.numZ + j;

                    //The acceleration of a water column depends on the height of this water column and the 4 surrounding water columns
                    //a_i = k * (h_i-1 + h_i+1 - 2*h_i) <- 2d 

                    float h = this.heights[id];
                    
                    //Calculate the surrounding columns heights
                    float sumH = 0f;

                    //If we are on the border we say the surrounding height is the same as this column's height
                    //The will make the waves reflect against the wall
                    sumH += i > 0 ? this.heights[id - this.numZ] : h;
                    sumH += i < this.numX - 1 ? this.heights[id + this.numZ] : h;
                    sumH += j > 0 ? this.heights[id - 1] : h;
                    sumH += j < this.numZ - 1 ? this.heights[id + 1] : h;

                    //k = c in the source code, but he uses k in the video
                    float acc = k * (sumH - 4f * h);

                    this.velocities[id] += dt * acc;

                    //We calculate new height based on velocity later

                    //Positional damping
                    //This should be done after the loop is finished?
                    this.heights[id] += (0.25f * sumH - h) * pd;  
                }
            }


            //Loop 2. Calculate new heights
            for (int i = 0; i < this.numCells; i++)
            {
                //Velocity damping
                this.velocities[i] *= vd;
                
                //Calculate new height based on the velocity
                this.heights[i] += this.velocities[i] * dt;
            }
        }



        //Is called from FixedUpdate
        public void Simulate(float dt)
        {
            SimulateCoupling(dt);

            SimulateSurface(dt);

            //We dont need to do this in FixedUpdate
            //UpdateVisMesh();
        }



        //Update water surface mesh
        //Is called from Update
        public void UpdateVisMesh()
        {
            //Update the height
            for (int i = 0; i < this.numCells; i++)
            {
                waterVertices[i].y = this.heights[i];
            }

            //Update the mesh
            waterMesh.SetVertices(waterVertices);

            waterMesh.RecalculateNormals();
            waterMesh.RecalculateBounds();

            //Display the mesh
            Graphics.DrawMesh(waterMesh, Vector3.zero, Quaternion.identity, waterMaterial, 0);
        }
    }
}