using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClothSimulationTutorial : IGrabbable
{
	private readonly ClothData clothData;

	//Same as in ball physics
	private readonly float[] pos;
	private readonly float[] prevPos;
	private readonly float[] vel;

	//For soft body cloth physics
	//Inverese mass w = 1/m where m is how nuch mass is connected to each particle
	//If a particle is fixed we set its mass to 0
	private readonly float[] invMass;
	//vertex index of the edges that prevent stretching (2 vertices)
	private readonly int[] stretchingIds;
	//vertex index of the edges that prevent bending (4 vertices, the first 2 are the common edge, the last 2 are the vertices the bending edge is going between)
	private readonly int[] bendingIds;
	//The rest length of each edge in the triangulation 
	private readonly float[] stretchingRestLengths;
	//The rest length of each edge that connnects two triangles across the common edge to minimize bending
	private readonly float[] bendingRestLengths;

	//This array should be global so we don't have to create it a million times
	//Gradients needed when we calculate the edge constraints 
	private readonly float[] grads = new float[4 * 3];
	

	//The Unity mesh to display the cloth
	private Mesh clothMesh;

	//How many vertices (particles) do we have?
	private readonly int numParticles;

	//Simulation settings
	private readonly float[] gravity = { 0f, -9.81f, 0f };
	private readonly int numSubSteps = 5;
	private bool simulate = true;

	//Soft body behavior settings
	//Compliance (alpha) is the inverse of physical stiffness (k)
	//alpha = 0 means infinitely stiff (hard)
	private readonly float stretchingCompliance;
	private readonly float bendingCompliance;


	//Grabbing with mouse to move mesh around

	//The id of the particle we grabed with mouse
	private int grabId = -1;
	//We grab a single particle and then we sit its inverted mass to 0. When we ungrab we have to reset its inverted mass to what itb was before 
	private float grabInvMass = 0f;
	//For custom raycasting
	public List<Vector3> GetMeshVertices => GenerateMeshVertices(this.pos);
	public int[] GetMeshTriangles => clothData.GetFaceTriIds;
	public int GetGrabId => grabId;



	public ClothSimulationTutorial(MeshFilter meshFilter, ClothData clothData, Vector3 startPosOffset, float meshScale = 1f, float bendingCompliance = 1f)
	{
		this.clothData = clothData;
	
		//Particles
		this.numParticles = clothData.GetVerts.Length / 3;

		this.pos = (float[])clothData.GetVerts.Clone();
		//These can start at 0 because they are filled with their correct values after the first iteration 
		this.prevPos = new float[this.pos.Length];
		this.vel = new float[this.pos.Length];
		this.invMass = new float[this.numParticles];

		//Give the mesh the correct scale
		for (int i = 0; i < this.pos.Length; i++)
		{
			this.pos[i] *= meshScale;
		}

		//Give the mesh the correct start position
		Translate(startPosOffset.x, startPosOffset.y, startPosOffset.z);


		//Stretching and bending constraints

		//If an edge has a neighbor, the neighbors global edge number is in this list (-1 if has no neighbor)
		int[] neighbors = FindTriNeighbors(clothData.GetFaceTriIds);

		int numTris = clothData.GetFaceTriIds.Length / 3;
		
		List<int> edgeIds = new ();
		List<int> triPairIds = new ();

		//For each triangle
		for (int i = 0; i < numTris; i++)
		{
			//For each edge in the triangle
			for (int j = 0; j < 3; j++)
			{
				int id0 = clothData.GetFaceTriIds[3 * i + j];
				int id1 = clothData.GetFaceTriIds[3 * i + (j + 1) % 3];

				//Global edge number
				int n = neighbors[3 * i + j];

				//Each edge only once
				//Create distance constraint
				if (n < 0 || id0 < id1)
				{
					edgeIds.Add(id0);
					edgeIds.Add(id1);
				}

				//Tri pair
				//Create bending constraint
				if (n >= 0)
				{
					//Opposite ids
					//From global edge number to local edge number 
					int ni = Mathf.FloorToInt(n / 3);
					int nj = n % 3;
					
					int id2 = clothData.GetFaceTriIds[3 * i + (j + 2) % 3];
					int id3 = clothData.GetFaceTriIds[3 * ni + (nj + 2) % 3];
					
					//The vertices of the common edge
					triPairIds.Add(id0);
					triPairIds.Add(id1);
					//The vertices the bending edge is going between
					triPairIds.Add(id2);
					triPairIds.Add(id3);
				}
			}
		}


		this.stretchingIds = edgeIds.ToArray();
		this.bendingIds = triPairIds.ToArray();
		this.stretchingRestLengths = new float[this.stretchingIds.Length / 2];
		this.bendingRestLengths = new float[this.bendingIds.Length / 4];

		this.stretchingCompliance = 0f;
		this.bendingCompliance = bendingCompliance;

		//Init the array values
		InitArrays(clothData.GetFaceTriIds);

		//Init the mesh
		InitMesh(meshFilter, clothData.GetFaceTriIds);
	}



	//Identify triangle neighboring edges - also known as opposite or common edge
	//Explained in the video https://www.youtube.com/watch?v=z5oWopN39OU at 4:00
	//Edges are identified by their global edge number (-1 if has no neighbor)
	//How to use this data?
	//Compute the global edge number: 3 * triNumber + localEdgeNumber where localEdgeNumber is from the vertex the edge is going from in counter-clockwise order
	//What's the opposite edge to t0, e2?
	//globalEdgeNumber = 3 * 0 + 2 = 2 -> neighbors[2] = 4
	//How do we go from global edge number to local edge number? 
	//triangle index = FloorToInt(4 / 3) = 1.3333 = 1
	//edge index = 4 % 3 = 1
	//So opposite edge is t1, e1
	private int[] FindTriNeighbors(int[] triIds)
	{
		//Create a list with all edges
		List<ClothEdge> edges = new ();

		int numTris = triIds.Length / 3;

		//For each triangle
		for (int i = 0; i < numTris; i++)
		{
			//For each vertex in the triangle, create 1 edge going from that vertex to the next vertex in the triangle  
			for (int j = 0; j < 3; j++)
			{
				int id0 = triIds[3 * i + j];
				int id1 = triIds[3 * i + (j + 1) % 3]; //% 3 so the last vertex connects to the first vertex

				int globalEdgeNumber = 3 * i + j;

				edges.Add( new ClothEdge(Mathf.Min(id0, id1), Mathf.Max(id0, id1), globalEdgeNumber));
			}
		}

		//Sort so common edges are next to each other, meaning the edge going from 1 -> 2 is followed by the edge going from 2 -> 1, which is now also going from 1 -> 2 because how we defined the edges
		edges.Sort((a, b) => ((a.id0 < b.id0) || (a.id0 == b.id0 && a.id1 < b.id1)) ? -1 : 1);

		//Find matching edges
		int[] neighbors = new int[triIds.Length];

		//Init all edges to have no neighbors
		System.Array.Fill(neighbors, -1);

		//Find opposite edges
		int nr = 0;

		while (nr < edges.Count)
		{
			ClothEdge e0 = edges[nr];
			
			nr++;
			
			if (nr < edges.Count)
			{
				ClothEdge e1 = edges[nr];

				if (e0.id0 == e1.id0 && e0.id1 == e1.id1)
				{
					neighbors[e0.edgeNr] = e1.edgeNr;
					neighbors[e1.edgeNr] = e0.edgeNr;
				}

				nr++;
			}
		}

		return neighbors;
	}




	private void InitArrays(int[] triIds)
	{
		//Init inverse mass
		//How much mass is connected to a vertex? Use the area of the triangle and divide by 3
		int numTris = triIds.Length / 3;

		float[] e0 = { 0f, 0f, 0f };
		float[] e1 = { 0f, 0f, 0f };
		float[] c  = { 0f, 0f, 0f };

		for (int i = 0; i < numTris; i++)
		{
			//Calculate the area of the triangle
			int id0 = triIds[3 * i];
			int id1 = triIds[3 * i + 1];
			int id2 = triIds[3 * i + 2];

			//a
			VectorArrays.VecSetDiff(e0, 0, this.pos, id1, this.pos, id0);
			//b
			VectorArrays.VecSetDiff(e1, 0, this.pos, id2, this.pos, id0);
			//a x b
			VectorArrays.VecSetCross(c, 0, e0, 0, e1, 0);

			//A = 0.5 * |a x b|
			float A = 0.5f * Mathf.Sqrt(VectorArrays.VecLengthSquared(c, 0));
			
			float pInvMass = A > 0f ? 1f / (A / 3f) : 0f;
			
			this.invMass[id0] += pInvMass;
			this.invMass[id1] += pInvMass;
			this.invMass[id2] += pInvMass;
		}


		//Init stretching lengths
		for (int i = 0; i < this.stretchingRestLengths.Length; i++)
		{
			int id0 = this.stretchingIds[2 * i];
			int id1 = this.stretchingIds[2 * i + 1];
			
			this.stretchingRestLengths[i] = Mathf.Sqrt(VectorArrays.VecDistSquared(this.pos, id0, this.pos, id1));
		}

		//Init bending lengths
		for (int i = 0; i < this.bendingRestLengths.Length; i++)
		{
			//Index 0 and 1 in the array is the common edge, 2 and 3 are the vertices the edge is going between
			int id0 = this.bendingIds[4 * i + 2];
			int id1 = this.bendingIds[4 * i + 3];

			this.bendingRestLengths[i] = Mathf.Sqrt(VectorArrays.VecDistSquared(this.pos, id0, this.pos, id1));
		}


		//Attach the cloth to the roof so it doesnt fall down (the vertex to the top left and top right)
		float minX = float.MaxValue;
		float maxX = -float.MaxValue;
		float maxY = -float.MaxValue;

		for (int i = 0; i < this.numParticles; i++)
		{
			minX = Mathf.Min(minX, this.pos[3 * i]);
			maxX = Mathf.Max(maxX, this.pos[3 * i]);
			maxY = Mathf.Max(maxY, this.pos[3 * i + 1]);
		}

		float eps = 0.0001f;

		for (int i = 0; i < this.numParticles; i++)
		{
			float x = this.pos[3 * i];
			float y = this.pos[3 * i + 1];

			if ((y > maxY - eps) && (x < minX + eps || x > maxX - eps))
			{
				this.invMass[i] = 0f;
			}
		}
	}



	//
	// Custom Unity methods being called from another script 
	//

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

		if (simulate)
		{
			//Update the visual mesh
			UpdateMesh();
		}
	}



	public Mesh MyOnDestroy()
	{
		return clothMesh;
	}



	//
	// Simulation
	//

	//Main cloth simulation loop
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



	private void PreSolve(float dt, float[] gravity)
	{
		for (int i = 0; i < this.numParticles; i++)
		{
			//The particle is fixed, so don't simulate it
			if (this.invMass[i] == 0f)
			{
				continue;
			}

			//v = v + dt * g
			VectorArrays.VecAdd(this.vel, i, gravity, 0, dt);

			//xPrev = x
			VectorArrays.VecCopy(this.prevPos, i, this.pos, i);

			//x = x + dt * v
			VectorArrays.VecAdd(this.pos, i, this.vel, i, dt);

			//Floor collision
			float y = this.pos[3 * i + 1];

			if (y < 0f)
			{
				VectorArrays.VecCopy(this.pos, i, this.prevPos, i);

				this.pos[3 * i + 1] = 0.01f;
			}
		}
	}



	private void SolveConstraints(float dt)
	{
		//Two edge constraints
		SolveStretching(this.stretchingCompliance, dt);
		SolveBending(this.bendingCompliance, dt);
	}



	private void PostSolve(float dt)
	{
		//For each particle
		for (int i = 0; i < this.numParticles; i++)
		{
			if (this.invMass[i] == 0f)
			{
				continue;
			}

			//Fix velocity
			//v = (x - xPrev) / dt
			VectorArrays.VecSetDiff(this.vel, i, this.pos, i, this.prevPos, i, 1f / dt);
		}
	}



	//Same as distance constraint in soft body physics, making each edge in the triangulation move towards its rest length
	private void SolveStretching(float compliance, float dt)
	{
		float alpha = compliance / (dt * dt);

		//For each edge
		for (var i = 0; i < this.stretchingRestLengths.Length; i++)
		{
			//2 vertices per edge in the data structure, so multiply by 2 to get the correct vertex index
			int id0 = this.stretchingIds[2 * i];
			int id1 = this.stretchingIds[2 * i + 1];

			float w0 = this.invMass[id0];
			float w1 = this.invMass[id1];

			float wTot = w0 + w1;

			if (wTot == 0f)
			{
				continue;
			}

			//The current length of the edge l

			//x0-x1
			//The result is stored in grads array
			VectorArrays.VecSetDiff(this.grads, 0, this.pos, id0, this.pos, id1);

			//sqrMargnitude(x0-x1)
			float lSqr = VectorArrays.VecLengthSquared(this.grads, 0);

			float l = Mathf.Sqrt(lSqr);

			//If they are at the same pos we get a divisio by 0 later so ignore
			if (l == 0f)
			{
				continue;
			}

			//(xo-x1) * (1/|x0-x1|) = gradC
			VectorArrays.VecScale(this.grads, 0, 1f / l);

			float l_rest = this.stretchingRestLengths[i];

			float C = l - l_rest;

			//lambda because |grad_Cn|^2 = 1 because if we move a particle 1 unit, the distance between the particles also grows with 1 unit, and w = w0 + w1
			float lambda = -C / (wTot + alpha);

			//Move the vertices x = x + deltaX where deltaX = lambda * w * gradC
			VectorArrays.VecAdd(this.pos, id0, this.grads, 0, lambda * w0);
			VectorArrays.VecAdd(this.pos, id1, this.grads, 0, -lambda * w1);
		}
	}



	//Similar to how stretching constraints work 
	//The only difference is how we identify which vertices are part of the edge 
	private void SolveBending(float compliance, float dt)
	{
		float alpha = compliance / (dt * dt);

		//For each edge
		for (int i = 0; i < this.bendingRestLengths.Length; i++)
		{
			//2 vertices per edge, but this edge is going between two vertices opposite of each other in two triangles, crossing the common edge 
			int id0 = this.bendingIds[4 * i + 2];
			int id1 = this.bendingIds[4 * i + 3];
			
			float w0 = this.invMass[id0];
			float w1 = this.invMass[id1];
			
			float wTot = w0 + w1;
			
			if (wTot == 0f)
			{
				continue;
			}

			//The current length of the edge l

			//x0-x1
			//The result is stored in grads array
			VectorArrays.VecSetDiff(this.grads, 0, this.pos, id0, this.pos, id1);

			//sqrMargnitude(x0-x1)
			float lSqr = VectorArrays.VecLengthSquared(this.grads, 0);

			float l = Mathf.Sqrt(lSqr);

			//If they are at the same pos we get a divisio by 0 later so ignore
			if (l == 0f)
			{
				continue;
			}

			//(xo-x1) * (1/|x0-x1|) = gradC
			VectorArrays.VecScale(this.grads, 0, 1f / l);

			float l_rest = this.bendingRestLengths[i];

			float C = l - l_rest;

			//lambda because |grad_Cn|^2 = 1 because if we move a particle 1 unit, the distance between the particles also grows with 1 unit, and w = w0 + w1
			float lambda = -C / (wTot + alpha);

			//Move the vertices x = x + deltaX where deltaX = lambda * w * gradC
			VectorArrays.VecAdd(this.pos, id0, this.grads, 0, lambda * w0);
			VectorArrays.VecAdd(this.pos, id1, this.grads, 0, -lambda * w1);
		}
	}



	//
	// Unity mesh 
	//

	//Init the mesh when the simulation is started
	private void InitMesh(MeshFilter meshFilter, int[] triangles)
	{
		Mesh mesh = new();

		List<Vector3> vertices = GenerateMeshVertices(this.pos);

		mesh.SetVertices(vertices);
		mesh.triangles = triangles;

		mesh.RecalculateBounds();
		mesh.RecalculateNormals();

		meshFilter.sharedMesh = mesh;

		this.clothMesh = meshFilter.sharedMesh;
		this.clothMesh.MarkDynamic();
	}

	//Update the mesh with new vertex positions
	private void UpdateMesh()
	{
		List<Vector3> vertices = GenerateMeshVertices(this.pos);

		this.clothMesh.SetVertices(vertices);

		this.clothMesh.RecalculateBounds();
		this.clothMesh.RecalculateNormals();
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
	// Help methods
	//

	//Move all vertices a distance of (x, y, z)
	private void Translate(float x, float y, float z)
	{
		float[] moveDist = new float[] { x, y, z };

		for (int i = 0; i < this.numParticles; i++)
		{
			VectorArrays.VecAdd(this.pos, i, moveDist, 0);
			VectorArrays.VecAdd(this.prevPos, i, moveDist, 0);
		}
	}



	//
	// Mesh user interactions
	//


	//Yeet the mesh upwards
	private void Yeet()
	{
		for (int i = 0; i < this.numParticles; i++)
		{
			//Dont move the fixed particles
			if (invMass[i] == 0f)
			{
				continue;
            }
		
			//Add constant to y coordinate
			this.pos[3 * i + 1] += 0.1f;
		}
	}



	//Input pos is the pos in a triangle we get when doing ray-triangle intersection
	public void StartGrab(Vector3 triangleIntersectionPos)
	{
		float[] p = new float[] { triangleIntersectionPos.x, triangleIntersectionPos.y, triangleIntersectionPos.z };

		//Find the closest vertex to the pos on a triangle in the mesh
		float minD2 = float.MaxValue;

		this.grabId = -1;

		for (int i = 0; i < this.numParticles; i++)
		{
			float d2 = VectorArrays.VecDistSquared(p, 0, this.pos, i);

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
			VectorArrays.VecCopy(this.pos, this.grabId, p, 0);
		}
	}



	public void MoveGrabbed(Vector3 newPos)
	{
		if (this.grabId >= 0)
		{
			float[] p = new float[] { newPos.x, newPos.y, newPos.z };

			VectorArrays.VecCopy(this.pos, this.grabId, p, 0);
		}
	}



	public void EndGrab(Vector3 newPos, Vector3 vel)
	{
		if (this.grabId >= 0)
		{
			//Set the mass to whatever mass it was before we grabbed it
			this.invMass[this.grabId] = this.grabInvMass;

			float[] v = new float[] { vel.x, vel.y, vel.z };

			VectorArrays.VecCopy(this.vel, this.grabId, v, 0);
		}

		this.grabId = -1;
	}



	public void IsRayHittingBody(Ray ray, out CustomHit hit)
	{
		//Mesh data
		Vector3[] vertices = GetMeshVertices.ToArray();

		int[] triangles = GetMeshTriangles;

		//Find if the ray hit a triangle in the mesh
		Intersections.IsRayHittingMesh(ray, vertices, triangles, out hit);
	}



	public Vector3 GetGrabbedPos()
	{
		Vector3 grabbedPos = new Vector3(pos[3 * grabId + 0], pos[3 * grabId + 1], pos[3 * grabId + 2]);

		return grabbedPos;
	}
}
