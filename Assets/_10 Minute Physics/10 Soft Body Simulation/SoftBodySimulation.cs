using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoftBodySimulation
{
	//Tetrahedralizer data structures
	private readonly int[] tetIds;
	private readonly int[] tetEdgeIds;

	//Same as in ball physics
	private readonly float[] pos;
	private readonly float[] prevPos;
	private readonly float[] vel;

	//For soft body physics using tetrahedrons
	//The volume at start before deformation
	private readonly float[] restVol;
	//The length of an edge before deformation
	private readonly float[] restEdgeLengths;
	//Inverese mass w = 1/m where m is how nuch mass is connected to each particle
	//If a particle is fixed we set its mass to 0
	private readonly float[] invMass;
	//These two arrays should be global so we don't have to create them a million times
	//Needed when we calculate the volume of a tetrahedron
	private readonly float[] temp = new float[4 * 3];
	//Gradients needed when we calculate the edge and volume constraints 
	private readonly float[] grads = new float[4 * 3];

	//The Unity mesh to display the soft body mesh
	private Mesh softBodyMesh;

	//How many vertices (particles) and tets do we have?
	private readonly int numParticles;
	private readonly int numTets;

	//Simulation settings
	private readonly float[] gravity = new float[] { 0f, -9.81f, 0f };
	private readonly int numSubSteps = 10;
	private bool simulate = true;

	//Soft body behavior settings
	//Compliance (alpha) is the inverse of physical stiffness (k)
	//alpha = 0 means infinitely stiff (hard)
	private readonly float edgeCompliance = 5.0f;
	//Should be 0 or the mesh becomes very flat even for small values 
	private readonly float volCompliance = 0.0f;

	//Environment collision data 
	private readonly float floorHeight = 0f;

	//Grabbing with mouse
	private int grabId = -1;
	//We grab a single particle and then we sit its inverted mass to 0. When we ungrab we have to reset its inverted mass to what itb was before 
	private float grabInvMass = 0f;



	public SoftBodySimulation(MeshFilter meshFilter, TetrahedronData tetraData, Vector3 startPos, float meshScale = 2f)
	{
		//Tetra data structures
		float[] verts = tetraData.GetVerts;

		this.tetIds = tetraData.GetTetIds;
		this.tetEdgeIds = tetraData.GetTetEdgeIds;

		this.numParticles = tetraData.GetNumberOfVertices;
		this.numTets = tetraData.GetNumberOfTetrahedrons;

		//Init the pos, prev pos, and vel
		this.pos = new float[verts.Length];

		for (int i = 0; i < pos.Length; i++)
		{
			pos[i] = verts[i];
			pos[i] *= meshScale;
		}

		this.prevPos = new float[verts.Length];
		
		for (int i = 0; i < prevPos.Length; i++)
		{
			prevPos[i] = verts[i];
			prevPos[i] *= meshScale;
		}
		
		this.vel = new float[verts.Length];

		//Init the data structures that are new for soft body mesh
		this.restVol = new float[this.numTets];
		this.restEdgeLengths = new float[tetraData.GetNumberOfEdges]; 
		this.invMass = new float[this.numParticles];

		//Fill the data structures that are new for soft body mesh with start data
		InitSoftBodyPhysics();

		//Move the mesh to its start position
		Translate(startPos.x, startPos.y, startPos.z);

		//Init the mesh
		InitMesh(meshFilter, tetraData);
	}



	public void MyFixedUpdate()
	{
		if (!simulate)
		{
			return;
        }
	
		Simulate();
	}



	public void MyUpdate()
	{
		simulate = true;
	
		//Launch the mesh upwards when pressing space
		if (Input.GetKey(KeyCode.Space))
		{
			Yeet();
		}

		//Make the mesh flat when holding right mouse 
		if (Input.GetMouseButton(1))
		{
			Squeeze();

			simulate = false;
		}

		//Grabbing with left mouse button
	}



	public Mesh MyOnDestroy()
	{
		return softBodyMesh;
	}



	//
	// Simulation
	//

	//Fill the data structures needed or soft body physics
	private void InitSoftBodyPhysics()
	{
		//Init rest volume
		for (int i = 0; i < this.numTets; i++)
		{
			this.restVol[i] = GetTetVolume(i);
		}


		//Init inverse mass (1/w)
		for (int i = 0; i < this.numTets; i++)
		{
			float vol = restVol[i];
			
			//The mass connected to a particle in a tetra is roughly volume / 4
			float pInvMass = vol > 0f ? 1f / (vol / 4f) : 0f;

			this.invMass[this.tetIds[4 * i + 0]] += pInvMass;
			this.invMass[this.tetIds[4 * i + 1]] += pInvMass;
			this.invMass[this.tetIds[4 * i + 2]] += pInvMass;
			this.invMass[this.tetIds[4 * i + 3]] += pInvMass;
		}


		//Init rest edge length
		for (var i = 0; i < this.restEdgeLengths.Length; i++)
		{
			var id0 = this.tetEdgeIds[2 * i + 0];
			var id1 = this.tetEdgeIds[2 * i + 1];

			this.restEdgeLengths[i] = Mathf.Sqrt(VecDistSquared(this.pos, id0, this.pos, id1));
		}
	}



	//Main soft body simulation loop
	void Simulate()
	{
		float dt = Time.fixedDeltaTime;

		float sdt = dt / this.numSubSteps;

		for (int step = 0; step < this.numSubSteps; step++)
		{
			PreSolve(sdt, this.gravity);

			SolveConstraints(sdt);

			PostSolve(sdt);

			UpdateMeshes();
		}
	}



	//Move the particles and handle environment collision
	void PreSolve(float dt, float[] gravity)
	{
		//For each particle
		for (var i = 0; i < this.numParticles; i++)
		{
			//This means the particle is fixed, so don't simulate it
			if (this.invMass[i] == 0f)
			{
				continue;
			}
			
			//v = v + dt * g
			VecAdd(this.vel, i, gravity, 0, dt);
			
			//xPrev = x
			VecCopy(this.prevPos, i, this.pos, i);

			//x = x + dt * v
			VecAdd(this.pos, i, this.vel, i, dt);
			

			//Handle environment collision

			//Floor collision
			float y = this.pos[3 * i + 1];
			
			if (y < 0f)
			{
				//Set the pos to previous pos
				VecCopy(this.pos, i, this.prevPos, i);
				//But the y of the previous pos should be at the ground
				this.pos[3 * i + 1] = 0f;
			}
		}
	}



	//Handle the soft body physics
	void SolveConstraints(float dt)
	{
		//Constraints
		//Enforce constraints by moving each vertex: x = x + deltaX
		//- Correction vector: deltaX = lambda * w * gradC
		//- Inverse mass: w
		//- lambda = -C / (w1 * |grad_C1|^2 + w2 * |grad_C2|^2 + ... + wn * |grad_C|^2 + (alpha / dt^2)) where 1, 2, ... n is the number of participating particles in the constraint.
		//		- n = 2 if we have an edge, n = 4 if we have a tetra
		//		- |grad_C1|^2 is the squared length
		//		- (alpha / dt^2) is what makes the costraint soft. Remove it and you get a hard constraint
		//- Compliance (inverse stiffness): alpha 

		this.SolveEdges(this.edgeCompliance, dt);
		this.SolveVolumes(this.volCompliance, dt);
	}



	//Fix velocity
	void PostSolve(float dt)
	{
		//For each particle
		for (var i = 0; i < this.numParticles; i++)
		{
			if (this.invMass[i] == 0f)
			{
				continue;
			}

			//v = (x - xPrev) / dt
			VecSetDiff(this.vel, i, this.pos, i, this.prevPos, i, 1f / dt);
		}
	}


	//Solve distance constraint
	//2 particles:
	//Positions: x0, x1
	//Inverse mass: w0, w1
	//Rest length: l_rest
	//Current length: l
	//Constraint function: C = l - l_rest which is 0 when the constraint is fulfilled 
	//Gradients of constraint function grad_C0 = (x1 - x0) / abs(x1 - x0) and grad_C1 = -grad_C0
	//Which was shown here https://www.youtube.com/watch?v=jrociOAYqxA (12:10)
	void SolveEdges(float compliance, float dt)
	{
		float alpha = compliance / (dt * dt);

		//For each edge
		for (int i = 0; i < this.restEdgeLengths.Length; i++)
		{
			//2 vertices per edge in the data structure, so multiply by 2 to get the correct vertex index
			int id0 = this.tetEdgeIds[2 * i    ];
			int id1 = this.tetEdgeIds[2 * i + 1];

			float w0 = this.invMass[id0];
			float w1 = this.invMass[id1];
			float wTot = w0 + w1;
			
			//This edge is fixed so dont simulate
			if (wTot == 0f)
			{
				continue;
			}

			//The current length of the edge l

			//x0-x1
			//The result is stored in grads array
			VecSetDiff(this.grads, 0, this.pos, id0, this.pos, id1);

			//sqrMargnitude(x0-x1)
			float lSqr = VecLengthSquared(this.grads, 0);

			float l = Mathf.Sqrt(lSqr);

			//If they are at the same pos we get a divisio by 0 later so ignore
			if (l == 0f)
			{
				continue;
			}

			//(xo-x1) * (1/abs(x0-x1)) = gradC
			VecScale(this.grads, 0, 1f / l);
			
			float l_rest = this.restEdgeLengths[i];
			
			float C = l - l_rest;

			//lambda because |grad_Cn|^2 = 1 because if we move a particle 1 unit, the distance between the particles also grows with 1 unit, and w = w0 + w1
			float lambda = -C / (wTot + alpha);
			
			//Move the vertices x = x + deltaX where deltaX = lambda * w * gradC
			VecAdd(this.pos, id0, this.grads, 0,  lambda * w0);
			VecAdd(this.pos, id1, this.grads, 0, -lambda * w1);
		}
	}



	//Solve volume constraint
	//Constraint function is now defined as C = 6(V - V_rest). The 6 is to make the equation simpler because of volume
	//4 gradients:
	//grad_C1 = (x4-x2)x(x3-x2) <- direction perpendicular to the triangle opposite of p1 to maximally increase the volume when moving p1
	//grad_C2 = (x3-x1)x(x4-x1)
	//grad_C3 = (x4-x1)x(x2-x1)
	//grad_C4 = (x2-x1)x(x3-x1)
	//V = 1/6 * ((x2-x1)x(x3-x1))*(x4-x1)
	//lambda =  6(V - V_rest) / (w1 * |grad_C1|^2 + w2 * |grad_C2|^2 + w3 * |grad_C3|^2 + w4 * |grad_C4|^2 + alpha/dt^2)
	//delta_xi = -lambda * w_i * grad_Ci
	//Which was shown here https://www.youtube.com/watch?v=jrociOAYqxA (13:50)
	void SolveVolumes(float compliance, float dt)
	{
		float alpha = compliance / (dt * dt);

		//For each tetra
		for (int i = 0; i < this.numTets; i++)
		{
			float wTimesGrad = 0f;

			//Foreach vertex in the tetra
			for (int j = 0; j < 4; j++)
			{
				//The 3 opposite vertices
				int id0 = this.tetIds[4 * i + TetrahedronData.volIdOrder[j][0]];
				int id1 = this.tetIds[4 * i + TetrahedronData.volIdOrder[j][1]];
				int id2 = this.tetIds[4 * i + TetrahedronData.volIdOrder[j][2]];

				//(x4 - x2)
				VecSetDiff(this.temp, 0, this.pos, id1, this.pos, id0);
				//(x3 - x2)
				VecSetDiff(this.temp, 1, this.pos, id2, this.pos, id0);

				//(x4 - x2)x(x3 - x2)
				VecSetCross(this.grads, j, this.temp, 0, this.temp, 1);
				
				//Multiplying by 1/6 in the denominator is the same as multiplying by 6 in the numerator
				//Im not sure why hes doing it, because it should be faster to multiply C by 6 as in the formula...
				VecScale(this.grads, j, 1f / 6f);

				//w1 * |grad_C1|^2
				wTimesGrad += this.invMass[this.tetIds[4 * i + j]] * VecLengthSquared(this.grads, j);
			}

			//All vertices are fixed so dont simulate
			if (wTimesGrad == 0f)
			{
				continue;
			}

			float vol = GetTetVolume(i);
			float restVol = this.restVol[i];
			
			float C = vol - restVol;

			//The guy in the video is dividing by 6 in the code but multiplying in the video
			//C *= 6f;

			float lambda = -C / (wTimesGrad + alpha);

			//Move each vertex
			for (int j = 0; j < 4; j++)
			{
				int id = this.tetIds[4 * i + j];

				//Move the vertices x = x + deltaX where deltaX = lambda * w * gradC
				VecAdd(this.pos, id, this.grads, j, lambda * this.invMass[id]);
			}
		}
	}



	//
	// Unity mesh 
	//

	//Init the mesh when the simulation is started
	private void InitMesh(MeshFilter meshFilter, TetrahedronData tetraData)
	{
		Mesh mesh = new();

		List<Vector3> vertices = GenerateMeshVertices(this.pos);

		mesh.SetVertices(vertices);
		mesh.triangles = tetraData.GetTetSurfaceTriIds;
		mesh.RecalculateNormals();

		meshFilter.sharedMesh = mesh;

		this.softBodyMesh = meshFilter.sharedMesh;
		this.softBodyMesh.MarkDynamic();
	}

	//Update the mesh with new vertex positions
	private void UpdateMeshes()
	{
		List<Vector3> vertices = GenerateMeshVertices(this.pos);

		this.softBodyMesh.SetVertices(vertices);
		this.softBodyMesh.RecalculateBounds();
		this.softBodyMesh.RecalculateNormals();
	}

	//Generate the List of vertices needed for a Unity mesh
	private List<Vector3> GenerateMeshVertices(float[] pos)
	{
		List<Vector3> vertices = new();

		for (int i = 0; i < pos.Length; i += 3)
		{
			Vector3 v = new(pos[i], pos[i + 1], pos[i + 2]);

			vertices.Add(v);
		}

		return vertices;
	}



	//
	// Vector operations
	//

	//anr = index of vertex a in the list of all vertices if it had an (x,y,z) position at that index. BUT we dont have that list, so to make it easier to loop through all particles where x, y, z are at 3 difference indices, we multiply by 3
	//anr = 0 -> 0 * 3 = 0 -> 0, 1, 2
	//anr = 1 -> 1 * 3 = 3 -> 3, 4, 5

	//a = 0
	private void VecSetZero(float[] a, int anr)
	{
		anr *= 3;

		a[anr    ] = 0f;
		a[anr + 1] = 0f;
		a[anr + 2] = 0f;
	}

	//a * scale
	private void VecScale(float[] a, int anr, float scale)
	{
		anr *= 3;

		a[anr    ] *= scale;
		a[anr + 1] *= scale;
		a[anr + 2] *= scale;
	}

	//a = b
	private void VecCopy(float[] a, int anr, float[] b, int bnr)
	{
		anr *= 3; 
		bnr *= 3;

		a[anr    ] = b[bnr    ];
		a[anr + 1] = b[bnr + 1];
		a[anr + 2] = b[bnr + 2];
	}

	//a = a + (b * scale) 
	private void VecAdd(float[] a, int anr, float[] b, int bnr, float scale = 1f)
	{
		anr *= 3; 
		bnr *= 3;
		
		a[anr    ] += b[bnr    ] * scale;
		a[anr + 1] += b[bnr + 1] * scale;
		a[anr + 2] += b[bnr + 2] * scale;
	}

	//diff = (a - b) * scale
	//Need the scale to simplify this v = (x - xPrev) / dt then scale is 1f/dt
	private void VecSetDiff(float[] diff, int dnr, float[] a, int anr, float[] b, int bnr, float scale = 1f)
	{
		dnr *= 3; 
		anr *= 3; 
		bnr *= 3;
		
		diff[dnr    ] = (a[anr    ] - b[bnr    ]) * scale;
		diff[dnr + 1] = (a[anr + 1] - b[bnr + 1]) * scale;
		diff[dnr + 2] = (a[anr + 2] - b[bnr + 2]) * scale;
	}


	//sqrMagnitude(a) 
	private float VecLengthSquared(float[] a, int anr)
	{
		anr *= 3;

		float a0 = a[anr    ]; 
		float a1 = a[anr + 1]; 
		float a2 = a[anr + 2];
		
		float lengthSqr = a0 * a0 + a1 * a1 + a2 * a2;

		return lengthSqr;
	}
	
	//sqrMagnitude(a - b)
	private float VecDistSquared(float[] a, int anr, float[] b, int bnr)
	{
		anr *= 3; 
		bnr *= 3;
		
		float a0 = a[anr    ] - b[bnr    ]; 
		float a1 = a[anr + 1] - b[bnr + 1]; 
		float a2 = a[anr + 2] - b[bnr + 2];
		
		float distSqr = a0 * a0 + a1 * a1 + a2 * a2;

		return distSqr;
	}

	//a dot b
	private float VecDot(float[] a, int anr, float[] b, int bnr)
	{
		anr *= 3; 
		bnr *= 3;
		
		float dot = a[anr] * b[bnr] + a[anr + 1] * b[bnr + 1] + a[anr + 2] * b[bnr + 2];

		return dot;
	}

	//a = b x c
	private void VecSetCross(float[] a, int anr, float[] b, int bnr, float[] c, int cnr)
	{
		anr *= 3; 
		bnr *= 3; 
		cnr *= 3;
		
		a[anr    ] = b[bnr + 1] * c[cnr + 2] - b[bnr + 2] * c[cnr + 1];
		a[anr + 1] = b[bnr + 2] * c[cnr    ] - b[bnr    ] * c[cnr + 2];
		a[anr + 2] = b[bnr    ] * c[cnr + 1] - b[bnr + 1] * c[cnr    ];
	}



	//
	// Help methods
	//

	//Move all vertices a distance of (x, y, z)
	private void Translate(float x, float y, float z)
	{
		float[] moveDist = new float[] { x, y, z };

		for (var i = 0; i < this.numParticles; i++)
		{
			VecAdd(this.pos,     i, moveDist, 0);
			VecAdd(this.prevPos, i, moveDist, 0);
		}
	}



	//Calculate the volume of a tetrahedron
	//V = 1/6 * (a x b) * c where a,b,c all originate from the same vertex 
	//Tetra p1 p2 p3 p4 -> a = p2-p1, b = p3-p1, c = p4-p1
	float GetTetVolume(int nr)
	{
		//The 4 vertices belonging to this tetra 
		int id0 = this.tetIds[4 * nr + 0];
		int id1 = this.tetIds[4 * nr + 1];
		int id2 = this.tetIds[4 * nr + 2];
		int id3 = this.tetIds[4 * nr + 3];

		//a, b, c
		//temp has size 12 so we fill 3*3 = 9 positions in that array where a is the first 3 coordinates
		VecSetDiff(this.temp, 0, this.pos, id1, this.pos, id0);
		VecSetDiff(this.temp, 1, this.pos, id2, this.pos, id0);
		VecSetDiff(this.temp, 2, this.pos, id3, this.pos, id0);
		
		//a x b
		//Here we fill the last 3 positions in the array with the cross product, a starts at index 0*3 and b at index 1*3
		VecSetCross(this.temp, 3, this.temp, 0, this.temp, 1);

		//1/6 * (a x b) * c
		//(a x b) is stored at index 3*3 and c is in index 2*3
		float volume = VecDot(this.temp, 3, this.temp, 2) / 6f;

		return volume;
	}



	//
	// Mesh user interactions
	//

	//Yeet the mesh upwards
	private void Yeet()
	{
		for (var i = 0; i < this.numParticles; i++)
		{
			//Add constant to y coordinate
			this.pos[3 * i + 1] += 0.1f;
		}
	}



	//Squash the mesh so it becomes flat against the ground
	void Squeeze()
	{
		for (var i = 0; i < this.numParticles; i++)
		{
			//Set y coordinate to slightly above floor height
			this.pos[3 * i + 1] = this.floorHeight + 0.01f;
		}

		UpdateMeshes();
	}



	private void StartGrab(float[] pos)
	{
		float[] p = new float[] { pos[0], pos[1], pos[2] };

		float minD2 = float.MaxValue;
		
		this.grabId = -1;
		
		for (int i = 0; i < this.numParticles; i++)
		{
			float d2 = VecDistSquared(p, 0, this.pos, i);
			
			if (d2 < minD2)
			{
				minD2 = d2;
				this.grabId = i;
			}
		}

		//We have found a vertex
		if (this.grabId >= 0)
		{
			//Save the current innverted mass
			this.grabInvMass = this.invMass[this.grabId];
			
			//Set the inverted mass to 0 to mark it as fixed
			this.invMass[this.grabId] = 0f;

			//Set the position of the vertex to the position of the mouse coordinate
			VecCopy(this.pos, this.grabId, p, 0);
		}
	}



	private void MoveGrabbed(float[] pos, float[] vel)
	{
		if (this.grabId >= 0)
		{
			float[] p = new float[] { pos[0], pos[1], pos[2] };

			VecCopy(this.pos, this.grabId, p, 0);
		}
	}



	private void EndGrab()
	{
		if (this.grabId >= 0)
		{
			//Set the mass to whatever mass it was before we grabbed it
			this.invMass[this.grabId] = this.grabInvMass;

			float[] v = new float[] { vel[0], vel[1], vel[2] };
			
			VecCopy(this.vel, this.grabId, v, 0);
		}

		this.grabId = -1;
	}
}

