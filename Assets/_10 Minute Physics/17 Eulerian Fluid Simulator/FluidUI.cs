using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EulerianFluidSimulator
{
    public class FluidUI
    {
        private readonly FluidSimController controller;

        private bool mouseDown = false;

        private Vector2 mousePos;



        public FluidUI(FluidSimController controller)
        {
            this.controller = controller;
        }



        public void DisplayUI(FluidScene scene)
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
                controller.SetupScene(FluidScene.SceneNr.WindTunnel);
            }
            if (GUILayout.Button("Hires Tunnel", buttonStyle))
            {
                controller.SetupScene(FluidScene.SceneNr.HighResWindTunnel);
            }
            if (GUILayout.Button("Tank", buttonStyle))
            {
                controller.SetupScene(FluidScene.SceneNr.Tank);
            }
            if (GUILayout.Button("Paint", buttonStyle))
            {
                controller.SetupScene(FluidScene.SceneNr.Paint);
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

            
            //Show the min ad max pressure as text
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



        //Has to be called from OnGUI because we use Event???
        public void Interaction(FluidScene scene)
        {            
            //Click with left mouse
            if (Input.GetMouseButtonDown(0))
            {
                //Fire a ray against a plane to get the position of the mouse in world space
                Plane plane = new (-Vector3.forward, Vector3.zero);

                //Create a ray from the mouse click position
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

                if (plane.Raycast(ray, out float enter))
                {
                    //Get the point that is clicked in world space
                    Vector3 mousePos3D = ray.GetPoint(enter);

                    //Debug.Log(mousePos);

                    //From world space to simulation space
                    Vector2 coordinates = scene.WorldToSim(mousePos3D.x, mousePos3D.y);

                    //Is this coordinate within the simulation space (Or we will move the object when trying to interact with the UI)
                    if (scene.fluid.IsWithinArea(coordinates.x, coordinates.y))
                    {
                        StartDrag(coordinates.x, coordinates.y);

                        this.mousePos = coordinates;
                    }
                }
            }



            //Drag if we hold down left mouse
            if (mouseDown)
            {
                //Fire a ray against a plane to get the position of the mouse in world space
                Plane plane = new(-Vector3.forward, Vector3.zero);

                //Create a ray from the mouse click position
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

                if (plane.Raycast(ray, out float enter))
                {
                    //Get the point that is clicked in world space
                    Vector3 mousePos3D = ray.GetPoint(enter);

                    //Debug.Log(mousePos);

                    //From world space to simulation space
                    Vector2 coordinates = scene.WorldToSim(mousePos3D.x, mousePos3D.y);

                    //Is this coordinate within the simulation space (Or we will move the object when trying to interact with the UI)
                    if (scene.fluid.IsWithinArea(coordinates.x, coordinates.y))
                    {
                        //Have we moved the mouse since we clicked it, meaning we are dragging the mouse
                        if (coordinates.x != this.mousePos.x || coordinates.y != this.mousePos.y)
                        {
                            Drag(coordinates.x, coordinates.y);
                        }

                        this.mousePos = coordinates;
                    }
                }
            }



            //Release left mouse
            if (Input.GetMouseButtonUp(0))
            {
                EndDrag();
            }


            
            //Pause the simulation
            if (Input.GetKeyDown(KeyCode.P))
            {
                scene.isPaused = !scene.isPaused;
            }
            //Move the simulation one step forward
            else if (Input.GetKeyDown(KeyCode.M))
            {
                scene.isPaused = false;

                controller.Simulate();

                scene.isPaused = true;
            }
        }



        //x,y are in simulation space
        private void StartDrag(float x, float y)
        {
            mouseDown = true;

            //Debug.Log(x + " " + y);

            controller.SetObstacle(x, y, true);
        }



        //x,y are in simulation space
        private void Drag(float x, float y)
        {
            if (mouseDown)
            {
                controller.SetObstacle(x, y, false);
            }
        }



        private void EndDrag()
        {
            mouseDown = false;
        }
    }
}