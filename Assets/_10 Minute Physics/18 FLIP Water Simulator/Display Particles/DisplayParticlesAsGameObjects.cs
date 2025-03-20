using FLIPFluidSimulator;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Display particles using GameObjects
public class DisplayParticlesAsGameObjects
{
    private Transform[] allParticlesTrans;



    public DisplayParticlesAsGameObjects(GameObject particlePrefabObj, FLIPFluidSim fluidSim)
    {
        //Create the particles we can see
        int totalParticles = fluidSim.numParticles;

        float r = fluidSim.particleRadius;

        float simHeight = fluidSim.SimHeight;

        allParticlesTrans = new Transform[totalParticles];

        for (int i = 0; i < totalParticles; i++)
        {
            Transform newParticleTrans = GameObject.Instantiate(particlePrefabObj).transform;

            //Scale is diameter
            //But r is in sim space which is NOT the same as world space
            //height of simulation is 3 m but the plane we use is 1m high
            float rGlobal = r / simHeight;

            newParticleTrans.localScale = Vector3.one * rGlobal * 2f;

            allParticlesTrans[i] = newParticleTrans;
        }
    }



    public void UpdateParticles(FLIPFluidScene scene)
    {
        FLIPFluidSim fluidSim = scene.fluid;
    
        //The position of each particle (x, y) after each other in simulation space
        float[] particleFlatPositions = fluidSim.particlePos;

        //The global postion of each particle
        Vector3[] particleGlobalPositions = new Vector3[particleFlatPositions.Length / 2];

        for (int i = 0; i < particleFlatPositions.Length; i += 2)
        {
            float localX = particleFlatPositions[i];
            float localY = particleFlatPositions[i + 1];

            //Circle center in global space
            Vector2 globalCenter2D = scene.SimToWorld(new(localX, localY));

            //3d space infront of the texture
            Vector3 circleCenter = new(globalCenter2D.x, globalCenter2D.y, -0.1f);

            //0, 1, 2, 3, 4, 5, 6, 7, 8, 9
            //0, 1, 2, 3, 4
            //0 -> 0
            //2 -> 1
            //4 -> 2
            //6 -> 3
            //8 -> 4
            particleGlobalPositions[i / 2] = circleCenter;
        }


        //Update the transforms
        for (int i = 0; i < particleGlobalPositions.Length; i++)
        {
            allParticlesTrans[i].position = particleGlobalPositions[i];
        }
    }
}
