using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HeightFieldWaterSim
{
    //Has to be called MyPhysicsScene because PhysicsScene is a Unity thing
    public static class MyPhysicsScene
    {
		public static Vector3 gravity = new(0f, -10f, 0f);

		//Tank dimensions
        public static Vector3 tankSize = new(2.5f, 1.0f, 3.0f);
		public static float tankBorder = 0.03f;
		
		//Water data
		//The water height 
		public static float waterHeight = 0.8f;
		//The size of each cell, width and depth
		public static float waterSpacing = 0.03f;
		
		public static bool isPaused = true;
		
		public static WaterSurface waterSurface = null;
		
		//The floating objects
		public static List<HFBall> objects = new();
    }
}