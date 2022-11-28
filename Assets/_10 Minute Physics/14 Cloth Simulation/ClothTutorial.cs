using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClothTutorial : MonoBehaviour
{
	private readonly ClothData clothData;

	//Same as in ball physics
	private readonly float[] pos;
	private readonly float[] prevPos;
	private readonly float[] vel;

	//For soft body cloth physics
	private readonly float[] restPos;
	//Inverese mass w = 1/m where m is how nuch mass is connected to each particle
	//If a particle is fixed we set its mass to 0
	private readonly float[] invMass;
	//Neighboring edges (-1 if has no neighbor)
	private int[] neighbors;
	//
	private int[] stretchingIds;
	//
	private int[] bendingIds;
	//
	private readonly float[] stretchingLengths;
	//
	private readonly float[] bendingLengths;


	//These two arrays should be global so we don't have to create them a million times
	//Needed when we calculate the volume of a tetrahedron
	private readonly float[] temp = new float[4 * 3];
	//Gradients needed when we calculate the edge and volume constraints 
	private readonly float[] grads = new float[4 * 3];
	

	//The Unity mesh to display the cloth
	private Mesh clothMesh;

	//How many vertices (particles) and tets do we have?
	private readonly int numParticles;

	//Simulation settings
	private readonly float[] gravity = { 0f, -9.81f, 0f };
	private readonly int numSubSteps = 15;
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
	public List<Vector3> GetMeshVertices => GenerateMeshVertices(pos);
	public int[] GetMeshTriangles => clothData.GetFaceTriIds;
	public int GetGrabId => grabId;



	public ClothTutorial(MeshFilter meshFilter, ClothData clothData, float bendingCompliance = 1f)
	{
		this.clothData = clothData;
	
		//Particles
		this.numParticles = clothData.GetVerts.Length / 3;

		this.pos = (float[])clothData.GetVerts.Clone();
		this.prevPos = (float[])clothData.GetVerts.Clone();
		this.restPos = (float[])clothData.GetVerts.Clone();
		this.vel = new float[3 * this.numParticles];
		this.invMass = new float[this.numParticles];

		//Stretching and bending constraints
		neighbors = FindTriNeighbors(clothData.GetFaceTriIds);

		int numTris = clothData.GetFaceTriIds.Length / 3;
		
		List<int> edgeIds = new ();
		List<int> triPairIds = new ();

		for (int i = 0; i < numTris; i++)
		{
			for (int j = 0; j < 3; j++)
			{
				int id0 = clothData.GetFaceTriIds[3 * i + j];
				int id1 = clothData.GetFaceTriIds[3 * i + (j + 1) % 3];

				//Each edge only once
				int n = neighbors[3 * i + j];

				if (n < 0 || id0 < id1)
				{
					edgeIds.Add(id0);
					edgeIds.Add(id1);
				}

				//Tri pair
				if (n >= 0)
				{
					//Opposite ids
					int ni = Mathf.FloorToInt(n / 3); //NOT SURE IF THESE ARE INTS
					int nj = n % 3;
					
					int id2 = clothData.GetFaceTriIds[3 * i + (j + 2) % 3];
					int id3 = clothData.GetFaceTriIds[3 * ni + (nj + 2) % 3];
					
					triPairIds.Add(id0);
					triPairIds.Add(id1);
					triPairIds.Add(id2);
					triPairIds.Add(id3);
				}
			}
		}


		this.stretchingIds = edgeIds.ToArray();
		this.bendingIds = triPairIds.ToArray();
		this.stretchingLengths = new float[this.stretchingIds.Length / 2];
		this.bendingLengths = new float[this.bendingIds.Length / 4];

		this.stretchingCompliance = 0f;
		this.bendingCompliance = bendingCompliance;


		//Init the mesh
		InitMesh(meshFilter, clothData.GetFaceTriIds);
	}




	private int[] FindTriNeighbors(int[] triIds)
	{
		//Create common edges
		List<ClothEdge> edges = new ();

		int numTris = triIds.Length / 3;

		for (int i = 0; i < numTris; i++)
		{
			for (int j = 0; j < 3; j++)
			{
				int id0 = triIds[3 * i + j];
				int id1 = triIds[3 * i + (j + 1) % 3];

				edges.Add( new ClothEdge(Mathf.Min(id0, id1), Mathf.Max(id0, id1), 3 * i + j));
			}
		}

		//Sort so common edges are next to each other
		edges.Sort((a, b) => ((a.id0 < b.id0) || (a.id0 == b.id0 && a.id1 < b.id1)) ? -1 : 1);

		//Find matching edges
		neighbors = new int[3 * numTris];

		//Set all edges to be open
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
}
