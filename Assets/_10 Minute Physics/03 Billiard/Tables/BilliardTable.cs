using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Base class for billiard tables
public abstract class BilliardTable : MonoBehaviour
{
    public abstract void Init();

    public abstract void HandleBallCollision(Ball ball, float restitution = 1f);

    public abstract bool IsBallOutsideOfTable(Vector3 ballPos, float ballRadius);

    public abstract bool IsBallInHole(Ball ball);
}
