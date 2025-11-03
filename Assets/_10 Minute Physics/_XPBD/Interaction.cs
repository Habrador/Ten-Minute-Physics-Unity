using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace XPBD
{
    //Interact with the physics simulator by dragging the rbs around with the mouse
    public class Interaction
    {
        private bool hasSelectedRb = false;
        //Distance from position we hit to mouse position
        private float d;
        private readonly Camera thisCamera;



        public Interaction(Camera camera)
        {
            this.thisCamera = camera;
        }



        //Have to pass simulator as parameter because we create a new one when we switch scenes
        public void DragWithMouse(XPBDPhysicsSimulator rbSimulator)
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
                        //Debug.Log(thisRb.rbVisualObj.GetInstanceID());
                    
                        //If the ids match
                        if (thisRb.rbVisualObj.GetInstanceID() == id)
                        {
                            //Debug.Log("Identified the rb");

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
            else if (!Input.GetMouseButtonUp(0) && hasSelectedRb == true)
            {
                //On mouse move -> update p by using distance d and new mouse ray
                Ray ray = thisCamera.ScreenPointToRay(Input.mousePosition);

                //Update p_m using d
                Vector3 p_m = ray.origin + ray.direction * this.d;

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