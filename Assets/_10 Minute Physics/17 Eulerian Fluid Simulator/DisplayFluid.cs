using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Display the fluid simulation on a texture
public class DisplayFluid
{
	public void Draw(Scene scene)
	{
		//c is CanvasRenderingContext2D which is like a texture
		//c.clearRect(0, 0, canvas.width, canvas.height);

		//c.fillStyle = "#FF0000";
		
		Fluid f = scene.fluid;
		
		int n = f.numY;

		float cellScale = 1.1f;

		float h = f.h;

		//Find min and max pressure
		float minP = f.p[0];
		float maxP = f.p[0];

		for (int i = 0; i < f.numCells; i++)
		{
			minP = Mathf.Min(minP, f.p[i]);
			maxP = Mathf.Max(maxP, f.p[i]);
		}

		//id = c.getImageData(0, 0, canvas.width, canvas.height)

		Color color = new Color(255, 255, 255, 255);

		for (int i = 0; i < f.numX; i++)
		{
			for (int j = 0; j < f.numY; j++)
			{
				if (scene.showPressure)
				{
					float p = f.p[i * n + j];
					float s = f.m[i * n + j];

					color = GetSciColor(p, minP, maxP);
					
					//To color the smoke according to the scientific color scheme 
					if (scene.showSmoke)
					{
						color.r = Mathf.Max(0f, color.r - 255 * s);
						color.g = Mathf.Max(0f, color.g - 255 * s);
						color.b = Mathf.Max(0f, color.b - 255 * s);
					}
				}
				else if (scene.showSmoke)
				{
					float s = f.m[i * n + j];
					
					color.r = 255 * s;
					color.g = 255 * s;
					color.b = 255 * s;
					
					if (scene.sceneNr == 2)
					{
						color = GetSciColor(s, 0f, 1f);
					}	
				}
				//Obstacle means black
				else if (f.s[i * n + j] == 0f)
				{
					color.r = 0;
					color.g = 0;
					color.b = 0;
				}

				//Put the color on the texture
				/*
				int x = Mathf.FloorToInt(cX(i * h));
				int y = Mathf.FloorToInt(cY((j + 1) * h));
				int cx = Mathf.FloorToInt(cScale * cellScale * h) + 1;
				int cy = Mathf.FloorToInt(cScale * cellScale * h) + 1;

				float r = color.r;
				float g = color.g;
				float b = color.b;

				for (int yi = y; yi < y + cy; yi++)
				{
					var p = 4 * (yi * canvas.width + x)

					for (var xi = 0; xi < cx; xi++)
					{
						id.data[p++] = r;
						id.data[p++] = g;
						id.data[p++] = b;
						id.data[p++] = 255;
					}
				}
				*/
			}
		}

		//c.putImageData(id, 0, 0);

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
			//ShowObstacle();
		}

		//Moved the display of min and max pressure as text to the UI method
	}

	/*
	private float cX(float x)
	{
		return x * cScale;
	}

	private float cY(float y)
	{
		return canvas.height - y * cScale;
	}
	*/


	/*
	private void ShowVelocities()
	{
		c.strokeStyle = "#000000";
		scale = 0.02;

		for (var i = 0; i < f.numX; i++)
		{
			for (var j = 0; j < f.numY; j++)
			{

				var u = f.u[i * n + j];
				var v = f.v[i * n + j];

				c.beginPath();

				x0 = cX(i * h);
				x1 = cX(i * h + u * scale);
				y = cY((j + 0.5) * h);

				c.moveTo(x0, y);
				c.lineTo(x1, y);
				c.stroke();

				x = cX((i + 0.5) * h);
				y0 = cY(j * h);
				y1 = cY(j * h + v * scale)

					c.beginPath();
				c.moveTo(x, y0);
				c.lineTo(x, y1);
				c.stroke();

			}
		}
	}
	*/
	/*
	private void ShowStreamlines()
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

	/*
	private void ShowObstacle()
	{
		c.strokeW
			r = scene.obstacleRadius + f.h;
		if (scene.showPressure)
			c.fillStyle = "#000000";
		else
			c.fillStyle = "#DDDDDD";
		c.beginPath();
		c.arc(
			cX(scene.obstacleX), cY(scene.obstacleY), cScale * r, 0.0, 2.0 * Math.PI);
		c.closePath();
		c.fill();

		c.lineWidth = 3.0;
		c.strokeStyle = "#000000";
		c.beginPath();
		c.arc(
			cX(scene.obstacleX), cY(scene.obstacleY), cScale * r, 0.0, 2.0 * Math.PI);
		c.closePath();
		c.stroke();
		c.lineWidth = 1.0;
	}
	*/

	/*
	function setColor(r, g, b)
	{
		c.fillStyle = `rgb(
			${ Math.floor(255 * r)},
			${ Math.floor(255 * g)},
			${ Math.floor(255 * b)})`
		c.strokeStyle = `rgb(
			${ Math.floor(255 * r)},
			${ Math.floor(255 * g)},
			${ Math.floor(255 * b)})`
	}
	*/



	//Scientific color scheme
	private Color GetSciColor(float val, float minVal, float maxVal)
	{
		val = Mathf.Min(Mathf.Max(val, minVal), maxVal - 0.0001f);
		
		float d = maxVal - minVal;
		
		val = d == 0.0f ? 0.5f : (val - minVal) / d;
		
		float m = 0.25f;

		int num = Mathf.FloorToInt(val / m);
		
		float s = (val - num * m) / m;

		float r = 0f;
		float g = 0f;
		float b = 0f;

		switch (num)
		{
			case 0: r = 0f; g = s;      b = 1f;     break;
			case 1: r = 0f; g = 1f;     b = 1f - s; break;
			case 2: r = s;  g = 1f;     b = 0f;     break;
			case 3: r = 1f; g = 1f - s; b = 0f;     break;
		}

		return new Color(255 * r, 255 * g, 255 * b, 255);
	}
}
