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

	private readonly float floorHeight = 0f;

	private float edgeCompliance = 5.0f;

	private Mesh softBodyMesh;
	
	private int numParticles;
	private int numTets;

	//Same as in previous examples
	private float[] pos;
	private float[] prevPos;
	private float[] vel;

	//For soft body 
	private float[] restVol;
	private float[] edgeLengths;
	private float[] invMass;
	private float volCompliance = 0.0f;
	private float[] temp;
	private float[] grads;

	private int[][] volIdOrder = new int[][] { new int[] { 1, 3, 2 }, new int[] { 0, 2, 3 }, new int[] { 0, 3, 1 }, new int[] { 0, 1, 2 } };

	//Grabbing
	private int grabId = -1;
	private float grabInvMass = 0.0f;



	public SoftBodySimulation(MeshFilter meshFilter, TetrahedronData tetraData, float meshScale = 2f)
	{
		//Tetra data structures
		float[] verts = tetraData.GetVerts;

		this.tetIds = tetraData.GetTetIds;
		this.tetEdgeIds = tetraData.GetTetEdgeIds;

		this.numParticles = verts.Length / 3;
		this.numTets = tetIds.Length / 4;

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
		this.edgeLengths = new float[this.tetEdgeIds.Length / 2];
		this.invMass = new float[this.numParticles];
		this.temp = new float[4 * 3];
		this.grads = new float[4 * 3];

		InitPhysics();

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

	void InitPhysics()
	{
		for (var i = 0; i < invMass.Length; i++)
		{
			invMass[i] = 0.0f;
		}

		for (var i = 0; i < restVol.Length; i++)
		{
			restVol[i] = 0.0f;
		}

		for (var i = 0; i < this.numTets; i++)
		{
			var vol = this.GetTetVolume(i);
			this.restVol[i] = vol;
			var pInvMass = vol > 0.0f ? 1.0f / (vol / 4.0f) : 0.0f;
			this.invMass[this.tetIds[4 * i]] += pInvMass;
			this.invMass[this.tetIds[4 * i + 1]] += pInvMass;
			this.invMass[this.tetIds[4 * i + 2]] += pInvMass;
			this.invMass[this.tetIds[4 * i + 3]] += pInvMass;
		}

		for (var i = 0; i < this.edgeLengths.Length; i++)
		{
			var id0 = this.tetEdgeIds[2 * i];
			var id1 = this.tetEdgeIds[2 * i + 1];

			this.edgeLengths[i] = Mathf.Sqrt(VecDistSquared(this.pos, id0, this.pos, id1));
		}
	}



	void Simulate()
	{
		float dt = Time.fixedDeltaTime;

		var sdt = dt / this.numSubSteps;

		for (var step = 0; step < this.numSubSteps; step++)
		{
			PreSolve(sdt, gravity);

			Solve(sdt);

			PostSolve(sdt);
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
			
			VecAdd(this.vel, i, gravity, 0, dt);
			VecCopy(this.prevPos, i, this.pos, i);
			VecAdd(this.pos, i, this.vel, i, dt);
			
			//Floor collision
			float y = this.pos[3 * i + 1];
			
			if (y < 0f)
			{
				VecCopy(this.pos, i, this.prevPos, i);

				this.pos[3 * i + 1] = 0.0f;
			}
		}
	}



	void Solve(float dt)
	{
		this.SolveEdges(this.edgeCompliance, dt);
		this.SolveVolumes(this.volCompliance, dt);
	}



	void PostSolve(float dt)
	{
		for (var i = 0; i < this.numParticles; i++)
		{
			if (this.invMass[i] == 0.0f)
				continue;
			VecSetDiff(this.vel, i, this.pos, i, this.prevPos, i, 1.0f / dt);
		}
		this.UpdateMeshes();
	}



	void SolveEdges(float compliance, float dt)
	{
		var alpha = compliance / dt / dt;

		for (var i = 0; i < this.edgeLengths.Length; i++)
		{
			var id0 = this.tetEdgeIds[2 * i];
			var id1 = this.tetEdgeIds[2 * i + 1];
			var w0 = this.invMass[id0];
			var w1 = this.invMass[id1];
			var w = w0 + w1;
			if (w == 0.0f)
				continue;

			VecSetDiff(this.grads, 0, this.pos, id0, this.pos, id1);
			var len = Mathf.Sqrt(VecLengthSquared(this.grads, 0));
			if (len == 0.0f)
				continue;
			VecScale(this.grads, 0, 1.0f / len);
			var restLen = this.edgeLengths[i];
			var C = len - restLen;
			var s = -C / (w + alpha);
			VecAdd(this.pos, id0, this.grads, 0, s * w0);
			VecAdd(this.pos, id1, this.grads, 0, -s * w1);
		}
	}



	void SolveVolumes(float compliance, float dt)
	{
		var alpha = compliance / dt / dt;

		for (var i = 0; i < this.numTets; i++)
		{
			var w = 0.0f;

			for (var j = 0; j < 4; j++)
			{
				var id0 = this.tetIds[4 * i + this.volIdOrder[j][0]];
				var id1 = this.tetIds[4 * i + this.volIdOrder[j][1]];
				var id2 = this.tetIds[4 * i + this.volIdOrder[j][2]];

				VecSetDiff(this.temp, 0, this.pos, id1, this.pos, id0);
				VecSetDiff(this.temp, 1, this.pos, id2, this.pos, id0);
				VecSetCross(this.grads, j, this.temp, 0, this.temp, 1);
				VecScale(this.grads, j, 1.0f / 6.0f);

				w += this.invMass[this.tetIds[4 * i + j]] * VecLengthSquared(this.grads, j);
			}
			if (w == 0.0f)
				continue;

			var vol = this.GetTetVolume(i);
			var restVol = this.restVol[i];
			var C = vol - restVol;
			var s = -C / (w + alpha);

			for (var j = 0; j < 4; j++)
			{
				var id = this.tetIds[4 * i + j];
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

		mesh.vertices = vertices.ToArray();
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
	// Help methods
	//

	void VecSetZero(float[] a, int anr)
	{
		anr *= 3;
		a[anr++] = 0.0f;
		a[anr++] = 0.0f;
		a[anr] = 0.0f;
	}

	void VecScale(float[] a, int anr, float scale)
	{
		anr *= 3;
		a[anr++] *= scale;
		a[anr++] *= scale;
		a[anr] *= scale;
	}

	void VecCopy(float[] a, int anr, float[] b, int bnr)
	{
		anr *= 3; bnr *= 3;
		a[anr++] = b[bnr++];
		a[anr++] = b[bnr++];
		a[anr] = b[bnr];
	}

	void VecAdd(float[] a, int anr, float[] b, int bnr, float scale = 1.0f)
	{
		anr *= 3; bnr *= 3;
		a[anr++] += b[bnr++] * scale;
		a[anr++] += b[bnr++] * scale;
		a[anr] += b[bnr] * scale;
	}

	void VecSetDiff(float[] dst, int dnr, float[] a, int anr, float[] b, int bnr, float scale = 1.0f)
	{
		dnr *= 3; anr *= 3; bnr *= 3;
		dst[dnr++] = (a[anr++] - b[bnr++]) * scale;
		dst[dnr++] = (a[anr++] - b[bnr++]) * scale;
		dst[dnr] = (a[anr] - b[bnr]) * scale;
	}

	float VecLengthSquared(float[] a, int anr)
	{
		anr *= 3;
		float a0 = a[anr]; float a1 = a[anr + 1]; float a2 = a[anr + 2];
		return a0 * a0 + a1 * a1 + a2 * a2;
	}

	float VecDistSquared(float[] a, int anr, float[] b, int bnr)
	{
		anr *= 3; bnr *= 3;
		float a0 = a[anr] - b[bnr]; float a1 = a[anr + 1] - b[bnr + 1]; float a2 = a[anr + 2] - b[bnr + 2];
		return a0 * a0 + a1 * a1 + a2 * a2;
	}

	float VecDot(float[] a, int anr, float[] b, int bnr)
	{
		anr *= 3; bnr *= 3;
		return a[anr] * b[bnr] + a[anr + 1] * b[bnr + 1] + a[anr + 2] * b[bnr + 2];
	}

	void VecSetCross(float[] a, int anr, float[] b, int bnr, float[] c, int cnr)
	{
		anr *= 3; bnr *= 3; cnr *= 3;
		a[anr++] = b[bnr + 1] * c[cnr + 2] - b[bnr + 2] * c[cnr + 1];
		a[anr++] = b[bnr + 2] * c[cnr + 0] - b[bnr + 0] * c[cnr + 2];
		a[anr] = b[bnr + 0] * c[cnr + 1] - b[bnr + 1] * c[cnr + 0];
	}

	//Move all vertices
	void Translate(float x, float y, float z)
	{
		for (var i = 0; i < this.numParticles; i++)
		{
			VecAdd(this.pos, i, new float[] { x, y, z }, 0);
			VecAdd(this.prevPos, i, new float[] { x, y, z }, 0);
		}
	}

	//Calculate the volume of a tetrahedron
	float GetTetVolume(int nr)
	{
		var id0 = this.tetIds[4 * nr];
		var id1 = this.tetIds[4 * nr + 1];
		var id2 = this.tetIds[4 * nr + 2];
		var id3 = this.tetIds[4 * nr + 3];

		VecSetDiff(this.temp, 0, this.pos, id1, this.pos, id0);
		VecSetDiff(this.temp, 1, this.pos, id2, this.pos, id0);
		VecSetDiff(this.temp, 2, this.pos, id3, this.pos, id0);
		
		VecSetCross(this.temp, 3, this.temp, 0, this.temp, 1);
		
		float volume= VecDot(this.temp, 3, this.temp, 2) / 6.0f;

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
			this.pos[3 * i + 1] += 0.1f;
		}
	}


	//Squash the mesh so it becomes flat against the ground
	void Squash()
	{
		for (var i = 0; i < this.numParticles; i++)
		{
			//Squash y coordinate which is up
			this.pos[3 * i + 1] = this.floorHeight + 0.01f;
		}

		//Is needed!
		this.UpdateMeshes();
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

