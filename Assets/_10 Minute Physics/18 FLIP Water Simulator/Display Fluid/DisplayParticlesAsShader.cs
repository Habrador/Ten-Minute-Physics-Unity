using FLIPFluidSimulator;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Display particles using a shader on a plane
public class DisplayParticlesAsShader
{
    private Material particlesMaterial;

    private GameObject particlesPlane;

    private struct ParticleToShader
    {
        public Vector2 position;
        //public Vector3 rgbValues;
    }

    private ComputeBuffer particleBuffer;
    private ParticleToShader[] particles;



    public DisplayParticlesAsShader(GameObject particlesPlane, Material particlesMaterial)
    {
        this.particlesPlane = particlesPlane;
        this.particlesMaterial = particlesMaterial;
    }



    public void UpdateParticles(FLIPFluidScene scene)
    {
        FLIPFluidSim fluidSim = scene.fluid;

        //The position of each particle (x, y) after each other in simulation space
        float[] particleFlatPositions = fluidSim.particlePos;

        int particleCount = particleFlatPositions.Length / 2;

        particles = new ParticleToShader[particleCount];

        //8 is the size of ParticleToShader struct
        //20 is the size of Particle struct (8 for position + 12 for color)
        particleBuffer = new ComputeBuffer(particleCount, 8);

        float simWidth = fluidSim.SimWidth;
        float simHeight = fluidSim.SimHeight;

        for (int i = 0; i < particleFlatPositions.Length; i += 2)
        {
            float localX = particleFlatPositions[i];
            float localY = particleFlatPositions[i + 1];

            //Simulation space to shader space [0,1]
            Vector2 shaderCenter2D = new(localX / simWidth, localY / simHeight);

            //0, 1, 2, 3, 4, 5, 6, 7, 8, 9
            //0, 1, 2, 3, 4
            //0 -> 0
            //2 -> 1
            //4 -> 2
            //6 -> 3
            //8 -> 4
            particles[i / 2] = new ParticleToShader
            {
                position = shaderCenter2D,
                //rgbValues = new Vector3(0f, 0f, 1f)
            };
        }


        float particleRadius = fluidSim.particleRadius / simHeight;

        Transform planeTrans = particlesPlane.transform;

        Vector2 planeScale = new(planeTrans.lossyScale.x, planeTrans.lossyScale.y);

        //Debug.Log(planeScale);


        //Pass data from a script to a shader

        //Buffers are particularly useful when you need to send large amounts of data from CPU to GPU

        //You typically use a ComputeBuffer to store the data you want to send to the shader. A ComputeBuffer is a block of memory that can be read by the GPU.
        particleBuffer.SetData(particles);

        //In the shader, you define a StructuredBuffer that matches the structure of the data in the ComputeBuffer. This allows the shader to access the data efficiently.

        //You use the SetBuffer method on a material or shader to bind the ComputeBuffer to the StructuredBuffer in the shader.
        //This makes the data available to the shader for rendering or computation.
        particlesMaterial.SetBuffer("_Particles", particleBuffer);

        //Single values
        particlesMaterial.SetFloat("_ParticleRadius", particleRadius);
        //particlesMaterial.SetColor("_ParticleColor", particleColor);
        particlesMaterial.SetInt("_ParticleCount", particleCount);
        particlesMaterial.SetVector("_PlaneScale", planeScale);
    }



    public void MyOnDestroy()
    {
        particleBuffer.Release();
    }
}
