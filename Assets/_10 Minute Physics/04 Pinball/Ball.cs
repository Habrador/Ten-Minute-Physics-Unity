using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PinballMachine
{
    public class Ball
    {
        public Vector3 vel;
        public Vector3 pos;

        private Transform ballTrans;

        public readonly float radius;

        private float mass;
        private float restitution;



        public Ball(Vector3 ballVel, Transform ballTrans, float mass, float restitution)
        {
            this.vel = ballVel;
            this.pos = ballTrans.position;
            this.ballTrans = ballTrans;
            this.mass = mass;
            this.restitution = restitution;

            this.radius = ballTrans.localScale.x * 0.5f;
        }



        public void UpdateVisualPostion()
        {
            ballTrans.position = pos;
        }



        public void SimulateBall(float dt, Vector3 gravity)
        {
            vel += gravity * dt;
            pos += vel * dt;


            //WallCollisionDetection();
        }



        public void HandleWallCollision()
        {
            //Collision detection

            //Make sure the all is within the area, which is 5 m in all directions (except y)
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

            //2d simulation
            //if (pos.y < 0f + radius)
            //{
            //    pos.y = 0f + 0.1f;
            //    vel.y *= -1f;
            //}
            //Sky is the limit
            //if (ballPos.y > halfSimSize)
            //{
            //    ballPos.y = halfSimSize;
            //    ballVel.y *= -1f;
            //}

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