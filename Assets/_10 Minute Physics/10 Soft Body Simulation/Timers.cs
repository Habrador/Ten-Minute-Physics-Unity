using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Timers
{
    //Use ElapsedTicks because better than milliseconds

    public static long preSolve;

    public static long constraints;

    public static long postSolve;

    public static long wTimesGrad;

    public static long volume;

    public static long move;
    

    public static void Display()
    {
        //Debug.Log($"pre: {preSolve}, constraints: {constraints}, postSolve: {postSolve}");

        Debug.Log($"grad: {wTimesGrad}, volume: {volume}, move: {move}");
    }

    public static void Reset()
    {
        preSolve = 0;
        constraints = 0;
        postSolve = 0;

        wTimesGrad = 0;
        volume = 0;
        move = 0;
    }
}
