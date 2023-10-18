using EulerianFluidSimulator;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.ParticleSystem;


namespace FLIPFluidSimulator
{
    public class DisplayFLIPFluid : MonoBehaviour
    {
        private static Mesh circleMesh;

        private static Mesh[] particleMeshes;

        private static Mesh particleMesh;

        private static Transform[] particleTransforms;



        //Called every Update
        public static void Draw(FLIPFluidScene scene, GameObject particlePrefabGO)
        {
            //UpdateTexture(scene);

            //if (scene.showVelocities)
            //{
            //    ShowVelocities(scene);
            //}

            ////scene.showStreamlines = true;

            //if (scene.showStreamlines)
            //{
            //    ShowStreamlines(scene);
            //}

            if (scene.showObstacle)
            {
                ShowObstacle(scene);
            }

            if (scene.showParticles)
            {
                ShowParticles(scene, particlePrefabGO);
            }

            //Moved the display of min and max pressure as text to the UI class
        }



        //
        // Display the circle obstacle
        //
        private static void ShowObstacle(FLIPFluidScene scene)
        {
            FLIPFluidSim f = scene.fluid;

            //Make it slightly bigger to hide the jagged edges we get because we use a grid with square cells which will not match the circle edges prefectly
            float circleRadius = scene.obstacleRadius + f.h;

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
        private static void ShowParticles(FLIPFluidScene scene, GameObject particlePrefabGO)
        {
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
    }
}
