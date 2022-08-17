using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Billiard
{
    public static class BilliardMaterials
    {
        //Actual colors
        private readonly static Color32[] colors = { 
            new Color32(255, 215, 0, 255), //Yellow
            new Color32(0, 0, 255, 255), //Blue
            new Color32(255, 0, 0, 255), //Red
            new Color32(75, 0, 130, 255), //Purple
            new Color32(255, 69, 0, 255), //Orange
            new Color32(34, 139, 34, 255), //Green
            new Color32(128, 0, 0, 255), //Maroon
            new Color32(0, 0, 0, 255), //Black
            new Color32(255, 255, 255, 255) //White
        };


        private static List<Material> materials;


        public static Material GetRandomBilliardBallMaterial(Material baseMaterial)
        {
            if (materials == null)
            {
                materials = new List<Material>();

                foreach (Color32 color in colors)
                {
                    Material newMaterial = new(baseMaterial);

                    newMaterial.color = color;

                    materials.Add(newMaterial);
                }
            }


            Material randomMaterial = materials[Random.Range(0, materials.Count)];
            
            return randomMaterial;
        }



        public static Material GetLerpedMaterial(Material baseMaterial, int number, int total)
        {
            Material newMaterial = new(baseMaterial);

            Color color = Color.blue;

            if (total != 0)
            {
                color = Color.Lerp(Color.blue, Color.red, (float)number / (float)total);
            }

            newMaterial.color = color;

            return newMaterial;
        }


        public static void GiveBallsRandomColor(GameObject ballPrefabGO, List<BilliardBall> allBalls)
        {
            Material ballBaseMaterial = ballPrefabGO.GetComponent<MeshRenderer>().sharedMaterial;

            for (int i = 0; i < allBalls.Count; i++)
            {
                Material randomBallMaterial = BilliardMaterials.GetRandomBilliardBallMaterial(ballBaseMaterial);

                allBalls[i].ballTransform.GetComponent<MeshRenderer>().material = randomBallMaterial;
            }
        }



        public static void GiveBallsGradientColor(GameObject ballPrefabGO, List<BilliardBall> allBalls)
        {
            Material ballBaseMaterial = ballPrefabGO.GetComponent<MeshRenderer>().sharedMaterial;

            for (int i = 0; i < allBalls.Count; i++)
            {
                Material lerpedMaterial = BilliardMaterials.GetLerpedMaterial(ballBaseMaterial, i, allBalls.Count - 1);

                allBalls[i].ballTransform.GetComponent<MeshRenderer>().material = lerpedMaterial;
            }
        }
    }
}