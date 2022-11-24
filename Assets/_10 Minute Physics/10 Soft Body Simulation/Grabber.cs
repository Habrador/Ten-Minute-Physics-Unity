using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Grabber
{
    //Data needed 
    private readonly Camera mainCamera;

    //The mesh we grab
    private SoftBodySimulation grabbedSoftBody = null;

    //Mesh grabbing data

    //When we have grabbed a mesh by using ray-triangle itersection we identify the closest vertex. The distance from camera to this vertex is constant so we can move it around without doing another ray-triangle itersection  
    private float distanceToVertex;

    //To give the mesh a velocity when we release it
    private Vector3 lastVertexPos;



    public Grabber(Camera mainCamera)
    {
        this.mainCamera = mainCamera;
    }



    public void StartGrab(SoftBodySimulation softBody)
    {
        if (grabbedSoftBody != null)
        {
            return;
        }

        //A ray from the mouse into the scene
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        //Find if the ray hit a triangle in the mesh
        CustomPhysicsRaycast(ray, out CustomHit hit, softBody);

        if (hit != null)
        {
            //Debug.Log("Ray hit");

            grabbedSoftBody = softBody;

            //StartGrab is finding the closest vertex and setting it to the position where the ray hit the triangle
            grabbedSoftBody.StartGrab(hit.location);

            lastVertexPos = hit.location;

            distanceToVertex = (ray.origin - hit.location).magnitude;
        }
        else
        {
            //Debug.Log("Ray missed");
        }
    }




    public void MoveGrab()
    {
        if (grabbedSoftBody == null)
        {
            return;
        }

        //A ray from the mouse into the scene
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        Vector3 vertexPos = ray.origin + ray.direction * distanceToVertex;

        lastVertexPos = vertexPos;

        //Moved the vertex to the new pos
        grabbedSoftBody.MoveGrabbed(vertexPos);
    }



    public void EndGrab()
    {
        if (grabbedSoftBody == null)
        {
            return;
        }

        //Add a velocity to the ball

        //A ray from the mouse into the scene
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        Vector3 vertexPos = ray.origin + ray.direction * distanceToVertex;

        float vel = (vertexPos - lastVertexPos).magnitude / Time.deltaTime;

        Vector3 dir = (vertexPos - lastVertexPos).normalized;

        grabbedSoftBody.EndGrab(dir * vel);

        grabbedSoftBody = null;
    }



    //Cant use Physics.Raycast because it requires a mesh collider
    private void CustomPhysicsRaycast(Ray ray, out CustomHit hit, SoftBodySimulation softBody)
    {
        hit = null;

        List<Vector3> vertices = softBody.GetMeshVertices;

        int[] triangles = softBody.GetMeshTriangles; 
    }
}
