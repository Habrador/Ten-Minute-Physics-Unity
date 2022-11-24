using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IGrabbable
{
    public void StartGrab(Vector3 grabPos);

    public void MoveGrabbed(Vector3 grabPos);

    public void EndGrab(Vector3 grabPos, Vector3 vel);

    public void IsRayHittingBody(Ray ray, out CustomHit hit);
}
