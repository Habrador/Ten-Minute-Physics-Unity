using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActualBilliardTable : BilliardTable
{
    public Transform borderEdgesParent;

    public Transform innerDimensionsParent;

    public Transform bigHolesParent;
    public Transform smallHolesParent;

    //The diameter of a pool ball is 57 mm
    private readonly float ballRadius = 0.0285f;

    //The holes are roughly twice the size
    private readonly float bigHoleRadius = 0.08f;
    private readonly float smallHoleRadius = 0.06f;

    public override void Init()
    {
        throw new System.NotImplementedException();
    }

    public override bool HandleBallCollision(Ball ball, float restitution = 1)
    {
        throw new System.NotImplementedException();
    }

    public override bool IsBallInHole(Ball ball)
    {
        throw new System.NotImplementedException();
    }

    public override bool IsBallOutsideOfTable(Vector3 ballPos, float ballRadius)
    {
        throw new System.NotImplementedException();
    }



    private void OnDrawGizmos()
    {
        //Display the outline
        DisplayEdgesFromChildrent(borderEdgesParent);

        //DisplayEdgesFromChildrent(innerDimensionsParent);



        //Display the holes
        Gizmos.color = Color.black;

        Debug.Log(bigHoleRadius);

        foreach (Transform child in bigHolesParent)
        {
            Gizmos.DrawSphere(child.position, bigHoleRadius);
        }

        foreach (Transform child in smallHolesParent)
        {
            Gizmos.DrawSphere(child.position, smallHoleRadius);
        }
    }



    private void DisplayEdgesFromChildrent(Transform parent)
    {
        if (parent == null)
        {
            return;
        }
    
        List<Vector3> verts = new();

        foreach (Transform child in parent)
        {
            verts.Add(child.position);
        }


        Gizmos.color = Color.white;

        for (int i = 1; i < verts.Count; i++)
        {
            Gizmos.DrawLine(verts[i - 1], verts[i]);
        }

        Gizmos.DrawLine(verts[^1], verts[0]);
    }
}
