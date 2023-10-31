using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//General class to interpolate a grid
//The values we want to interpolate can be on:
//- The center of the cell
//- In the middle of the vertical lines (staggered grid)
//- In the middle of the horizontal lines (staggered grid)
public static class GridInterpolation
{
    //Derivation of how to find the linear interpolation of P by using A, B, C, D and their respective coordinates
    //This is a square with side length h (I did my best)
    // C------D
    // |      |
    // |___P  |
    // |   |  |
    // A------B
    // The points have the coordinates
    // A: (xA, yA)
    // B: (xB, yB)
    // C: (xC, yC)
    // D: (xD, yD)
    // P: (xP, yP)
    //
    //We need to do 3 linear interpolations to find P:
    // P_AB = (1 - tx) * A + tx * B
    // P_CD = (1 - tx) * C + tx * D
    // P = (1 - ty) * P_AB + ty * P_CD
    //
    //Insert P_AB and P_CD into P and we get: 
    // P = (1 - ty) * [(1 - tx) * A + tx * B] + ty * [(1 - tx) * C + tx * D] 
    // P = (1 - tx) * (1 - ty) * A + tx * (1 - ty) * B + (1 - tx) * ty * C + tx * ty * D
    //
    //t is a parameter in the range [0, 1]. If tx = 0 we get A or if tx = 1 we get B in the P_AB case
    //The parameter can be written as:
    // tx = (xp - xA) / (xB - xA) = (xP - xA) / h = deltaX / h
    // ty = (yp - yA) / (yB - yA) = (yP - yA) / h = deltaY / h
    //
    //Define:
    // sx = 1 - tx
    // sy = 1 - ty
    //
    //And we get the following:
    // P = sx * sy * A + tx * sy * B + sx * ty * C + tx * ty * D
    //
    //Simplify the weights:
    // wA = sx * sy
    // wB = tx * sy
    // wC = sx * ty
    // wD = tx * ty
    //
    //Note that: wA + wB + wC + wD = 1
    //
    //The final iterpolation:
    // P = wA * A + wB * B + wC * C + wD * D 
    //
    //In simple code (which is slightly slower than the above because we do some calculations multiple times but easy to understand):
    //float tx = math.unlerp(x0, x1, xp); //Similar to Mathf.InverseLerp()
    //float ty = math.unlerp(y0, y1, yp);
    //float P_AB = math.lerp(A, B, tx); //Similar to Mathf.Lerp()
    //float P_CD = math.lerp(C, D, tx);
    //float P = math.lerp(P_AB, P_CD, ty);
    public static void GetWeights(
        float xP, float yP, 
        float xA, float yA, 
        float one_over_h, 
        out float wA, out float wB, out float wC, out float wD)
    {
        float deltaX = xP - xA;
        float deltaY = yP - yA;

        float tx = deltaX * one_over_h;
        float ty = deltaY * one_over_h;

        float sx = 1 - tx;
        float sy = 1 - ty;

        wA = sx * sy;
        wB = tx * sy;
        wC = sx * ty;
        wD = tx * ty;
    }



    //Clamp the iterpolation point so we know we can interpolate from 4 grid points
    public static void ClampInterpolationPoint()
    {

    }

}
