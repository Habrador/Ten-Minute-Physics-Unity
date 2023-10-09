using System.Collections;
using System.Collections.Generic;
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

        private int numX;
        private int numZ;

        private float spacing;
        private int numCells;

        private float[] heights;
        private float[] bodyHeights;
        private float[] prevHeights;
        private float[] velocities;

        //Water mesh to show the water surface
        private Mesh waterMesh;
        private Vector3[] waterVertices;
        //private int[] waterTriangles;
        private Material waterMaterial;


        public WaterSurface(float sizeX, float sizeZ, float depth, float spacing, Material waterMaterial)
        {
            //Physics data
            this.waveSpeed = 2f;
            this.posDamping = 1f;
            this.velDamping = 0.3f;
            this.alpha = 0.5f;
            this.time = 0f;

            //Where is the +1 coming from? 
            //Are these vertices and not columns? 
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


            //Generate the visual mesh showing the water surface

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

                    float u = i / this.numX;
                    float v = j / this.numZ;

                    uvs[i * this.numZ + j] = new Vector2(u, v);
                }
            }

            //Triangles?
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
        }



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



        private void SimulateSurface(float dt)
        {
            this.waveSpeed = Mathf.Min(this.waveSpeed, 0.5f * this.spacing / dt);

            float c = this.waveSpeed * this.waveSpeed / this.spacing / this.spacing;

            float pd = Mathf.Min(this.posDamping * dt, 1f);
            float vd = Mathf.Max(0f, 1f - this.velDamping * dt);

            for (int i = 0; i < this.numX; i++)
            {
                for (int j = 0; j < this.numZ; j++)
                {
                    int id = i * this.numZ + j;

                    float h = this.heights[id];
                    
                    float sumH = 0f;

                    sumH += i > 0 ? this.heights[id - this.numZ] : h;
                    sumH += i < this.numX - 1 ? this.heights[id + this.numZ] : h;
                    sumH += j > 0 ? this.heights[id - 1] : h;
                    sumH += j < this.numZ - 1 ? this.heights[id + 1] : h;

                    this.velocities[id] += dt * c * (sumH - 4f * h);

                    //Positional damping
                    this.heights[id] += (0.25f * sumH - h) * pd;  
                }
            }

            for (int i = 0; i < this.numCells; i++)
            {
                //Velocity damping
                this.velocities[i] *= vd;       
                this.heights[i] += this.velocities[i] * dt;
            }
        }



        public void Simulate(float dt)
        {
            this.time += dt;

            SimulateCoupling(dt);

            SimulateSurface(dt);

            //We dont need to do this in FixedUpdate
            //UpdateVisMesh();
        }



        //Update water surface mesh
        public void UpdateVisMesh()
        {
            //Update the height
            for (int i = 0; i < this.numCells; i++)
            {
                waterVertices[i].y = this.heights[i];
            }

            waterMesh.SetVertices(waterVertices);

            waterMesh.RecalculateNormals();
            waterMesh.RecalculateBounds();

            Graphics.DrawMesh(waterMesh, Vector3.zero, Quaternion.identity, waterMaterial, 0);
        }



        public void SetVisible(bool visible)
        {
            //this.visMesh.visible = visible;
        }
    }
}