using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using UnityEngine;
using static UnityEditor.PlayerSettings;

namespace HeightFieldWaterSim
{
    public class Ball
    {
        public Vector3 pos;
        public float radius;
        public float mass;
        public Vector3 vel;

        public bool grabbed;

        public float restitution;


        public Ball(Vector3 pos, float radius, float density, UnityEngine.Color color)
        {
            //Physics data 

            this.pos = pos;
            this.radius = radius;
            this.mass = 4f * Mathf.PI / 3f * radius * radius * radius * density;
            this.vel = Vector3.zero;
            this.grabbed = false;
            this.restitution = 0.1f;

            //Visual mesh
            /*
            let geometry = new THREE.SphereGeometry(radius, 32, 32);
            let material = new THREE.MeshPhongMaterial({ color: color});
            this.visMesh = new THREE.Mesh(geometry, material);
		    this.visMesh.position.copy(pos);
			this.visMesh.userData = this;		// for raycasting
		    this.visMesh.layers.enable(1);
            gThreeScene.add(this.visMesh);
            */
        }



        public void ApplyForce(float force)
        {
            this.vel.y += PhysicsScene.dt * force / this.mass;
            
            this.vel *= 0.999f;
        }
    }
}