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

            //Set scale
            line.transform.localScale = new Vector3(width, size / 4f, width);

            //Set color
            line.GetComponent<Renderer>().material.color = color;

            //Position the cylinder so it starts at the origin and extends in positive direction
            //In our case we have to offset all vertices in the mesh
            Mesh cylinderMesh = line.GetComponent<MeshFilter>().mesh;

            Vector3[] vertices = cylinderMesh.vertices;

            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i] += new Vector3(0f, 1f, 0f);
            }

            cylinderMesh.vertices = vertices;

            line.GetComponent<MeshFilter>().mesh = cylinderMesh;

            //Deactivate collider
            line.GetComponent<Collider>().enabled = false;


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

            //Add pos and rot 
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



        public void Dispose()
        {
            GameObject.Destroy(this.xObj);
            GameObject.Destroy(this.yObj);
            GameObject.Destroy(this.zObj);
        }
    }
}