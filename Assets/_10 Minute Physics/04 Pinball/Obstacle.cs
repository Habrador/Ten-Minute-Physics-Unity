using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PinballMachine
{ 
    //The obstacles within the pinball machine are currently the discs that makes the ball bounce around
    public class Obstacle
    {
        public float radius;
        
        public Vector3 pos;

        //The velocity the ball gets if it collides with this obstacle
        public float pushVel;


        public Obstacle(float radius, Vector3 pos, float pushVel)
        {
            this.radius = radius;
            this.pos = pos;
            this.pushVel = pushVel;
        }
    }
}