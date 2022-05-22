using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PinballMachine
{
    public class Flipper : MonoBehaviour
    {
        public float radius;

        public Vector3 pos;

        private float length;

        //Angles should be in radians!
        private float restAngle;

        private float maxRotation;

        private float sign;

        private float angularVel;

        private float restitution;

        //Changing
        private float rotation;

        private float currentAngularVel;

        public float touchIdentifier = -1f; //Is this flipper activated?



        public Flipper(float radius, Vector3 pos, float length, float restAngle, float maxRotation, float angularVel, float restitution)
        {
            this.radius = radius;
            this.pos = pos;
            this.length = length;
            this.restAngle = restAngle;
            this.maxRotation = Mathf.Abs(maxRotation);
            this.sign = Mathf.Sign(maxRotation);
            this.angularVel = angularVel;
            this.restitution = restitution;

            this.rotation = 0f;
            this.currentAngularVel = 0f;
        }



        public void Simulate(float dt)
        {
            float prevRotation = this.rotation;

            bool pressed = this.touchIdentifier >= 0f;

            if (pressed)
            {
                this.rotation = Mathf.Min(this.rotation + dt * angularVel, this.maxRotation);
            }
            else
            {
                this.rotation = Mathf.Max(this.rotation - dt * angularVel, 0f);
            }

            this.currentAngularVel = this.sign * (this.rotation - prevRotation) / dt;
        }



        //Is the flipper activated if we click close to it (if we are controlling the flipper with the mouse)
        public bool Select(Vector3 pos)
        {
            Vector3 d = this.pos - pos;

            bool isSelected = d.magnitude < this.length;

            return isSelected;
        }



        public Vector3 GetTip()
        {
            float angle = this.restAngle + this.sign * this.rotation;

            float x = Mathf.Cos(angle);
            float y = Mathf.Sin(angle);

            //This one is already normalized
            Vector3 dir = new Vector3(x, y, 0f);

            Vector3 tip = this.pos + dir * length;

            return tip;
        }
    }
}