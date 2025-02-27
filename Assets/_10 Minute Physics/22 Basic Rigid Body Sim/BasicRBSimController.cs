using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

//Based on "22 How to write a basic rigid body simulator using position based dynamics"
//from https://matthias-research.github.io/pages/tenMinutePhysics/index.html
public class BasicRBSimController : MonoBehaviour
{
    private Vector3 gravity;

    //private RigidbodySimulator rbSimulator;

    private float density;


    private void Start()
    {

    }

    private void InitScene(int nr)
    {
        this.gravity = new Vector3(0f, -10.0f, 0f);

        //let timeStepSize = parseFloat(document.getElementById("timeStep").value);

        //gSimulator = new RigidBodySimulator(gThreeScene, timeStepSize, gravity);

        this.density = 1000.0f;

        if (nr == 0)
        {
            bool unilateral = true;
            float compliance = 0f;

            float length = 0.9f;
            float thickness = 0.04f;
            float height = 0.3f;
            float baseRadius = 0.08f;
            float distance = 0.5f * length - thickness;

            Vector3 barSize = new Vector3(length, thickness, thickness);
            Vector3 angles = new Vector3(0.0f, 0.0f, 0.0f);
            Vector3 barPos = new Vector3(0.0f, 2.5f, 0.0f);

            //Build the tree of rigidbodies
            MyRigidBody prevBar = null;

            int numLevels = 5;

            float volBar = length * thickness * thickness;
            float volTree = 2.0f * 4.0f / 3.0f * Mathf.PI * baseRadius * baseRadius * baseRadius + volBar;

            List<float> radii = new List<float>() { baseRadius };

            for (int i = 1; i < numLevels; i++)
            {
                // sphere of volume volTree
                float radius = Mathf.Pow(3.0f / 4.0f / Mathf.PI * volTree, 1.0f / 3.0f);
                //Push
                radii.Add(radius);
                volTree = 2.0f * volTree + volBar;
            }

            for (int i = 0; i < numLevels; i++)
            {
                float radius = radii[numLevels - i - 1];
                MyRigidBody bar = new MyRigidBody(MyRigidBody.Types.Box, barSize, density, barPos, angles);
                //gSimulator.addRigidBody(bar);

                Vector3 p0 = new Vector3(barPos.x, barPos.y + 0.5f * thickness, barPos.z);
                Vector3 p1 = new Vector3(barPos.x, barPos.y + height - 0.5f * thickness, barPos.z);
                //let barConstraint = new DistanceConstraint(gThreeScene, bar, prevBar, p0, p1, height - thickness, compliance, unilateral);
                //gSimulator.addDistanceConstraint(barConstraint);

                Vector3 spherePos = new Vector3(barPos.x + distance, barPos.y - height, barPos.z);
                Vector3 sphereSize = new Vector3(radius, radius, radius);
                MyRigidBody sphere = new MyRigidBody(MyRigidBody.Types.Sphere, sphereSize, density, spherePos, angles);
                //gSimulator.addRigidBody(sphere);

                //Vector3 p0 = new Vector3(spherePos.x, spherePos.y + 0.5f * radius, spherePos.z);
                //Vector3 p1 = new Vector3(spherePos.x, spherePos.y + height - 0.5f * thickness, spherePos.z);
                //let sphereConstraint = new DistanceConstraint(gThreeScene, sphere, bar, p0, p1, height - thickness, compliance, unilateral);
                //gSimulator.addDistanceConstraint(sphereConstraint);

                if (i == numLevels - 1)
                {
                    spherePos.x -= 2.0f * distance;
                    sphere = new MyRigidBody(MyRigidBody.Types.Sphere, sphereSize, density, spherePos, angles);
                    //gSimulator.addRigidBody(sphere);
                    p0.x -= 2.0f * distance;
                    p1.x -= 2.0f * distance;
                    //sphereConstraint = new DistanceConstraint(gThreeScene, sphere, bar, p0, p1, height - thickness, compliance, unilateral);
                    //gSimulator.addDistanceConstraint(sphereConstraint);
                }

                prevBar = bar;
                barPos.y -= height;
                barPos.x -= distance;
            }
        }
        //Boxes hanging from the roof connected to each other by a rope
        else
        {
            bool unilateral = false;
            float compliance = 0.001f;

            Vector3 boxSize = new Vector3(0.1f, 0.1f, 0.1f);
            Vector3 boxPos = new Vector3(0.0f, 2.5f, 0.0f);
            Vector3 boxAngles = new Vector3(0.0f, 0.0f, 0.0f);

            float width = 0.01f;
            float fontSize = 0.03f;
            float dist = 0.2f;
            float prevY = 2.5f;
            float prevSize = 0.0f;

            MyRigidBody prevBox = null;

            for (int level = 0; level < 4; level++)
            {
                prevY = boxPos.y;
                boxPos.y -= dist + boxSize.y;
                MyRigidBody box = new MyRigidBody(MyRigidBody.Types.Box, boxSize, density, boxPos, boxAngles, fontSize);
                box.damping = 5.0f;
                //gSimulator.addRigidBody(box);

                Vector3 p0 = new Vector3(0.4f * prevSize, boxPos.y + 0.5f * boxSize.y, 0.0f);
                Vector3 p1 = new Vector3(0.4f * prevSize, prevY - 0.5f * prevSize, 0.0f);
                //let barConstraint = new DistanceConstraint(gThreeScene, box, prevBox, p0, p1, p1.y - p0.y, compliance, unilateral, width, fontSize);
                //gSimulator.addDistanceConstraint(barConstraint);

                prevBox = box;
                prevSize = boxSize.y;
                //boxSize.multiplyScalar(Math.cbrt(2.0));
            }
        }

    }

    private float GetRandom(float min, float max)
    {
        return min + Random.value * (max - min);
    }
    
}