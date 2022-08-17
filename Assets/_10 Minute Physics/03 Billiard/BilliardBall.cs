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


        public BilliardBall(Transform ballTrans) : base(ballTrans)
        {
            
        }



        public void SimulateBall(int subSteps, float sdt)
        {
            Vector3 gravity = Vector3.zero;

            for (int step = 0; step < subSteps; step++)
            {
                vel += gravity * sdt;
                pos += vel * sdt;
            }
        }
    }
}