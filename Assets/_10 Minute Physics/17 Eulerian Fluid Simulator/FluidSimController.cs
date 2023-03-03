using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Most basic fluid simulator
//Based on: "How to write an Eulerian Fluid Simulator with 200 lines of code" https://matthias-research.github.io/pages/tenMinutePhysics/
//Eulerian means we simulate the fluid in a grid - not by using particles (Lagrangian). One can also use a combination of both methods
//Can simulate both liquids and gas
//Assume incompressible fluid with zero viscosity (inviscid) which are good approximations for water and gas
public class FluidSimController : MonoBehaviour
{
    private Scene scene;



    private void Start()
    {
        scene = new Scene();
    }



    private void Update()
    {
        //Display the fluid
        //Draw();
    }



    private void FixedUpdate()
    {
        return;
    
        //Simulate the fluid
        if (!scene.isPaused)
        {
            scene.fluid.Simulate(scene.dt, scene.gravity, scene.numIters);

            scene.frameNr++;
        }
    }



    //Init the simulation after a button has been pressed
    private void SetupScene(Scene.SceneNr sceneNr = Scene.SceneNr.Tank)
    {
        scene.sceneNr = sceneNr;
        scene.obstacleRadius = 0.15f;
        scene.overRelaxation = 1.9f;

        scene.dt = Time.fixedDeltaTime;
        scene.numIters = 40;

        //How detailed the simulation is in height direction
        int res = 100;

        if (sceneNr == Scene.SceneNr.Tank)
        {
            res = 50;
        }
        else if (sceneNr == Scene.SceneNr.HighResWindTunnel)
        {
            res = 200;
        }


        //The height of the simulation is 1 m (in the tutorial) but the guy is also setting simHeight = 1.1 annd domainHeight = 1 so Im not sure which is which. But he says 1 m in the video
        float simHeight = 1f;

        //The size of a cell
        float h = simHeight / res;

        //How many cells do we have
        //y is up
        int numY = Mathf.FloorToInt(simHeight / h);
        //Twice as wide
        int numX = 2 * numY;

        //Density of the fluid (water)
        float density = 1000f;

        Fluid f = scene.fluid = new Fluid(density, numX, numY, h);

        //not same as numY above because we add a border?
        int n = f.numY;

        //Tank
        if (sceneNr == Scene.SceneNr.Tank)
        {           
            //Add a solid border
            for (int i = 0; i < f.numX; i++)
            {
                for (int j = 0; j < f.numY; j++)
                {
                    //Fluid
                    float s = 1f;
                    
                    if (i == 0 || i == f.numX - 1 || j == 0)
                    {
                        s = 0f;
                    }

                    f.s[i * n + j] = s;
                }
            }

            scene.gravity = -9.81f;
            scene.showPressure = true;
            scene.showSmoke = false;
            scene.showStreamlines = false;
            scene.showVelocities = false;
        }
        //Wind tunnel
        else if (sceneNr == Scene.SceneNr.WindTunnel || sceneNr == Scene.SceneNr.HighResWindTunnel)
        {
            //Wind velocity
            float inVel = 2f;
            
            for (int i = 0; i < f.numX; i++)
            {
                for (int j = 0; j < f.numY; j++)
                {
                    //Fluid
                    float s = 1f;

                    if (i == 0 || j == 0 || j == f.numY - 1)
                    {
                        //Solid
                        s = 0f;
                    }
                    f.s[i * n + j] = s;

                    if (i == 1)
                    {
                        f.u[i * n + j] = inVel;
                    }
                }
            }

            //Add smoke
            float pipeH = 0.1f * f.numY;
            
            int minJ = Mathf.FloorToInt(0.5f * f.numY - 0.5f * pipeH);
            int maxJ = Mathf.FloorToInt(0.5f * f.numY + 0.5f * pipeH);

            for (var j = minJ; j < maxJ; j++)
            {
                f.m[j] = 0f; //Why is this 0???
            }


            //setObstacle(0.4, 0.5, true);


            scene.gravity = 0f; //???
            scene.showPressure = false;
            scene.showSmoke = true;
            scene.showStreamlines = false;
            scene.showVelocities = false;

            if (sceneNr == Scene.SceneNr.HighResWindTunnel)
            {
                //scene.dt = 1.0 / 120.0;
                scene.numIters = 100;
                scene.showPressure = true;
            }
        }
        //Paint
        else if (sceneNr == Scene.SceneNr.Paint)
        {
            scene.gravity = 0f;
            scene.overRelaxation = 1f;
            scene.showPressure = false;
            scene.showSmoke = true;
            scene.showStreamlines = false;
            scene.showVelocities = false;
            scene.obstacleRadius = 0.1f;
        }
    }



    //UI
    private void OnGUI()
    {
        GUILayout.BeginHorizontal("box");

        int fontSize = 20;

        RectOffset offset = new (10, 10, 10, 10);

        //Buttons
        GUIStyle buttonStyle = new (GUI.skin.button);

        //buttonStyle.fontSize = 0; //To reset because fontSize is cached after you set it once 

        buttonStyle.fontSize = fontSize;

        buttonStyle.margin = offset;

        if (GUILayout.Button($"Wind Tunnel", buttonStyle))
        {
            SetupScene(Scene.SceneNr.WindTunnel);
        }
        if (GUILayout.Button("Hires Tunnel", buttonStyle))
        {
            SetupScene(Scene.SceneNr.HighResWindTunnel);
        }
        if (GUILayout.Button("Tank", buttonStyle))
        {
            SetupScene(Scene.SceneNr.Tank);
        }
        if (GUILayout.Button("Paint", buttonStyle))
        {
            SetupScene(Scene.SceneNr.Paint);
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
    }
}
