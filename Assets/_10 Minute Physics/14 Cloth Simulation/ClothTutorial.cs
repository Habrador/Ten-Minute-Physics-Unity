using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClothTutorial : MonoBehaviour
{
	//Same as in ball physics
	private readonly float[] pos;
	private readonly float[] prevPos;
	private readonly float[] vel;

	//For soft body cloth physics
	private readonly float[] restPos;
	//Inverese mass w = 1/m where m is how nuch mass is connected to each particle
	//If a particle is fixed we set its mass to 0
	private readonly float[] invMass;

	/*
	//The volume at start before deformation
	private readonly float[] restVol;
	//The length of an edge before deformation
	private readonly float[] restEdgeLengths;
	
	//These two arrays should be global so we don't have to create them a million times
	//Needed when we calculate the volume of a tetrahedron
	private readonly float[] temp = new float[4 * 3];
	//Gradients needed when we calculate the edge and volume constraints 
	private readonly float[] grads = new float[4 * 3];
	*/

	//The Unity mesh to display the cloth
	private Mesh clothMesh;

	//How many vertices (particles) and tets do we have?
	private readonly int numParticles;

	//Simulation settings
	private readonly float[] gravity = { 0f, -9.81f, 0f };
	private readonly int numSubSteps = 15;
	private bool simulate = true;


	public ClothTutorial(ClothDataTutorial mesh, float bendingCompliance = 1f)
	{
		
    }
}
