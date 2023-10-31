using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//General class to interpolate a grid
//The values we want to interpolate can be on:
//- The center of the cell
//- In the middle of the vertical lines (staggered grid)
//- In the middle of the horizontal lines (staggered grid)
public class GridInterpolation
{
    //Derivation of how to find the linear interpolation of P by using A, B, C, D and their respective coordinates
    //This is a square with side length h (I did my best)
    // C------D
    // |      |
    // |___P  |
    // |   |  |
    // A------B
    // The points have the coordinates
    // A: (x0, y0)
    // B: (x1, y0)
    // C: (x0, y1)
    // D: (x1, y1)
    // P: (xp, yp)
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
    // tx = (xp - x0) / (x1 - x0) = (xp - x0) / h = deltaX / h
    // ty = (yp - y0) / (y1 - y0) = (yp - y0) / h = deltaY / h
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
}
