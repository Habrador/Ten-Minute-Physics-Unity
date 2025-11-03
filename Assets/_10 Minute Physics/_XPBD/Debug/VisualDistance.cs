using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace XPBD
{
    //Display the distance between two points with a red cylinder
    class VisualDistance
    {
        private readonly GameObject cylinderObj;
        private readonly Transform cylinderTrans;

        public Vector3 Pos => cylinderTrans.position;



        //Default color doesnt work...
        public VisualDistance(UnityEngine.Color color, float width = 0.01f)
        {
            //Create a cylinder for visualization
            this.cylinderObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);

            this.cylinderObj.transform.localScale = new Vector3(width, 1f, width);

            this.cylinderObj.GetComponent<Renderer>().material.color = color;

            //Remove collider because the constraint is not part of the raycasting
            this.cylinderObj.GetComponent<Collider>().enabled = false;

            this.cylinderTrans = this.cylinderObj.transform;
        }



        //Rotate and scale the cylinder so it goes between two positions
        public float UpdateMesh(Vector3 startPos, Vector3 endPos)
        {
            //Calculate the center point
            Vector3 center = (startPos + endPos) * 0.5f;

            //Calculate the direction vector
            Vector3 direction = endPos - startPos;

            float length = direction.magnitude;

            //Create a rotation quaternion
            Quaternion quaternion = Quaternion.FromToRotation(new Vector3(0f, 1f, 0f), direction.normalized);

            //Update cylinder's transformation
            this.cylinderTrans.transform.SetPositionAndRotation(center, quaternion);

            Vector3 currentScale = this.cylinderTrans.transform.localScale;

            currentScale.y = length * 0.5f;

            this.cylinderTrans.transform.localScale = currentScale;

            return length;
        }



        public void SetVisible(bool visible)
        {
            this.cylinderObj.SetActive(visible);
        }



        public void Dispose()
        {
            if (this.cylinderObj)
            {
                GameObject.Destroy(this.cylinderObj);
            }
        }
    }
}