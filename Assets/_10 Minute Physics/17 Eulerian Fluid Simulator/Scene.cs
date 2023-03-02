using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Scene
{
    public bool showStreamlines = false;
    public bool showVelocities = false;
    public bool showPressure = false;
    public bool showSmoke = true;
    public bool useOverRelaxation = true; //Is not in the tutorial but needs to be there to make Unity's toggles work

    public float overRelaxation = 1.9f;

    public int sceneNr = 0;

    public float obstacleRadius = 0.15f;

    public float dt;

    public int numIters = 100;

    public float gravity = -9.81f;

    public Fluid fluid;
}
