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

    //Use arms or lines and balls to display the pendulum?
    public bool UsePendulumArms;


    //Private

    //The pendulum itself
    private NPendulumSimulator pendulum;

    //How many pendulum sections?
    private readonly int numberOfPendulumSections = 3;

    //The total length of the pendulum 
    private readonly float pendulumLength = 5.5f;

    //Visualize the pendulum with arms
    private List<Arm> pendulumArms = new List<Arm>();

    //Visualize the pendulum with lines and balls
    private List<Transform> pendulumBalls = new List<Transform>();

    //To draw the historical positions of the pendulum
    private Queue<Vector3> historicalPositions = new Queue<Vector3>();



    private void Start()
    {
        //Create a new pendulum
        pendulum = new NPendulumSimulator(numberOfPendulumSections, pendulumLength, wall.position);
    

        //Generate what we need to visualize the pendulum
        for (int n = 0; n < numberOfPendulumSections; n++)
        {
            //Scale depends on mass
            float radius = pendulum.pendulumSections[n + 1].mass;

            //Use arm to show position of pendulum
            if (UsePendulumArms)
            {
                GameObject newArmGO = GameObject.Instantiate(armPrefabGO);

                newArmGO.SetActive(true);

                Arm newArm = newArmGO.GetComponent<Arm>();

                newArm.Init(radius);

                pendulumArms.Add(newArm);
            }
            //Use line and ball to show position of pendulum
            else
            {
                GameObject newBall = GameObject.Instantiate(ballPrefabGO);

                newBall.transform.localScale = Vector3.one * radius;
                
                newBall.SetActive(true);

                pendulumBalls.Add(newBall.transform);
            }
        }
    }



    private void Update()
    {
        //Update the transforms so we can see the pendulum
        List<Node> pendulumSections = pendulum.pendulumSections;

        if (UsePendulumArms)
        {
            for (int i = 1; i < pendulumSections.Count; i++)
            {
                Node prevNode = pendulumSections[i - 1];
                Node thisNode = pendulumSections[i];

                bool isOffset = i % 2 == 0;

                pendulumArms[i - 1].UpdateSection(prevNode.pos, thisNode.pos, isOffset);
            }
        }
        else
        {
            for (int i = 1; i < pendulumSections.Count; i++)
            {
                pendulumBalls[i - 1].position = pendulumSections[i].pos;
            }
        }



        //Save the position of the last node so we can display it
        Vector3 lastPos = pendulumSections[^1].pos;

        //So the historical position is always behind the pendulum arms but infront of the pendulum holder
        lastPos += Vector3.forward * 0.3f;

        historicalPositions.Enqueue(lastPos);

        //Dont save too many
        //Better to save all so we can see there's no repetetive pattern
        //if (historicalPositions.Count > 20000)
        //{
        //    historicalPositions.Dequeue();
        //}
    }



    private void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;

        pendulum.MyFixedUpdate(dt);
    }



    private void LateUpdate()
    {
        //Display the pendulum sections with a line
        if (!UsePendulumArms)
        {
            List<Vector3> vertices = new List<Vector3>();

            List<Node> pendulumSections = pendulum.pendulumSections;

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
