using System.Collections;
using System.Collections.Generic;
using UnityEngine;



namespace XPBD
{
    //Interact with the physics simulator by dragging the rbs around with the mouse
    public class Interaction
    {
        //Distance from position we hit to mouse position
        private float d;
        private readonly Camera thisCamera;
        public readonly float dragCompliance = 0.001f;



        public Interaction(Camera camera)
        {
            this.thisCamera = camera;
        }



        //Have to pass simulator as parameter because we create a new one when we switch scenes
        public void DragWithMouse(XPBDPhysicsSimulator rbSimulator)
        {
            bool hasSelectedRb = rbSimulator.dragConstraint != null;
        
            //Try to select rb
            if (Input.GetMouseButtonDown(0) && hasSelectedRb == false)
            {
                TryStartDrag(rbSimulator);
            }

            //Drag selected rb
            else if (!Input.GetMouseButtonUp(0) && hasSelectedRb == true)
            {
                Drag(rbSimulator);
            }

            //Deselect rb
            if (Input.GetMouseButtonUp(0) && hasSelectedRb == true)
            {
                EndDrag(rbSimulator);
            }
        }



        public void TryStartDrag(XPBDPhysicsSimulator rbSimulator)
        {
            //Fire ray from camera and see if we hit one of the rbs
            //We can use Unity's native collision system for this so we dont have to make our own
            //Make sure the physics objects have colliders!
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
                    if (thisRb.visualObjects.ID == id)
                    {
                        //Debug.Log("Identified the rb");

                        this.d = hit.distance;

                        Vector3 hitPos = hit.point;

                        //Create a distance constraint
                        //TODO: this is some default parameter in the original code and doesnt say what it is in this section, so might be true or false
                        bool unilateral = false;

                        rbSimulator.dragConstraint = new DistanceConstraint(thisRb, null, hitPos, hitPos, 0f, this.dragCompliance, unilateral);

                        break;
                    }
                }
            }
        }



        public void Drag(XPBDPhysicsSimulator rbSimulator)
        {
            //On mouse move -> update p by using distance d and new mouse ray
            Ray ray = thisCamera.ScreenPointToRay(Input.mousePosition);

            //Update p_m using d
            Vector3 newPos = ray.origin + ray.direction * this.d;

            if (rbSimulator.dragConstraint != null)
            {
                rbSimulator.dragConstraint.worldPos1 = newPos;
            }

            //Vector3 p0 = rbSimulator.dragConstraint.worldPos0;

            //Debug.DrawLine(p0, newPos, UnityEngine.Color.blue);
        }



        public void EndDrag(XPBDPhysicsSimulator rbSimulator)
        {
            if (rbSimulator.dragConstraint != null)
            {
                rbSimulator.dragConstraint.Dispose();
                rbSimulator.dragConstraint = null;
            }
        }
    }
}