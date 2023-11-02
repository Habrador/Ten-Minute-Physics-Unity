using System.Collections;
using System.Collections.Generic;
using UnityEngine;



namespace EulerianFluidSimulator
{
	//Display the fluid simulation data on a texture
	//Display streamlines and velocities with lines
	//Display obstacles as mesh
	public static class DisplayFluid
	{
		private static Mesh circleMesh;
		private static float circleRadius = 0f;



		//For testing
		public static void TestDraw(FluidScene scene)
		{
			if (scene.fluidTexture == null)
			{
				scene.fluidTexture = new(4, 2);

				//So the pixels dont blend
				scene.fluidTexture.filterMode = FilterMode.Point;

				scene.fluidTexture.wrapMode = TextureWrapMode.Clamp;

				scene.fluidMaterial.mainTexture = scene.fluidTexture;
			}
	
			//The colors array is a flattened 2D array, where pixels are laid out left to right, bottom to top, which fits how the fluid simulation arrays are set up
			//Color32 is 0->255 (byte)
			//Color is 0->1 (float)
			Color32[] colors = new Color32[8];

			//This works even though Color is 0->1 because it converts float->byte
			colors[0] = Color.black; //BL 
			colors[1] = Color.white;
			colors[2] = Color.white;
			colors[3] = Color.blue; //BR

			colors[4] = Color.red; //TL
			colors[5] = Color.white;
			colors[6] = Color.white;
			colors[7] = Color.green; //TR

			scene.fluidTexture.SetPixels32(colors);

			scene.fluidTexture.Apply();

			//Debug.Log(colors[4]); //RGBA(255, 0, 0, 255)
		}



		//Called every Update
		public static void Draw(FluidScene scene)
		{
			UpdateTexture(scene);

			if (scene.showVelocities)
			{
				ShowVelocities(scene);
			}

			//scene.showStreamlines = true;

			if (scene.showStreamlines)
			{
				ShowStreamlines(scene);
			}

			if (scene.showObstacle)
			{
				ShowObstacle(scene);
			}

			//Moved the display of min and max pressure as text to the UI class
		}



		//
		// Show the fluid simulation data on a texture
		//
		private static void UpdateTexture(FluidScene scene)
		{
			FluidSim f = scene.fluid;

			Texture2D fluidTexture = scene.fluidTexture;

			//Generate a new texture if none exists or if we have changed resolution
			if (fluidTexture == null || fluidTexture.width != f.numX || fluidTexture.height != f.numY)
			{
				fluidTexture = new(f.numX, f.numY);

				//Dont blend the pixels
				//fluidTexture.filterMode = FilterMode.Point;

				//Blend the pixels 
				fluidTexture.filterMode = FilterMode.Bilinear;

				//Don't wrap the border with the border on the opposite side of the texture
				fluidTexture.wrapMode = TextureWrapMode.Clamp;

				scene.fluidMaterial.mainTexture = fluidTexture;
				
				scene.fluidTexture = fluidTexture;
			}


			//The texture colors
			Color32[] textureColors = new Color32[f.numX * f.numY];

			//Find min and max pressure
			MinMax minMaxP = f.GetMinMaxPressure();

			//Find the colors
			//This was an array in the source, but we can treat the Vector4 as an array to make the code match
			//Vector4 color = new (255, 255, 255, 255);

			for (int i = 0; i < f.numX; i++)
			{
				for (int j = 0; j < f.numY; j++)
				{
					//This was an array in the source, but we can treat the Vector4 as an array to make the code match
					//Moved to here from before the loop so it resets every time so we can display the walls if we deactivate both pressure and smoke
					Vector4 color = new(255, 255, 255, 255);

					if (scene.showPressure)
					{
						float p = f.p[f.To1D(i, j)];

						//Blue means low pressure and red is high pressure
						color = UsefulMethods.GetSciColor(p, minMaxP.min, minMaxP.max);

						//Color the smoke according to the scientific color scheme 
						//Everything that's not smoke becomes black
						//Everything that's smoke shows the pressure field
						if (scene.showSmoke)
						{
							//How much smoke in this cell?
							float smoke = f.m[f.To1D(i, j)];

							//smoke = 0 means max smoke, so will be 0 if no smoke in the cell (smoke = 1)
							color[0] = Mathf.Max(0f, color[0] - 255 * smoke);
							color[1] = Mathf.Max(0f, color[1] - 255 * smoke);
							color[2] = Mathf.Max(0f, color[2] - 255 * smoke);
						}
					}
					else if (scene.showSmoke)
					{
						//How much smoke in this cell?
						float smoke = f.m[f.To1D(i, j)];

						//smoke = 0 means max smoke, and 255 * 0 = 0 -> black 
						color[0] = 255 * smoke;
						color[1] = 255 * smoke;
						color[2] = 255 * smoke;

						//In the paint scene we color the smoke according to the scientific color scheme
						if (scene.sceneNr == FluidScene.SceneNr.Paint)
						{
							color = UsefulMethods.GetSciColor(smoke, 0f, 1f);
						}
					}
					//If both pressure and smoke are deactivated, then display obstacles as black, the rest as white
					//There was a bug in the source code where everything turned back, but "f.s[f.To1D(i, j)] == 0f" should mean only walls should be black
					else if (f.s[f.To1D(i, j)] == 0f)
                    {
                        color[0] = 0;
                        color[1] = 0;
						color[2] = 0;
                    }

                    //Add the color to this pixel
                    //Color32 is 0-255
                    Color32 pixelColor = new((byte)color[0], (byte)color[1], (byte)color[2], (byte)color[3]);

					textureColors[f.To1D(i, j)] = pixelColor;
				}
			}
			
			//Add all colors to the texture
			fluidTexture.SetPixels32(textureColors);

			//Copies changes you've made in a CPU texture to the GPU
			fluidTexture.Apply(false);
		}
	


		//
		// Show the u and v velocities at each cell by drawing lines
		//
		private static void ShowVelocities(FluidScene scene)
		{
			FluidSim f = scene.fluid;

			//Cell width
			float h = f.h;

			//The length of the lines which will be scaled by the velocity in simulation space
			float scale = 0.02f;

			List<Vector3> linesToDisplay = new ();

			//So the lines are drawn infront of the simulation plane
			float z = -0.01f;

			for (int i = 0; i < f.numX; i++)
			{
				for (int j = 0; j < f.numY; j++)
				{
					float u = f.u[f.To1D(i, j)];
					float v = f.v[f.To1D(i, j)];


					//u velocity
					float x0 = i * h;
					float x1 = i * h + u * scale;
					float y = (j + 0.5f) * h; //the u vel is in the middle of the cell in y direction, thus the 0.5

					scene.SimToWorld(x0, y, out float uStartX, out float uStartY);
					scene.SimToWorld(x1, y, out float uEndX, out float uEndY);

					linesToDisplay.Add(new Vector3(uStartX, uStartY, z));
					linesToDisplay.Add(new Vector3(uEndX, uEndY, z));


					//v velocity
					float x = (i + 0.5f) * h;
					float y0 = j * h;
					float y1 = j * h + v * scale;

					scene.SimToWorld(x, y0, out float vStartX, out float vStartY);
					scene.SimToWorld(x, y1, out float vEndX, out float vEndY);

					linesToDisplay.Add(new Vector3(vStartX, vStartY, z));
					linesToDisplay.Add(new Vector3(vEndX, vEndY, z));
				}
			}

			//Display the lines with some black color
			DisplayShapes.DrawLineSegments(linesToDisplay, DisplayShapes.ColorOptions.Black);
		}

		

		//
		// Show streamlines that follows the velocity to easier visualize how the fluid flows
		//
		private static void ShowStreamlines(FluidScene scene)
		{		
			FluidSim f = scene.fluid;

			//How many segments per streamline?
			int numSegs = 15;

			List<Vector3> streamlineCoordinates = new ();

			//To display the line infront of the plane
			float z = -0.01f;

			//Dont display a streamline from each cell because it makes it difficult to see, so every 5 cell
			for (int i = 1; i < f.numX - 1; i += 5)
			{
				for (int j = 1; j < f.numY - 1; j += 5)
				{
					//Reset
					streamlineCoordinates.Clear();

					//Center of the cell in simulation space
					float x = (i + 0.5f) * f.h;
					float y = (j + 0.5f) * f.h;

					//Simulation space to global
					scene.SimToWorld(x, y, out float startPosX, out float startPosY);

					streamlineCoordinates.Add(new Vector3(startPosX, startPosY, z));

					//Build the line
					for (int n = 0; n < numSegs; n++)
					{
						//The velocity at the current coordinate
						float u = f.SampleField(x, y, FluidSim.SampleArray.uField);
						float v = f.SampleField(x, y, FluidSim.SampleArray.vField);
						
						//Move a small step in the direction of the velocity
						x += u * 0.01f;
						y += v * 0.01f;

						//Stop the line if we are outside of the simulation area
						//The guy in the video is only checking x > f.GetWidth() for some reason...
						if (x > f.SimWidth || x < 0f || y > f.SimHeight || y < 0f)
						{
							break;
						}

						//Add the next coordinate of the streamline
						scene.SimToWorld(x, y, out float nextPos2DX, out float nextPos2DY);

						streamlineCoordinates.Add(new Vector3(nextPos2DX, nextPos2DY, z));
					}

					//Display the line
					DisplayShapes.DrawLine(streamlineCoordinates, DisplayShapes.ColorOptions.Black);
				}
			}
		}
		


		//
		// Display the circle obstacle
		//
		private static void ShowObstacle(FluidScene scene)
		{		
			FluidSim f = scene.fluid;

			//Make it slightly bigger to hide the jagged edges we get because we use a grid with square cells which will not match the circle edges prefectly
			float circleRadius = scene.obstacleRadius + f.h;
			
			//The color of the circle
			DisplayShapes.ColorOptions color = DisplayShapes.ColorOptions.Gray;

			//Black like the bg to make it look nicer
			if (scene.showPressure)
			{
				color = DisplayShapes.ColorOptions.Black;
			}

			//Circle center in global space
			scene.SimToWorld(scene.obstacleX, scene.obstacleY, out float globalCenter2DX, out float globalCenter2DY);

			//3d space infront of the texture
			Vector3 circleCenter = new (globalCenter2DX, globalCenter2DY, -0.1f);

			//Generate a new circle mesh if we havent done so before or radius has changed 
			if (circleMesh == null || DisplayFluid.circleRadius != circleRadius)
			{
                circleMesh = DisplayShapes.GenerateCircleMesh_XY(Vector3.zero, circleRadius, 50);

				DisplayFluid.circleRadius = circleRadius;
            }

			//Display the circle mesh
			Material material = DisplayShapes.GetMaterial(color);

			Graphics.DrawMesh(circleMesh, circleCenter, Quaternion.identity, material, 0, Camera.main, 0);


			//The guy is also giving the circle a black border, which we could replicate by drawing a smaller circle but it doesn't matter! 
		}
	}
}
