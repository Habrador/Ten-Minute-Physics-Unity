using System.Collections;
using System.Collections.Generic;
using UnityEngine;


//Simulates an n-pendulum with double precisions
public class NPendulumSimulatorDouble
{
    //Public
    public readonly List<NodeDouble> pendulumSections = new List<NodeDouble>();


    //Private
    //The total length of the pendulum 
    private readonly double pendulumLength;

    //How many pendulum sections?
    private readonly int numberOfPendulumSections;

    //How long is a single section?
    private double SectionLength => pendulumLength / (double)numberOfPendulumSections;

    private readonly Vector3Double gravity = new (0.0, -9.81, 0.0);

    //To easier replicate a scenario
    private readonly int seed = 0;



    // - numberOfPendulumSections - 3 of we want a 3-pendulum
    // - length - total length of the pedulum
    // - startPos - where the pendulum is attached
    // - startAngleOffset - adds a small offset to the startAngle of the pendulum arms
    public NPendulumSimulatorDouble(int numberOfPendulumSections, double length, Vector3 startPos, float startAngleOffset = 0)
    {
        this.numberOfPendulumSections = numberOfPendulumSections;

        this.pendulumLength = length;


        Random.InitState(seed);


        //Add the wall
        Vector3Double startPosDouble = new (startPos.x, startPos.y, startPos.z);

        NodeDouble wallSection = new (startPosDouble, 0.0, true);

        pendulumSections.Add(wallSection);

        //Add the sections
        Vector3 pendulumStartDir = new Vector3(1.0f, 0.6f, 0.0f).normalized;

        for (int n = 0; n < numberOfPendulumSections; n++)
        {
            startPos += pendulumStartDir * (float)SectionLength;

            //Random or fixed mass?
            float mass = 0.5f;
            //float mass = Random.Range(0.1f, 1f);

            //Add the node
            //Float to double so we dont have to write a custom quaternion for doubles
            startPosDouble = new Vector3Double(startPos.x, startPos.y, startPos.z);

            NodeDouble newSection = new NodeDouble(startPosDouble, mass, false);

            pendulumSections.Add(newSection);


            //Change direction to next section to get a more chaotic behavior
            //Otherwise we get what looks like a rope 
            float randomAngleZ = Random.Range(0f, 85f);

            //Add a small offset to show the butterfly effect 
            randomAngleZ += startAngleOffset;

            pendulumStartDir = Quaternion.Euler(0f, 0f, randomAngleZ) * pendulumStartDir;
        }
    }



    //Simulate the pendulum one step
    public void Simulate(double dt)
    {
        //Always ignore first node because its fixed to a wall

        //Update velocity and position
        for (int i = 1; i < pendulumSections.Count; i++)
        {
            NodeDouble thisNode = pendulumSections[i];

            thisNode.StartStep(dt, gravity);
        }


        //Ensure constraints
        for (int i = 1; i < pendulumSections.Count; i++)
        {
            NodeDouble prevNode = pendulumSections[i - 1]; //x1
            NodeDouble thisNode = pendulumSections[i]; //x2

            //The direction between the nodes
            Vector3Double dir = thisNode.pos - prevNode.pos;

            //The current distance between the nodes
            double currentLength = dir.Magnitude;

            //Move the node based on its mass and the mass of the connected node
            //w = 0 if we have infinite mass, meaning the node is connected to a wall 
            double w1 = !prevNode.isFixed ? 1.0 / prevNode.mass : 0.0;
            double w2 = !thisNode.isFixed ? 1.0 / thisNode.mass : 0.0;

            //x1_moveDist = 0.5 * (currentLength - wantedLegth) * (x2-x1).normalized
            //x2_moveDist = - 0.5 * (currentLength - wantedLegth) * (x2-x1).normalized

            //But if we have masses, we can replace 0.5 with: w1 / (w1 + w2) where w = 1 / m
            //This means no movement at all if w = 0 when node is connected to a wall

            //So we get
            //prevNode.pos += (w1 / (w1 + w2)) * (currentLength - sectionLength) * dir.normalized;
            //thisNode.pos -= (w2 / (w1 + w2)) * (currentLength - sectionLength) * dir.normalized;

            //But above can be simplified (according to the video) to:
            //Which is faster becase we dont need to normalize
            double correction = (SectionLength - currentLength) / currentLength / (w1 + w2);

            //Move the nodes
            //Why are we not using the normalized direction?
            //- and + are inverted from the equations because the guy in the video is inverting the correction 
            prevNode.pos -= w1 * correction * dir;
            thisNode.pos += w2 * correction * dir;
        }


        //Fix velocity
        for (int i = 1; i < pendulumSections.Count; i++)
        {
            NodeDouble thisNode = pendulumSections[i];

            thisNode.EndStep(dt);
        }
    }
}
