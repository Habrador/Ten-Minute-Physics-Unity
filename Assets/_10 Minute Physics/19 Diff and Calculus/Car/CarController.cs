using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

//Simulate a basic car
//You can see the distance (white), velocity (yellow), acceleration (red) in a graph
//Control car with Left and Right arrows
public class CarController : MonoBehaviour
{
    public Transform carTrans;

    public Material graphMaterial;

    private float maxVel = 10f;
    private float maxAcc = 2f;

    private Queue<float> carPos = new();
    private Queue<float> carVel = new();
    private Queue<float> carAcc = new();

    private float pos;
    private float vel;
    private float acc;

    private int pedalPos = 0;

    private bool shouldSimulate = true;

    private Vector3 startPos;



    void Start()
    {
        pos = carTrans.position.x;

        carPos.Enqueue(pos);
        carVel.Enqueue(0f);
        carAcc.Enqueue(0f);

        startPos = carTrans.position;
    }



    private void FixedUpdate()
    {
        //Acc
        float pedalFactor = 5f;
    
        if (pedalPos == 1)
        {
            acc += pedalFactor * Time.fixedDeltaTime;
        }
        if (pedalPos == -1)
        {
            acc -= pedalFactor * Time.fixedDeltaTime;
        }

        acc = Mathf.Clamp(acc, -maxAcc, maxAcc);


        //Vel
        vel += acc * Time.fixedDeltaTime;

        vel = Mathf.Clamp(vel, -maxVel, maxVel);


        //Pos
        pos += vel * Time.fixedDeltaTime;

        //Clamp so we can drive outside of map
        if (pos < startPos.x)
        {
            pos = startPos.x;
        }
        if (pos > startPos.x * -1f)
        {
            pos = startPos.x * -1f;
        }


        //Cache
        carPos.Enqueue(pos);
        carVel.Enqueue(vel);
        carAcc.Enqueue(acc);
    }



    private void Update()
    {
        //Update pedqal pos
        pedalPos = 0;

        if (Input.GetKey(KeyCode.RightArrow))
        {
            pedalPos = 1;
        }
        else if (Input.GetKey(KeyCode.LeftArrow))
        {
            pedalPos = -1; 
        }

        //Update the visual position of the car
        Vector3 visualCarPos = carTrans.position;

        visualCarPos.x = pos;

        carTrans.position = visualCarPos;


        //Show the graphs
        ShowGraph();
    }



    public void ShowGraph()
    {
        //Time 
        Vector3 start = new Vector3(startPos.x, 0f, 0f);

        Vector3 end = new Vector3(startPos.x * -1f, 0f, 0f);

        DisplayShapes.DrawLine(new List<Vector3>() { start, end }, DisplayShapes.ColorOptions.White);

        //Data
        DisplayGraph( new MinMax(startPos.x, startPos.x * -1f), carPos, DisplayShapes.ColorOptions.White);
        DisplayGraph( new MinMax(-maxVel, maxVel), carVel, DisplayShapes.ColorOptions.Yellow);
        DisplayGraph( new MinMax(-maxAcc, maxAcc), carAcc, DisplayShapes.ColorOptions.Red);
    }



    private void DisplayGraph(MinMax dataRange, Queue<float> data, DisplayShapes.ColorOptions color)
    {
        //To make the data fit on the y-axis we need to normalize all values to this range
        MinMax graphRange = new MinMax(-3f, 3f);

        //Time ticks on constantly with some value
        float yScale = 0.05f;

        //float -> Vector3
        List<Vector3> graphPos3D = new();

        //Distance traveled is y-axis and time is x-axis
        List<float> graphY = data.ToList();

        Vector3 graphPos = new Vector3(startPos.x, 0f, 0f);

        for (int i = 0; i < graphY.Count; i++)
        {
            graphPos3D.Add(graphPos);

            if (i > 0)
            {
                //Time ticks on constantly with some value
                graphPos.x += yScale;

                //Make it fit on the screen
                float diff = graphY[i] - graphY[i - 1];

                float diffNormalized = UsefulMethods.Remap(diff, new MinMax(startPos.x, startPos.x * -1f), graphRange);

                graphPos.y += diffNormalized;
            }
        }

        DisplayShapes.DrawLine(graphPos3D, color);
    }
}
