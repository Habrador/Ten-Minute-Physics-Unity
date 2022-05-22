using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PinballMachine
{
    public class PinballBall
    {
        public Ball ball;
    
        private float restitution;



        public PinballBall(Vector3 ballVel, Transform ballTrans, float restitution)
        {
            this.ball = new Ball(ballTrans);

            this.ball.vel = ballVel;
        
            this.restitution = restitution;
        }



        public void UpdateVisualPostion()
        {
            this.ball.UpdateVisualPosition();
        }



        public void SimulateBall(float dt, Vector3 gravity)
        {
            this.ball.vel += gravity * dt;
            this.ball.pos += this.ball.vel * dt;
        }
    }
}