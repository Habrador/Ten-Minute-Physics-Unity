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
    }
}