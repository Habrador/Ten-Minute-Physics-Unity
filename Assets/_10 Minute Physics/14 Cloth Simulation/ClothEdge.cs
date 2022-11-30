using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Help struct to easier find neighboring edges
public struct ClothEdge
{
    public int id0, id1;

    public int edgeNr;

    public ClothEdge(int id0, int id1, int edgeNr)
    {
        this.id0 = id0;
        this.id1 = id1;
        this.edgeNr = edgeNr;
    }
}
