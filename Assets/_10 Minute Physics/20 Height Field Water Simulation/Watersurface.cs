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
        private float waveSpeed;
        private float posDamping;
        private float velDamping;
        private float alpha;
        private float time;

        //Number of vertices = water columns
        private int numX;
        private int numZ;
        //numX * numZ
        private int numCells;
        //The distance between each water columns
        private float spacing;
        
        //The height of a water columns
        private float[] heights;
        private float[] prevHeights;
        //The velocity of a water column
        private float[] velocities;
        
        private float[] bodyHeights;
        
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
            this.time = 0f;

            //The water columns are not between 4 vertices, they are ON the vertices of the water mesh
            this.numX = Mathf.FloorToInt(sizeX / spacing) + 1;
            this.numZ = Mathf.FloorToInt(sizeZ / spacing) + 1;

            this.spacing = spacing;
            this.numCells = this.numX * this.numZ;

            this.heights = new float[this.numCells];
            this.bodyHeights = new float[this.numCells];
            this.prevHeights = new float[this.numCells];
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
            //Center
            int cx = Mathf.FloorToInt(this.numX / 2f);
            int cz = Mathf.FloorToInt(this.numZ / 2f);
            
            float h1 = 1f / this.spacing;

            //Swap buffers
            (this.prevHeights, this.bodyHeights) = (this.bodyHeights, this.prevHeights);
            //Reset
            System.Array.Fill(this.bodyHeights, 0f);

            for (int i = 0; i < MyPhysicsScene.objects.Count; i++)
            {
                HFBall ball = MyPhysicsScene.objects[i];
                Vector3 pos = ball.pos;
                float br = ball.radius;
                float h2 = this.spacing * this.spacing;

                int x0 = Mathf.Max(0, cx + Mathf.FloorToInt((pos.x - br) * h1));
                int x1 = Mathf.Min(this.numX - 1, cx + Mathf.FloorToInt((pos.x + br) * h1));
                int z0 = Mathf.Max(0, cz + Mathf.FloorToInt((pos.z - br) * h1));
                int z1 = Mathf.Min(this.numZ - 1, cz + Mathf.FloorToInt((pos.z + br) * h1));

                for (int xi = x0; xi <= x1; xi++)
                {
                    for (int zi = z0; zi <= z1; zi++)
                    {
                        float x = (xi - cx) * this.spacing;
                        float z = (zi - cz) * this.spacing;
                        
                        float r2 = (pos.x - x) * (pos.x - x) + (pos.z - z) * (pos.z - z);
                        
                        if (r2 < br * br)
                        {
                            float bodyHalfHeight = Mathf.Sqrt(br * br - r2);
                            
                            float waterHeight = this.heights[xi * this.numZ + zi];

                            float bodyMin = Mathf.Max(pos.y - bodyHalfHeight, 0f);
                            float bodyMax = Mathf.Min(pos.y + bodyHalfHeight, waterHeight);
                            
                            float bodyHeight = Mathf.Max(bodyMax - bodyMin, 0f);
                            
                            if (bodyHeight > 0f)
                            {
                                ball.ApplyForce(-bodyHeight * h2 * MyPhysicsScene.gravity.y, dt);
                                
                                this.bodyHeights[xi * this.numZ + zi] += bodyHeight;
                            }
                        }
                    }
                }
            }


            for (int iter = 0; iter < 2; iter++)
            {
                for (int xi = 0; xi < this.numX; xi++)
                {
                    for (int zi = 0; zi < this.numZ; zi++)
                    {
                        int id = xi * this.numZ + zi;

                        int num = xi > 0 && xi < this.numX - 1 ? 2 : 1;
                        
                        num += zi > 0 && zi < this.numZ - 1 ? 2 : 1;
                        
                        float avg = 0f;
                        
                        if (xi > 0) avg += this.bodyHeights[id - this.numZ];
                        if (xi < this.numX - 1) avg += this.bodyHeights[id + this.numZ];
                        if (zi > 0) avg += this.bodyHeights[id - 1];
                        if (zi < this.numZ - 1) avg += this.bodyHeights[id + 1];
                        
                        avg /= num;
                        
                        this.bodyHeights[id] = avg;
                    }
                }
            }

            for (int i = 0; i < this.numCells; i++)
            {
                float bodyChange = this.bodyHeights[i] - this.prevHeights[i];
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

                    float h = this.heights[id];
                    
                    //Calculate the surrounding columns heights
                    float sumH = 0f;

                    //If we are on the border we say the surrounding height is the same as this column's height
                    //The will make the waves reflect against the wall
                    sumH += i > 0 ? this.heights[id - this.numZ] : h;
                    sumH += i < this.numX - 1 ? this.heights[id + this.numZ] : h;
                    sumH += j > 0 ? this.heights[id - 1] : h;
                    sumH += j < this.numZ - 1 ? this.heights[id + 1] : h;

                    //The acceleration of a water column depends on the height of this water column and the 4 surrounding water columns
                    //a_i = k * (h_i-1 + h_i+1 - 2*h_i) <- 2d 

                    //k = c in the source code, but he uses k in the video
                    float acc = k * (sumH - 4f * h);

                    this.velocities[id] += dt * acc;

                    //We calculate new height based on velocity later

                    //Positional damping
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
            this.time += dt;

            //SimulateCoupling(dt);

            SimulateSurface(dt);

            //We dont need to do this in FixedUpdate
            //UpdateVisMesh();

            //Debug.Log("Hello");
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



        //public void SetVisible(bool visible)
        //{
        //    //this.visMesh.visible = visible;
        //}
    }
}