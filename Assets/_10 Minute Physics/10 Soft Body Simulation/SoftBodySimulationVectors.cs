using System.Collections;
using System.Collections.Generic;
using UnityEngine;


//Same as SoftBodySimulation but is using Vector3s instead of arrays where an index in the array is x, y, or z 
public class SoftBodySimulationVectors : IGrabbable
{
	//Tetrahedralizer data structures
	private readonly TetrahedronData tetraData;
	private readonly int[] tetIds;
	private readonly int[] tetEdgeIds;

	//Same as in ball physics
	private readonly Vector3[] pos;
	private readonly Vector3[] prevPos;
	private readonly Vector3[] vel;

	//For soft body physics using tetrahedrons
	//The volume at start before deformation
	private readonly float[] restVol;
	//The length of an edge before deformation
	private readonly float[] restEdgeLengths;
	//Inverese mass w = 1/m where m is how nuch mass is connected to each particle
	//If a particle is fixed we set its mass to 0
	private readonly float[] invMass;
	//Should be global so we don't have to create them a million times
	private readonly Vector3[] gradients = new Vector3[4];

	//The Unity mesh to display the soft body mesh
	private Mesh softBodyMesh;

	//How many vertices (particles) and tets do we have?
	private readonly int numParticles;
	private readonly int numTets;

	//Simulation settings
	private readonly Vector3 gravity = new Vector3(0f, -9.81f, 0f);
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
	private readonly Vector3 halfPlayGroundSize = new Vector3(5f, 8f, 5f); 


	//Grabbing with mouse to move mesh around
	
	//The id of the particle we grabed with mouse
	private int grabId = -1;
	//We grab a single particle and then we sit its inverted mass to 0. When we ungrab we have to reset its inverted mass to what itb was before 
	private float grabInvMass = 0f;
	//For custom raycasting
	//public List<Vector3> GetMeshVertices => new List<Vector3>(pos);
	public int[] GetMeshTriangles => tetraData.GetTetSurfaceTriIds;
	public int GetGrabId => grabId;

	//Optimizations
	System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();



	public SoftBodySimulationVectors(MeshFilter meshFilter, TetrahedronData tetraData, Vector3 startPos, float meshScale = 2f)
	{
		//Tetra data structures
		this.tetraData = tetraData;

		float[] verts = tetraData.GetVerts;

		this.tetIds = tetraData.GetTetIds;
		this.tetEdgeIds = tetraData.GetTetEdgeIds;

		this.numParticles = tetraData.GetNumberOfVertices;
		this.numTets = tetraData.GetNumberOfTetrahedrons;

		//Init the pos, prev pos, and vel
		this.pos = new Vector3[verts.Length / 3];

		for (int i = 0; i < verts.Length; i += 3)
		{
			float x = verts[i + 0];
			float y = verts[i + 1];
			float z = verts[i + 2];

			pos[i / 3] = new Vector3(x, y, z) * meshScale;
		}

		this.prevPos = new Vector3[verts.Length / 3];

		for (int i = 0; i < pos.Length; i++)
		{
			prevPos[i] = pos[i];
		}

		this.vel = new Vector3[verts.Length / 3];

		//Init the data structures that are new for soft body mesh
		this.restVol = new float[this.numTets];
		this.restEdgeLengths = new float[tetraData.GetNumberOfEdges]; 
		this.invMass = new float[this.numParticles];

		//Fill the data structures that are new for soft body mesh with start data
		InitSoftBodyPhysics();

		//Move the mesh to its start position
		Translate(startPos);

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

		if (simulate)
		{
			//Update the visual mesh
			UpdateMesh();
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
		for (int i = 0; i < this.restEdgeLengths.Length; i++)
		{
			int id0 = this.tetEdgeIds[2 * i + 0];
			int id1 = this.tetEdgeIds[2 * i + 1];

			this.restEdgeLengths[i] = Vector3.Magnitude(this.pos[id0] - this.pos[id1]);
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
		}
	}



	//Move the particles and handle environment collision
	void PreSolve(float dt, Vector3 gravity)
	{
		//For each particle
		for (int i = 0; i < this.numParticles; i++)
		{
			//This means the particle is fixed, so don't simulate it
			if (this.invMass[i] == 0f)
			{
				continue;
			}

			//Update vel
			vel[i] += dt * gravity;

			//Save old pos
			prevPos[i] = pos[i];

			//Update pos
			pos[i] += dt * vel[i];


			//Handle environment collision

			//Floor collision
			float x = pos[i].x;
			float y = pos[i].y;
			float z = pos[i].z;

			if (y < 0f)
			{
				//Set the pos to previous pos
				pos[i] = prevPos[i];
				//But the y of the previous pos should be at the ground
				pos[i].y = 0f;
			}
			else if (y > halfPlayGroundSize.y)
			{
				pos[i] = prevPos[i];
				pos[i].y = halfPlayGroundSize.y;
			}

			//X
			if (x < -halfPlayGroundSize.x)
			{
				pos[i] = prevPos[i];
				pos[i].x = -halfPlayGroundSize.x;
			}
			else if (x > halfPlayGroundSize.x)
			{
				pos[i] = prevPos[i];
				pos[i].x = halfPlayGroundSize.x;
			}

			//Z
			if (z < -halfPlayGroundSize.z)
			{
				pos[i] = prevPos[i];
				pos[i].z = -halfPlayGroundSize.z;
			}
			else if (z > halfPlayGroundSize.z)
			{
				pos[i] = prevPos[i];
				pos[i].z = halfPlayGroundSize.z;
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
		float oneOverdt = 1f / dt;
	
		//For each particle
		for (int i = 0; i < this.numParticles; i++)
		{
			if (this.invMass[i] == 0f)
			{
				continue;
			}

			//v = (x - xPrev) / dt
			vel[i] = (pos[i] - prevPos[i]) * oneOverdt;
		}
	}


	//Solve distance constraint
	//2 particles:
	//Positions: x0, x1
	//Inverse mass: w0, w1
	//Rest length: l_rest
	//Current length: l
	//Constraint function: C = l - l_rest which is 0 when the constraint is fulfilled 
	//Gradients of constraint function grad_C0 = (x1 - x0) / |x1 - x0| and grad_C1 = -grad_C0
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
			Vector3 id0_minus_id1 = pos[id0] - pos[id1];

			//sqrMargnitude(x0-x1)
			float l = Vector3.Magnitude(id0_minus_id1);

			//If they are at the same pos we get a divisio by 0 later so ignore
			if (l == 0f)
			{
				continue;
			}

			//(xo-x1) * (1/|x0-x1|) = gradC
			Vector3 gradC = id0_minus_id1 / l;
			
			float l_rest = this.restEdgeLengths[i];
			
			float C = l - l_rest;

			//lambda because |grad_Cn|^2 = 1 because if we move a particle 1 unit, the distance between the particles also grows with 1 unit, and w = w0 + w1
			float lambda = -C / (wTot + alpha);

			//Move the vertices x = x + deltaX where deltaX = lambda * w * gradC
			pos[id0] += lambda * w0 * gradC;
			pos[id1] += -lambda * w1 * gradC;
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

				//TODO: These two vec diffs are for some reason the bottleneck
				//(x4 - x2)
				//VecSetDiff(temp, 0, pos, id1, pos, id0);
				Vector3 id1_minus_id0 = pos[id1] - pos[id0];
				//(x3 - x2)
				//VecSetDiff(temp, 1, pos, id2, pos, id0);
				Vector3 id2_minus_id0 = pos[id2] - pos[id0];

				//(x4 - x2)x(x3 - x2)
				//VecSetCross(this.grads, j, this.temp, 0, this.temp, 1);
				Vector3 cross = Vector3.Cross(id1_minus_id0, id2_minus_id0);

				//Multiplying by 1/6 in the denominator is the same as multiplying by 6 in the numerator
				//Im not sure why hes doing it, because it should be faster to multiply C by 6 as in the formula...
				//VecScale(this.grads, j, 1f / 6f);
				Vector3 gradC = cross * (1f / 6f);

				gradients[j] = gradC;

				//w1 * |grad_C1|^2
				//wTimesGrad += this.invMass[this.tetIds[4 * i + j]] * VecLengthSquared(this.grads, j);
				wTimesGrad += this.invMass[this.tetIds[4 * i + j]] * Vector3.SqrMagnitude(gradC);
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
				//VecAdd(this.pos, id, this.grads, j, lambda * this.invMass[id]);
				pos[id] += lambda * this.invMass[id] * gradients[j];
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

		mesh.vertices = pos;
		mesh.triangles = tetraData.GetTetSurfaceTriIds;

		mesh.RecalculateBounds();
		mesh.RecalculateNormals();

		meshFilter.sharedMesh = mesh;

		this.softBodyMesh = meshFilter.sharedMesh;
		this.softBodyMesh.MarkDynamic();
	}

	//Update the mesh with new vertex positions
	private void UpdateMesh()
	{
		this.softBodyMesh.vertices = pos;

		this.softBodyMesh.RecalculateBounds();
		this.softBodyMesh.RecalculateNormals();
	}



	//
	// Help methods
	//

	//Move all vertices a distance of (x, y, z)
	private void Translate(Vector3 moveDist)
	{
		for (int i = 0; i < this.numParticles; i++)
		{
			pos[i] += moveDist;
			prevPos[i] += moveDist;
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
		Vector3 a = pos[id1] - pos[id0];
		Vector3 b = pos[id2] - pos[id0];
		Vector3 c = pos[id3] - pos[id0];

		//a x b
		Vector3 aXb = Vector3.Cross(a, b);

		//1/6 * (a x b) * c
		float volume = Vector3.Dot(aXb, c) / 6f;

		return volume;
	}



	//
	// Mesh user interactions
	//

	//Yeet the mesh upwards
	private void Yeet()
	{
		for (int i = 0; i < this.numParticles; i++)
		{
			//Add constant to y coordinate
			this.pos[i].y += 0.1f;
		}
	}



	//Squash the mesh so it becomes flat against the ground
	void Squeeze()
	{
		for (int i = 0; i < this.numParticles; i++)
		{
			//Set y coordinate to slightly above floor height
			this.pos[i].y = this.floorHeight + 0.01f;
		}

		UpdateMesh();
	}



	//Input pos is the pos in a triangle we get when doing ray-triangle intersection
	public void StartGrab(Vector3 triangleIntersectionPos)
	{
		//Find the closest vertex to the pos on a triangle in the mesh
		float minD2 = float.MaxValue;
		
		this.grabId = -1;
		
		for (int i = 0; i < this.numParticles; i++)
		{
			float d2 = Vector3.SqrMagnitude(triangleIntersectionPos - pos[i]);
			
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

			//Set the position of the vertex to the position where the ray hit the triangle
			pos[grabId] = triangleIntersectionPos;
		}
	}



	public void MoveGrabbed(Vector3 newPos)
	{
		if (this.grabId >= 0)
		{
			pos[grabId] = newPos;
		}
	}



	public void EndGrab(Vector3 newPos, Vector3 newParticleVel)
	{
		if (this.grabId >= 0)
		{
			//Set the mass to whatever mass it was before we grabbed it
			this.invMass[this.grabId] = this.grabInvMass;

			vel[grabId] = newParticleVel;
		}

		this.grabId = -1;
	}



	public void IsRayHittingBody(Ray ray, out CustomHit hit)
	{
		//Mesh data
		Vector3[] vertices = pos;

		int[] triangles = GetMeshTriangles;

		//Find if the ray hit a triangle in the mesh
		UsefulMethods.IsRayHittingMesh(ray, vertices, triangles, out hit);
	}



	public Vector3 GetGrabbedPos()
	{
		return pos[grabId];
    }
}

