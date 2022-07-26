using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Simulate a pedulum with n sections

//Simulate hard distance constraints (the position between partices is constant) by using Position Based Dynamics
//Is useful for ropes, cloth, fur, sand, robot arms, etc
//Based on: https://matthias-research.github.io/pages/tenMinutePhysics/
public class NPendulumController : MonoBehaviour
{
    //Public
    public GameObject ballPrefabGO;
    public Transform wall;
    public GameObject armPrefabGO;

    public bool UsePendulumArms;


    //Private
    private readonly List<Node> pendulumSections = new List<Node>();

    //The total length of the pendulum 
    private float pendulumLength = 5.5f;

    //How many pendulum sections?
    private int numberOfPendulumSections = 3;

    //How long is a single section?
    private float SectionLength => pendulumLength / (float)numberOfPendulumSections;

    private Vector3 gravity = new Vector3(0f, -9.81f, 0f);

    //To draw the historical positions of the pendulum
    private Queue<Vector3> historicalPositions = new Queue<Vector3>();

    //Fewer susteps results in more damping and less chaos.
    //The guy in the video is using up to 10k substeps to match the behavior of an actual 3-pendulum
    private readonly int subSteps = 50;

    //To easier replicate a scenario
    private readonly int seed = 0;



    private void Start()
    {
        Random.InitState(seed);


        //Add the wall
        Node wallSection = new Node(wall,null, true);

        pendulumSections.Add(wallSection);


        //Add the sections
        Vector3 pendulumStartDir = new Vector3(1f, -0.6f, 0f).normalized;

        for (int n = 0; n < numberOfPendulumSections; n++)
        {
            Vector3 pos = wall.transform.position + pendulumStartDir * SectionLength * (n + 1);
        

            //Use ball and string to show position of pendulum
            GameObject newBall = GameObject.Instantiate(ballPrefabGO, pos, Quaternion.identity);

            //Scale is later turned into mass
            float radius = 0.5f;
            //float radius = Random.Range(0.1f, 1f);

            newBall.transform.localScale = Vector3.one * radius;


            //Use arm to show position of pendulum
            GameObject newArm = GameObject.Instantiate(armPrefabGO);

            newArm.GetComponent<Arm>().Init(radius);


            if (UsePendulumArms)
            {
                newBall.SetActive(false);
                newArm.SetActive(true);
            }
            else
            {
                newBall.SetActive(true);
                newArm.SetActive(false);
            }
            

            //Add the node
            Node newSection = new Node(newBall.transform, newArm.GetComponent<Arm>());

            pendulumSections.Add(newSection);


            //Change direction to next section to get a more chaotic behavior
            //Otherwise we get what looks like a rope 
            float randomZ = Random.Range(0f, 25f);

            pendulumStartDir = Quaternion.Euler(0f, 0f, randomZ) * pendulumStartDir;
        }
    }



    private void Update()
    {
        if (UsePendulumArms)
        {
            for (int i = 1; i < pendulumSections.Count; i++)
            {
                Node prevNode = pendulumSections[i - 1];
                Node thisNode = pendulumSections[i];

                bool isOffset = i % 2 == 0;

                pendulumSections[i].UpdateArmPosition(prevNode.pos, thisNode.pos, isOffset);
            }
        }
        else
        {
            foreach (Node node in pendulumSections)
            {
                node.UpdateVisualPosition();
            }
        }
    }


    private void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;

        float sdt = dt / (float)subSteps;

        for (int step = 0; step < subSteps; step++)
        {
            Simulate(sdt, gravity);
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



    //Simulate the pendulum one step
    private void Simulate(float dt, Vector3 gravity)
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



    private void LateUpdate()
    {
        //Display the pendulum sections with a line
        if (!UsePendulumArms)
        {
            List<Vector3> vertices = new List<Vector3>();

            foreach (Node n in pendulumSections)
            {
                vertices.Add(n.pos);
            }

            DisplayShapes.DrawLine(vertices, DisplayShapes.ColorOptions.White);
        }


        //Display the historical positions of the pendulum
        List<Vector3> historicalVertices = new List<Vector3>(historicalPositions);

        DisplayShapes.DrawLine(historicalVertices, DisplayShapes.ColorOptions.Yellow);
    }
}
