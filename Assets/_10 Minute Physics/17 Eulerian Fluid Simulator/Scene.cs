using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Settings for the fluid simulation and a ref to the fluid simulation itself
public class Scene
{
    public bool showStreamlines = false;
    public bool showVelocities = false;
    public bool showPressure = false;
    public bool showSmoke = true;
    public bool useOverRelaxation = true; //Is not in the tutorial but needs to be there to make Unity's toggles work

    

    public int sceneNr = 0;

    public float dt;

    public int numIters = 100;

    public float gravity = -9.81f;

    public float overRelaxation = 1.9f;

    public Fluid fluid;

    //Useful for debugging 
    public int frameNr = 0;

    public bool isPaused = false;

    //Obstacles
    public bool showObstacle = false;

    public float obstacleX = 0f;
    public float obstacleY = 0f;

    public float obstacleRadius = 0.15f;
}
