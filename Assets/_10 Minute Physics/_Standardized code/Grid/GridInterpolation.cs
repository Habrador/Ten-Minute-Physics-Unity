using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static EulerianFluidSimulator.FluidSim;

//General class to interpolate a grid
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
    //float tx = Mathf.InverseLerp(xA, xB, xP);
    //float ty = Mathf.InverseLerp(yA, yB, yP);
    //float P_AB = Mathf.Lerp(A, B, tx); 
    //float P_CD = Mathf.Lerp(C, D, tx);
    //float P = Mathf.Lerp(P_AB, P_CD, ty);
    public static void GetWeights(
        float xP, float yP,
        float xA, float yA,
        GridConstants gridData,
        out float wA, out float wB, out float wC, out float wD)
    {
        float deltaX = xP - xA;
        float deltaY = yP - yA;

        float tx = deltaX * gridData.one_over_h;
        float ty = deltaY * gridData.one_over_h;

        float sx = 1 - tx;
        float sy = 1 - ty;

        wA = sx * sy;
        wB = tx * sy;
        wC = sx * ty;
        wD = tx * ty;
    }



    //3x3 Staggered grid (c is not staggered)
    //0,0 is bottom left
    //Notice there's no u on the vertical line to the right of the last cell and no v at the top
    //+-----+-----+-----+
    //|     |     |     |
    //u  c  u  c  u  c  |
    //|     |     |     |
    //+--v--+--v--+--v--+
    //|     |     |     |
    //u  c  u  c  u  c  |
    //|     |     |     |
    //+--v--+--v--+--v--+
    //|     |     |     |
    //u  c  u  c  u  c  |
    //|     |     |     |
    //+--v--+--v--+--v--+

    //The values we want to interpolate can be on:
    //- The center of the cell
    //- In the middle of the vertical lines (staggered grid): u
    //- In the middle of the horizontal lines (staggered grid): v
    public enum Grid
    {
        u,
        v,
        center
    }



    //Clamp the interpolation point P so we know we can interpolate from 4 grid points
    public static void ClampInterpolationPoint(float xP, float yP, GridConstants gridData, Grid sampleField, out float xP_clamped, out float yP_clamped)
    {
        float minXOffset, maxXOffset, minYOffset, maxYOffset;

        minXOffset = maxXOffset = minYOffset = maxYOffset = gridData.half_h;

        //Which grid to we want to interpolate from? 
        switch (sampleField)
        {
            case Grid.u: minXOffset = 0f; maxXOffset = gridData.h; break;
            case Grid.v: minYOffset = 0f; maxYOffset = gridData.h; break;
        }

        //In the center case we have to push it in 0.5*h in all directions
        //In the u case:
        //- We have to push it in 0 from the left side because the point can be on the vertical line
        //- h from the right side because there's no u on the right side of the last cell
        //In the v case we have to push it in 0.5 from both left and right side in x direction

        //Clamp to be inside of the grid on the maximum side 
        xP_clamped = Mathf.Min(xP, gridData.XLength - maxXOffset);
        yP_clamped = Mathf.Min(yP, gridData.YLength - maxYOffset);

        //Clamp to be inside of the grid on the minimum side
        xP_clamped = Mathf.Max(xP_clamped, minXOffset);
        yP_clamped = Mathf.Max(yP_clamped, minYOffset);
    }



    //Get grid indices to interpolate from
    //We assume the interpolation point has been clamped
    //We only need the indices of A, the other ones are just xA_index + 1 and yA_index + 1 
    public static void GetInterpolationArrayIndices(float xP, float yP, GridConstants gridData, Grid sampleField, out int xA_index, out int yA_index)
    {
        GetGridOffsets(sampleField, gridData.half_h, out float dx, out float dy);

        //To go from coordinate to cell we generally do: FloorToInt(pos / cellSize)
        //But we have to compensate because of the staggered grid
        int cellIndexX = Mathf.FloorToInt((xP - dx) * gridData.one_over_h);
        int cellIndexY = Mathf.FloorToInt((yP - dy) * gridData.one_over_h);

        //Clamp
        //The input position is assumed to be within the grid
        //To interpolate between 4 values we have to make sure we are 1 extra cell in from the maximum side
        //So on the maximum side we will interpolate between gridData.numX - 2 and gridData.numX - 1 in x direction 
        xA_index = Mathf.Min(cellIndexX, gridData.numX - 2);
        yA_index = Mathf.Min(cellIndexY, gridData.numY - 2);
    }



    //Get grid coordinates to interpolate from
    //We only need the coordinates of A
    public static void GetACoordinates(Grid sampleField, int xA_index, int yA_index, GridConstants gridData, out float xA, out float yA)
    {
        GetGridOffsets(sampleField, gridData.half_h, out float dx, out float dy);
    
        xA = xA_index * gridData.h + dx;
        yA = yA_index * gridData.h + dy;
    }



    //Get parameters so we can standardize the code depending on which grid data we want to sample
    private static void GetGridOffsets(Grid sampleField, float half_h, out float dx, out float dy)
    {
        dx = 0f;
        dy = 0f;

        //Which grid to we want to interpolate from? 
        switch (sampleField)
        {
            case Grid.u: dy = half_h; break;
            case Grid.v: dx = half_h; break;
            case Grid.center: dx = half_h; dy = half_h; break;
        }
    }
}
