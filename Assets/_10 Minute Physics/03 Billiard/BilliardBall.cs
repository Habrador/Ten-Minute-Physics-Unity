using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Billiard
{
    public class BilliardBall : Ball
    {

        public BilliardBall(Vector3 ballVel, Transform ballTrans) : base(ballTrans)
        {
            vel = ballVel;
        }



        public void SimulateBall(int subSteps)
        {
            float sdt = Time.fixedDeltaTime / (float)subSteps;

            Vector3 gravity = Vector3.zero;

            for (int step = 0; step < subSteps; step++)
            {
                vel += gravity * sdt;
                pos += vel * sdt;
            }
        }



        //
        // Collision with environment detection
        //

        public void HandleSquareCollision(float wallLength)
        {
            //Make sure the ball is within the area, which is 5 m in all directions (except y)
            //If outside, reset ball and mirror velocity
            float halfSimSize = wallLength - radius;

            if (pos.x < -halfSimSize)
            {
                pos.x = -halfSimSize;
                vel.x *= -1f;
            }
            if (pos.x > halfSimSize)
            {
                pos.x = halfSimSize;
                vel.x *= -1f;
            }

            //2d simulation, so no y
            if (pos.z < -halfSimSize)
            {
                pos.z = -halfSimSize;
                vel.z *= -1f;
            }
            if (pos.z > halfSimSize)
            {
                pos.z = halfSimSize;
                vel.z *= -1f;
            }
        }
    }
}