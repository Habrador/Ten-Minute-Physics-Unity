using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Fluid Simulation in 200 lines of code (excluding comments)
//The state of a fluid at a given instant of time is modeled as a velocity vector field. The Navier-Stokes equations describe the evolution of this velocity field over time.  
// - Walls - The velocity from a wall/obstacle is zero unless we add wind from a turbine or move the wall/obstacle
// - Border cell - The velocities in border cells are treated as if they are connected to some infinite big fluid in the normal direction
//To figure out what's going on I used the following sources:
//- The book "Fluid Simulation for Computer Graphics" by Robert Bridson is explaining good what's going on
//- The fluid simulations by Jos Stam: "Real-Time Fluid Dynamics for Games" and "GPU Gems: Fast Fluid Dynamics Simulation on the GPU"
//- The paper "Realistic Animation of Liquids" by Foster and Metaxas is also similar to what's going on here, especially the projection step
//- The paper "Fluid flow for the rest of us" is good (it has the same pressure equation)
namespace EulerianFluidSimulator
{
	public class FluidSim
	{
		//Simulation parameters
		private readonly float density;
	

		//Simulation grid settings
		public int numX;
		public int numY;
		public int numCells;
		//Cell height
		public float h;

		//Simulation data structures
		//Orientation of the grid:
		// i + 1 means right, j + 1 means up
		// (0,0) is bottom-left
		//Velocity field (u, v, w) 
		//A staggered grid is improving the numerical results with less artificial dissipation  
		//u component stored in the middle of the left vertical line of each cell
		//v component stored in the middle of the bottom horizontal line of each cell
		//This means we can't have a velocity vector because u,v are at different locations
		public readonly float[] u; 
		public readonly float[] v;
		private readonly float[] uNew;
		private readonly float[] vNew;
		//Pressure field
		public float[] p;
		//If obstacle (0) or fluid (1)
		//Should use float instead of bool because it makes some calculations simpler
		public float[] s;
		//Smoke density [0,1]: 0 means max smoke
		//m short for mass? He calls it density field in the video
		//Makes sense when we multiply smoke density with 255 to get a color because 0 * 255 = 0 -> black color
		public readonly float[] m;
		private readonly float[] mNew;

		public enum SampleArray
		{
			uField, vField, smokeField
		}

		//Convert between 2d and 1d array
		//The conversion can cause great confusion, so we better do it in one place throughout all code
		//Was (i * numY) + j in tutorial but should be i + (numX * j) if we want them row-by-row after each other in the flat array
		//Otherwise we get them column by column which is maybe how js prefers them when displaying...
		//https://softwareengineering.stackexchange.com/questions/212808/treating-a-1d-data-structure-as-2d-grid
		public int To1D(int i, int j) => i + (numX * j);

		//These are not the same as the height we set at start because of the two border cells
		public float GetWidth() => numX * h;
		public float GetHeight() => numY * h;

		//Is a coordinate within the simulation area?
		public bool IsWithinArea(float x, float y) => (x > 0 && x < GetWidth() && y > 0 && y < GetHeight());



		public FluidSim(float density, int numX, int numY, float h)
		{
			this.density = density;

			//Add 2 extra cells because we need a border, or are we adding two u's on each side???
			//Because we use a staggered grid, then there will be no u on the right side of the cells in the last column if we add new cells... The p's are in the middle and of the same size, so we add two new cells while ignoring there's no u's on the right side of the last column. The book "Fluid Simulation for Computer Graphics" says that the velocity arrays should be one larger than the pressure array because we have 1 extra velocity on the right side of the last column. 
			//He says border cells in the video
			//Why are we adding 2 cells anyway, it makes it confusing because the height of the simulation changes...
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



		//Simulation loop for the fluid
		public void Simulate(float dt, float gravity, int numIters, float overRelaxation)
		{
			//Modify velocity values (add exteral forces like gravity or a fan blowing)
			//This is the F term in Navier-Stokes
			Integrate(dt, gravity);

			//Make the fluid incompressible (projection)
			//This is the conservation of mass term in Navier-Stokes: nabla u = 0
			//Here we also calculate pressure - the pressure term in Navier-Stokes
			//This one is the bottleneck
			SolveIncompressibility(numIters, dt, overRelaxation);

			//Fix border velocities
			//See "Fluid flow for the rest of us"
			Extrapolate();

			//Advection
			//Advection is the motion of particles along the bulk flow

			//Move the velocity field along itself (self-advection)
			//Advection should be done in a divergence-free velocity field, which also satisfies the required boundary conditions -> so advect has to come after project or you may get odd artifacts
			//This will introduce viscosity because of numerical error in the advection term (we are using averages) which can be reduced with vorticity confinement
			AdvectVel(dt);

			//Move the smoke along the velocity field
			//Perfume propagates both because it's carried along with the fluidlike air and because it diffuses. But we don't need to add diffusion to the smoke because numerical error in the advection term (we are using averages) causes it to diffuse anyway. 
			//Light objects, like smoke particles, are just carried along with the velocity field. But moving particles is expensive, so they are replaced with a smoke density at each cell. 
			AdvectSmoke(dt);

			//Diffusion - the viscosity term in Navier-Stokes
			//Diffusion for temperatures and perfume particles tend to redistribute their properties over time. It's the same with the fluid's velocity. 
			//Is not needed here because we dont take viscocity into account (yet). Higher viscocity means the fluid will come to rest faster (honey). Viscocity is a how resistive a fluid is to flow = an internal friction from layers of fluids interacting with each other. The resistance results in diffusion of momentum which becomes distributed throughout the fluid. The velocity is dissipated = slowed down.

			//for (int j = 0; j < numY; j++)
			//{
			//	Debug.Log(u[To1D(numX - 1, j)]);
   //         }
		}



		//Modify velocity values from external forces like gravity
		private void Integrate(float dt, float gravity)
		{
			//TODO: Ignore the border except for x on right side??? Might be a bug and should be numX - 1
			for (int i = 1; i < numX - 1; i++)
			{
				for (int j = 1; j < numY - 1; j++)
				{
					//If this cell is not an obstacle and cell below is not an obstacle
					if (s[To1D(i, j)] != 0f && s[To1D(i, j - 1)] != 0f)
					{
						//Forward Euler
						//Only horizontal component of the velocity (v) is affected by gravity
						v[To1D(i, j)] += gravity * dt;
					}
				}
			}
		}



		//Make the fluid incompressible (fluid in = fluid out) by modifying the velocity values 
		//Will also calculate pressure as a bonus
		//When the method is done the fluid should be incompressible and we have the pressure that was needed to make the fluid incompressible
		private void SolveIncompressibility(int numIters, float dt, float overRelaxation)
		{
			//Reset pressure
			System.Array.Fill(p, 0f);

			//Pressure is whatever it takes to make the fluid incompressible and enforce the solid wall boundary conditions
			//Particles in higher pressure regions are pushed towards lower pressure regions
			//To calculate the total pressure needed to make the fluid incompressible we update it incrementally,  summing up the total pressure needed to make the fluid incompressible 
			//This is equation 14 in the paper "Fluid flow for the rest of us"
			//p = p + (d/s) * ((rho * h) / dt)
			//Where
			//d - divergence [m/s]
			//s - number of surrounding cells that are not obstacles
			//rho - density [kg/m^3]
			//h - cell size [m]
			//dt - time step [s]

			//To optimize the pressure calculations -> p = p + (d/s) * cp
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
						//if (s[i + numX * j] == 0f)
						{
							continue;
						}

						//Cache how many of the surrounding cells are obstacles 
						float sx0 = s[To1D(i - 1, j)]; //Left
						float sx1 = s[To1D(i + 1, j)]; //Right
						float sy0 = s[To1D(i, j - 1)]; //Bottom
						float sy1 = s[To1D(i, j + 1)]; //Top

						//Experiment to see if To1D is slowing down the calculations, but they are not
						//float sx0 = s[(i-1) + numX * j]; //Left
						//float sx1 = s[(i+1) + numX * j]; //Right
						//float sy0 = s[i + numX * (j-1)]; //Bottom
						//float sy1 = s[i + numX * (j+1)]; //Top

						float sTot = sx0 + sx1 + sy0 + sy1;

						//Do nothing if all surrounding cells are obstacles
						if (sTot == 0f)
						{
							continue;
						}

						//Divergence = total amount of fluid the leaves the cell 
						//- If it's positive we have too much outflow
						//- If it's negative we have too much inflow
						//- If it's zero the fluid is incompressible
						//if u[To1D(i + 1, j)] > 0 fluid leaves the cell
						//if u[To1D(i, j)] > 0 fluid enters the cell, so should be negative because we calculate total outflow
						//So total outflow flow in u-direction is u[To1D(i + 1, j)] - u[To1D(i, j)]
						//Same idea applies to v-direction
						//So if u[To1D(i, j)] = 2 and the rest is 0, then divergence = -2, meaning too much inflow 
						float divergence = u[To1D(i + 1, j)] - u[To1D(i, j)] + v[To1D(i, j + 1)] - v[To1D(i, j)];
						//float divergence = u[(i + 1) + numX * j] - u[i + numX * j] + v[i + numX * (j+1)] - v[i + numX * j];

						//Why the minus sign?
						//From "Realistic Animation of Liquids:"
						//- A positive divergence represents an influx of fluid and would correspond to an increase in cell pressure and subsequent increase in fluid outflow from the cell.
						//- A negative divergence lowers internal pressure and increases inflow from neighboring cells. 
						float divergence_Over_sTot = -divergence / sTot;

						divergence_Over_sTot *= overRelaxation;

						//Calculate the pressure
						//Should overRelaxation be included in the pressure calculations? Relaxation is used to speed up convergence. Because we multiply relaxation with the divergence we get a larger divergence and thus larger pressure 
						//p[i + numX * j] += cp * divergence_Over_sTot;
						p[To1D(i, j)] += cp * divergence_Over_sTot;

						//Update velocities to ensure incompressibility
						//Signs are flipped compared to video because of the -divergence
						//If sx0 = 0 it means theres an obstacle to the left of this cell, meaning the velocity from that wall is fixed (can be 0, can have a velocity from a wind turbine, or can have a velocity from a moving obstacle) and we can't update it to fix the divergence. This means we can only modify the three other velocities  
						//Why are we using u,v instead of uNew and vNew? From "Real time simulation and control of Newtonian fluids...": The convergence rate of the iterations [when using Gauss-Seidel] can be improved by using the newly computed values directly in the same iteration instead of saving them to the next iteration step. This make it more difficult to parallelize. If you need to parallelize use Jacobi
						u[To1D(i, j)] -= sx0 * divergence_Over_sTot;
						u[To1D(i + 1, j)] += sx1 * divergence_Over_sTot;
						v[To1D(i, j)] -= sy0 * divergence_Over_sTot;
						v[To1D(i, j + 1)] += sy1 * divergence_Over_sTot;
						
						//u[i + numX * j] -= sx0 * divergence_Over_sTot;
						//u[(i + 1) + numX * j] += sx1 * divergence_Over_sTot;
						//v[i + numX * j] -= sy0 * divergence_Over_sTot;
						//v[i + numX * (j + 1)] += sy1 * divergence_Over_sTot;
					}
				}
			}
		}



		//Fix the border velocities by copying neighbor values in the tangential direction
		//The velocities in the normal direction become whatever they need to be to make the fluid incompressible
		private void Extrapolate()
		{
			//For each column
			for (int i = 0; i < numX; i++)
			{
				//Bottom border row gets the same velocity in u direction as the row above
				u[To1D(i, 0)] = u[To1D(i, 1)];

				//Top border row gets the same velocity in u direction as the row below
				u[To1D(i, numY - 1)] = u[To1D(i, numY - 2)];
			}

			//For each row
			for (int j = 0; j < numY; j++)
			{
				//Left border column gets the same velocity in v direction as the column to the right
				v[To1D(0, j)] = v[To1D(1, j)];

				//Right border column gets the same velocity in v direction as the column to the left 
				v[To1D(numX - 1, j)] = v[To1D(numX - 2, j)];
			}
		}



		//Move the velocity field along itself
		//Semi-Lagrangian advection where we are simulating particles
		//At time dt in the past what fluid would have arrived where we are now?
		//1. Calculate (u, v_average) at the u component
		//2. The previous pos x_prev = x - dt * v if we assume the particle moved in a straight line
		//3. Interpolate the velocity at x_prev
		private void AdvectVel(float dt)
		{
			//Copy current velocities to the new velocities because some cells are not being processed, such as obstacles
			this.u.CopyTo(this.uNew, 0);
			this.v.CopyTo(this.vNew, 0);

			float h = this.h;
			//The position of the velocity components are in the middle of the border of the cells
			float h2 = 0.5f * h;

			for (int i = 1; i < numX; i++)
			{
				for (int j = 1; j < this.numY; j++)
				{
					//Is just set to 0 at the start and is just accumulated here...
					//cnt++;

					//Update u component
					//If this cell and the cell left of it is not an obstacle
					//Why j < this.numY - 1 and not in the for loop? Then u < this.numX - 1 wouldn't have been included either
					//Using j < this.numY would have resulted in an error becuase we need the surrounding vs to this u 
					if (this.s[To1D(i, j)] != 0f && this.s[To1D(i - 1, j)] != 0f && j < this.numY - 1)
					{
						//The pos of the u velocity in simulation space
						float x = i * h;
						float y = j * h + h2;

						//The current velocity
						float u = this.u[To1D(i, j)];
						float v = AverageV(i, j);
						
						//The pos of the fluid particle that moved to this u position
						x -= dt * u;
						y -= dt * v;
						
						//The interpolated u as this position
						u = SampleField(x, y, SampleArray.uField);
						
						this.uNew[To1D(i, j)] = u;
					}
					//Update v component
					//If this cell and the cell below is not an obstacle
					if (this.s[To1D(i, j)] != 0f && this.s[To1D(i, j - 1)] != 0f && i < numX - 1)
					{
						//The pos of the v velocity in simulation space
						float x = i * h + h2;
						float y = j * h;

						//The current velocity
						float u = AverageU(i, j);
						float v = this.v[To1D(i, j)];

						//The pos of the fluid particle that moved to this v position
						x -= dt * u;
						y -= dt * v;
						
						//The interpolated v at this position
						v = SampleField(x, y, SampleArray.vField);
						
						this.vNew[To1D(i, j)] = v;
					}
				}
			}

			this.uNew.CopyTo(this.u, 0);
			this.vNew.CopyTo(this.v, 0);
		}



		//Move the smoke field
		//Same as advecting velocity
		//Use the velocity at the center of the cell and walk back in a straight line 
		//Find the particles that over a single time step ended up exactly at the cell's center
		private void AdvectSmoke(float dt)
		{
			//Copy all values from m to newM, we cant just swap because of obstacles and border???
			this.m.CopyTo(mNew, 0);

			float h = this.h;
			
			float h2 = 0.5f * h;

			//For all cells except the border
			for (int i = 1; i < numX - 1; i++)
			{
				for (int j = 1; j < this.numY - 1; j++)
				{
					//If this cell is not an obstacle
					if (this.s[To1D(i, j)] != 0f)
					{
						//The velocity in the middle of the cell is the average of the velocities on the border
						float u = (this.u[To1D(i, j)] + this.u[To1D(i + 1, j)]) * 0.5f;
						float v = (this.v[To1D(i, j)] + this.v[To1D(i, j + 1)]) * 0.5f;

						//The position of the center in simulation space
						float x = i * h + h2;
						float y = j * h + h2;

						//The pos of the smoke particle that moved to this u,v position
						x -= dt * u;
						y -= dt * v;

						//Sample the smoke field at this position 
						this.mNew[To1D(i, j)] = SampleField(x, y, SampleArray.smokeField);
					}
				}
			}

			this.mNew.CopyTo(this.m, 0);
		}



		//
		// Help methods
		//

		//Average u around v[To1D(i, j)]
		private float AverageU(int i, int j)
		{
			float uTot = u[To1D(i, j - 1)] + u[To1D(i, j)] + u[To1D(i + 1, j - 1)] + u[To1D(i + 1, j)];

			float uAverage = uTot * 0.25f;
		
			return uAverage;
		}

		//Average v around u[To1D(i, j)] 
		private float AverageV(int i, int j)
		{
			float vTot = v[To1D(i - 1, j)] + v[To1D(i, j)] + v[To1D(i - 1, j + 1)] + v[To1D(i, j + 1)];

			float vAverage = vTot * 0.25f;

			return vAverage;
		}



		//Get data (u, v, smoke density) from the simulation at coordinate x,y
		//x,y are NOT cell indices but coordinates in simulation space
		public float SampleField(float x, float y, SampleArray field)
		{
			float h = this.h;

			//Make sure x and y are within the simulation space 
			//Why is minimum h and not 0.5 * h? Because later we do x - (0.5 * h) to calculate which cell we are in, so x has to be at least h or we end up to the left of the velocity to the left we want to interpolate from (if we interpolate v at least) 
			x = Mathf.Max(Mathf.Min(x, numX * h), h); //this.numX * h = total width
			y = Mathf.Max(Mathf.Min(y, this.numY * h), h); //this.numY * h = total height

			float dx = 0f;
			float dy = 0f;

			//Which array do we want to sample
			//Using f is confusing because its whats being used for FluidSim elsewhere... 
			float[] f = null;

			float h2 = 0.5f * h;

			switch (field)
			{
				case SampleArray.uField: f = this.u; dy = h2; break; //u is stored in the middle of the vertical cell lines
				case SampleArray.vField: f = this.v; dx = h2; break; //v is stored in the middle of the horizontal cell lines
				case SampleArray.smokeField: f = this.m; dx = h2; dy = h2; break; //Is stored in the center of each cell
			}

			if (f == null)
			{
				Debug.Log("Something is very wrong");

				return -1f;
			}

			//Which cell indices do we want to interpolate between?
			float h1 = 1f / h;

			//To go from coordinate to cell we do: FloorToInt(pos / cellSize) 
			//We've already made sure the cell index can't go below 0
			int x0 = Mathf.Min(Mathf.FloorToInt((x - dx) * h1), numX - 1);
			int x1 = Mathf.Min(x0 + 1, numX - 1);

			int y0 = Mathf.Min(Mathf.FloorToInt((y - dy) * h1), this.numY - 1);
			int y1 = Mathf.Min(y0 + 1, this.numY - 1);

			//The weights used to interpolate between the 4 values
			//This basically 3 lerps
			//According to the video, the weights are:
			//w_00 = 1 - x/h
			//w_01 = x/h
			//w_10 = 1 - y/h
			//w_11 = y/h
			float tx = ((x - dx) - x0 * h) * h1; //w_01
			float ty = ((y - dy) - y0 * h) * h1; //w_11

			float sx = 1f - tx; //w_00
			float sy = 1f - ty; //w_10

			//v =
			//	w_00 * w_10 * v_i,j +
			//	w_01 * w_10 * v_i+1,j +
			//	w_00 * w_11 * v_i,j+1 +
			//	w_01 * w_11 * v_i+1,j+1
			float val = 
				sx * sy * f[To1D(x0, y0)] +
				tx * sy * f[To1D(x1, y0)] +
				tx * ty * f[To1D(x1, y1)] +
				sx * ty * f[To1D(x0, y1)];

			return val;
		}



		//Return min and max pressure
		public MinMax GetMinMaxPressure()
		{
			float minP = p[0];
			float maxP = p[0];

			for (int i = 0; i < numCells; i++)
			{
				minP = Mathf.Min(minP, p[i]);
				maxP = Mathf.Max(maxP, p[i]);
			}

			MinMax minMaxPressure = new (minP, maxP);

			return minMaxPressure;
		}
	}
}