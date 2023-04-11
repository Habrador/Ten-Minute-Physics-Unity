using System.Collections;
using System.Collections.Generic;
using UnityEngine;



namespace EulerianFluidSimulator
{
    //User interactions with the fluid
    //Buttons and checkboxes
    //Position the obstacle with the mouse
    //Pause simulation (P) and step forward the simulation (M)
    public class FluidUI
    {
        private readonly FluidSimController controller;

        //For mouse drag
        private Vector2 lastMousePos;



        public FluidUI(FluidSimController controller)
        {
            this.controller = controller;
        }



        //Buttons, checkboxes, show min/max pressure
        public void MyOnGUI(FluidScene scene)
        {
            GUILayout.BeginHorizontal("box");

            int fontSize = 20;

            RectOffset offset = new(5, 5, 5, 5);


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

            
            //Show the min and max pressure as text
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



        public void Interaction(FluidScene scene)
        {            
            //Teleport obstacle if we click with left mouse
            if (Input.GetMouseButtonDown(0))
            {
                Vector2 mousePos = GetMousePos(scene);

                //Is this coordinate within the simulation space (Or we will move the object when trying to interact with the UI)
                if (scene.fluid.IsWithinArea(mousePos.x, mousePos.y))
                {
                    controller.SetObstacle(mousePos.x, mousePos.y, true);

                    this.lastMousePos = mousePos;
                }
            }
            //Drag obstacle if we hold down left mouse
            else if (Input.GetMouseButton(0))
            {
                Vector2 mousePos = GetMousePos(scene);

                //Is this coordinate within the simulation space (Or we will move the object when trying to interact with the UI)
                if (scene.fluid.IsWithinArea(mousePos.x, mousePos.y))
                {
                    //Has the mouse positioned changed = we are dragging?
                    if (mousePos.x != this.lastMousePos.x && mousePos.y != this.lastMousePos.y)
                    {
                        controller.SetObstacle(mousePos.x, mousePos.y, false);

                        this.lastMousePos = mousePos;
                    }
                }
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



            SampleWithMouse(scene);
        }



        //Sample the field with the mouse position
        private void SampleWithMouse(FluidScene scene)
        {
            Vector2 mousePos = GetMousePos(scene);

            Vector2Int cellPos = scene.SimToCell(mousePos.x, mousePos.y);

            //Debug.Log(cellPos);

            FluidSim f = scene.fluid;

            int x = cellPos.x;
            int y = cellPos.y;

            if (x >= 0 && x < f.numX && y >= 0 && y < f.numY)
            {
                float velU = f.u[f.To1D(x, y)]; //velocity in u direction
                float velV = f.v[f.To1D(x, y)]; //velocity in v direction
                float p = f.p[f.To1D(x, y)]; //pressure
                float s = f.s[f.To1D(x, y)]; //solid (0) or fluid (1)
                float m = f.m[f.To1D(x, y)]; //smoke density

                Debug.Log($"u: {velU}, v: {velV}, p: {p}, s: {s}, m: {m}");
            }
        }



        //Get the mouse coordinates in simulation space
        private Vector2 GetMousePos(FluidScene scene)
        {
            //Default if raycasting doesnt work - which it always should
            Vector2 mousePos = Vector2.zero;
        
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
                mousePos = scene.WorldToSim(mousePos3D.x, mousePos3D.y);
            }

            return mousePos;
        }
    }
}