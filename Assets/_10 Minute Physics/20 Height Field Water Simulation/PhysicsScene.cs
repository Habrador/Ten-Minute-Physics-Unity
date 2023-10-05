using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HeightFieldWaterSim
{
    public class PhysicsScene
    {
		public static Vector3 gravity = new(0f, -10f, 0f);
		
		public static float dt = 1.0f / 30.0f;
		
		public static Vector3 tankSize = new(2.5f, 1.0f, 3.0f);
        public static float tankBorder = 0.03f;
		
		float waterHeight = 0.8f;
		float waterSpacing = 0.02f;
		
		bool paused = true;
		
		Mesh waterSurface = null;
		
		public static Ball[] objects;
    }
}