using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace XPBD
{
    //The visual objects shwoing the rigidbody
    //Can maybe be seen as Unitys Gameobject
    public class MyRigidBodyVisuals
    {
        //The gameobject that represents this rigidbody
        //Can be seen as the collider
        public readonly GameObject rbVisualObj;
        //Faster to cache it
        private readonly Transform rbVisualTrans;
        //In future tutorials we add a more complicated mesh above this mesh
        //and we should be able to switch between them
        public bool showVisualObj = true;
        //The more detailed object
        public GameObject rbDetailedObj;
        public Transform rbDetailedTrans;

        //Get ID of the collider
        public int ID => rbVisualObj.GetInstanceID();



        public MyRigidBodyVisuals(GameObject rbVisualObj)
        {
            this.rbVisualObj = rbVisualObj;
            this.rbVisualTrans = rbVisualObj.transform;

            this.rbVisualObj.GetComponent<MeshRenderer>().material.color = UnityEngine.Color.white;
        }



        public void UpdateVisualObjects(Vector3 pos, Quaternion rot)
        {
            this.rbVisualTrans.SetPositionAndRotation(pos, rot);

            if (this.rbDetailedObj != null && !this.showVisualObj)
            {
                this.rbDetailedTrans.SetPositionAndRotation(pos, rot);
            }
        }



        public void SetDetailedObject(GameObject detailedObj)
        {
            this.rbDetailedObj = detailedObj;
            this.rbDetailedTrans = detailedObj.transform;
        }



        public void Dispose()
        {
            GameObject.Destroy(rbVisualObj);

            if (rbDetailedObj != null)
            {
                GameObject.Destroy(rbDetailedObj);
            }
        }
    }
}