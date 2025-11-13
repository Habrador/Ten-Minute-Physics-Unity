using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace XPBD
{
    //A coordinate axis
    class VisualFrame
    {
        private GameObject xObj;
        private GameObject yObj;
        private GameObject zObj;

        public VisualFrame(float width, float size = 0.1f)
        {
            //Create axis line objects
            xObj = CreateAxis(width, size, UnityEngine.Color.red);
            yObj = CreateAxis(width, size, UnityEngine.Color.green);
            zObj = CreateAxis(width, size, UnityEngine.Color.blue);
        }



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

            //Update X axis (red)
            Quaternion xRot = Quaternion.FromToRotation(new Vector3(0f, 1f, 0f), xAxis.normalized);

            this.xObj.transform.SetPositionAndRotation(pos, xRot);

            //Update Y axis (green)
            Quaternion yRot = Quaternion.FromToRotation(new Vector3(0f, 1f, 0f), yAxis.normalized);

            this.yObj.transform.SetPositionAndRotation(pos, yRot);

            //Update Z axis (blue)
            Quaternion zRot = Quaternion.FromToRotation(new Vector3(0f, 1f, 0f), zAxis.normalized);

            this.zObj.transform.SetPositionAndRotation(pos, zRot);
        }



        public void SetVisible(bool visible)
        {
            this.xObj.SetActive(visible);
            this.yObj.SetActive(visible);
            this.zObj.SetActive(visible);
        }
    }
}