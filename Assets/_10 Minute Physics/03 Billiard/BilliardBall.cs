using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Billiard
{
    public class BilliardBall : Ball
    {
        private bool isActive = true;



        public BilliardBall(Vector3 ballVel, Transform ballTrans) : base(ballTrans)
        {
            vel = ballVel;
        }



        public BilliardBall(Transform ballTrans) : base(ballTrans)
        {
            
        }



        public void SimulateBall(int subSteps, float sdt)
        {
            if (!isActive)
            {
                return;
            }
        
            Vector3 gravity = Vector3.zero;

            for (int step = 0; step < subSteps; step++)
            {
                vel += gravity * sdt;
                pos += vel * sdt;
            }
        }



        public void DeActivateBall()
        {
            isActive = false;

            ballTransform.gameObject.SetActive(false);
        }
    }
}