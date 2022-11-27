using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//
// Vector operations when the array with vectors is flat [x0, y0, z0, x1, y1, z1, ...]
//

//anr = index of vertex a in the list of all vertices if it had an (x,y,z) position at that index. BUT we dont have that list, so to make it easier to loop through all particles where x, y, z are at 3 difference indices, we multiply by 3
//anr = 0 -> 0 * 3 = 0 -> 0, 1, 2
//anr = 1 -> 1 * 3 = 3 -> 3, 4, 5
public static class VectorArrays
{
	//a = 0
	public static void VecSetZero(float[] a, int anr)
	{
		anr *= 3;

		a[anr] = 0f;
		a[anr + 1] = 0f;
		a[anr + 2] = 0f;
	}

	//a * scale
	public static void VecScale(float[] a, int anr, float scale)
	{
		anr *= 3;

		a[anr] *= scale;
		a[anr + 1] *= scale;
		a[anr + 2] *= scale;
	}

	//a = b
	public static void VecCopy(float[] a, int anr, float[] b, int bnr)
	{
		anr *= 3;
		bnr *= 3;

		a[anr] = b[bnr];
		a[anr + 1] = b[bnr + 1];
		a[anr + 2] = b[bnr + 2];
	}

	//a = a + (b * scale) 
	public static void VecAdd(float[] a, int anr, float[] b, int bnr, float scale = 1f)
	{
		anr *= 3;
		bnr *= 3;

		a[anr] += b[bnr] * scale;
		a[anr + 1] += b[bnr + 1] * scale;
		a[anr + 2] += b[bnr + 2] * scale;
	}

	//diff = (a - b) * scale
	//Need the scale to simplify this v = (x - xPrev) / dt then scale is 1f/dt
	public static void VecSetDiff(float[] diff, int dnr, float[] a, int anr, float[] b, int bnr, float scale = 1f)
	{
		dnr *= 3;
		anr *= 3;
		bnr *= 3;

		diff[dnr] = (a[anr] - b[bnr]) * scale;
		diff[dnr + 1] = (a[anr + 1] - b[bnr + 1]) * scale;
		diff[dnr + 2] = (a[anr + 2] - b[bnr + 2]) * scale;
	}


	//sqrMagnitude(a) 
	public static float VecLengthSquared(float[] a, int anr)
	{
		anr *= 3;

		float a0 = a[anr];
		float a1 = a[anr + 1];
		float a2 = a[anr + 2];

		float lengthSqr = a0 * a0 + a1 * a1 + a2 * a2;

		return lengthSqr;
	}

	//sqrMagnitude(a - b)
	public static float VecDistSquared(float[] a, int anr, float[] b, int bnr)
	{
		anr *= 3;
		bnr *= 3;

		float a0 = a[anr] - b[bnr];
		float a1 = a[anr + 1] - b[bnr + 1];
		float a2 = a[anr + 2] - b[bnr + 2];

		float distSqr = a0 * a0 + a1 * a1 + a2 * a2;

		return distSqr;
	}

	//a dot b
	public static float VecDot(float[] a, int anr, float[] b, int bnr)
	{
		anr *= 3;
		bnr *= 3;

		float dot = a[anr] * b[bnr] + a[anr + 1] * b[bnr + 1] + a[anr + 2] * b[bnr + 2];

		return dot;
	}

	//a = b x c
	public static void VecSetCross(float[] a, int anr, float[] b, int bnr, float[] c, int cnr)
	{
		anr *= 3;
		bnr *= 3;
		cnr *= 3;

		a[anr] = b[bnr + 1] * c[cnr + 2] - b[bnr + 2] * c[cnr + 1];
		a[anr + 1] = b[bnr + 2] * c[cnr] - b[bnr] * c[cnr + 2];
		a[anr + 2] = b[bnr] * c[cnr + 1] - b[bnr + 1] * c[cnr];
	}



}
