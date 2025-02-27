using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using UnityEngine;
using static UnityEditor.PlayerSettings;

public class MyRigidBody
{
    //Types of rbs we can simulate
    public enum Types
    {
        Box,
        Sphere
    }

    Types type;

    Vector3 size;
    float dt;
    public float damping;

    Vector3 pos;
    Quaternion rot;
    Vector3 vel;
    Vector3 omega;

    Vector3 prevPos;
    Quaternion prevRot;
    Quaternion dRot;
    Quaternion invRot;

    float invMass;
    Vector3 invInertia;

    List<Mesh> meshes;
    float[] vertices;
    int[] triIds;

 

    //Removed parameter scene which is a gThreeScene whish is like the sim environment
    public MyRigidBody(Types type, Vector3 size, float density, Vector3 pos, Vector3 angles, float fontSize = 0f)
    {
        this.type = type;
        this.size = new Vector3(size.x, size.y, size.z);
        this.dt = 0f;
        this.damping = 0f;

        this.pos = new Vector3(pos.x, pos.y, pos.z);
        this.rot = new Quaternion();
        this.rot.eulerAngles = angles;
        this.vel = Vector3.zero;
        this.omega = Vector3.zero;

        this.prevPos = this.pos;
        this.prevRot = this.rot;
        this.dRot = new Quaternion();
        this.invRot = this.rot;
        //this.invRot.invert();

        this.invMass = 0f;
        this.invInertia = Vector3.zero;

        this.meshes = new List<Mesh>();
        this.vertices = null;
        this.triIds = null;

        float mass = 0f;
        
        if (type == Types.Box)
        {
            //let mesh = new THREE.Mesh(new THREE.BoxBufferGeometry(size.x, size.y, size.z), new THREE.MeshPhongMaterial({ color: 0xffffff }));
        
            //this.meshes.push(mesh);
        
            if (density > 0f)
            {
                mass = density * size.x * size.y * size.z;

                this.invMass = 1f / mass;
                
                float Ix = 1f / 12f * mass * (size.y * size.y + size.z * size.z);
                float Iy = 1f / 12f * mass * (size.x * size.x + size.z * size.z);
                float Iz = 1f / 12f * mass * (size.x * size.x + size.y * size.y);
                
                this.invInertia = new Vector3(1f / Ix, 1f / Iy, 1f / Iz);
            }

            float ex = 0.5f * size.x;
            float ey = 0.5f * size.y;
            float ez = 0.5f * size.z;

            this.vertices = new float[]
            {
                -ex, -ey, -ez,
                ex, -ey, -ez,
                ex, ey, -ez,
                -ex, ey, -ez,
                -ex, -ey, ez,
                ex, -ey, ez,
                ex, ey, ez,
                -ex, ey, ez
            };
        }
        else if (type == Types.Sphere) 
        {
            //let hemiSphere0 = new THREE.Mesh(new THREE.SphereBufferGeometry(size.x, 32, 32, 0.0, Math.PI), new THREE.MeshPhongMaterial({ color: 0xffffff }));

            //let hemiSphere1 = new THREE.Mesh(new THREE.SphereBufferGeometry(size.x, 32, 32, Math.PI, Math.PI), new THREE.MeshPhongMaterial({ color: 0xff0000 }));
            
            //this.meshes.push(hemiSphere0);
            //this.meshes.push(hemiSphere1);
            if (density > 0f)
            {
                mass = 4f / 3f * Mathf.PI * size.x * size.x * size.x * density;
                
                this.invMass = 1f / mass;
                
                float I = 2f / 5f * mass * size.x * size.x;
                
                this.invInertia = new Vector3(1f / I, 1f / I, 1f / I);
            }
        }

                    
        //for (int i = 0; i < this.meshes.Count; i++)
        //{
        //    Mesh mesh = this.meshes[i];
        //    mesh.body = this;       // for raycasting
        //    mesh.layers.enable(1);
        //    mesh.castShadow = true;
        //    mesh.receiveShadow = true;
        //    scene.add(mesh);
        //}

        //Create text renderer for mass display
        //this.textRenderer = null;
        
        //if (fontSize > 0.0)
        //{
        //    this.textRenderer = new TextRenderer(scene, fontSize);
        //    this.textRenderer.loadFont().then(() => {
        //    this.textRenderer.createText(`${ mass.toFixed(1)} kg`, this.meshes[0].position);});
        //}
                    
        UpdateMeshes();
    }

    private void UpdateMeshes()
    {
        for (int i = 0; i < this.meshes.Count; i++)
        {
            //this.meshes[i].position.copy(this.pos);
            //this.meshes[i].quaternion.copy(this.rot);
            //this.meshes[i].geometry.computeBoundingSphere();
        }

        //if (this.textRenderer)
        //{
        //    this.textRenderer.updatePosition(this.meshes[0].position);
        //    this.textRenderer.updateRotation(gCamera.quaternion);
        //}
    }



    //
    // Begin simulation functions
    //
    //private void LocalToWorld(localPos, worldPos)
    //{
    //    worldPos.copy(localPos);
    //    worldPos.applyQuaternion(this.rot);
    //    worldPos.add(this.pos);
    //}

    //private void WorldToLocal(worldPos, localPos)
    //                {
    //    localPos.copy(worldPos);
    //    localPos.sub(this.pos);
    //    localPos.applyQuaternion(this.invRot);
    //}

    private void Integrate(float dt, Vector3 gravity)
                    {
        this.dt = dt;

        if (this.invMass == 0f)
        {
            return;
        }

        //Linear motion
        this.prevPos = this.pos;

        this.vel += gravity * dt;

        this.pos += this.vel * dt;

        //Angular motion
        this.prevRot = this.rot;

        //this.dRot.set(
        //    this.omega.x,
        //    this.omega.y,
        //    this.omega.z,
        //    0.0
        //);

        //this.dRot.multiply(this.rot);
        //this.rot.x += 0.5 * dt * this.dRot.x;
        //this.rot.y += 0.5 * dt * this.dRot.y;
        //this.rot.z += 0.5 * dt * this.dRot.z;
        //this.rot.w += 0.5 * dt * this.dRot.w;
        //this.rot.normalize();
        //this.invRot.copy(this.rot);
        //this.invRot.invert();
    }

    private void UpdateVelocities()
    {
        if (this.invMass == 0f)
        {
            return;
        }

        // linear motion
        //this.vel.subVectors(this.pos, this.prevPos);
        //this.vel.multiplyScalar(1.0 / this.dt);

        //// angular motion
        //this.prevRot.invert();
        //this.dRot.multiplyQuaternions(this.rot, this.prevRot);
        //this.omega.set(
        //    this.dRot.x * 2.0 / this.dt,
        //    this.dRot.y * 2.0 / this.dt,
        //    this.dRot.z * 2.0 / this.dt
        //);
        //if (this.dRot.w < 0.0)
        //    this.omega.negate();

        //this.vel.multiplyScalar(Math.max(1.0 - this.damping * this.dt, 0.0));
    }

    private float GetInverseMass(Vector3 normal, Vector3 pos)
    {
        if (this.invMass == 0f)
        {
            return 0f;
        }

        //let rn = normal.clone();

        //if (pos == undefined)  // angular case
        //{
        //    rn.applyQuaternion(this.invRot);
        //}
        //else            // linear case
        //{
        //    rn.subVectors(pos, this.pos);
        //    rn.cross(normal);
        //    rn.applyQuaternion(this.invRot);
        //}

        //let w =
        //    rn.x * rn.x * this.invInertia.x +
        //    rn.y * rn.y * this.invInertia.y +
        //    rn.z * rn.z * this.invInertia.z;

        //if (pos != undefined)
        //    w += this.invMass;

        //return w;
        return 0f;
    }

    private void _ApplyCorrection(Vector3 corr, Vector3 pos)
    {
        if (this.invMass == 0f)
        {
            return;
        }

        //// linear correction

        //this.pos.addScaledVector(corr, this.invMass);

        //// angular correction

        //let dOmega = corr.clone();

        //dOmega.subVectors(pos, this.pos);
        //dOmega.cross(corr);
        //dOmega.applyQuaternion(this.invRot);
        //dOmega.multiply(this.invInertia);
        //dOmega.applyQuaternion(this.rot);

        //this.dRot.set(
        //    dOmega.x,
        //    dOmega.y,
        //    dOmega.z,
        //    0.0
        //);

        //this.dRot.multiply(this.rot);
        //this.rot.x += 0.5 * this.dRot.x;
        //this.rot.y += 0.5 * this.dRot.y;
        //this.rot.z += 0.5 * this.dRot.z;
        //this.rot.w += 0.5 * this.dRot.w;
        //this.rot.normalize();
        //this.invRot.copy(this.rot);
        //this.invRot.invert();
    }

    private void ApplyCorrection(float compliance, Vector3 corr, Vector3 pos, MyRigidBody otherBody, Vector3 otherPos)
    {
        //if (corr.lengthSq() == 0.0)
        //    return;

        //let C = corr.length();
        //let normal = corr.clone();
        //normal.normalize();

        //let w = this.getInverseMass(normal, pos);
        //if (otherBody != undefined)
        //    w += otherBody.getInverseMass(normal, otherPos);

        //if (w == 0.0)
        //    return;

        //// XPBD
        //let alpha = compliance / this.dt / this.dt;
        //let lambda = -C / (w + alpha);
        //normal.multiplyScalar(-lambda);

        //this._applyCorrection(normal, pos);
        //if (otherBody != undefined)
        //{
        //    normal.multiplyScalar(-1.0);
        //    otherBody._applyCorrection(normal, otherPos);
        //}
        //return lambda / this.dt / this.dt;
    }

    //
    // End simulation functions
    //

    private void Dispose() 
    {
        //for (let i = 0; i < this.meshes.length; i++)
        //{
        //    if (this.meshes[i].geometry) this.meshes[i].geometry.dispose();
        //    if (this.meshes[i].material) this.meshes[i].material.dispose();
        //    gThreeScene.remove(this.meshes[i]);
        //}
        //if (this.textRenderer)
        //{
        //    this.textRenderer.dispose();
        //}
    }
    
}
