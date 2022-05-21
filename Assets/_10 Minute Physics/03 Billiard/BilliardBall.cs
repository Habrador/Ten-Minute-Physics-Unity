using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Billiard
{
    public class BilliardBall
    {
        public Vector3 vel;
        public Vector3 pos;

        private Transform ballTrans;

        public readonly float radius;



        public BilliardBall(Vector3 ballVel, Transform ballTrans)
        {
            this.vel = ballVel;
            this.pos = ballTrans.position;
            this.ballTrans = ballTrans;

            this.radius = ballTrans.localScale.x * 0.5f;
        }



        public void UpdateVisualPostion()
        {
            ballTrans.position = pos;
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



        public void HandleWallCollision()
        {
            //Make sure the ball is within the area, which is 5 m in all directions (except y)
            //If outside, reset ball and mirror velocity
            float halfSimSize = 5f - radius;

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