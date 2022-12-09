using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//The state of a fluid at a given instant of time is modeled as a velocity vector field and a pressure field, assuming density and temperature are constant. The velocity of air at a radiator is pointing upwards due to heat rising. The Navier-Stokes equations describe the evolution of this velocity field over time. 
//Light objects, like smoke particles, are just carried along with the velocity field. But moving particles is expensive, so they are replaced with a smoke density at each cell.  
//Similar to the fluid simulations by Jos Stam so read "Real-Time Fluid Dynamics for Games" and "GPU Gems: Fast Fluid Dynamics Simulation on the GPU" if you want to learn what's going on
//Improvements:
// - Conjugate gradient solver which has better convergence propertied instead of Gauss-Seidel relaxation
// - Vorticity confinement - improves the fact that the simulated fluids dampen faster than they should IRL (numerical dissipation). Read "Visual simulation of smoke" by Jos Stam
// - Multiple fluids - see p.12 "Real-Time Fluid Dynamics for Games"
public class FluidSimTutorial
{
	//Simulation parameters
	private readonly float density;
	private const float GRAVITY = -9.81f;
	//We need several íterations each update to make the fluid incompressible 
	private readonly int numIters = 100;
	//Trick to get a stable simulation by speeding up convergence [1, 2]
	//Multiply it with the divergence
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
	//A staggered grid is improving the numerical results with less artificial dissipation  
	private readonly float[] u; //x component stored in the middle of the left vertical line of each cell
	private readonly float[] v; //y component stored in the middle of the bottom horizontal line of each cell
	private readonly float[] uNew;
	private readonly float[] vNew;
	//Pressure field
	private readonly float[] p;
	//Scalar value to determine if obstacle (0) or fluid (1), should be float because easier to sample
	private readonly float[] s;
	//Smoke density [0,1]
	private readonly float[] m;
	private readonly float[] mNew;

	private enum SampleArray
	{
		uField, vField, smokeField
	}

	//Convert between 2d and 1d array
	//i is x and j is y
	private int To1D(int i, int j) => (i * numY) + j;



	public FluidSimTutorial(float density, int numX, int numY, float h)
    {
		this.density = density;

		//Add 2 extra cells because we need a border
		this.numX = numX + 2; 
		this.numY = numY + 2;
		this.numCells = this.numX * this.numY;
		this.h = h;
		
		this.u = new float[this.numCells];
		this.v = new float[this.numCells];
		this.uNew = new float[this.numCells];
		this.vNew = new float[this.numCells];

		this.p = new float[this.numCells];
		this.s = new float[this.numCells];

		this.m = new float[this.numCells];
		this.mNew = new float[this.numCells];

		System.Array.Fill(this.m, 1f);
	}



	//
	// Simulation
	//

	//Simulation loop for the fluid
	//1. External forces. Modify velocity values by adding:
	//	- Body forces applied to entire fluid like gravity and buoyancy from temperature differences
	//	- Local forces applied to a region of the fluid like a fan blowing
	//2. Projection. Make the fluid incompressible by projecting a vector field. What creates the vortices that produces swirly-like flows. Here we calculate the pressure  
	//3. Advection. Move the velocity field along itself (self-advection)
	//(4.) Diffusion. Viscocity is a how resistive a fluid is to flow. The resistance results in diffusion of momentum (and thus velocity (the velocity is dissipated = slowed down). Is not needed here because we dont take viscocity into account (yet).  
	//Simulation loop for the smoke
	//1. Advection. Move the smoke along the velocity field 
	//...one can also add diffusion to make the densities spread across the cells. This is not always needed because numerical error in the advection term causes it to diffuse anyway
	private void Simulate(float dt, int numIters)
	{
		//1. Modify velocity values (add exteral forces like gravity)
		Integrate(dt, GRAVITY);

		//2. Make the fluid incompressible (projection)
		SolveIncompressibility(numIters, dt);

		//Fix border velocities 
		Extrapolate();

		//3. Move the velocity field (advection). We will here simulate particles, meaning we get a Semi-Lagrangian advection
		AdvectVel(dt);
		
		AdvectSmoke(dt);
	}



	//Modify velocity values from external forces like gravity
	private void Integrate(float dt, float gravity)
	{
		//TODO: Ignore the border except for x on right side??? Might be a bug and should be numX - 1
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
		//Reset pressure
		System.Array.Fill(p, 0f);

		//Used in the pressure calculations: p = p + (d/s) * (rho * h / dt) = p + (d/s) * cp
		float cp = density * h / dt;

		//Gauss-Seidel relaxation
		for (int iter = 0; iter < numIters; iter++)
		{
			//For each cell except the border
			for (int i = 1; i < numX - 1; i++)
			{
				for (int j = 1; j < numY - 1; j++)
				{
					//Ignore this cell if its an obstacle
					if (s[To1D(i, j)] == 0f)
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
					//We need the += because even though pressure is initialized as zero before the method, we are running this method several times each update 
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



	//Fix the border velocities by copying neighbor values 
	//If the fluid is in a box with solid walls: 
	//The horizontal component of the velocity should be 0 on the vertical walls
	//The vertical component of the velocity should be 0 on the horizontal walls
	private void Extrapolate()
	{
		for (int i = 0; i < numX; i++)
		{
			u[To1D(i, 0)] = u[To1D(i, 1)];

			u[To1D(i, numY - 1)] = u[To1D(i, numY - 2)];
		}

		for (int j = 0; j < numY; j++)
		{
			v[To1D(0, j)] = v[To1D(1, j)];

			v[To1D(numX - 1, j)] = v[To1D(numX - 2, j)];
		}
	}



	//Move the velocity field
	//Semi-Lagrangian advection so we are simulating particles
	//1. Calculate (u, v_bar) at the u component, where v_bar is the average
	//2. The previous pos x_prev = x - dt * v if we assume the particle moved in a straight line
	//3. Interpolate the velocity at x(prev)
	private void AdvectVel(float dt)
	{
		this.u.CopyTo(uNew, 0);
		this.v.CopyTo(vNew, 0);

		int n = this.numY;

		float h = this.h;
		//The position of the velocity components are on the borders of the cell - not the middle
		float h2 = 0.5f * h;

		for (var i = 1; i < this.numX; i++)
		{
			for (var j = 1; j < this.numY; j++)
			{

				//cnt++; //Is just set to 0 at the start and is just accumulated here...

				//Update u component
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
					this.uNew[i * n + j] = u;
				}
				//Update v component
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
					this.vNew[i * n + j] = v;
				}
			}
		}

		this.uNew.CopyTo(u, 0);
		this.vNew.CopyTo(v, 0);
	}



	//Move the smoke field
	//Same as advecting velocity
	//Use the velocity at the center of the cell and walk back in a straight line 
	//Find the particles that over a single time step ended up exactly at the cell's center. Remember to use the densities of the previous update to find the densities this update  
	private void AdvectSmoke(float dt)
	{
		//Copy all values from m to newM, we cant just swap because of obstacles and border???
		this.m.CopyTo(mNew, 0);

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

					this.mNew[i * n + j] = SampleField(x, y, SampleArray.smokeField);
				}
			}
		}

		this.mNew.CopyTo(this.m, 0);
	}



	//
	// Help methods
	//

	private float AvgU(int i, int j)
	{
		float uTot = u[To1D(i, j - 1)] + u[To1D(i, j)] + u[To1D(i + 1, j - 1)] + u[To1D(i + 1, j)];

		float uAverage = uTot * 0.25f;
		
		return uAverage;
	}

	private float AvgV(int i, int j)
	{
		float vTot = v[To1D(i - 1, j)] + v[To1D(i, j)] + v[To1D(i - 1, j + 1)] + v[To1D(i, j + 1)];

		float vAverage = vTot * 0.25f;

		return vAverage;
	}



	//Get data from the simulation at coordinate x, y which are NOT cell coordinates
	//If we want the velocity we compute a weighted average of the 4 closest velocity components
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
			case SampleArray.smokeField: f = this.m; dx = h2; dy = h2; break;
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
