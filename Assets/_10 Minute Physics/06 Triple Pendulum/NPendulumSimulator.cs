using System.Collections;
using System.Collections.Generic;
using UnityEngine;


//Simulates an n-pendulum
public class NPendulumSimulator
{
    //Public
    public readonly List<Node> pendulumSections = new List<Node>();


    //Private
    //The total length of the pendulum 
    private readonly float pendulumLength;

    //How many pendulum sections?
    private readonly int numberOfPendulumSections;

    //How long is a single section?
    private float SectionLength => pendulumLength / (float)numberOfPendulumSections;

    private readonly Vector3 gravity = new Vector3(0f, -9.81f, 0f);

    //To easier replicate a scenario
    private readonly int seed = 0;



    public NPendulumSimulator(int numberOfPendulumSections, float length, Vector3 startPos)
    {
        this.numberOfPendulumSections = numberOfPendulumSections;

        this.pendulumLength = length;


        Random.InitState(seed);


        //Add the wall
        Node wallSection = new Node(startPos, 0f, true);

        pendulumSections.Add(wallSection);

        //Add the sections
        Vector3 pendulumStartDir = new Vector3(1f, 0.6f, 0f).normalized;

        for (int n = 0; n < numberOfPendulumSections; n++)
        {
            startPos += pendulumStartDir * SectionLength;

            //Random or fixed mass?
            float mass = 0.5f;
            //float mass = Random.Range(0.1f, 1f);

            //Add the node
            Node newSection = new Node(startPos, mass, false);

            pendulumSections.Add(newSection);


            //Change direction to next section to get a more chaotic behavior
            //Otherwise we get what looks like a rope 
            float randomZ = Random.Range(0f, 85f);

            pendulumStartDir = Quaternion.Euler(0f, 0f, randomZ) * pendulumStartDir;
        }
    }



    //Simulate the pendulum one step
    public void Simulate(float dt)
    {
        //Always ignore first node because its fixed to a wall

        //Update velocity and position
        for (int i = 1; i < pendulumSections.Count; i++)
        {
            Node thisNode = pendulumSections[i];

            thisNode.StartStep(dt, gravity);
        }


        //Ensure constraints
        for (int i = 1; i < pendulumSections.Count; i++)
        {
            Node prevNode = pendulumSections[i - 1]; //x1
            Node thisNode = pendulumSections[i]; //x2

            //The direction between the nodes
            Vector3 dir = thisNode.pos - prevNode.pos;

            //The current distance between the nodes
            float currentLength = dir.magnitude;

            //Move the node based on its mass and the mass of the connected node
            //w = 0 if we have infinite mass, meaning the node is connected to a wall 
            float w1 = !prevNode.isFixed ? 1f / prevNode.mass : 0f;
            float w2 = !thisNode.isFixed ? 1f / thisNode.mass : 0f;

            //x1_moveDist = 0.5 * (currentLength - wantedLegth) * (x2-x1).normalized
            //x2_moveDist = - 0.5 * (currentLength - wantedLegth) * (x2-x1).normalized

            //But if we have masses, we can replace 0.5 with: w1 / (w1 + w2) where w = 1 / m
            //This means no movement at all if w = 0 when node is connected to a wall

            //So we get
            //prevNode.pos += (w1 / (w1 + w2)) * (currentLength - sectionLength) * dir.normalized;
            //thisNode.pos -= (w2 / (w1 + w2)) * (currentLength - sectionLength) * dir.normalized;

            //But above can be simplified (according to the video) to:
            //Which is faster becase we dont need to normalize
            float correction = (SectionLength - currentLength) / currentLength / (w1 + w2);

            //Move the nodes
            //Why are we not using the normalized direction?
            //- and + are inverted from the equations because the guy in the video is inverting the correction 
            prevNode.pos -= w1 * correction * dir;
            thisNode.pos += w2 * correction * dir;
        }


        //Fix velocity
        for (int i = 1; i < pendulumSections.Count; i++)
        {
            Node thisNode = pendulumSections[i];

            thisNode.EndStep(dt);
        }
    }
}
