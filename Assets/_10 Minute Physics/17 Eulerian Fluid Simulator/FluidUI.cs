using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FluidSimulator
{
    public class FluidUI
    {
        private readonly FluidSimController controller;
    

        public FluidUI(FluidSimController controller)
        {
            this.controller = controller;
        }



        public void DisplayUI(Scene scene)
        {
            GUILayout.BeginHorizontal("box");

            int fontSize = 20;

            RectOffset offset = new(10, 10, 10, 10);

            //Buttons
            GUIStyle buttonStyle = new(GUI.skin.button);

            //buttonStyle.fontSize = 0; //To reset because fontSize is cached after you set it once 

            buttonStyle.fontSize = fontSize;

            buttonStyle.margin = offset;

            if (GUILayout.Button($"Wind Tunnel", buttonStyle))
            {
                controller.SetupScene(Scene.SceneNr.WindTunnel);
            }
            if (GUILayout.Button("Hires Tunnel", buttonStyle))
            {
                controller.SetupScene(Scene.SceneNr.HighResWindTunnel);
            }
            if (GUILayout.Button("Tank", buttonStyle))
            {
                controller.SetupScene(Scene.SceneNr.Tank);
            }
            if (GUILayout.Button("Paint", buttonStyle))
            {
                controller.SetupScene(Scene.SceneNr.Paint);
            }

            //Checkboxes
            GUIStyle toggleStyle = GUI.skin.GetStyle("Toggle");

            toggleStyle.fontSize = fontSize;
            toggleStyle.margin = offset;

            scene.showStreamlines = GUILayout.Toggle(scene.showStreamlines, "Streamlines", toggleStyle);

            scene.showVelocities = GUILayout.Toggle(scene.showVelocities, "Velocities");

            scene.showPressure = GUILayout.Toggle(scene.showPressure, "Pressure");

            scene.showSmoke = GUILayout.Toggle(scene.showSmoke, "Smoke");

            scene.useOverRelaxation = GUILayout.Toggle(scene.useOverRelaxation, "Overrelax");

            scene.overRelaxation = scene.useOverRelaxation ? 1.9f : 1.0f;

            GUILayout.EndHorizontal();

            /* 
            //This was in Draw() but should be here, we just have to calculate min/max pressure twice or cache it somewhere  
            if (scene.showPressure)
            {
                var s = "pressure: " + minP.toFixed(0) + " - " + maxP.toFixed(0) + " N/m";
                c.fillStyle = "#000000";
                c.font = "16px Arial";
                c.fillText(s, 10, 35);
            }
            */

            if (scene.showPressure)
            {
                if (scene.fluid == null)
                {
                    return;
                }

                //Find min and max pressure
                MinMax minMaxP = scene.fluid.GetMinMaxPressure();

                int intMinP = Mathf.RoundToInt(minMaxP.min);
                int intMaxP = Mathf.RoundToInt(minMaxP.max);

                string pressureText = $"Pressure: {intMinP}, {intMaxP} N/m";

                GUIStyle textStyle = GUI.skin.GetStyle("Label");

                textStyle.fontSize = fontSize;
                textStyle.margin = offset;

                GUILayout.Label(pressureText, textStyle);
            }
        }
    }
}