using System.Collections;
using System.Collections.Generic;
using UnityEngine;


//Same as SoftBodySimulation but is using Vector3 instead of arrays where an index in the array is x, y, or z 
//This makes the code simpler to read buy maye a little slower according to the guy in the video, but I don't notice much difference...
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
	//The volume of each undeformed tetrahedron
	private readonly float[] restVolumes;
	//The length of an undeformed tetrahedron edge
	private readonly float[] restEdgeLengths;
	//Inverese mass w = 1/m where m is how much mass is connected to a particle
	//If a particle is fixed we set its mass to 0
	private readonly float[] invMass;
	//Should be global so we don't have to create them a million times
	private readonly Vector3[] gradients = new Vector3[4];

	//The Unity mesh to display the soft body mesh
	private Mesh softBodyMesh;

	//How many vertices (particles) and tets do we have?
	private readonly int numParticles;
	private readonly int numTets;
	private readonly int numEdges;

	//Simulation settings
	private readonly Vector3 gravity = new Vector3(0f, -9.81f, 0f);
	//3 steps is minimum or the bodies will lose their shape  
	private readonly int numSubSteps = 3;
	//To pause the simulation
	private bool simulate = true;

	//Soft body behavior settings
	//Compliance (alpha) is the inverse of physical stiffness (k)
	//alpha = 0 means infinitely stiff (hard)
	private readonly float edgeCompliance = 2f;
	//Should be 0 or the mesh becomes very flat even for small values 
	private readonly float volCompliance = 0.0f;

	//Environment collision data 
	private readonly float floorHeight = 0f;
	private Vector3 halfPlayGroundSize = new Vector3(5f, 8f, 5f); 


	//Grabbing with mouse to move mesh around
	
	//The id of the particle we grabed with mouse
	private int grabId = -1;
	//We grab a single particle and then we sit its inverted mass to 0. When we ungrab we have to reset its inverted mass to what itb was before 
	private float grabInvMass = 0f;
	//For custom raycasting
	public int[] GetMeshTriangles => tetraData.GetTetSurfaceTriIds;
	public int GetGrabId => grabId;



	public SoftBodySimulationVectors(MeshFilter meshFilter, TetrahedronData tetraData, Vector3 startPos, float meshScale = 2f)
	{
		//Tetra data structures
		this.tetraData = tetraData;

		tetIds = tetraData.GetTetIds;
		tetEdgeIds = tetraData.GetTetEdgeIds;

		numParticles = tetraData.GetNumberOfVertices;
		numTets = tetraData.GetNumberOfTetrahedrons;
		numEdges = tetraData.GetNumberOfEdges;

		//Init the arrays 
		//Has to be done in the constructor because readonly
		pos = new Vector3[numParticles];
		prevPos = new Vector3[numParticles];
		vel = new Vector3[numParticles];
		invMass = new float[numParticles];

		restVolumes = new float[numTets];

		restEdgeLengths = new float[numEdges];
		

		//Fill the arrays
		FillArrays(meshScale);

		//Move the mesh to its start position
		Translate(startPos);

		//Init the mesh
		InitMesh(meshFilter, tetraData);
	}



	//Fill the data structures needed or soft body physics
	private void FillArrays(float meshScale)
	{
		//[x0, y0, z0, x1, y1, z1, ...]
		float[] flatVerts = tetraData.GetVerts;


		//Particle position
		for (int i = 0; i < flatVerts.Length; i += 3)
		{
			float x = flatVerts[i + 0];
			float y = flatVerts[i + 1];
			float z = flatVerts[i + 2];

			pos[i / 3] = new Vector3(x, y, z) * meshScale;
		}


		//Particle previous position
		//Not needed because is already set to 0s


		//Particle velocity
		//Not needed because is already set to 0s


		//Rest volume
		for (int i = 0; i < numTets; i++)
		{
			restVolumes[i] = GetTetVolume(i);
		}


		//Inverse mass (1/w)
		for (int i = 0; i < numTets; i++)
		{
			float vol = restVolumes[i];

			//The mass connected to a particle in a tetra is roughly volume / 4
			float pInvMass = vol > 0f ? 1f / (vol / 4f) : 0f;

			invMass[tetIds[4 * i + 0]] += pInvMass;
			invMass[tetIds[4 * i + 1]] += pInvMass;
			invMass[tetIds[4 * i + 2]] += pInvMass;
			invMass[tetIds[4 * i + 3]] += pInvMass;
		}


		//Rest edge length
		for (int i = 0; i < restEdgeLengths.Length; i++)
		{
			int id0 = tetEdgeIds[2 * i + 0];
			int id1 = tetEdgeIds[2 * i + 1];

			restEdgeLengths[i] = Vector3.Magnitude(pos[id0] - pos[id1]);
		}
	}



	public void MyFixedUpdate()
	{
		if (!simulate)
		{
			return;
        }

		float dt = Time.fixedDeltaTime;

		//ShrinkWalls(dt);

		Simulate(dt);
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

	//Main soft body simulation loop
	void Simulate(float dt)
	{
		float sdt = dt / numSubSteps;

		for (int step = 0; step < numSubSteps; step++)
		{		
			PreSolve(sdt, gravity);

			SolveConstraints(sdt);

			HandleEnvironmentCollision();

			PostSolve(sdt);
		}
	}



	//Move the particles and handle environment collision
	void PreSolve(float dt, Vector3 gravity)
	{
		//For each particle
		for (int i = 0; i < numParticles; i++)
		{
			//This means the particle is fixed, so don't simulate it
			if (invMass[i] == 0f)
			{
				continue;
			}

			//Update vel
			vel[i] += dt * gravity;

			//Save old pos
			prevPos[i] = pos[i];

			//Update pos
			pos[i] += dt * vel[i];
		}
	}



	private void HandleEnvironmentCollision()
	{
		for (int i = 0; i < numParticles; i++)
		{
			EnvironmentCollision(i);
		}
	}



	//Collision with invisible walls and floor
	private void EnvironmentCollision(int i)
	{
		//Floor collision
		float x = pos[i].x;
		float y = pos[i].y;
		float z = pos[i].z;

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

		//Y
		if (y < floorHeight)
		{
			//Set the pos to previous pos
			pos[i] = prevPos[i];
			//But the y of the previous pos should be at the ground
			pos[i].y = floorHeight;
		}
		else if (y > halfPlayGroundSize.y)
		{
			pos[i] = prevPos[i];
			pos[i].y = halfPlayGroundSize.y;
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



	//Handle the soft body physics
	private void SolveConstraints(float dt)
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

		SolveEdges(edgeCompliance, dt);
		SolveVolumes(volCompliance, dt);
	}



	//Fix velocity
	private void PostSolve(float dt)
	{
		float oneOverdt = 1f / dt;
	
		//For each particle
		for (int i = 0; i < numParticles; i++)
		{
			if (invMass[i] == 0f)
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
	private void SolveEdges(float compliance, float dt)
	{
		float alpha = compliance / (dt * dt);

		//For each edge
		for (int i = 0; i < numEdges; i++)
		{
			//2 vertices per edge in the data structure, so multiply by 2 to get the correct vertex index
			int id0 = tetEdgeIds[2 * i    ];
			int id1 = tetEdgeIds[2 * i + 1];

			float w0 = invMass[id0];
			float w1 = invMass[id1];

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
			
			float l_rest = restEdgeLengths[i];
			
			float C = l - l_rest;

			//lambda because |grad_Cn|^2 = 1 because if we move a particle 1 unit, the distance between the particles also grows with 1 unit, and w = w0 + w1
			float lambda = -C / (wTot + alpha);

			//Move the vertices x = x + deltaX where deltaX = lambda * w * gradC
			pos[id0] += lambda * w0 * gradC;
			pos[id1] += -lambda * w1 * gradC;
		}
	}


	//TODO: This method is the bottleneck
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
	private void SolveVolumes(float compliance, float dt)
	{
		float alpha = compliance / (dt * dt);

		//For each tetra
		for (int i = 0; i < numTets; i++)
		{
			float wTimesGrad = 0f;
		
			//Foreach vertex in the tetra
			for (int j = 0; j < 4; j++)
			{
				int idThis = tetIds[4 * i + j];

                //The 3 opposite vertices ids
                int id0 = tetIds[4 * i + TetrahedronData.volIdOrder[j][0]];
                int id1 = tetIds[4 * i + TetrahedronData.volIdOrder[j][1]];
                int id2 = tetIds[4 * i + TetrahedronData.volIdOrder[j][2]];

                //(x4 - x2)
                Vector3 id1_minus_id0 = pos[id1] - pos[id0];
				//(x3 - x2)
				Vector3 id2_minus_id0 = pos[id2] - pos[id0];

				//(x4 - x2)x(x3 - x2)
				Vector3 cross = Vector3.Cross(id1_minus_id0, id2_minus_id0);

				//Multiplying by 1/6 in the denominator is the same as multiplying by 6 in the numerator
				//Im not sure why hes doing it... maybe because alpha should not be affected by it?  
				Vector3 gradC = cross * (1f / 6f);

				gradients[j] = gradC;

				//w1 * |grad_C1|^2
				wTimesGrad += invMass[idThis] * Vector3.SqrMagnitude(gradC);
			}

			//All vertices are fixed so dont simulate
			if (wTimesGrad == 0f)
			{
				continue;
			}

			float vol = GetTetVolume(i);
			float restVol = restVolumes[i];

			float C = vol - restVol;

			//The guy in the video is dividing by 6 in the code but multiplying in the video
			//C *= 6f;

			float lambda = -C / (wTimesGrad + alpha);
			
            //Move each vertex
            for (int j = 0; j < 4; j++)
            {
                int id = tetIds[4 * i + j];

				//Move the vertices x = x + deltaX where deltaX = lambda * w * gradC
				pos[id] += lambda * invMass[id] * gradients[j];
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

		softBodyMesh = meshFilter.sharedMesh;
		softBodyMesh.MarkDynamic();
	}



	//Update the mesh with new vertex positions
	private void UpdateMesh()
	{
		softBodyMesh.vertices = pos;

		softBodyMesh.RecalculateBounds();
		softBodyMesh.RecalculateNormals();
	}



	//
	// Help methods
	//

	//Move all vertices a distance of (x, y, z)
	private void Translate(Vector3 moveDist)
	{
		for (int i = 0; i < numParticles; i++)
		{
			pos[i] += moveDist;
			prevPos[i] += moveDist;
		}
	}



	//Calculate the volume of a tetrahedron
	private float GetTetVolume(int nr)
	{
		//The 4 vertices belonging to this tetra 
		int id0 = tetIds[4 * nr + 0];
		int id1 = tetIds[4 * nr + 1];
		int id2 = tetIds[4 * nr + 2];
		int id3 = tetIds[4 * nr + 3];

		Vector3 a = pos[id0];
		Vector3 b = pos[id1];
		Vector3 c = pos[id2];
		Vector3 d = pos[id3];

		float volume = Tetrahedron.Volume(a, b, c, d);

		return volume;
	}



	//
	// Mesh user interactions
	//

	//Shrink walls
	private void ShrinkWalls(float dt)
	{
		//Shrink walls
		float wallSpeed = 0.4f;

		halfPlayGroundSize.x -= wallSpeed * dt;
		halfPlayGroundSize.z -= wallSpeed * dt;

		float minWallSize = 0.2f;

		halfPlayGroundSize.x = Mathf.Clamp(halfPlayGroundSize.x, minWallSize, 100f);
		halfPlayGroundSize.z = Mathf.Clamp(halfPlayGroundSize.z, minWallSize, 100f);
	}



	//Yeet the mesh upwards
	private void Yeet()
	{
		Translate(new Vector3(0f, 0.2f, 0f));
	}



	//Squash the mesh so it becomes flat against the ground
	void Squeeze()
	{
		for (int i = 0; i < numParticles; i++)
		{
			//Set y coordinate to slightly above floor height
			pos[i].y = floorHeight + 0.01f;
		}

		UpdateMesh();
	}



	//Input pos is the pos in a triangle we get when doing ray-triangle intersection
	public void StartGrab(Vector3 triangleIntersectionPos)
	{
		//Find the closest vertex to the pos on a triangle in the mesh
		float minD2 = float.MaxValue;
		
		grabId = -1;
		
		for (int i = 0; i < numParticles; i++)
		{
			float d2 = Vector3.SqrMagnitude(triangleIntersectionPos - pos[i]);
			
			if (d2 < minD2)
			{
				minD2 = d2;
				grabId = i;
			}
		}

		//We have found a vertex
		if (grabId >= 0)
		{
			//Save the current innverted mass
			grabInvMass = invMass[grabId];
			
			//Set the inverted mass to 0 to mark it as fixed
			invMass[grabId] = 0f;

			//Set the position of the vertex to the position where the ray hit the triangle
			pos[grabId] = triangleIntersectionPos;
		}
	}



	public void MoveGrabbed(Vector3 newPos)
	{
		if (grabId >= 0)
		{
			pos[grabId] = newPos;
		}
	}



	public void EndGrab(Vector3 newPos, Vector3 newParticleVel)
	{
		if (grabId >= 0)
		{
			//Set the mass to whatever mass it was before we grabbed it
			invMass[grabId] = grabInvMass;

			vel[grabId] = newParticleVel;
		}

		grabId = -1;
	}



	public void IsRayHittingBody(Ray ray, out CustomHit hit)
	{
		//Mesh data
		Vector3[] vertices = pos;

		int[] triangles = GetMeshTriangles;

		//Find if the ray hit a triangle in the mesh
		Intersections.IsRayHittingMesh(ray, vertices, triangles, out hit);
	}



	public Vector3 GetGrabbedPos()
	{
		return pos[grabId];
    }
}

