using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FLIPFluidSimulator
{
    public class FLIPFluidSim
    {
        //How it works:
        //- Treat air as nothing because has much smaller density than water. Dont process or access velocities between air cells
        //- Use particles with position and velocity. A water cell is a cell with particles in it -> PIC (Particle in Cell)

        //PIC:
        //Simulate Particles
        //Velocity transfer: Particles -> Grid
        //Make the grid velocities incompressible
        //Velocity transfer: Grid -> Particles
        //(Particles carry velocity so no grid advection step is needed!!!)
        //Introduces viscocity

        //FLIP:
        //Simulate Particles
        //Velocity transfer: Particles -> Grid and make a copy of the grid velocities
        //Make the grid velocities incompressible
        //Velocity transfer: Add velcity changes to the particles: incompressible vel - copy of the grid velocities
        //Introduces noise

        //Combine PIC with FLIP to minimize viscocity and noise: 90% FLIP + 10% PIC = success!

        //Make the solver aware of drift (fluid sinks to the bottom disappearing)
        //Decrease time step or increase iteration count = slow simulation
        //Better to push particles apart
        //...and compute particle density in each cell to reduce divergence in dense regions -> more outward push in dense regions 

    }

}