using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Simulate hard distance constraints (the position between partices is constant) by using Position Based Dynamics
//Is useful for ropes, cloth, fur, sand, robot arms, etc
//Based on: https://matthias-research.github.io/pages/tenMinutePhysics/
public class TriplePendulumController : MonoBehaviour
{
    public Transform ball_1;
    public Transform ball_2;
    public Transform ball_3;
    public Transform wall;

    private List<Node> pendulumSections = new List<Node>();

    //The distance we want between each node
    private float sectionLength = 2f;

    private Vector3 gravity = new Vector3(0f, -9.81f, 0f);

    //To draw the historical positions of the pendulum
    private Queue<Vector3> historicalPositions = new Queue<Vector3>();

    private int subSteps = 5;



    private void Start()
    {
        //-1 means infinite mass
        Node wallSection = new Node(-1f, wall.position, wall);

        Node pendulum_1_Section = new Node(1f, ball_1.position, ball_1);
        Node pendulum_2_Section = new Node(1.5f, ball_2.position, ball_2);
        Node pendulum_3_Section = new Node(0.5f, ball_3.position, ball_3);

        pendulumSections.Add(wallSection);
        pendulumSections.Add(pendulum_1_Section);
        pendulumSections.Add(pendulum_2_Section);
        pendulumSections.Add(pendulum_3_Section);

        //To avoid making the pendulum freak out at the start you can make sure that each section has the correct length
        //for (int i = 1; i < pendulumSections.Count; i++)
        //{
        //    Node prevNode = pendulumSections[i - 1]; 
        //    Node thisNode = pendulumSections[i];

        //    Vector3 dir = prevNode.pos - thisNode.pos;

        //    thisNode.pos += (dir.magnitude - sectionLength) * dir.normalized;
        //}
    }



    private void Update()
    {
        foreach (Node n in pendulumSections)
        {
            n.trans.position = n.pos;
        }
    }



    private void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;

        float sdt = dt / (float)subSteps;

        for (int step = 0; step < subSteps; step++)
        {
            //Ignore first node because its fixed to a wall
            for (int i = 1; i < pendulumSections.Count; i++)
            {
                Node thisNode = pendulumSections[i];

                thisNode.StartStep(sdt, gravity);
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
                float w1 = prevNode.mass > 0f ? 1f / prevNode.mass : 0f;
                float w2 = thisNode.mass > 0f ? 1f / thisNode.mass : 0f;

                //x1_moveDist = 0.5 * (currentLength - wantedLegth) * (x2-x1).normalized
                //x2_moveDist = - 0.5 * (currentLength - wantedLegth) * (x2-x1).normalized

                //But if we have masses, we can replace 0.5 with: w1 / (w1 + w2) where w = 1 / m
                //This means no movement at all if w = 0 when node is connected to a wall

                //So we get
                //prevNode.pos += (w1 / (w1 + w2)) * (currentLength - sectionLength) * dir.normalized;
                //thisNode.pos -= (w2 / (w1 + w2)) * (currentLength - sectionLength) * dir.normalized;

                //But above can be simplified (according to the video) to:
                //Which is faster becase we dont need to normalize
                float correction = (sectionLength - currentLength) / currentLength / (w1 + w2);

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

                thisNode.EndStep(sdt);
            }
        }


        //Save the position of the last node so we can save it
        Vector3 lastPos = pendulumSections[pendulumSections.Count - 1].pos;

        historicalPositions.Enqueue(lastPos);

        //Dont save too many
        if (historicalPositions.Count > 200)
        {
            historicalPositions.Dequeue();
        }
    }



    private void LateUpdate()
    {
        //Display the pendulum sections
        List<Vector3> vertices = new List<Vector3>();

        foreach (Node n in pendulumSections)
        {
            vertices.Add(n.pos);
        }

        DisplayShapes.DrawLineSegments(vertices, Color.white);


        //Display the historical positions of the pendulum
        List<Vector3> historicalVertices = new List<Vector3>(historicalPositions);

        DisplayShapes.DrawLineSegments(historicalVertices, Color.yellow);
    }
}
