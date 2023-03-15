using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Display the fluid simulation on a texture
namespace EulerianFluidSimulator
{
	public class DisplayFluid
	{
		private readonly Material fluidMaterial;

		private Texture2D fluidTexture;



		public DisplayFluid(Material fluidMaterial)
		{
			this.fluidMaterial = fluidMaterial;
		}



		//For testing
		public void TestDraw()
		{
			if (fluidTexture == null)
			{
				fluidTexture = new(4, 2);

				//So the pixels dont blend
				fluidTexture.filterMode = FilterMode.Point;

				fluidTexture.wrapMode = TextureWrapMode.Clamp;

				this.fluidMaterial.mainTexture = fluidTexture;
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

			fluidTexture.SetPixels32(colors);

			fluidTexture.Apply();

			//Debug.Log(colors[4]); //RGBA(255, 0, 0, 255)
		}



		public void Draw(FluidScene scene)
		{
			UpdateTexture(scene);

			//float cellScale = 1.1f;

			//float h = f.h;

			if (scene.showVelocities)
			{
				//ShowVelocities();
			}

			if (scene.showStreamlines)
			{
				//ShowStreamlines();
			}

			if (scene.showObstacle)
			{
				ShowObstacle(scene);
			}

			//Moved the display of min and max pressure as text to the UI method
		}



		//Paint the fluid, not obstacles
		private void UpdateTexture(FluidScene scene)
		{
			FluidSim f = scene.fluid;

			//Generate a new texture if none exists or if we have changed resolution
			if (this.fluidTexture == null || this.fluidTexture.width != f.numX || this.fluidTexture.height != f.numY)
			{
				this.fluidTexture = new(f.numX, f.numY);

				//So the pixels dont blend
				//fluidTexture.filterMode = FilterMode.Point;

				//Blend the pixels 
				this.fluidTexture.filterMode = FilterMode.Bilinear;

				//So the borders dont wrap with the border on the other side
				this.fluidTexture.wrapMode = TextureWrapMode.Clamp;

				this.fluidMaterial.mainTexture = fluidTexture;
			}

			Color32[] textureColors = new Color32[f.numX * f.numY];

			//To convert from 2d to 1d array
			int n = f.numY;

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
						float p = f.p[i * n + j];

						color = GetSciColor(p, minMaxP.min, minMaxP.max);

						//Color the smoke according to the scientific color scheme 
						//Everything that's not smoke becomes black
						if (scene.showSmoke)
						{
							//Smoke, which is confusing because s is solid in FluidSim
							//s = 0 means max smoke
							float s = f.m[i * n + j];

							color[0] = Mathf.Max(0f, color[0] - 255 * s);
							color[1] = Mathf.Max(0f, color[1] - 255 * s);
							color[2] = Mathf.Max(0f, color[2] - 255 * s);
						}
					}
					else if (scene.showSmoke)
					{
						//Not sure why hes using s in stead of m because s means obstacle in the simulation part
						float s = f.m[i * n + j];

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
					else if (f.s[i * n + j] == 0f)
					{
						color[0] = 0;
						color[1] = 0;
						color[2] = 0;
					}

					//Add the color to this pixel
					//Color32 is 0-255
					Color32 pixelColor = new((byte)color[0], (byte)color[1], (byte)color[2], (byte)color[3]);

					textureColors[i * n + j] = pixelColor;
				}
			}

			//Add all colors to the texture
			fluidTexture.SetPixels32(textureColors);

			fluidTexture.Apply();
		}
	


		//Show the u and v velocities at each cell by drawing lines
		private void ShowVelocities(FluidScene scene)
		{
			FluidSim f = scene.fluid;

			//To convert from 2d to 1d array
			int n = f.numY;

			float h = f.h;

			//The length of the lines which will be scaled by the velocity
			float scale = 0.02f;

			List<Vector2> linesToDisplay = new List<Vector2>();

			for (int i = 0; i < f.numX; i++)
			{
				for (int j = 0; j < f.numY; j++)
				{
					float u = f.u[i * n + j];
					float v = f.v[i * n + j];

					//u velocity
					float x0 = i * h;
					float x1 = i * h + u * scale;
					float y = (j + 0.5f) * h;

					Vector2 uStart = scene.SimToWorld(x0, y);
					
					Vector2 uEnd = scene.SimToWorld(x1, y);

					//v velocity
					float x = (i + 0.5f) * h;
					float y0 = j * h;
					float y1 = j * h + v * scale;

					//x, y0
					Vector2 vStart = scene.SimToWorld(x, y0);
					//x, y1
					Vector2 vEnd = scene.SimToWorld(x, y1);


					linesToDisplay.Add(uStart);
					linesToDisplay.Add(uEnd);
					linesToDisplay.Add(vStart);
					linesToDisplay.Add(vEnd);
				}
			}

			//Display the lines with some black color
		}

		/*
		private void ShowStreamlines(FluidScene scene)
		{
			var segLen = f.h * 0.2;
			var numSegs = 15;

			c.strokeStyle = "#000000";

			for (var i = 1; i < f.numX - 1; i += 5)
			{
				for (var j = 1; j < f.numY - 1; j += 5)
				{

					var x = (i + 0.5) * f.h;
					var y = (j + 0.5) * f.h;

					c.beginPath();
					c.moveTo(cX(x), cY(y));

					for (var n = 0; n < numSegs; n++)
					{
						var u = f.sampleField(x, y, U_FIELD);
						var v = f.sampleField(x, y, V_FIELD);
						l = Math.sqrt(u * u + v * v);
						// x += u/l * segLen;
						// y += v/l * segLen;
						x += u * 0.01;
						y += v * 0.01;
						if (x > f.numX * f.h)
							break;

						c.lineTo(cX(x), cY(y));
					}
					c.stroke();
				}
			}
		}
		*/


		private void ShowObstacle(FluidScene scene)
		{
			FluidSim f = scene.fluid;

			//Make it slightly bigger to avoid jagged edges?
			float r = scene.obstacleRadius + f.h;

			DisplayShapes.ColorOptions color = DisplayShapes.ColorOptions.Gray;

			//Black like the bg 
			if (scene.showPressure)
			{
				color = DisplayShapes.ColorOptions.Black;
			}

			//Circle center in global space
			Vector2 globalCenter2D = scene.SimToWorld(scene.obstacleX, scene.obstacleY);

			Vector3 circleCenter = new (globalCenter2D.x, globalCenter2D.y, 0.1f);

			//Display a circle mesh
			DisplayShapes.DrawCircle(circleCenter, r, color, DisplayShapes.Space2D.XY);
		
		
			//The guy is also giving the circle a black border...
		}



		//Scientific color scheme
		//Rainbow (jet) or hot-to-cold
		//Similar to HSV color mode where we change the hue (except the purple part)
		//Rainbow is a linear interpolation between (0,0,255) and (255,0,0) in RGB color space (ignoring the purple part which would loop the circle like in HSV)
		//Blue means low pressure and red is high pressure
		//https://stackoverflow.com/questions/7706339/grayscale-to-red-green-blue-matlab-jet-color-scale
		private Vector4 GetSciColor(float val, float minVal, float maxVal)
		{
			//Clamp val to be within the range
			//val has to be less than maxVal or "int num = Mathf.FloorToInt(val / m);" wont work
			val = Mathf.Min(Mathf.Max(val, minVal), maxVal - 0.0001f);

			//Convert to 0->1 range
			float d = maxVal - minVal;
		
			//If min and max are the same, set val to be in the middle or we get a division by zero
			val = (d == 0.0f) ? 0.5f : (val - minVal) / d;

			//0.25 means 4 buckets 0->3
			//Why 4? A walk on the edges of the RGB color cube: blue -> cyan -> green -> yellow -> red
			float m = 0.25f;

			int num = Mathf.FloorToInt(val / m);
		
			//s is strength?
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

			return color;
		}
	}
}
