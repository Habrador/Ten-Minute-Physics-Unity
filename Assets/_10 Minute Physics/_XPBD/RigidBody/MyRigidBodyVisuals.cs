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
        //The more detailed object (there can be multiple meshes connected to the simple mesh)
        public List<GameObject> rbDetailedObjs = new();
        public List<Transform> rbDetailedTrans = new();

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

            if (!this.showVisualObj)
            {
                foreach (Transform detailedObjTrans in rbDetailedTrans)
                {
                    detailedObjTrans.SetPositionAndRotation(pos, rot);
                }
            }
        }



        public void AddDetailedObject(GameObject detailedObj)
        {
            this.rbDetailedObjs.Add(detailedObj);
            this.rbDetailedTrans.Add(detailedObj.transform);
        }



        public void Dispose()
        {
            GameObject.Destroy(rbVisualObj);

            foreach (GameObject detailedObj in rbDetailedObjs)
            {
                GameObject.Destroy(detailedObj);
            }
        }
    }
}