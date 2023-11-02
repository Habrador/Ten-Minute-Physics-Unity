using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Fluid Simulation in 200 lines of code (excluding comments)
//The state of a fluid at a given instant of time is modeled as a velocity vector field. The Navier-Stokes equations describe the evolution of this velocity field over time.  
// - Walls - The velocity from a wall/obstacle is zero unless we add wind from a turbine or drag the obstacle with mouse
// - Border cell - The velocities in border cells are treated as if they are connected to some infinite big fluid in the normal direction. In the tangential direction, the velocity is copied from the closest cell
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
		//Cell height and width
		public float h;

		//Simulation data structures
		//Orientation of the grid:
		// i + 1 means right, j + 1 means up
		// (0,0) is bottom-left
		//Velocity field (u, v, w) 
		//A staggered grid is improving the numerical results with less artificial dissipation  
		//u component stored in the middle of the left vertical line of each cell
		//v component stored in the middle of the bottom horizontal line of each cell
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
		//m short for mass? It's called density field in the video
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
		//Otherwise we get them column by column which is maybe how js prefers them when displaying???
		//https://softwareengineering.stackexchange.com/questions/212808/treating-a-1d-data-structure-as-2d-grid
		public int To1D(int i, int j) => i + (numX * j);

		//These are not the same as the height we set at start because of the two border cells
		public float SimWidth => numX * h;
		public float SimHeight => numY * h;

		//Is a coordinate within the simulation area?
		public bool IsWithinArea(float x, float y) => (x > 0 && x < SimWidth && y > 0 && y < SimHeight);



		public FluidSim(float density, int numX, int numY, float h)
		{
			this.density = density;

			//Add 2 extra cells because we need a border, or are we adding two u's on each side???
			//Because we use a staggered grid, then there will be no u on the right side of the cells in the last column if we add new cells... The p's are in the middle and of the same size, so we add two new cells while ignoring there's no u's on the right side of the last column. The book "Fluid Simulation for Computer Graphics" says that the velocity arrays should be one larger than the pressure array because we have 1 extra velocity on the right side of the last column. 
			//He says border cells in the video
			this.numX = numX + 2; 
			this.numY = numY + 2;
			this.h = h;

			int numCells = this.numX * this.numY;

			this.u = new float[numCells];
			this.v = new float[numCells];
			this.uNew = new float[numCells];
			this.vNew = new float[numCells];

			this.p = new float[numCells];
			//Will init all cells to walls (0)
			this.s = new float[numCells];

			this.m = new float[numCells];
			this.mNew = new float[numCells];

			//Init all smoke densities to 0 = no smoke
			System.Array.Fill(this.m, 1f);
		}



		//Simulation loop for the fluid
		public void Simulate(float dt, float gravity, int numIters, float overRelaxation)
		{
			//Modify velocity values
			//Add external forces like gravity or a fan blowing. But we will only add gravity (if needed) and we will add the fan in the wind tunnel by adding a velocity to a wall
			//This is the F term in Navier-Stokes
			Integrate(dt, gravity);

			//Make the fluid incompressible (projection)
			//This is the conservation of mass term in Navier-Stokes
			//Here we also calculate pressure - the pressure term in Navier-Stokes - because the pressure is whatever pressure was needed to make the fluid incompressible
			//This one is the bottleneck
			SolveIncompressibility(numIters, dt, overRelaxation);

			//Fix border velocities
			Extrapolate();

			//Advection
			//Advection is the motion of particles along the bulk flow

			//Move the velocity field along itself (self-advection)
			//Advection should be done in a divergence-free velocity field, which also satisfies the required boundary conditions -> so advect has to come after project or you may get odd artifacts
			//This will introduce viscosity because of numerical error in the advection term (we are using averages) which can be reduced with vorticity confinement
			AdvectVel(dt);

			//Move the smoke along the velocity field
			//Smoke propagates both because it's carried along with the fluid and because it diffuses. The smoke diffuses anyway because numerical error in the advection term (we are using averages)
			AdvectSmoke(dt);

			//Diffusion - the viscosity term in Navier-Stokes 
			//We dont take viscocity into account (yet). But it's not a big problem because the water/air we simulate has low viscocity - and not honey. Also the fluid diffuses anyway because numerical error in the advection term (we are using averages)
		}



		//Modify velocity values from external forces like gravity
		private void Integrate(float dt, float gravity)
		{
			//The source code ignored the border except for x on right side. Might be a bug and should be numX - 1 - not just numX
			for (int j = 1; j < this.numY - 1; j++)
			{
				for (int i = 1; i < this.numX - 1; i++)
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
		//Will also calculate pressure as a bonus because the pressure is the pressure that was needed to make the fluid incompressible
		private void SolveIncompressibility(int numIters, float dt, float overRelaxation)
		{
			//Reset pressure
			System.Array.Fill(p, 0f);

			//Pressure is whatever it takes to make the fluid incompressible and enforce the solid wall boundary conditions
			//Particles in higher pressure regions are pushed towards lower pressure regions
			//To calculate the total pressure needed to make the fluid incompressible we update it incrementally, summing up the total pressure needed to make the fluid incompressible 
			//This is equation 14 in the paper "Fluid flow for the rest of us"
			//p = p + (d/s) * ((rho * h) / dt)
			//Where
			//d - divergence [m/s]
			//s - number of surrounding cells that are not obstacles
			//rho - density [kg/m^3]
			//h - cell size [m]
			//dt - time step [s]

			//To optimize the pressure calculations -> p = p + (d/s) * cp we only need to calculate this once
			float cp = density * h / dt;

			//Gauss-Seidel relaxation
			for (int iter = 0; iter < numIters; iter++)
			{
				//For each cell except the border
				//Looping j before i is faster
				for (int j = 1; j < this.numY - 1; j++)
				{
					for (int i = 1; i < this.numX - 1; i++)
					{
						//Caching these is faster
						int indexThis = To1D(i, j);
						int indexLeft = To1D(i - 1, j);
						int indexRight = To1D(i + 1, j);
						int indexBottom = To1D(i, j - 1);
						int indexTop = To1D(i, j + 1);

						//Cache how many of the surrounding cells are obstacles
						float sBottom = s[indexBottom];
						float sLeft = s[indexLeft];

						//Ignore this cell if its an obstacle
						//Having it here is best from a performance perspective
						if (s[indexThis] == 0f)
						{
							continue;
						}

						float sRight = s[indexRight];
						float sTop = s[indexTop];

						float sTot = sLeft + sRight + sBottom + sTop;

						//Do nothing if all surrounding cells are obstacles
						if (sTot == 0f)
						{
							continue;
						}

						//Divergence = total amount of fluid velocity the leaves the cell 
						//- If it's positive we have too much outflow
						//- If it's negative we have too much inflow
						//- If it's zero the fluid is incompressible = what we want
						//if u[To1D(i + 1, j)] > 0 fluid leaves the cell
						//if u[To1D(i, j)] > 0 fluid enters the cell, so should be negative because we calculate total outflow
						//So total outflow flow in u-direction is u[To1D(i + 1, j)] - u[To1D(i, j)]
						//Same idea applies to v-direction 
						float divergence = u[indexRight] - u[indexThis] + v[indexTop] - v[indexThis];

						//Why the minus sign?
						//From "Realistic Animation of Liquids:"
						//- A positive divergence represents an influx of fluid and would correspond to an increase in cell pressure and subsequent increase in fluid outflow from the cell.
						//- A negative divergence lowers internal pressure and increases inflow from neighboring cells. 
						float divergence_Over_sTot = -divergence / sTot;

						//Multiply by the overrelaxation coefficient to speed up the convergence of Gauss-Seidel relaxation 
						divergence_Over_sTot *= overRelaxation;

						//Calculate the pressure
						//Should overRelaxation be included in the pressure calculations? Relaxation is used to speed up convergence by pretending that the divergence is greater than it actually is. Because we multiply relaxation with the divergence we get a larger divergence and thus larger pressure 
						p[indexThis] += cp * divergence_Over_sTot;

						//Update velocities to ensure incompressibility
						//Signs are flipped compared to video because of the -divergence
						//If sx0 = 0 theres an obstacle to the left of this cell, meaning the velocity from that wall is a constant (can be 0, can have a velocity from a wind turbine, or can have a velocity from a moving obstacle) and we can't modify it to fix the divergence. This means we can only modify the three other velocities  
						//Why are we using u,v instead of uNew and vNew? From "Real time simulation and control of Newtonian fluids...": The convergence rate of the iterations [when using Gauss-Seidel] can be improved by using the newly computed values directly in the same iteration instead of saving them to the next iteration step. This makes it more difficult to parallelize. If you need to parallelize use Jacobi
						u[indexThis] -= sLeft * divergence_Over_sTot;
						u[indexRight] += sRight * divergence_Over_sTot;
						v[indexThis] -= sBottom * divergence_Over_sTot;
						v[indexTop] += sTop * divergence_Over_sTot;
					}
				}
			}
		}



		//Fix the border velocities
		//Copy neighbor values in the tangential direction
		//In the normal direction we have an outflow boundary condition, so fluid can leave or enter, which is useful when we have an outflow from the wind tunnel 
		//The velocities in the normal direction become whatever they need to be to make the fluid incompressible, so we don't touch them here
		//Theres a bug in the original code where we don't check for wall, so we are currently setting a velocity in the wall, but it makes no difference but can be confusing when we sample the cell 
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
		//Will introduce viscocity but can be improved by using a more complicated integration method than Euler Forward
		private void AdvectVel(float dt)
		{
			//Copy current velocities to the new velocities because some cells are not being processed, such as obstacles
			this.u.CopyTo(this.uNew, 0);
			this.v.CopyTo(this.vNew, 0);

			float h = this.h;
			//The position of the velocity components are in the middle of the border of the cells
			float half_h = 0.5f * h;

			for (int j = 1; j < this.numY; j++)
			{
				for (int i = 1; i < this.numX; i++)
				{
					//Update u component
					//u is on the border between 2 cells, so neither of them can be an obstacle, because those velocities are constant and not allowed to be updated
					//Why j < this.numY - 1 here and not in the for loop? Then i < this.numX - 1 wouldn't have been included either
					//Using j < this.numY would have resulted in an error becuase we need the surrounding vs to this u 
					if (this.s[To1D(i, j)] != 0f && this.s[To1D(i - 1, j)] != 0f && j < this.numY - 1)
					{
						//The pos of the u velocity in simulation space
						float x = i * h;
						float y = j * h + half_h;

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
					//v is on the border between 2 cells, so neither of them can be an obstacle
					if (this.s[To1D(i, j)] != 0f && this.s[To1D(i, j - 1)] != 0f && i < numX - 1)
					{
						//The pos of the v velocity in simulation space
						float x = i * h + half_h;
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
		//Find the density of smoke that over a single time step ended up exactly at the cell's center
		private void AdvectSmoke(float dt)
		{
			//Copy all values from m to newM
			this.m.CopyTo(this.mNew, 0);
			
			float h2 = 0.5f * this.h;

			//For all cells except the border
			for (int j = 1; j < this.numY - 1; j++)
			{
				for (int i = 1; i < this.numX - 1; i++)
				{
					//If this cell is not an obstacle
					if (this.s[To1D(i, j)] != 0f)
					{
						//The velocity in the middle of the cell is the average of the velocities on the cell border
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

			//Copy all values from newM to m
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
		//Input is coordinates in simulation space - NOT cell indices
		//See class GridInterpolation for a better explanation of this works 
		public float SampleField(float xP, float yP, SampleArray field)
		{
			GridConstants gridData = new(this.h, this.numX, this.numY);
			

            //Which array do we want to sample?
            //- u is stored in the middle of the vertical cell lines
            //- v is stored in the middle of the horizontal cell lines
            //- smoke is stored in the center of each cell
            GridInterpolation.Grid fieldToSample = GridInterpolation.Grid.center;
            
			float[] arrayToSample = this.m;

            switch (field)
			{
				case SampleArray.uField: fieldToSample = GridInterpolation.Grid.u; arrayToSample = this.u; break;
				case SampleArray.vField: fieldToSample = GridInterpolation.Grid.v; arrayToSample = this.v; break;
            }


			//Sample!
            // C-----D
            // |     |
            // |___P |
            // |   | |
            // A-----B

            //Clamp the sample point so we know we can sample from 4 grid points
            GridInterpolation.ClampInterpolationPoint(xP, yP, gridData, fieldToSample, out float xP_clamped, out float yP_clamped);


            //Figure out which values to interpolate between

            //Get the array index of A 
            GridInterpolation.GetAIndices(xP_clamped, yP_clamped, gridData, fieldToSample, out int xA_index, out int yA_index);

			//The values we want to interpolate between
            float A = arrayToSample[To1D(xA_index + 0, yA_index + 0)];
			float B = arrayToSample[To1D(xA_index + 1, yA_index + 0)];
			float C = arrayToSample[To1D(xA_index + 0, yA_index + 1)];
			float D = arrayToSample[To1D(xA_index + 1, yA_index + 1)];


            //Figure out the interpolation weights

            //Get the (x,y) coordinates of A
            GridInterpolation.GetACoordinates(fieldToSample, xA_index, yA_index, gridData, out float xA, out float yA);

            //The weights for the interpolation between the values
            GridInterpolation.GetWeights(xP_clamped, yP_clamped, xA, yA, gridData, out float wA, out float wB, out float wC, out float wD);


            //The final interpolation
            float interpolatedValue =
				wA * A +
                wB * B +
				wC * C +
				wD * D;

			return interpolatedValue;
		}



		//This is closer how it looked like in the tutorial
        public float SampleFieldOld(float xp_pos, float yp_pos, SampleArray field)
        {
            //Cellsize
            float h = this.h;
            //To simplify and speed up calculations
            float halfH = 0.5f * h;
            float oneOverH = 1f / h;


            //Make sure the sample point is within an area so we can interpolate between 4 points 
            //- u is stored in the middle of the vertical cell lines
            //- v is stored in the middle of the horizontal cell lines
            //- smoke is stored in the center of each cell
            float minXOffset, maxXOffset, minYOffset, maxYOffset;

            minXOffset = maxXOffset = minYOffset = maxYOffset = halfH;

            switch (field)
            {
                case SampleArray.uField: minXOffset = 0f; maxXOffset = h; break;
                case SampleArray.vField: minYOffset = 0f; maxYOffset = h; break;
            }

            xp_pos = Mathf.Max(Mathf.Min(xp_pos, this.numX * h - maxXOffset), minXOffset);
            yp_pos = Mathf.Max(Mathf.Min(yp_pos, this.numY * h - maxYOffset), minYOffset);



            //Figure out which array indices to interpolate between
            //To go from coordinate to cell we generally do: FloorToInt(pos / cellSize) on a non-staggered grid but here we have to compensate for the staggerness 
            float dx = 0f;
            float dy = 0f;

            //Which array do we want to sample?
            float[] f = null;

            switch (field)
            {
                case SampleArray.uField: f = this.u; dy = halfH; break;
                case SampleArray.vField: f = this.v; dx = halfH; break;
                case SampleArray.smokeField: f = this.m; dx = halfH; dy = halfH; break;
            }

            int x0_index = Mathf.Min(Mathf.FloorToInt((xp_pos - dx) * oneOverH), this.numX - 2);
            int x1_index = x0_index + 1;

            int y0_index = Mathf.Min(Mathf.FloorToInt((yp_pos - dy) * oneOverH), this.numY - 2);
            int y1_index = y0_index + 1;


            //Calculate the deltas:
            // C-----D
            // |     |
            // |___P |
            // |   | |
            // A-----B
            //- delta x = the length of the horizontal line going to P
            //- delta y = the length of the vertical line going to P
            float x0_pos = x0_index * h + dx;
            float y0_pos = y0_index * h + dy;

            float deltaX = xp_pos - x0_pos;
            float deltaY = yp_pos - y0_pos;


            //The weights for the interpolation
            float w_01 = deltaX * oneOverH;
            float w_11 = deltaY * oneOverH;

            float w_00 = 1f - w_01;
            float w_10 = 1f - w_11;


            //The values we want to interpolate from
            // C-----D
            // |     |
            // |___P |
            // |   | |
            // A-----B
            float A = f[To1D(x0_index, y0_index)];
            float B = f[To1D(x1_index, y0_index)];
            float C = f[To1D(x0_index, y1_index)];
            float D = f[To1D(x1_index, y1_index)];


            //The final interpolation
            float interpolatedValue =
                w_00 * w_10 * A +
                w_01 * w_10 * B +
                w_00 * w_11 * C +
                w_01 * w_11 * D;


            return interpolatedValue;
        }



        //Return min and max pressure
        public MinMax GetMinMaxPressure()
		{
			float minP = p[0];
			float maxP = p[0];

			for (int i = 0; i < p.Length; i++)
			{
				minP = Mathf.Min(minP, p[i]);
				maxP = Mathf.Max(maxP, p[i]);
			}

			MinMax minMaxPressure = new (minP, maxP);

			return minMaxPressure;
		}
	}
}