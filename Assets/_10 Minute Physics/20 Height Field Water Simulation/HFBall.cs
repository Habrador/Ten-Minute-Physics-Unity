using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using UnityEngine;
using static UnityEditor.PlayerSettings;

namespace HeightFieldWaterSim
{
    //Has to be called HFBall to avoid confusion 
    public class HFBall
    {
        public Vector3 pos;
        public Vector3 vel;

        public float radius;
        public float mass;
        public float restitution;

        public bool isGrabbed;

        private readonly Transform visMesh;



        public HFBall(Vector3 pos, float radius, float density, Material ballMaterial, Transform ballsParent)
        {
            //Physics data 
            this.pos = pos;
            this.radius = radius;
            //m = V * rho = 4/3 * pi * r^3 * rho
            this.mass = (4f/3f) * Mathf.PI * radius * radius * radius * density;
            this.vel = Vector3.zero;
            this.isGrabbed = false;
            this.restitution = 0.1f;


            //Generate the visual mesh showing the ball
            GameObject newBall = GameObject.CreatePrimitive(PrimitiveType.Sphere);

            newBall.transform.parent = ballsParent;

            newBall.transform.position = pos;

            //Unity scale is diameter
            newBall.transform.localScale = 2f * radius * Vector3.one;

            newBall.GetComponent<MeshRenderer>().material = ballMaterial;

            this.visMesh = newBall.transform;
        }



        //Collision with other balls
        public void HandleCollision(HFBall other)
        {
            Vector3 dir = other.pos - this.pos;

            float d = dir.magnitude;

            float minDist = this.radius + other.radius;
            
            if (d >= minDist)
            {
                return;
            }
                
            dir *= (1f / d);
            
            float corr = (minDist - d) / 2f;
            
            this.pos += dir * -corr;

            other.pos += dir * corr;

            float v1 = Vector3.Dot(this.vel, dir);
            float v2 = Vector3.Dot(other.vel, dir);

            float m1 = this.mass;
            float m2 = other.mass;

            float newV1 = (m1 * v1 + m2 * v2 - m2 * (v1 - v2) * this.restitution) / (m1 + m2);
            float newV2 = (m1 * v1 + m2 * v2 - m1 * (v2 - v1) * this.restitution) / (m1 + m2);

            this.vel += dir * (newV1 - v1);
            other.vel += dir * (newV2 - v2);
        }




        public void Simulate(float dt)
        {
            if (this.isGrabbed)
            {
                return;
            }
               

            //Simulate the ball
            this.vel += MyPhysicsScene.gravity * dt;

            this.pos += this.vel * dt;


            //Handle collision with the environment (except water)
            float wx = 0.5f * MyPhysicsScene.tankSize.x - this.radius - 0.5f * MyPhysicsScene.tankBorder;
            float wz = 0.5f * MyPhysicsScene.tankSize.z - this.radius - 0.5f * MyPhysicsScene.tankBorder;

            if (this.pos.x < -wx)
            {
                this.pos.x = -wx; this.vel.x = -this.restitution * this.vel.x;
            }
            if (this.pos.x > wx)
            {
                this.pos.x = wx; this.vel.x = -this.restitution * this.vel.x;
            }
            if (this.pos.z < -wz)
            {
                this.pos.z = -wz; this.vel.z = -this.restitution * this.vel.z;
            }
            if (this.pos.z > wz)
            {
                this.pos.z = wz; this.vel.z = -this.restitution * this.vel.z;
            }
            if (this.pos.y < this.radius)
            {
                this.pos.y = this.radius; this.vel.y = -this.restitution * this.vel.y;
            }

            this.visMesh.position = this.pos;
        }



        public void ApplyForce(float force, float dt)
        {
            this.vel.y += dt * force / this.mass;
            
            this.vel *= 0.999f;
        }



        public void StartGrab(Vector3 pos)
        {
            this.isGrabbed = true;

            this.pos = pos;
            this.visMesh.position = pos;
        }

        public void MoveGrabbed(Vector3 pos)
        {
            this.pos = pos;
            this.visMesh.position = pos;
        }

        public void EndGrab(Vector3 pos, Vector3 vel)
        {
            this.isGrabbed = false;
            this.vel = vel;
        }
    }
}