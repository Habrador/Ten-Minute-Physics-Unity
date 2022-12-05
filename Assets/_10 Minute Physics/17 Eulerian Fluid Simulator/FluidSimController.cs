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

        fluidSim = new FluidSimTutorial(density, numX, numY, h);
    }
}
