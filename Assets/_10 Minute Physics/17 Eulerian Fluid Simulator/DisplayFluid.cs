using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Display the fluid simulation on a texture
namespace EulerianFluidSimulator
{
	//Display the fluid simulation on a texture
	//Display streamlines and velocities with lines
	//Display obstacles as mesh
	public static class DisplayFluid
	{
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
	
			//The colors array is a flattened 2D array, where pixels are laid out left to right, bottom to top (i.e. row after row)
			//which fits how the fluid simulation arrays are set up
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



		public static void Draw(FluidScene scene)
		{
			UpdateTexture(scene);

			if (scene.showVelocities)
			{
				ShowVelocities(scene);
			}

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



		//Show the fluid simulation on a texture
		private static void UpdateTexture(FluidScene scene)
		{
			FluidSim f = scene.fluid;

			Texture2D fluidTexture = scene.fluidTexture;

			//Generate a new texture if none exists or if we have changed resolution
			if (fluidTexture == null || fluidTexture.width != f.numX || fluidTexture.height != f.numY)
			{
				fluidTexture = new(f.numX, f.numY);

				//So the pixels dont blend
				//fluidTexture.filterMode = FilterMode.Point;

				//Blend the pixels 
				fluidTexture.filterMode = FilterMode.Bilinear;

				//So the borders dont wrap with the border on the other side
				fluidTexture.wrapMode = TextureWrapMode.Clamp;

				scene.fluidMaterial.mainTexture = fluidTexture;
			}

			Color32[] textureColors = new Color32[f.numX * f.numY];

			//To convert from 2d to 1d array
			//int n = f.numY;

			//Find min and max pressure
			MinMax minMaxP = f.GetMinMaxPressure();

			//Find the colors
			//Better to use array instead of Color32 to avoid confusion when converting between float, byte, int, etc
			//And it also matches the original code better
			Vector4 color = new (255, 255, 255, 255); 

			for (int i = 0; i < f.numX; i++)
			{
				for (int j = 0; j < f.numY; j++)
				{
					if (scene.showPressure)
					{
						float p = f.p[f.To1D(i, j)];

						color = GetSciColor(p, minMaxP.min, minMaxP.max);

						//Color the smoke according to the scientific color scheme 
						//Everything that's not smoke becomes black
						if (scene.showSmoke)
						{
							//Smoke, which is confusing because s is solid in FluidSim
							//s = 0 means max smoke
							float s = f.m[f.To1D(i, j)];

							color[0] = Mathf.Max(0f, color[0] - 255 * s);
							color[1] = Mathf.Max(0f, color[1] - 255 * s);
							color[2] = Mathf.Max(0f, color[2] - 255 * s);
						}
					}
					else if (scene.showSmoke)
					{
						//Not sure why hes using s in stead of m because s means obstacle in the simulation part
						float s = f.m[f.To1D(i, j)];

						//s = 0 means max smoke, and 255 * 0 = 0 -> black 
						color[0] = 255 * s;
						color[1] = 255 * s;
						color[2] = 255 * s;

						//In the paint scene we color the smoke 
						if (scene.sceneNr == FluidScene.SceneNr.Paint)
						{
							color = GetSciColor(s, 0f, 1f);
						}
					}
					//If both pressure and smoke are deactivated, then paint everything black
					//The obstacle itself will be painted later
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

			fluidTexture.Apply();
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

					Vector2 uStart = scene.SimToWorld(x0, y);
					Vector2 uEnd = scene.SimToWorld(x1, y);

					linesToDisplay.Add(new Vector3(uStart.x, uStart.y, z));
					linesToDisplay.Add(new Vector3(uEnd.x, uEnd.y, z));


					//v velocity
					float x = (i + 0.5f) * h;
					float y0 = j * h;
					float y1 = j * h + v * scale;

					Vector2 vStart = scene.SimToWorld(x, y0);
					Vector2 vEnd = scene.SimToWorld(x, y1);

					linesToDisplay.Add(new Vector3(vStart.x, vStart.y, z));
					linesToDisplay.Add(new Vector3(vEnd.x, vEnd.y, z));
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

			//The length of a single segment in simulation space
			//float segLen = f.h * 0.2f;
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
					Vector2 startPos = scene.SimToWorld(x, y);

					streamlineCoordinates.Add(new Vector3(startPos.x, startPos.y, z));

					//Build the line
					for (int n = 0; n < numSegs; n++)
					{
						//The velocity at the current coordinate
						float u = f.SampleField(x, y, FluidSim.SampleArray.uField);
						float v = f.SampleField(x, y, FluidSim.SampleArray.vField);

						//Debug.Log(u);

						//float l = Mathf.Sqrt(u * u + v * v);
						
						// x += u/l * segLen;
						// y += v/l * segLen;
						
						//Move in the direction of the velocity
						x += u * 0.01f;
						y += v * 0.01f;

						//Stop the line if we are outside of the simulation area
						//The guy in the video is only checking x > f.GetWidth() for some reason...
						if (x > f.GetWidth() || x < 0f || y > f.GetHeight() || y < 0f)
						{
							break;
						}

						//Add the next coordinate of the streamline
						Vector2 nextPos2D = scene.SimToWorld(x, y);

						streamlineCoordinates.Add(new Vector3(nextPos2D.x, nextPos2D.y, z));
					}

					//Display the line
					DisplayShapes.DrawLine(streamlineCoordinates, DisplayShapes.ColorOptions.Black);
				}
			}
		}
		


		//
		// Show the circle obstacle
		//
		private static void ShowObstacle(FluidScene scene)
		{		
			FluidSim f = scene.fluid;

			//Make it slightly bigger to hide the jagged edges we get because we use a grid with square cells which will not match the circle edges prefectly
			float r = scene.obstacleRadius + f.h;
			
			DisplayShapes.ColorOptions color = DisplayShapes.ColorOptions.Gray;

			//Black like the bg 
			if (scene.showPressure)
			{
				color = DisplayShapes.ColorOptions.Black;
			}

			//Circle center in global space
			Vector2 globalCenter2D = scene.SimToWorld(scene.obstacleX, scene.obstacleY);

			//3d space infront of the texture
			Vector3 circleCenter = new (globalCenter2D.x, globalCenter2D.y, -0.1f);

			//Generate the circle mesh
			Mesh circleMesh = GenerateCircleMesh(circleCenter, r, 50);

			//Display the circle mesh
			Material material = DisplayShapes.GetMaterial(color);

			Graphics.DrawMesh(circleMesh, Vector3.zero, Quaternion.identity, material, 0, Camera.main, 0);


			//The guy is also giving the circle a black border, which we could replicate by drawing a smaller circle but it doesn't matter! 
		}



		//Generate a circle mesh 
		private static Mesh GenerateCircleMesh(Vector3 circleCenter, float radius, int segments)
		{
			//Generate the vertices
			List<Vector3> vertices = GetCircleSegments_XY(circleCenter, radius, segments);

			//Add the center to make it easier to trianglulate
			vertices.Insert(0, circleCenter);


			//Generate the triangles
			List<int> triangles = new();

			for (int i = 2; i < vertices.Count; i++)
			{
				triangles.Add(0); //0 because the center vertex was added at position 0
				triangles.Add(i);
				triangles.Add(i - 1);
			}

			//Generate the mesh
			Mesh m = new();

			m.SetVertices(vertices);
			m.SetTriangles(triangles, 0);

			m.RecalculateNormals();

			return m;
		}



		//Generate vertices on the circle circumference in xy space
		private static List<Vector3> GetCircleSegments_XY(Vector3 circleCenter, float radius, int segments)
		{
			List<Vector3> vertices = new();

			float angleStep = 360f / (float)segments;

			float angle = 0f;

			for (int i = 0; i < segments + 1; i++)
			{
				float x = radius * Mathf.Cos(angle * Mathf.Deg2Rad);
				float y = radius * Mathf.Sin(angle * Mathf.Deg2Rad);

				Vector3 vertex = new Vector3(x, y, 0f) + circleCenter;

				vertices.Add(vertex);

				angle += angleStep;
			}

			return vertices;
		}



		//
		// The scientific color scheme
		//

		//Get a color from a color gradient which is colored according to the scientific color scheme
		//The color scheme is also called rainbow (jet) or hot-to-cold
		//Similar to HSV color mode where we change the hue (except the purple part)
		//Rainbow is a linear interpolation between (0,0,255) and (255,0,0) in RGB color space (ignoring the purple part which would loop the circle like in HSV)
		//Blue means low pressure and red is high pressure
		//https://stackoverflow.com/questions/7706339/grayscale-to-red-green-blue-matlab-jet-color-scale
		private static Vector4 GetSciColor(float val, float minVal, float maxVal)
		{
			//Clamp val to be within the range
			//val has to be less than maxVal or "int num = Mathf.FloorToInt(val / m);" wont work
			//This will not always work because of floating point precision issues, so we have to fix it later
			val = Mathf.Min(Mathf.Max(val, minVal), maxVal - 0.0001f);

            //Convert to 0->1 range
            float d = maxVal - minVal;
		
			//If min and max are the same, set val to be in the middle or we get a division by zero
			val = (d == 0f) ? 0.5f : (val - minVal) / d;

			//0.25 means 4 buckets 0->3
			//Why 4? A walk on the edges of the RGB color cube: blue -> cyan -> green -> yellow -> red
			float m = 0.25f;

			int num = Mathf.FloorToInt(val / m);

			//Clamp num
			//If val = maxVal, we will end up in bucket 4
			//And we can't make val smaller because of floating point precision issues
			if (num > 3)
			{
				num = 3;
            }

			//s is strength
			float s = (val - num * m) / m;

			float r = 0f;
			float g = 0f;
			float b = 0f;

			//blue -> green -> yellow -> red
			switch (num)
			{
				case 0: r = 0f; g = s;      b = 1f;     break; //blue   (0,0,1) -> cyan   (0,1,1)
				case 1: r = 0f; g = 1f;     b = 1f - s; break; //cyan   (0,1,1) -> green  (0,1,0)
				case 2: r = s;  g = 1f;     b = 0f;     break; //green  (0,1,0) -> yellow (1,1,0)
				case 3: r = 1f; g = 1f - s; b = 0f;     break; //yellow (1,1,0) -> red    (1,0,0)
			}

			Vector4 color = new ( 255 * r, 255 * g, 255 * b, 255);

			//Vector4 color = new(255, 0, 0, 255);

			return color;
		}
	}
}
