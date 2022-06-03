using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct Edge
{
    public int idA, idB, newTetNr, unknown;

    public Edge(int idA, int idB, int newTetNr, int unknown)
    {
        this.idA = idA;
        this.idB = idB;
        this.newTetNr = newTetNr;
        this.unknown = unknown;
    }
}
