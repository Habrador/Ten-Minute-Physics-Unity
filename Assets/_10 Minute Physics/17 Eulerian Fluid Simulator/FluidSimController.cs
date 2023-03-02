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
    private FluidSimTutorial fluidSim;

    private Scene scene;

    private void Start()
    {
        //Density of the fluid (water)
        float density = 1000f;

        //The height of the simulation is 1 m (in the tutorial) but the guy is also setting simHeight = 1.1 annd domainHeight = 1 so Im not sure which is which. But he says 1 m in the video
        float simHeight = 1f;

        //How detailed the simulation is in height direction
        int simResolution = 50;

        //The size of a cell
        float h = simHeight / simResolution;

        //How many cells do we have
        //y is up
        int numY = Mathf.FloorToInt(simHeight / h);
        //Twice as wide
        int numX = 2 * numY;

        //fluidSim = new FluidSimTutorial(density, numX, numY, h);

        scene = new Scene();
    }



    private void SetupScene(int sceneNr = 0)
    {

    }


    //UI
    private void OnGUI()
    {
        


        GUILayout.BeginHorizontal("box");

        int fontSize = 20;

        RectOffset offset = new RectOffset(10, 10, 10, 10);

        //Buttons
        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);

        //buttonStyle.fontSize = 0; //To reset because fontSize is cached after you set it once 

        buttonStyle.fontSize = fontSize;

        buttonStyle.margin = offset;

        if (GUILayout.Button($"Wind Tunnel", buttonStyle))
        {
            SetupScene(1);
        }
        if (GUILayout.Button("Hires Tunnel", buttonStyle))
        {
            SetupScene(3);
        }
        if (GUILayout.Button("Tank", buttonStyle))
        {
            SetupScene(0);
        }
        if (GUILayout.Button("Paint", buttonStyle))
        {
            SetupScene(2);
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
    }
}
