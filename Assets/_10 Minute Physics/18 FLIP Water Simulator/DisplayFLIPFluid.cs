using EulerianFluidSimulator;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using static UnityEngine.ParticleSystem;


namespace FLIPFluidSimulator
{
    public class DisplayFLIPFluid : MonoBehaviour
    {
        //The circle we can move around with mouse
        private static Mesh circleMesh;

        //Particles
        private static Mesh[] particleMeshes;

        private static Mesh particleMesh;

        private static Transform[] particleTransforms;

        //Grid
        private static Mesh gridMesh;



        //Called every Update
        public static void Draw(FLIPFluidScene scene, GameObject particlePrefabGO)
        {
            UpdateTexture(scene);

            if (scene.showObstacle)
            {
                ShowObstacle(scene);
            }

            if (scene.showParticles)
            {
                ShowParticles(scene, particlePrefabGO);
            }

            if (scene.showGrid)
            {
                DisplayGrid(scene);
            }
        }



        //
        // Show the fluid simulation data on a texture
        //
        private static void UpdateTexture(FLIPFluidScene scene)
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
            //Color of each cell (r, g, b) after each other. Color values are in the rannge [0,1]
            //private readonly float[] cellColor;
            //This is cellColor in the tutorial
            Color32[] textureColors = new Color32[f.NumX * f.NumY];

            //Find the colors
            //This was an array in the source, but we can treat the Vector4 as an array to make the code match
            //Vector4 color = new (255, 255, 255, 255);

            for (int x = 0; x < f.NumX; x++)
            {
                for (int y = 0; y < f.NumY; y++)
                {
                    //This was an array in the source, but we can treat the Vector4 as an array to make the code match
                    //Moved to here from before the loop so it resets every time so we can display the walls if we deactivate both pressure and smoke
                    Vector4 color = new(255, 255, 255, 255);

                    int index = f.To1D(x, y);

                    if (scene.showGrid)
                    {
                        //Solid
                        if (f.cellType[index] == f.SOLID_CELL)
                        {
                            //Gray
                            color[0] = 0.5f;
                            color[1] = 0.5f;
                            color[2] = 0.5f;
                        }
                        //Fluid
                        else if (f.cellType[index] == f.FLUID_CELL)
                        {
                            float d = f.particleDensity[index];

                            if (f.particleRestDensity > 0f)
                            {
                                d /= f.particleRestDensity;
                            }

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
                    Color32 pixelColor = new((byte)color[0], (byte)color[1], (byte)color[2], (byte)color[3]);

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
        private static void ShowObstacle(FLIPFluidScene scene)
        {
            FLIPFluidSim f = scene.fluid;

            //Make it slightly bigger to hide the jagged edges we get because we use a grid with square cells which will not match the circle edges prefectly
            float circleRadius = scene.obstacleRadius + f.Spacing;

            //Circle center in global space
            Vector2 globalCenter2D = scene.SimToWorld(scene.obstacleX, scene.obstacleY);

            //3d space infront of the texture
            Vector3 circleCenter = new(globalCenter2D.x, globalCenter2D.y, -0.1f);

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
        private static void UpdateParticleColors(FLIPFluidScene scene)
        {
            FLIPFluidSim f = scene.fluid;

            float one_over_h = 1f / f.Spacing;

            //For each particle
            for (int i = 0; i < f.numParticles; i++)
            {
                float s = 0.01f;

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

                float d0 = f.particleRestDensity;

                if (d0 > 0f)
                {
                    float relDensity = f.particleDensity[cellNr] / d0;

                    if (relDensity < 0.7f)
                    {
                        //Theres another s abover so this is s2
                        float s2 = 0.8f;

                        f.particleColor[3 * i + 0] = s2;
                        f.particleColor[3 * i + 1] = s2;
                        f.particleColor[3 * i + 2] = 1f;
                    }
                }
            }
        }

        private static void ShowParticles(FLIPFluidScene scene, GameObject particlePrefabGO)
        {
            //First update their colors
            UpdateParticleColors(scene);


            FLIPFluidSim f = scene.fluid;

            //
            float particleRadius = f.particleRadius;

            //The position of each particle (x, y) after each other
            float[] particleFlatPositions = f.particlePos;

            Vector3[] particleGlobalPositions = new Vector3[particleFlatPositions.Length / 2];

            for (int i = 0; i < particleFlatPositions.Length; i += 2)
            {
                float localX = particleFlatPositions[i];
                float localY = particleFlatPositions[i + 1];

                //Circle center in global space
                Vector2 globalCenter2D = scene.SimToWorld(localX, localY);

                //3d space infront of the texture
                Vector3 circleCenter = new(globalCenter2D.x, globalCenter2D.y, -0.1f);

                //0, 1, 2, 3, 4, 5, 6, 7, 8, 9
                //0, 1, 2, 3, 4
                //0 -> 0
                //2 -> 1
                //4 -> 2
                //6 -> 3
                //8 -> 4
                particleGlobalPositions[i / 2] = circleCenter;
            }


            //Draw the particles as a point cloud
            List<Vector3> verts = new(particleGlobalPositions);

            Material mat = DisplayShapes.GetMaterial(DisplayShapes.ColorOptions.Blue);

            DisplayShapes.DrawVertices(verts, mat);

            /*
            //Draw the particles as meshes

            //Putting them in an array and use Graphics.DrawMesh is slow as molasses - better to use instanced
            if (particleMeshes == null)
            {
                particleMeshes = new Mesh[particleGlobalPositions.Length];

                for (int i = 0; i < particleGlobalPositions.Length; i++)
                {
                    Mesh circleMesh = DisplayShapes.GenerateCircleMesh_XY(Vector3.zero, particleRadius, 8);

                    particleMeshes[i] = circleMesh;
                }
            }


            //Display the circle meshes
            Material particleMat = DisplayShapes.GetMaterial(DisplayShapes.ColorOptions.Blue); 

            for (int i = 0; i < particleMeshes.Length; i++)
            {
                Mesh mesh = particleMeshes[i];
            
                Vector3 pos = particleGlobalPositions[i];

                Graphics.DrawMesh(mesh, pos, Quaternion.identity, particleMat, 0, Camera.main, 0);
            }
            */


            //Graphics.RenderMeshInstanced limits you to 1k meshes
            //if (particleMesh == null)
            //{
            //    particleMesh = DisplayShapes.GenerateCircleMesh_XY(Vector3.zero, particleRadius, 8);
            //}

            /*
            //Better to let Unity handling the batching for now
            //That means we have to create a gameobject for each particle
            if (particleTransforms == null)
            {
                particleTransforms = new Transform[particleGlobalPositions.Length];

                Mesh particleMesh = DisplayShapes.GenerateCircleMesh_XY(Vector3.zero, particleRadius, 6);

                //Parent GO
                GameObject parentGO = new();

                parentGO.name = "Particles";

                //Material particleMat = DisplayShapes.GetMaterial(DisplayShapes.ColorOptions.Blue);

                for (int i = 0; i < particleGlobalPositions.Length; i++)
                {
                    GameObject particleGO = Instantiate(particlePrefabGO);

                    particleGO.transform.parent = parentGO.transform;

                    //MeshRenderer mr = particleGO.AddComponent<MeshRenderer>();
                    //MeshFilter mf = particleGO.AddComponent<MeshFilter>();

                    //mf.mesh = particleMesh;
                    //mr.material = particleMat;

                    //particleGO.transform.position = particleGlobalPositions[i];

                    particleGO.GetComponent<MeshFilter>().mesh = particleMesh;

                    particleTransforms[i] = particleGO.transform;
                }
            }

            //Set each particles pos
            for (int i = 0; i < particleGlobalPositions.Length; i++)
            {
                particleTransforms[i].position = particleGlobalPositions[i];
            }
            */
        }



        //
        // Display a grid to show each cell
        //

        private static void DisplayGrid(FLIPFluidScene scene)
        {
            if (gridMesh == null)
            {
                gridMesh = InitGridMesh(scene);
            }

            Material gridMat = DisplayShapes.GetMaterial(DisplayShapes.ColorOptions.Red);

            Graphics.DrawMesh(gridMesh, Vector3.zero, Quaternion.identity, gridMat, 0, Camera.main, 0);
        }


        //Generate a line mesh
        private static Mesh InitGridMesh(FLIPFluidScene scene)
        {
            //Generate the vertices
            List<Vector3> lineVertices = new();

            int numX = scene.fluid.NumX;
            int numY = scene.fluid.NumY;

            //These are in local space
            //float localCellWidthAndHeight = scene.fluid.Spacing;

            //This is the cell width and height in global space
            float cellWidthAndHeight = scene.simPlaneWidth / numX; 

            //Map width and height in global space
            float mapWidth = cellWidthAndHeight * numX;
            float mapHeight = cellWidthAndHeight * numY;

            //The map is centered around 0 so grid lines start in bottom-left corner
            Vector3 startPos = new(-mapWidth * 0.5f, -mapHeight * 0.5f, -0.1f);

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
