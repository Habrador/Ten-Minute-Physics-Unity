using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace XPBD
{
    public class Interaction
    {
        private bool hasSelectedRb = false;
        private float d;
        private readonly Camera thisCamera;

        private readonly XPBDPhysicsSimulator rbSimulator;



        public Interaction(Camera camera, XPBDPhysicsSimulator rbSimulator)
        {
            this.thisCamera = camera;
            this.rbSimulator = rbSimulator;
        }



        public void DragWithMouse()
        {
            //Try to select rb
            if (Input.GetMouseButtonDown(0) && hasSelectedRb == false)
            {
                //Raycasting
                Ray ray = thisCamera.ScreenPointToRay(Input.mousePosition);

                //If we hit a collider
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    //Each gameobject in Unity has a unique identifer which we can use to find it among the rb we simulate
                    int id = hit.transform.gameObject.GetInstanceID();

                    //Debug.Log(hit.transform.gameObject.GetInstanceID());
                    //Debug.Log("hit");

                    //Find the rigidbody with this id in the list of all rbs in the simulator
                    List<MyRigidBody> allRigidBodies = rbSimulator.allRigidBodies;

                    foreach (MyRigidBody thisRb in allRigidBodies)
                    {
                        //If the ids match
                        if (thisRb.rbVisualObj.GetInstanceID() == id)
                        {
                            //Debug.Log("Identified the rb");

                            //Data

                            //p_m - position where ray intersects with the collider
                            Vector3 p = hit.point;

                            //d - distance from position we hit to mouse
                            this.d = hit.distance;

                            //Create a distance constraint
                            rbSimulator.StartDrag(thisRb, hit.point);

                            hasSelectedRb = true;

                            break;
                        }
                    }
                }
            }



            //Drag selected rb
            if (hasSelectedRb == true)
            {
                //On mouse move -> update p by using distance d and new mouse ray
                Ray ray = thisCamera.ScreenPointToRay(Input.mousePosition);

                //Update p_m using d
                Vector3 p_m = ray.origin + ray.direction * d;

                rbSimulator.Drag(p_m);

                //Vector3 p0 = rbSimulator.dragConstraint.worldPos0;

                //Debug.DrawLine(p0, p_m, UnityEngine.Color.blue);
            }



            //Deselect rb
            if (Input.GetMouseButtonUp(0) && hasSelectedRb == true)
            {
                rbSimulator.EndDrag();

                hasSelectedRb = false;
            }
        }
    }
}