using EulerianFluidSimulator;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace FLIPFluidSimulator
{
    public class DisplayFLIPFluid : MonoBehaviour
    {
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
            float r = scene.obstacleRadius + f.h;

            //The color of the circle
            DisplayShapes.ColorOptions color = DisplayShapes.ColorOptions.Gray;

            //Circle center in global space
            Vector2 globalCenter2D = scene.SimToWorld(scene.obstacleX, scene.obstacleY);

            //3d space infront of the texture
            Vector3 circleCenter = new(globalCenter2D.x, globalCenter2D.y, -0.1f);

            //Generate the circle mesh
            Mesh circleMesh = GenerateCircleMesh(circleCenter, r, 50);

            //Display the circle mesh
            Material material = DisplayShapes.GetMaterial(color);

            Graphics.DrawMesh(circleMesh, Vector3.zero, Quaternion.identity, material, 0, Camera.main, 0);


            //The guy is also giving the circle a black border, which we could replicate by drawing a smaller circle but it doesn't matter! 
        }



        //Generate a circle mesh 
        private static Mesh GenerateCircleMesh(Vector3 circleCenter, float radius, int segments)
        {
            //Generate the vertices
            List<Vector3> vertices = GetCircleSegments_XY(circleCenter, radius, segments);

            //Add the center to make it easier to trianglulate
            vertices.Insert(0, circleCenter);

            //Generate the triangles
            List<int> triangles = new();

            for (int i = 2; i < vertices.Count; i++)
            {
                triangles.Add(0); //0 because the center vertex was added at position 0
                triangles.Add(i);
                triangles.Add(i - 1);
            }

            //Generate the mesh
            Mesh m = new();

            m.SetVertices(vertices);
            m.SetTriangles(triangles, 0);

            m.RecalculateNormals();

            return m;
        }



        //Generate vertices on the circle circumference in xy space
        private static List<Vector3> GetCircleSegments_XY(Vector3 circleCenter, float radius, int segments)
        {
            List<Vector3> vertices = new();

            float angleStep = 360f / (float)segments;

            float angle = 0f;

            for (int i = 0; i < segments + 1; i++)
            {
                float x = radius * Mathf.Cos(angle * Mathf.Deg2Rad);
                float y = radius * Mathf.Sin(angle * Mathf.Deg2Rad);

                Vector3 vertex = new Vector3(x, y, 0f) + circleCenter;

                vertices.Add(vertex);

                angle += angleStep;
            }

            return vertices;
        }
    }
}
