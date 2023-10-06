using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HeightFieldWaterSim
{
    //Has to be called MyPhysicsScene because PhysicsScene is a Unity thing
    public static class MyPhysicsScene
    {
		public static Vector3 gravity = new(0f, -10f, 0f);

        //public static float dt = 1.0f / 30.0f;
        public static float dt = Time.fixedDeltaTime;

        public static Vector3 tankSize = new(2.5f, 1.0f, 3.0f);
        
		public static float tankBorder = 0.03f;
		
		public static float waterHeight = 0.8f;
		public static float waterSpacing = 0.02f;
		
		public static bool isPaused = true;
		
		public static WaterSurface waterSurface = null;
		
		public static List<HFBall> objects = new();
    }
}