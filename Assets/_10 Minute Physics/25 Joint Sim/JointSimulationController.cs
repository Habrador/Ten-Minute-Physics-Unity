using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
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
    //json files can be found on the 10 minute physics github
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
    //How many steps each FixedUpdate
    //Was 20 in tutorial
    private readonly int numSubSteps = 20;
    //Tutorial is using dt = 0.03333; // 30 FPS
    //Default Unity dt in FixedUpdate is 0.02
    //which corresponds to 50 fixed updates per second
    //Time.fixedDeltaTime = 0.03333f;

    //Mouse interaction
    Interaction interaction;

    //Show simple or complicated meshes
    private bool showVisuals = true;
    //To control the joints (tutorial is using a touch control) but we shall use sliders
    private Vector2 controlVector;
    private Vector2 controlVelocity;



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
        UpdateControl();
    
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



    //Control motors and servers with sliders
    private void UpdateControl()
    {
        //Update control vector with velocity

        //Adjust this value to control return speed
        //float returnSpeed = 5f;
        
        //this.controlVelocity.x = -this.controlVector.x * returnSpeed;
        //this.controlVelocity.y = -this.controlVector.y * returnSpeed;

        //this.controlVector += this.controlVelocity * Time.deltaTime;

        //Apply control to motors and servos
        List<MyJoint> allJoints = rbSimulator.allJoints;

        for (int i = 0; i < allJoints.Count; i++)
        {
            MyJoint joint = allJoints[i];
            
            if (joint.Type() == MyJointSettings.Types.Motor)
            {
                //Scale factor for motor speed
                joint.settings.velocity = this.controlVector.y * 5f;
            }
            else if (joint.Type() == MyJointSettings.Types.Servo)
            {
                //Scale factor for steering angle
                joint.settings.targetAngle = this.controlVector.x * Mathf.PI / 4f;
            }
            else if (joint.Type() == MyJointSettings.Types.Cylinder)
            {
                //Scale factor for offset
                joint.settings.targetDistance = -this.controlVector.y * 0.1f;
            }
        }
    }



    //Buttons to select which scene to simulate and some settings
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

        //Show the detailed mesh och show the simple rigid bodies and the debug objects?
        if (GUILayout.Button("Toggle View", buttonStyle))
        {
            showVisuals = !showVisuals;

            List<MyRigidBody> allRbs = rbSimulator.allRigidBodies;

            foreach (MyRigidBody rb in allRbs)
            {
                rb.ShowSimulationView(showVisuals);
            }

            List<MyJoint> allJoints = rbSimulator.allJoints;
            
            foreach (MyJoint joint in allJoints)
            {
                joint.SetVisible(!showVisuals);
            }
        }

        GUILayout.EndHorizontal();


        GUILayout.BeginHorizontal("box");

        GUILayout.Label("Settings x:", textStyle); 
        
        controlVector.x = EditorGUILayout.Slider(controlVector.x, -1f, 1f); 

        GUILayout.EndHorizontal();


        GUILayout.BeginHorizontal("box");

        GUILayout.Label("Settings y:", textStyle);

        controlVector.y = EditorGUILayout.Slider(controlVector.y, -1f, 1f);

        GUILayout.EndHorizontal();
    }
}
