using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Help struct so we dont have to send a million parameters
public struct GridConstants
{
    //Cellsize
    public float h;
    public float half_h;
    public float one_over_h;

    //Grid size
    public int numX;
    public int numY;

    public GridConstants(float h, int numX, int numY)
    {
        this.h = h;
        this.half_h = h * 0.5f;
        this.one_over_h = 1f / h;
        this.numX = numX;
        this.numY = numY;
    }
}
