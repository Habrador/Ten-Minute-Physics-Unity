using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FluidSimTutorial
{
	//Simulation parameters
	private readonly float density;
	private const float GRAVITY = -9.81f;
	private int numIters = 100;
	//Parameter to get a stable simulation 
	private float overRelaxation = 1.9f;

	//Simulation grid settings
	private readonly int numX;
	private readonly int numY;
	private readonly int numCells;
	//Cell height
	private readonly float h;

	//Simulation data structures
	//Orientation of the grid: i + 1 means right, j + 1 means up, so 0,0 is bottom-left 
	//Velocity field
	private readonly float[] u; //x component stored in the middle of the left vertical line
	private readonly float[] v; //y component stored in the middle of the bottom horizontal line
	private readonly float[] newU;
	private readonly float[] newV;
	//Pressure field
	private readonly float[] p;
	//Scalar value to determine if obstacle (0) or fluid (1), should be float because easier to sample
	private readonly float[] s;
	//Smoke mass
	private readonly float[] m;
	private readonly float[] newM;

	private enum SampleArray
	{
		uField, vField, sField
	}


	//Covert betwen 2d and 1d array
	private int To1D(int i, int j) => (i * numY) + j;



	public FluidSimTutorial(float density, int numX, int numY, float h)
    {
		this.density = density;

		this.numX = numX + 2; //Add 2 extra because we need a border
		this.numY = numY + 2;
		this.numCells = this.numX * this.numY;
		this.h = h;
		
		this.u = new float[this.numCells];
		this.v = new float[this.numCells];
		this.newU = new float[this.numCells];
		this.newV = new float[this.numCells];

		this.p = new float[this.numCells];
		this.s = new float[this.numCells];

		this.m = new float[this.numCells];
		this.newM = new float[this.numCells];

		System.Array.Fill(this.m, 1f);

		//int num = numX * numY;
	}



	//
	// Simulation
	//

	//Simulation loop
	//1. Modify velocity values (add exteral forces like gravity)
	//2. Make the fluid incompressible (projection)
	//3. Move the velocity field (advection)
	private void Simulate(float dt, float gravity, int numIters)
	{
		//1. Modify velocity values (add exteral forces like gravity)
		Integrate(dt, gravity);

		//2. Make the fluid incompressible (projection)

		//Reset pressure
		System.Array.Fill(p, 0f);

		SolveIncompressibility(numIters, dt);

		Extrapolate();

		AdvectVel(dt);
		
		AdvectSmoke(dt);
	}



	//Modify velocity values from external forces like gravity
	private void Integrate(float dt, float gravity)
	{
		//int n = numY;

		//Ignore the border except for x on right side???
		for (int i = 1; i < numX; i++)
		{
			for (int j = 1; j < numY - 1; j++)
			{
				//If this cell is not an obstacle and cell below it is not a obstacle
				if (s[To1D(i, j)] != 0f && s[To1D(i, j - 1)] != 0f)
				{
					//v = v + dt * g
					//Only horizontal component of the velocity (v) is affected by gravity (so not u)  
					v[To1D(i, j)] += gravity * dt;
				}
			}
		}
	}



	//Make the fluid incompressible by modifying the velocity values
	//Divergence div = total outflow = total amount of fluid the leaves the cell, which should be zero if the fluid is incompressible 
	//Will also calculate pressure as a bonus
	private void SolveIncompressibility(int numIters, float dt)
	{
		int n = this.numY;

		//Used in the pressure calculations: p = p + (d/s) * (rho * h / dt) = p + (d/s) * cp
		float cp = this.density * this.h / dt;

		for (int iter = 0; iter < numIters; iter++)
		{
			//For each cell except the border
			for (int i = 1; i < this.numX - 1; i++)
			{
				for (int j = 1; j < this.numY - 1; j++)
				{
					//Ignore this cell if its an obstacle
					if (s[i * n + j] == 0f)
					{
						continue;
					}

					//Check how many of the surrounding cells are obstacles 
					float sx0 = s[To1D(i - 1, j)]; //Left
					float sx1 = s[To1D(i + 1, j)]; //Right
					float sy0 = s[To1D(i, j - 1)]; //Bottom
					float sy1 = s[To1D(i, j + 1)]; //Top

					float sTot = sx0 + sx1 + sy0 + sy1;

					//Do nothing if all surrounding cells are obstacles
					if (sTot == 0f)
					{
						continue;
					}

					//Divergence
					//if u[To1D(i + 1, j)] > 0 fluid leaves the cell
					//if u[To1D(i, j)] > 0 fluid enters the cell
					//So total flow in u-direction is u[To1D(i + 1, j)] - u[To1D(i, j)]
					float divergence = u[To1D(i + 1, j)] - u[To1D(i, j)] + v[To1D(i, j + 1)] - v[To1D(i, j)];

					float divergence_Over_sTot = -divergence / sTot;

					divergence_Over_sTot *= overRelaxation;
					
					//Calculate the pressure
					p[To1D(i, j)] += cp * divergence_Over_sTot;

					//Update velocities to ensure incompressibility
					//Signs are flipped compared to Tutorial video because divergence_Over_sTot has a negative sign in its calculation
					u[To1D(i, j)] -= sx0 * divergence_Over_sTot; //sx0 is 0 if left cell is wall so no fluid will enter the cell
					u[To1D(i + 1, j)] += sx1 * divergence_Over_sTot;
					v[To1D(i, j)] -= sy0 * divergence_Over_sTot;
					v[To1D(i, j + 1)] += sy1 * divergence_Over_sTot;
				}
			}
		}
	}



	private void Extrapolate()
	{
		int n = this.numY;

		for (int i = 0; i < this.numX; i++)
		{
			this.u[i * n + 0] = this.u[i * n + 1];
			this.u[i * n + this.numY - 1] = this.u[i * n + this.numY - 2];
		}

		for (int j = 0; j < this.numY; j++)
		{
			this.v[0 * n + j] = this.v[1 * n + j];
			this.v[(this.numX - 1) * n + j] = this.v[(this.numX - 2) * n + j];
		}
	}



	private void AdvectVel(float dt)
	{
		this.u.CopyTo(newU, 0);
		this.v.CopyTo(newV, 0);

		int n = this.numY;
		float h = this.h;
		float h2 = 0.5f * h;

		for (var i = 1; i < this.numX; i++)
		{
			for (var j = 1; j < this.numY; j++)
			{

				//cnt++; //Is just set to 0 at the start and is just accumulated here...

				// u component
				if (this.s[i * n + j] != 0.0 && this.s[(i - 1) * n + j] != 0.0 && j < this.numY - 1)
				{
					var x = i * h;
					var y = j * h + h2;
					var u = this.u[i * n + j];
					var v = AvgV(i, j);
					//var v = this.sampleField(x,y, V_FIELD);
					x = x - dt * u;
					y = y - dt * v;
					u = SampleField(x, y, SampleArray.uField);
					this.newU[i * n + j] = u;
				}
				// v component
				if (this.s[i * n + j] != 0.0 && this.s[i * n + j - 1] != 0.0 && i < this.numX - 1)
				{
					var x = i * h + h2;
					var y = j * h;
					var u = AvgU(i, j);
					//var u = this.sampleField(x,y, U_FIELD);
					var v = this.v[i * n + j];
					x = x - dt * u;
					y = y - dt * v;
					v = SampleField(x, y, SampleArray.vField);
					this.newV[i * n + j] = v;
				}
			}
		}

		this.newU.CopyTo(u, 0);
		this.newV.CopyTo(v, 0);
	}


	private void AdvectSmoke(float dt)
	{
		//Copy all values from m to newM, we cant just swap because of obstacles and border???
		this.m.CopyTo(newM, 0);

		int n = this.numY;
		float h = this.h;
		float h2 = 0.5f * h;

		//For all cells except the border
		for (int i = 1; i < this.numX - 1; i++)
		{
			for (int j = 1; j < this.numY - 1; j++)
			{
				//If this cell is not an obstacle
				if (this.s[i * n + j] != 0)
				{
					float u = (this.u[i * n + j] + this.u[(i + 1) * n + j]) * 0.5f;
					float v = (this.v[i * n + j] + this.v[i * n + j + 1]) * 0.5f;
					float x = i * h + h2 - dt * u;
					float y = j * h + h2 - dt * v;

					this.newM[i * n + j] = SampleField(x, y, SampleArray.sField);
				}
			}
		}

		this.newM.CopyTo(this.m, 0);
	}



	//
	// Help methods
	//

	private float AvgU(int i, int j)
	{
		int n = numY;

		float uAverage = (u[i * n + j - 1] + u[i * n + j] + u[(i + 1) * n + j - 1] + u[(i + 1) * n + j]) * 0.25f;
		
		return uAverage;
	}

	private float AvgV(int i, int j)
	{
		int n = numY;

		float vAverage = (v[(i - 1) * n + j] + v[i * n + j] + v[(i - 1) * n + j + 1] + v[i * n + j + 1]) * 0.25f;

		return vAverage;
	}



	private float SampleField(float x, float y, SampleArray field)
	{
		int n = this.numY;
		float h = this.h;
		
		float h1 = 1f / h;
		float h2 = 0.5f * h;

		x = Mathf.Max(Mathf.Min(x, this.numX * h), h);
		y = Mathf.Max(Mathf.Min(y, this.numY * h), h);

		float dx = 0f;
		float dy = 0f;

		float[] f = null;

		switch (field)
		{
			case SampleArray.uField: f = this.u; dy = h2; break;
			case SampleArray.vField: f = this.v; dx = h2; break;
			case SampleArray.sField: f = this.m; dx = h2; dy = h2; break;
		}

		if (f == null)
		{
			Debug.Log("Something is very wrong");

			return -1f;
        }

		int x0 = Mathf.Min(Mathf.FloorToInt((x - dx) * h1), this.numX - 1);
		float tx = ((x - dx) - x0 * h) * h1;
		int x1 = Mathf.Min(x0 + 1, this.numX - 1);

		int y0 = Mathf.Min(Mathf.FloorToInt((y - dy) * h1), this.numY - 1);
		float ty = ((y - dy) - y0 * h) * h1;
		int y1 = Mathf.Min(y0 + 1, this.numY - 1);

		float sx = 1f - tx;
		float sy = 1f - ty;

		float val = 
			sx * sy * f[x0 * n + y0] +
			tx * sy * f[x1 * n + y0] +
			tx * ty * f[x1 * n + y1] +
			sx * ty * f[x0 * n + y1];

		return val;
	}
}
