using EulerianFluidSimulator;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using static UnityEngine.ParticleSystem;


namespace FLIPFluidSimulator
{
    public class FLIPFluidDisplay
    {
        //The circle we can move around with mouse
        private Mesh circleMesh;

        //Grid
        private Mesh gridMesh;

        //z offsets so stuff doesnt intersect
        //Plane is at 0
        private readonly float obstacleOffset = -0.1f;
        private readonly float gridOffset = -0.07f;
        private readonly float particlesPlaneOffset = -0.05f;

        private DisplayParticlesAsShader displayParticlesAsShader;



        //
        // Show the fluid simulation data on a texture
        //
        public void UpdateTexture(FLIPFluidScene scene)
        {
            FLIPFluidSim f = scene.fluid;

            Texture2D fluidTexture = scene.fluidTexture;

            //Generate a new texture if none exists or if we have changed resolution
            if (fluidTexture == null || fluidTexture.width != f.NumX || fluidTexture.height != f.NumY)
            {
                fluidTexture = new(f.NumX, f.NumY);

                //Dont blend the pixels
                fluidTexture.filterMode = FilterMode.Point;

                //Blend the pixels 
                //fluidTexture.filterMode = FilterMode.Bilinear;

                //Don't wrap the border with the border on the opposite side of the texture
                fluidTexture.wrapMode = TextureWrapMode.Clamp;

                scene.fluidMaterial.mainTexture = fluidTexture;

                scene.fluidTexture = fluidTexture;
            }


            //The texture colors
            Color32[] textureColors = new Color32[f.NumX * f.NumY];

            //Find the color in each cell
            for (int x = 0; x < f.NumX; x++)
            {
                for (int y = 0; y < f.NumY; y++)
                {
                    //Start with white
                    Vector4 color = new(255, 255, 255, 255);

                    int index = f.To1D(x, y);

                    if (scene.showGrid)
                    {
                        //Solid
                        if (f.IsSolid(index))
                        {
                            //Gray
                            color[0] = 0.5f;
                            color[1] = 0.5f;
                            color[2] = 0.5f;
                        }
                        //Fluid
                        else if (f.IsFluid(index))
                        {
                            float d = f.particleDensity[index];

                            if (f.particleRestDensity > 0f)
                            {
                                //Current density divided by average density
                                d /= f.particleRestDensity;
                            }

                            //Should make high density areas green and low density areas light-blue
                            color = UsefulMethods.GetSciColor(d, 0f, 2f);
                        }
                        //Air
                        //Becomes black because we reset colors to 0 at the start
                    }
                    //If we dont want to show the grid, then make everything black
                    else
                    {
                        color[0] = 0;
                        color[1] = 0;
                        color[2] = 0;
                    }

                    //Add the color to this pixel
                    //Color32 is 0-255
                    byte r = (byte)color[0];
                    byte g = (byte)color[1];
                    byte b = (byte)color[2];
                    byte a = (byte)color[3];

                    Color32 pixelColor = new(r, g, b, a);

                    textureColors[f.To1D(x, y)] = pixelColor;
                }
            }

            //Add all colors to the texture
            fluidTexture.SetPixels32(textureColors);

            //Copies changes you've made in a CPU texture to the GPU
            fluidTexture.Apply(false);
        }



        //
        // Display the circle obstacle
        //
        public void ShowInteractiveCircleObstacle(FLIPFluidScene scene)
        {
            FLIPFluidSim f = scene.fluid;

            //Make it slightly bigger to hide the jagged edges we get because we use a grid with square cells which will not match the circle edges prefectly
            float circleRadius = scene.obstacleRadius + f.Spacing;

            //Circle center in global space
            Vector2 globalCenter2D = scene.SimToWorld(new(scene.obstacleX, scene.obstacleY));

            //3d space infront of the texture
            Vector3 circleCenter = new(globalCenter2D.x, globalCenter2D.y, obstacleOffset);

            //Generate a new circle mesh if we havent done so
            if (circleMesh == null)
            {
                circleMesh = DisplayShapes.GenerateCircleMesh_XY(Vector3.zero, circleRadius, 50);
            }

            //Display the circle mesh
            Graphics.DrawMesh(circleMesh, circleCenter, Quaternion.identity, scene.obstacleMaterial, 0, Camera.main, 0);
        }



        //
        // Display the fluid particles
        //

        //Update particle colors
        //We also update them in the simulation - if they collide the color of each particle is diffused
        //In particleColor array each particle has its color (r,g,b) after each other
        //(r,g,b) are in the range [0,1]
        private void UpdateParticleColors(FLIPFluidScene scene)
        {
            FLIPFluidSim f = scene.fluid;

            float one_over_h = 1f / f.Spacing;

            //For each particle
            for (int i = 0; i < f.numParticles; i++)
            {
                float s = 0.01f;

                //Make the color more blue by decreasing red and green while increasing the blue
                f.particleColor[3 * i + 0] = Mathf.Clamp(f.particleColor[3 * i + 0] - s, 0f, 1f);
                f.particleColor[3 * i + 1] = Mathf.Clamp(f.particleColor[3 * i + 1] - s, 0f, 1f);
                f.particleColor[3 * i + 2] = Mathf.Clamp(f.particleColor[3 * i + 2] + s, 0f, 1f);

                //Particle pos
                float x = f.particlePos[2 * i + 0];
                float y = f.particlePos[2 * i + 1];

                //The cell the particle is in
                int xi = Mathf.Clamp(Mathf.FloorToInt(x * one_over_h), 1, f.NumX - 1);
                int yi = Mathf.Clamp(Mathf.FloorToInt(y * one_over_h), 1, f.NumY - 1);

                //2d to 1d array
                int cellNr = f.To1D(xi, yi);

                //The average particle density before the simulation starts
                float d0 = f.particleRestDensity;

                if (d0 > 0f)
                {
                    //Current desity in this cell in relation to average density
                    float relDensity = f.particleDensity[cellNr] / d0;

                    if (relDensity < 0.7f)
                    {
                        //Theres another s above so this is s2
                        float s2 = 0.8f;

                        //Make the particle light blue in low density areas
                        f.particleColor[3 * i + 0] = s2;
                        f.particleColor[3 * i + 1] = s2;
                        f.particleColor[3 * i + 2] = 1f;
                    }
                }
            }
        }



        //Display the individual particles
        public void ShowParticles(FLIPFluidScene scene, GameObject particlesPlane, Material particlesMaterial)
        {
            //Init
            if (displayParticlesAsShader == null)
            {
                displayParticlesAsShader = new DisplayParticlesAsShader(particlesPlane, particlesMaterial);

                //Move the plane to correct z offset 
                Vector3 planePos = particlesPlane.transform.position;

                particlesPlane.transform.position = new(planePos.x, planePos.y, particlesPlaneOffset);
            }

            //First update their colors
            //UpdateParticleColors(scene);

            displayParticlesAsShader.UpdateParticles(scene);
        }



        //
        // Display a grid to show each cell
        //

        public void DisplayGrid(FLIPFluidScene scene)
        {
            if (gridMesh == null)
            {
                gridMesh = InitGridMesh(scene);
            }

            Material gridMat = DisplayShapes.GetMaterial(DisplayShapes.ColorOptions.Red);

            Graphics.DrawMesh(gridMesh, Vector3.zero, Quaternion.identity, gridMat, 0, Camera.main, 0);
        }


        //Generate a line mesh
        private Mesh InitGridMesh(FLIPFluidScene scene)
        {
            //Generate the vertices
            List<Vector3> lineVertices = new();

            int numX = scene.fluid.NumX;
            int numY = scene.fluid.NumY;

            //Debug.Log(numY);

            //These are in local space
            //float localCellWidthAndHeight = scene.fluid.Spacing;

            //This is the cell width and height in global space
            float cellWidthAndHeight = scene.simPlaneWidth / numX;
            
            //Map width and height in global space
            float mapWidth = cellWidthAndHeight * numX;
            float mapHeight = cellWidthAndHeight * numY;

            //The map is centered around 0 so grid lines start in bottom-left corner
            Vector3 startPos = new(-mapWidth * 0.5f, -mapHeight * 0.5f, gridOffset);
            //Debug.Log(-mapHeight * 0.5f);
            //Vertical lines                
            Vector3 linePosX = startPos;

            for (int x = 0; x <= numX; x++)
            {
                lineVertices.Add(linePosX);
                lineVertices.Add(linePosX + Vector3.up * mapHeight);

                linePosX += Vector3.right * cellWidthAndHeight;
            }


            //Horizontal lines
            Vector3 linePosY = startPos;

            for (int y = 0; y <= numY; y++)
            {
                lineVertices.Add(linePosY);
                lineVertices.Add(linePosY + Vector3.right * mapWidth);

                linePosY += Vector3.up * cellWidthAndHeight;
            }


            //Generate the indices
            List<int> indices = new();

            for (int i = 0; i < lineVertices.Count; i++)
            {
                indices.Add(i);
            }


            //Generate the mesh
            Mesh gridMesh = new();

            gridMesh.SetVertices(lineVertices);
            gridMesh.SetIndices(indices, MeshTopology.Lines, 0);


            return gridMesh;
        }
    }
}
