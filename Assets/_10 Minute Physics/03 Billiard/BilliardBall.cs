using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Billiard
{
    public class BilliardBall
    {
        public Ball ball;


        public BilliardBall(Vector3 ballVel, Transform ballTrans)
        {
            this.ball = new Ball(ballTrans);

            this.ball.vel = ballVel;
        }



        public void UpdateVisualPostion()
        {
            this.ball.UpdateVisualPosition();
        }



        public void SimulateBall(int subSteps)
        {
            float sdt = Time.fixedDeltaTime / (float)subSteps;

            Vector3 gravity = Vector3.zero;

            for (int step = 0; step < subSteps; step++)
            {
                ball.vel += gravity * sdt;
                ball.pos += ball.vel * sdt;
            }
        }



        public void HandleWallCollision()
        {
            //Make sure the ball is within the area, which is 5 m in all directions (except y)
            //If outside, reset ball and mirror velocity
            float halfSimSize = 5f - ball.radius;

            if (ball.pos.x < -halfSimSize)
            {
                ball.pos.x = -halfSimSize;
                ball.vel.x *= -1f;
            }
            if (ball.pos.x > halfSimSize)
            {
                ball.pos.x = halfSimSize;
                ball.vel.x *= -1f;
            }

            //2d simulation, so no y
            if (ball.pos.z < -halfSimSize)
            {
                ball.pos.z = -halfSimSize;
                ball.vel.z *= -1f;
            }
            if (ball.pos.z > halfSimSize)
            {
                ball.pos.z = halfSimSize;
                ball.vel.z *= -1f;
            }
        }
    }
}