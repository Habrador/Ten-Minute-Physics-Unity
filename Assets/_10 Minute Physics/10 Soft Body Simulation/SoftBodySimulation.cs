using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoftBodySimulation
{
	//Tetrahedralizer data structures
	private int[] tetIds;
	private int[] tetEdgeIds;

	//Simulation settings
	private readonly float[] gravity = new float[] { 0.0f, -9.81f, 0.0f };
	private readonly int numSubSteps = 10;

	//Environment collision data 
	private readonly float floorHeight = 0f;

	//Compliance alpha is the inverse of physical stiffness k
	//alpha = 0 means infinitely stiff (hard)
	private float edgeCompliance = 5.0f;
	//Should be 0 or the mesh becomes very flat even for small values 
	private float volCompliance = 0.0f;

	//The Unity mesh used to display the soft body mesh
	private Mesh softBodyMesh;
	
	//How many vertices (particles) and tets do we have?
	private readonly int numParticles;
	private readonly int numTets;


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
	private readonly float[] invMass;
	//Needed when we calculate the volume of a tetrahedron so we don't have to create an array a million times
	private readonly float[] temp;
	//C
	private readonly float[] grads;


	//Grabbing with mouse
	private int grabId = -1;
	private float grabInvMass = 0.0f;



	public SoftBodySimulation(MeshFilter meshFilter, TetrahedronData tetraData, float meshScale = 2f)
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
		this.restEdgeLengths = new float[this.tetEdgeIds.Length / 2];
		this.invMass = new float[this.numParticles];
		this.temp = new float[4 * 3];
		this.grads = new float[4 * 3];

		InitSoftBodyPhysics();

		//Move the bunny upwards
		Translate(0.0f, 20.0f, 0.0f);

		//Init the mesh
		InitMesh(meshFilter, tetraData);
	}

    



	public void MyFixedUpdate()
	{
		Simulate();
	}



	public void MyUpdate()
	{
		if (Input.GetKey(KeyCode.Space))
		{
			Yeet();
			//Squash();
		}
	}



	public Mesh MyOnDestroy()
	{
		return softBodyMesh;
	}



	//
	// Simulation
	//

	//Fill the data structures needed or soft body physics
	void InitSoftBodyPhysics()
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
			
			//How much mass is connected to a particle in a tetra = volume / 4
			//But we want the inverse mass because its how the lambda equation uses the mass so we save computations
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



	void Simulate()
	{
		float dt = Time.fixedDeltaTime;

		var sdt = dt / this.numSubSteps;

		for (var step = 0; step < this.numSubSteps; step++)
		{
			PreSolve(sdt, gravity);

			SolveConstraints(sdt);

			PostSolve(sdt);

			UpdateMeshes();
		}
	}



	void PreSolve(float dt, float[] gravity)
	{
		for (var i = 0; i < this.numParticles; i++)
		{
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



	void SolveConstraints(float dt)
	{
		//Constraints
		//x = x + deltaX where deltaX is the correction vector
		//deltaX = lambda * w * gradC
		//lambda = -C / (w1 * abs(grad_C1)^2 + w2 * abs(grad_C2)^2 + ... + (alpha / dt^2)) where w1, w2, ... wn is the number of participating particles in the constraint. n=2 if we have an edge, n=4 if we have a tetra
		//alpha = compliance

		this.SolveEdges(this.edgeCompliance, dt);
		this.SolveVolumes(this.volCompliance, dt);
	}



	void PostSolve(float dt)
	{
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
	//Positions: x1, x2
	//Inverse mass: w1, w2
	//Rest length l_rest
	//Current length: l
	//Constraint function: C = l - l_rest which is 0 when the constraint is fulfilled 
	//Gradients of constraint function grad_C1 = (x2 - x1) / abs(x2 - x1) and grad_C2 = -grad_C1
	//delta_x1 = w1 / (w1 + w2) * C * grad_C1
	//delta_x2 = w2 / (w1 + w2) * C * grad_C2
	//Which was shown here https://www.youtube.com/watch?v=jrociOAYqxA (13:30)
	void SolveEdges(float compliance, float dt)
	{
		var alpha = compliance / dt / dt;

		for (int i = 0; i < this.restEdgeLengths.Length; i++)
		{
			int id0 = this.tetEdgeIds[2 * i];
			int id1 = this.tetEdgeIds[2 * i + 1];

			float w0 = this.invMass[id0];
			float w1 = this.invMass[id1];
			float w = w0 + w1;
			
			if (w == 0f)
			{
				continue;
			}

			VecSetDiff(this.grads, 0, this.pos, id0, this.pos, id1);
			
			float len = Mathf.Sqrt(VecLengthSquared(this.grads, 0));

			if (len == 0.0f)
			{
				continue;
			}

			VecScale(this.grads, 0, 1.0f / len);
			
			float restLen = this.restEdgeLengths[i];
			float C = len - restLen;
			float s = -C / (w + alpha);
			
			VecAdd(this.pos, id0, this.grads, 0, s * w0);
			VecAdd(this.pos, id1, this.grads, 0, -s * w1);
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
	//lambda =  6(V - V_rest) / (w1 * abs(grad_C1)^2 + w2 * abs(grad_C2)^2 + w3 * abs(grad_C3)^2 + w4 * abs(grad_C4)^2)
	//delta_xi = -lambda * w_i * grad_Ci
	void SolveVolumes(float compliance, float dt)
	{
		var alpha = compliance / dt / dt;

		for (int i = 0; i < this.numTets; i++)
		{
			float w = 0f;

			for (int j = 0; j < 4; j++)
			{
				int id0 = this.tetIds[4 * i + TetrahedronData.volIdOrder[j][0]];
				int id1 = this.tetIds[4 * i + TetrahedronData.volIdOrder[j][1]];
				int id2 = this.tetIds[4 * i + TetrahedronData.volIdOrder[j][2]];

				VecSetDiff(this.temp, 0, this.pos, id1, this.pos, id0);
				VecSetDiff(this.temp, 1, this.pos, id2, this.pos, id0);

				VecSetCross(this.grads, j, this.temp, 0, this.temp, 1);
				
				VecScale(this.grads, j, 1.0f / 6.0f);

				w += this.invMass[this.tetIds[4 * i + j]] * VecLengthSquared(this.grads, j);
			}

			if (w == 0f)
			{
				continue;
			}

			float vol = this.GetTetVolume(i);
			float restVol = this.restVol[i];
			
			float C = vol - restVol;
			
			float s = -C / (w + alpha);

			for (int j = 0; j < 4; j++)
			{
				int id = this.tetIds[4 * i + j];

				VecAdd(this.pos, id, this.grads, j, s * this.invMass[id]);
			}
		}
	}



	//
	// Unity mesh 
	//

	private void InitMesh(MeshFilter meshFilter, TetrahedronData tetData)
	{
		Mesh mesh = new();

		List<Vector3> vertices = GenerateMeshVertices(this.pos);

		mesh.SetVertices(vertices);
		mesh.triangles = tetData.GetTetSurfaceTriIds;
		mesh.RecalculateNormals();

		meshFilter.sharedMesh = mesh;

		this.softBodyMesh = meshFilter.sharedMesh;
		this.softBodyMesh.MarkDynamic();
	}

	private void UpdateMeshes()
	{
		List<Vector3> vertices = GenerateMeshVertices(this.pos);

		this.softBodyMesh.SetVertices(vertices);
		this.softBodyMesh.RecalculateBounds();
		this.softBodyMesh.RecalculateNormals();
	}

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

	//anr = vertex a index in the list of all vertices if they had (x,y,z) position at that array index. BUT we dont have that list, so to make it easier to loop through all particles where x, y, z are at 3 difference indices, we just multiply by 3
	//anr = 0 -> 0 * 3 = 0 -> 0, 1, 2
	//anr = 1 -> 1 * 3 = 3 -> 3, 4, 5

	//a = 0
	private void VecSetZero(float[] a, int anr)
	{
		anr *= 3;

		a[anr + 0] = 0f;
		a[anr + 1] = 0f;
		a[anr + 2] = 0f;
	}

	//a * scale
	private void VecScale(float[] a, int anr, float scale)
	{
		anr *= 3;

		a[anr + 0] *= scale;
		a[anr + 1] *= scale;
		a[anr + 2] *= scale;
	}

	//a = b
	private void VecCopy(float[] a, int anr, float[] b, int bnr)
	{
		anr *= 3; 
		bnr *= 3;

		a[anr + 0] = b[bnr + 0];
		a[anr + 1] = b[bnr + 1];
		a[anr + 2] = b[bnr + 2];
	}

	//a = a + (b * scale) 
	private void VecAdd(float[] a, int anr, float[] b, int bnr, float scale = 1f)
	{
		anr *= 3; 
		bnr *= 3;
		
		a[anr + 0] += b[bnr + 0] * scale;
		a[anr + 1] += b[bnr + 1] * scale;
		a[anr + 2] += b[bnr + 2] * scale;
	}

	//diff = (a - b) * scale
	private void VecSetDiff(float[] diff, int dnr, float[] a, int anr, float[] b, int bnr, float scale = 1f)
	{
		dnr *= 3; 
		anr *= 3; 
		bnr *= 3;
		
		diff[dnr + 0] = (a[anr + 0] - b[bnr + 0]) * scale;
		diff[dnr + 1] = (a[anr + 1] - b[bnr + 1]) * scale;
		diff[dnr + 2] = (a[anr + 2] - b[bnr + 2]) * scale;
	}


	//lengthSqr(a) 
	private float VecLengthSquared(float[] a, int anr)
	{
		anr *= 3;

		float a0 = a[anr + 0]; 
		float a1 = a[anr + 1]; 
		float a2 = a[anr + 2];
		
		float lengthSqr = a0 * a0 + a1 * a1 + a2 * a2;

		return lengthSqr;
	}

	//lengthSqr(a - b)
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
		
		a[anr + 0] = b[bnr + 1] * c[cnr + 2] - b[bnr + 2] * c[cnr + 1];
		a[anr + 1] = b[bnr + 2] * c[cnr + 0] - b[bnr + 0] * c[cnr + 2];
		a[anr + 2] = b[bnr + 0] * c[cnr + 1] - b[bnr + 1] * c[cnr + 0];
	}


	//
	// Help methods
	//

	//Move all vertices a distance of (x, y, z)
	private void Translate(float x, float y, float z)
	{
		for (var i = 0; i < this.numParticles; i++)
		{
			VecAdd(this.pos, i, new float[] { x, y, z }, 0);
			VecAdd(this.prevPos, i, new float[] { x, y, z }, 0);
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

		//Is needed!
		UpdateMeshes();
	}



	void StartGrab(float[] pos)
	{
		var p = new float[] { pos[0], pos[1], pos[2] };
		float minD2 = System.Single.MaxValue;
		this.grabId = -1;
		for (int i = 0; i < this.numParticles; i++)
		{
			var d2 = VecDistSquared(p, 0, this.pos, i);
			if (d2 < minD2)
			{
				minD2 = d2;
				this.grabId = i;
			}
		}

		if (this.grabId >= 0)
		{
			this.grabInvMass = this.invMass[this.grabId];
			this.invMass[this.grabId] = 0.0f;
			VecCopy(this.pos, this.grabId, p, 0);
		}
	}



	void MoveGrabbed(float[] pos, float[] vel)
	{
		if (this.grabId >= 0)
		{
			var p = new float[] { pos[0], pos[1], pos[2] };
			VecCopy(this.pos, this.grabId, p, 0);
		}
	}



	void EndGrab(float[] pos, float[] vel)
	{
		if (this.grabId >= 0)
		{
			this.invMass[this.grabId] = this.grabInvMass;
			var v = new float[] { vel[0], vel[1], vel[2] };
			VecCopy(this.vel, this.grabId, v, 0);
		}
		this.grabId = -1;
	}
}

