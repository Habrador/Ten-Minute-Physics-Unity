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


    private void Start()
    {
        float density = 1000f;

        /*
        int res = 50;
        canvas.width = window.innerWidth - 20;
        canvas.height = window.innerHeight - 100;

        var simHeight = 1.1;
        var cScale = canvas.height / simHeight;
        var simWidth = canvas.width / cScale;

        float domainHeight = 1f;
        var domainWidth = domainHeight / simHeight * simWidth;
        var h = domainHeight / res;

        int numX = Mathf.FloorToInt(domainWidth / h);
        int numY = Mathf.FloorToInt(domainHeight / h);
        */

        int numX = 100;
        int numY = 50;

        float h = 0.2f;

        fluidSim = new FluidSimTutorial(density, numX, numY, h);
    }
}
