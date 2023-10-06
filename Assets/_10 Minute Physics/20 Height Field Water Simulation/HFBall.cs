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
        public float radius;
        public float mass;
        public Vector3 vel;

        public bool grabbed;

        public float restitution;

        private Transform visMesh;



        public HFBall(Vector3 pos, float radius, float density, Material ballMaterial, Transform ballsParent)
        {
            //Physics data 
            this.pos = pos;
            this.radius = radius;
            this.mass = 4f * Mathf.PI / 3f * radius * radius * radius * density;
            this.vel = Vector3.zero;
            this.grabbed = false;
            this.restitution = 0.1f;

            //Visual mesh
            GameObject newBall = GameObject.CreatePrimitive(PrimitiveType.Sphere);

            newBall.transform.parent = ballsParent;

            newBall.transform.position = pos;

            newBall.transform.localScale = Vector3.one * radius;

            newBall.GetComponent<MeshRenderer>().material = ballMaterial;

            this.visMesh = newBall.transform;
        }



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



        public void Simulate()
        {
            if (this.grabbed)
            {
                return;
            }
               

            this.vel += MyPhysicsScene.gravity * MyPhysicsScene.dt;

            this.pos += this.vel * MyPhysicsScene.dt;

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
            //this.visMesh.geometry.computeBoundingSphere();
        }



        public void ApplyForce(float force)
        {
            this.vel.y += MyPhysicsScene.dt * force / this.mass;
            
            this.vel *= 0.999f;
        }


        public void StartGrab(Vector3 pos)
        {
            this.grabbed = true;

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
            this.grabbed = false;
            this.vel = vel;
        }
    }
}