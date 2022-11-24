using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//General class to grab objects with mouse and throw them around
public class Grabber
{
    //Data needed 
    private readonly Camera mainCamera;

    //The mesh we grab
    private IGrabbable grabbedBody = null;

    //Mesh grabbing data

    //When we have grabbed a mesh by using ray-triangle itersection we identify the closest vertex. The distance from camera to this vertex is constant so we can move it around without doing another ray-triangle itersection  
    private float distanceToGrabPos;

    //To give the mesh a velocity when we release it
    private Vector3 lastGrabPos;



    public Grabber(Camera mainCamera)
    {
        this.mainCamera = mainCamera;
    }



    public void StartGrab(List<IGrabbable> bodies)
    {
        if (grabbedBody != null)
        {
            return;
        }

        //A ray from the mouse into the scene
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        float maxDist = float.MaxValue;

        IGrabbable closestBody = null;

        CustomHit closestHit = default;

        foreach (IGrabbable body in bodies)
        {
            body.IsRayHittingBody(ray, out CustomHit hit);

            if (hit != null)
            {
                //Debug.Log("Ray hit");

                //Debug.Log(hit.distance);

                if (hit.distance < maxDist)
                {
                    closestBody = body;

                    maxDist = hit.distance;

                    closestHit = hit;
                }
            }
            else
            {
                //Debug.Log("Ray missed");
            }
        }

        if (closestBody != null)
        {
            grabbedBody = closestBody;
        
            //StartGrab is finding the closest vertex and setting it to the position where the ray hit the triangle
            closestBody.StartGrab(closestHit.location);

            lastGrabPos = closestHit.location;

            //distanceToGrabPos = (ray.origin - hit.location).magnitude;
            distanceToGrabPos = closestHit.distance;
        }
    }




    public void MoveGrab()
    {
        if (grabbedBody == null)
        {
            return;
        }

        //A ray from the mouse into the scene
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        Vector3 vertexPos = ray.origin + ray.direction * distanceToGrabPos;

        //Cache the old pos before we assign it
        lastGrabPos = grabbedBody.GetGrabbedPos();

        //Moved the vertex to the new pos
        grabbedBody.MoveGrabbed(vertexPos);
    }



    public void EndGrab()
    {
        if (grabbedBody == null)
        {
            return;
        }

        //Add a velocity to the ball

        //A ray from the mouse into the scene
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        Vector3 grabPos = ray.origin + ray.direction * distanceToGrabPos;

        float vel = (grabPos - lastGrabPos).magnitude / Time.deltaTime;
        
        Vector3 dir = (grabPos - lastGrabPos).normalized;

        grabbedBody.EndGrab(grabPos, dir * vel);

        grabbedBody = null;
    }
}
