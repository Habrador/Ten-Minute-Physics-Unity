using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TextCore.Text;
using XPBD;

//Based on "25 Joint simulation made simple"
//from https://matthias-research.github.io/pages/tenMinutePhysics/index.html
//Simulate joints with XPBD - Extended Position Based Dynamics
public class JointSimulationController : MonoBehaviour
{
    private XPBDPhysicsSimulator rbSimulator;

    //The scenes we can chose from
    private enum Scenes
    {
        BasicJoints,
        Steering,
        Pendulums
    }

    //The scene data is in json files with data for all meshes, joints, etc
    private readonly Dictionary<Scenes, string> jointScenes = new()
    {
        { Scenes.BasicJoints, "basicJoints.json" },
        { Scenes.Steering, "steering.json" },
        { Scenes.Pendulums, "pendulum.json" },
    };

    private SceneImporter sceneImporter;

    //Simulation settings
    //In this sim gravity is 9.81 - not 10
    private Vector3 gravity = new(0f, -9.81f, 0f);
    //Needed to calculate mass
    //private readonly float density = 1000f;
    //How many steps each FixedUpdate
    //Was 20 in tutorial
    private readonly int numSubSteps = 20;

    //Mouse interaction
    Interaction interaction;

    //Show simple or complicated meshes
    private bool showVisuals = true;



    private void Start()
    {
        //Default scene
        InitScene(Scenes.BasicJoints);

        this.interaction = new Interaction(Camera.main);
    }



    private void Update()
    {
        rbSimulator.MyUpdate();

        //Should maybe be in LateUpdate()???
        this.interaction.DragWithMouse(this.rbSimulator);
    }



    private void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;

        //rbSimulator.MyFixedUpdate(dt, numSubSteps);
    }



    private void OnGUI()
    {
        MainGUI();
    }



    //Create a new rb simulation envrionment with some objects we want to simulate
    private void InitScene(Scenes scene)
    {
        if (this.sceneImporter == null)
        {
            this.sceneImporter = new SceneImporter();
        }
    
        //Destroy previous scene
        if (this.rbSimulator != null)
        {
            this.rbSimulator.Dispose();
        }

        //Create a new rb simulator
        this.rbSimulator = new XPBDPhysicsSimulator(gravity);

        sceneImporter.LoadScene(jointScenes[scene], this.rbSimulator);
    }



    //Buttons to select which scene to simulate
    private void MainGUI()
    {
        GUILayout.BeginHorizontal("box");

        int fontSize = 20;

        RectOffset offset = new(5, 5, 5, 5);


        //Buttons
        GUIStyle buttonStyle = new(GUI.skin.button)
        {
            //buttonStyle.fontSize = 0; //To reset because fontSize is cached after you set it once 

            fontSize = fontSize,
            margin = offset
        };

        GUIStyle textStyle = new(GUI.skin.label)
        {
            fontSize = fontSize,
            margin = offset
        };

        GUILayout.Label("Scenes:", textStyle);

        if (GUILayout.Button($"Basic joints", buttonStyle))
        {
            InitScene(Scenes.BasicJoints);
        }
        if (GUILayout.Button("Steering", buttonStyle))
        {
            InitScene(Scenes.Steering);
        }
        if (GUILayout.Button("Pendulums", buttonStyle))
        {
            InitScene(Scenes.Pendulums);
        }

        GUILayout.Label("Settings:", textStyle);

        if (GUILayout.Button("Toggle View", buttonStyle))
        {
            showVisuals = !showVisuals;

            List<MyRigidBody> allRbs = rbSimulator.allRigidBodies;

            foreach (MyRigidBody rb in allRbs)
            {
                rb.ShowSimulationView(showVisuals);
            }
        }

        GUILayout.EndHorizontal();
    }


}
