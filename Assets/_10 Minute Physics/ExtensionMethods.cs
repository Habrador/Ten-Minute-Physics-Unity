using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ExtensionMethods
{
    //The perpedicular vector in xy space
    public static Vector3 Perp(this Vector3 v)
    {
        return new Vector3(-v.y, v.x, 0f);
    }

    
}
