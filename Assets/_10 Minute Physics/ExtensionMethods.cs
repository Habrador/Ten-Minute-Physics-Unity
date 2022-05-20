using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ExtensionMethods
{
    //The perpedicular vector in xy space
    public static Vector3 Perp(this Vector3 v)
    {
        //Just flip x with y and set one to negative
        //The YT videos is setting y to negative
        return new Vector3(-v.y, v.x, 0f);
    }

    
}
