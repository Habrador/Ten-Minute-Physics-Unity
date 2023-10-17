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

            //Debug.Log(scene.obstacleRadius);

            //The color of the circle
            DisplayShapes.ColorOptions color = DisplayShapes.ColorOptions.Gray;

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
            Material material = DisplayShapes.GetMaterial(color);

            Graphics.DrawMesh(circleMesh, circleCenter, Quaternion.identity, material, 0, Camera.main, 0);
        }
    }
}
