using EulerianFluidSimulator;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace FLIPFluidSimulator
{
    public class DisplayFLIPFluid : MonoBehaviour
    {
        private static Mesh circleMesh;
        


        //Called every Update
        public static void Draw(FLIPFluidScene scene)
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
                ShowParticles(scene);
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
        private static void ShowParticles(FLIPFluidScene scene)
        {
            //FLIPFluidSim f = scene.fluid;

            //
            float particleRadius = scene.fluid.particleRadius;

            //The position of each particle (x, y) after each other
            float[] particleFlatPositions = scene.fluid.particlePos;

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

            List<Vector3> verts = new List<Vector3>(particleGlobalPositions);

            Material mat = DisplayShapes.GetMaterial(DisplayShapes.ColorOptions.Blue);

            DisplayShapes.DrawVertices(verts, mat);

            

            //Generate a new circle mesh if we havent done so
            //if (circleMesh == null)
            //{
            //    circleMesh = DisplayShapes.GenerateCircleMesh_XY(Vector3.zero, circleRadius, 50);
            //}

            //Display the circle mesh
            //Graphics.DrawMesh(circleMesh, circleCenter, Quaternion.identity, scene.obstacleMaterial, 0, Camera.main, 0);
        }
    }
}
