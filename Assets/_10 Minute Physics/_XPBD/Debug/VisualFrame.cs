using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace XPBD
{
    //A coordinate axis where each axis is represented by a cylinder with rgb color
    class VisualFrame
    {
        private readonly GameObject xObj;
        private readonly GameObject yObj;
        private readonly GameObject zObj;



        public VisualFrame(float width, float size = 0.1f)
        {
            //Create axis line objects
            xObj = CreateAxis(width, size, UnityEngine.Color.red);
            yObj = CreateAxis(width, size, UnityEngine.Color.green);
            zObj = CreateAxis(width, size, UnityEngine.Color.blue);
        }



        //Create the cylinder
        private GameObject CreateAxis(float width, float size, UnityEngine.Color color)
        {
            //Create a line representing an axis
            GameObject line = GameObject.CreatePrimitive(PrimitiveType.Cylinder);

            line.transform.localScale = new Vector3(width, width, size);

            line.GetComponent<Renderer>().material.color = color;

            //Position the cylinder so it starts at the origin and extends in positive direction
            line.transform.Translate(new Vector3(0f, size / 2f, 0f));

            return line;
        }



        public void UpdateMesh(Vector3 pos, Quaternion rot)
        {
            //Extract basis vectors from quaternion
            Vector3 xAxis = rot * new Vector3(1f, 0f, 0f);
            Vector3 yAxis = rot * new Vector3(0f, 1f, 0f);
            Vector3 zAxis = rot * new Vector3(0f, 0f, 1f);

            //Calculate the rotations
            Quaternion xRot = Quaternion.FromToRotation(new Vector3(0f, 1f, 0f), xAxis.normalized);
            Quaternion yRot = Quaternion.FromToRotation(new Vector3(0f, 1f, 0f), yAxis.normalized);
            Quaternion zRot = Quaternion.FromToRotation(new Vector3(0f, 1f, 0f), zAxis.normalized);

            this.xObj.transform.SetPositionAndRotation(pos, xRot);
            this.yObj.transform.SetPositionAndRotation(pos, yRot);
            this.zObj.transform.SetPositionAndRotation(pos, zRot);
        }



        //Show/hide the coordinate axis
        public void SetVisible(bool visible)
        {
            this.xObj.SetActive(visible);
            this.yObj.SetActive(visible);
            this.zObj.SetActive(visible);
        }
    }
}