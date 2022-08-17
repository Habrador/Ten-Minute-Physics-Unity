using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Base class for billiard tables
public abstract class Table : MonoBehaviour
{
    public abstract void Init();

    public abstract void HandleBallCollision(Ball ball, float restitution);

    public abstract bool IsBallOutsideOfTable(Vector3 ballPos, float ballRadius);
}
