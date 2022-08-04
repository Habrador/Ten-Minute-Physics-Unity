using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Simulate a pedulum with n sections

//Simulate hard distance constraints (the position between partices is constant) by using Position Based Dynamics
//Is useful for ropes, cloth, fur, sand, robot arms, etc
//Based on: https://matthias-research.github.io/pages/tenMinutePhysics/
public class NPendulumControllerYT : MonoBehaviour
{
    //Public
    public Transform wall;
    public GameObject armPrefabGO;

    //Use arms or lines (and balls) to display the pendulum?
    public bool usePendulumArms;
    public bool displayHistory;

    //For YT visualization
    public GameObject butterflyGO;
    public Material yellowGlow;

    //Pendulum settings

    //How many pendulum sections?
    public int pendulumArms = 3;

    //Can be useful to speed up the simulation
    public int simulationSpeed = 3;

    //Number of pendulums
    public int pendulums;

    //How long before the simulation starts
    public float pauseTimer = 0f;



    //Private

    //The pendulums
    private List<NPendulumSimulator> allPendulums = new List<NPendulumSimulator>();

    //The total length of the pendulum 
    private readonly float pendulumLength = 5.5f;

    //Fewer sub-steps results in more damping and less chaos
    //The guy in the video is using up to 10k sub-steps to match the behavior of an actual 3-pendulum
    private readonly int simulationSubSteps = 100;

    //Visualize the pendulum with arms
    private List<List<Arm>> allPendulumArms = new List<List<Arm>>();

    //Visualize the pendulum with lines and balls
    private List<Transform> pendulumBalls = new List<Transform>();

    //To distinguish multiple pendulums
    private List<Material> pendulumMaterials = new List<Material>();

    //To draw the historical positions of the pendulum
    private List<Queue<Vector3>> allHistoricalPositions = new List<Queue<Vector3>>();

    //So we can delay the simulation to easier see the start position
    private bool canSimulate = false;

    //To get the same pendulums each time
    private int seed = 0;

    //Cant use Unity's Random.Range because it's used in the pendulum script and setting Random.InitState(seed) at multiple locations screws things up
    private System.Random rnd;



    private void Start()
    {
        rnd = new System.Random(seed);
    
        //Offset in degrees so each pendulum get a slightly different start position to illustrate the butterfly effect
        float offset = 0f;

        for (int i = 0; i < pendulums; i++)
        {
            //Create a new pendulum
            NPendulumSimulator pendulum = new NPendulumSimulator(this.pendulumArms, pendulumLength, wall.position, offset);

            //Add a little offset for next pendulum
            offset += 0.001f;

            List<Arm> pendulumArms = new List<Arm>();

            //Generate what we need to visualize the pendulum
            for (int n = 0; n < this.pendulumArms; n++)
            {
                //Scale depends on mass
                float radius = pendulum.pendulumSections[n + 1].mass;

                //Use arm to show position of pendulum
                if (usePendulumArms)
                {
                    GameObject newArmGO = GameObject.Instantiate(armPrefabGO);

                    newArmGO.SetActive(true);

                    Arm newArm = newArmGO.GetComponent<Arm>();

                    newArm.Init(radius);

                    pendulumArms.Add(newArm);
                }
            }

            //Material
            if (!usePendulumArms)
            {
                Color firstColor = Color.white;
                Color lastColor = Color.blue;

                Material newMaterial = new Material(yellowGlow);

                Color thisColor = firstColor;

                if (pendulums > 1)
                {
                    thisColor = Color.Lerp(firstColor, lastColor, (float)(i) / (float)(pendulums - 1));
                }

                //Random color
                //Color thisColor = new Color(
                //    (float)rnd.NextDouble(),
                //    (float)rnd.NextDouble(),
                //    (float)rnd.NextDouble()
                //);

                //This will generate a blob
                //thisColor = Color.Lerp(firstColor, lastColor, (float)(i) / (float)(pendulums - 1));

                //thisColor = new Color(23.96863f, 23.71765f, 0f);

                //thisColor = Color.blue;

                //newMaterial.color = thisColor;

                float intensity = 5f;

                intensity = Mathf.Pow(2f, intensity);

                newMaterial.SetColor("_EmissionColor", thisColor * intensity);

                pendulumMaterials.Add(newMaterial);
            }


            allPendulums.Add(pendulum);
            allPendulumArms.Add(pendulumArms);
            allHistoricalPositions.Add(new Queue<Vector3>());

            //Debug.Log(i);
        }


        //Pause a little before the simulation starts
        StartCoroutine(WaitForSimulationToStart(pauseTimer));


        //Add a butterfly for visualization purposes
        if (butterflyGO != null && butterflyGO.activeInHierarchy)
        {
            NPendulumSimulator pendulum = allPendulums[0];
        
            butterflyGO.transform.position = pendulum.pendulumSections[^1].pos;
            butterflyGO.transform.position += Vector3.up * pendulum.pendulumSections[^1].mass * 0.5f;
            butterflyGO.transform.position -= Vector3.forward * 0.5f;

            butterflyGO.GetComponent<ButterflyController>().StartResting(pauseTimer);
        }

        //Debug.Log(allPendulums.Count);
    }



    private void Update()
    {
        for (int i = 0; i < allPendulums.Count; i++)
        {
            NPendulumSimulator pendulum = allPendulums[i];
        
            //Update the transforms so we can see the pendulum
            List<Node> pendulumSections = pendulum.pendulumSections;

            if (usePendulumArms)
            {
                List<Arm> pendulumArms = allPendulumArms[i];

                for (int j = 1; j < pendulumSections.Count; j++)
                {
                    Node prevNode = pendulumSections[j - 1];
                    Node thisNode = pendulumSections[j];

                    bool isOffset = j % 2 == 0;

                    pendulumArms[j - 1].UpdateSection(prevNode.pos, thisNode.pos, isOffset);
                }
            }
       


            //Save the position of the last node so we can display it
            if (displayHistory)
            {
                Vector3 lastPos = pendulumSections[^1].pos;

                //So the historical position is always behind the pendulum arms but infront of the pendulum holder
                lastPos += Vector3.forward * 0.3f;

                Queue<Vector3> historicalPositions = allHistoricalPositions[i];

                historicalPositions.Enqueue(lastPos);

                //Dont save too many
                //Better to save all so we can see there's no repetetive pattern
                //if (historicalPositions.Count > 20000)
                //{
                //    historicalPositions.Dequeue();
                //}
            }
        }

    }



    private void FixedUpdate()
    {
        if (!canSimulate)
        {
            return;
        }


        float dt = Time.fixedDeltaTime;

        float sdt = dt / (float)simulationSubSteps;

        foreach (NPendulumSimulator pendulum in allPendulums)
        {
            for (int i = 0; i < simulationSpeed; i++)
            {
                for (int step = 0; step < simulationSubSteps; step++)
                {
                    pendulum.Simulate(sdt);
                }
            }
        }
    }



    private void LateUpdate()
    {
        for (int i = 0; i < allPendulums.Count; i++)
        {
            NPendulumSimulator pendulum = allPendulums[i];
            
            //Display the pendulum sections with a line
            if (!usePendulumArms)
            {
                //if (i != 2)
                //{
                //    continue;
                //}
            
                List<Vector3> vertices = new List<Vector3>();

                List<Node> pendulumSections = pendulum.pendulumSections;

                foreach (Node n in pendulumSections)
                {
                    vertices.Add(n.pos);
                }

                DisplayShapes.DrawLine(vertices, pendulumMaterials[i]);
                //DisplayShapes.DrawLine(vertices, DisplayShapes.ColorOptions.Yellow);
            }


            if (displayHistory)
            {
                Queue<Vector3> historicalPositions = allHistoricalPositions[i];

                //Display the historical positions of the pendulum
                List<Vector3> historicalVertices = new List<Vector3>(historicalPositions);

                //DisplayShapes.DrawLine(historicalVertices, DisplayShapes.ColorOptions.Yellow);
                DisplayShapes.DrawLine(historicalVertices, yellowGlow);
            }
        }
    }



    //To delay the start of the simulation
    private IEnumerator WaitForSimulationToStart(float waitTime)
    {
        yield return new WaitForSeconds(waitTime);

        canSimulate = true;
    }

}
