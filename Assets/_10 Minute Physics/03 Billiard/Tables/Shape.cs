using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Some shape defined by edges
//The coordinates of these edges should be children to this GO
public class Shape : BilliardTable
{
    private List<Vector3> edges;



    public override void Init()
    {
        this.edges = GetEdges();
    }



    public override bool HandleBallEnvironmentCollision(Ball ball, float restitution = 1)
    {
        bool isColliding = BallCollisionHandling.HandleBallWallEdgesCollision(ball, edges, restitution);

        return isColliding;
    }



    public override bool IsBallOutsideOfTable(Vector3 ballPos, float ballRadius)
    {
        return false;
    }



    private List<Vector3> GetEdges()
    {
        List<Vector3> children = new();
        List<bool> isFixed = new();

        foreach (Transform child in this.transform)
        {
            children.Add(child.position);

            if (child.GetComponent<IsFixed>() != null)
            {
                isFixed.Add(true);
            }
            else
            {
                isFixed.Add(false);
            }
        }

        //Add new points between the old ones so we can smooth
        List<Vector3> extraChildren = new();
        List<bool> extraIsFixed = new();

        for (int i = 0; i < children.Count; i++)
        {
            extraChildren.Add(children[i]);
            extraIsFixed.Add(isFixed[i]);

            int iPlusOne = UsefulMethods.ClampListIndex(i + 1, children.Count);

            Vector3 posExtra = (children[i] + children[iPlusOne]) * 0.5f;

            extraChildren.Add(posExtra);
            extraIsFixed.Add(false);
        }

        //Smooth
        List<Vector3> smoothedCoordinates = new List<Vector3>();

        for (int i = 0; i < extraChildren.Count; i++)
        {
            if (extraIsFixed[i])
            {
                smoothedCoordinates.Add(extraChildren[i]);

                continue;
            }

            int prevIndex = UsefulMethods.ClampListIndex(i - 1, extraChildren.Count);
            int nextIndex = UsefulMethods.ClampListIndex(i + 1, extraChildren.Count);

            Vector3 smoothedChild = (extraChildren[prevIndex] + extraChildren[i] + extraChildren[nextIndex]) / 3f;

            smoothedCoordinates.Add(smoothedChild);
        }

        return smoothedCoordinates;
    }



    private void LateUpdate()
    {
        if (edges == null)
        {
            return;
        }
        
        //DisplayShapes.DrawLine(edges, DisplayShapes.ColorOptions.White);
    }



    private void OnDrawGizmos()
    {
        /*
        List<Vector3> smoothedCoordinates = GetEdges();

        Gizmos.color = Color.white;

        for (int i = 1; i < smoothedCoordinates.Count; i++)
        {
            Gizmos.DrawLine(smoothedCoordinates[i - 1], smoothedCoordinates[i]);
        }

        Gizmos.DrawLine(smoothedCoordinates[^1], smoothedCoordinates[0]);
        */
    }



    public override bool IsBallInHole(Ball ball)
    {
        return false;
    }

    public override void MyUpdate()
    {
        //throw new System.NotImplementedException();
    }
}
