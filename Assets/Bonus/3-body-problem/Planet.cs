using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Planet : Ball
{
    public Planet(Transform ballTransform) : base (ballTransform)
    {
        
    }



    public void SimulatePlanet(int subSteps, float sdt, Vector3 acceleration)
    {
        for (int step = 0; step < subSteps; step++)
        {
            vel += acceleration * sdt;
            pos += vel * sdt;
        }
    }
}
