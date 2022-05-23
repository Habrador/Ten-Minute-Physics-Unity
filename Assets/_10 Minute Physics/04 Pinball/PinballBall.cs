using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PinballMachine
{
    public class PinballBall : Ball
    {   
        private float restitution;



        public PinballBall(Vector3 ballVel, Transform ballTrans, float restitution) : base(ballTrans)
        {
            vel = ballVel;
        
            this.restitution = restitution;
        }



        public void SimulateBall(float dt, Vector3 gravity)
        {
            vel += gravity * dt;
            pos += vel * dt;
        }
    }
}