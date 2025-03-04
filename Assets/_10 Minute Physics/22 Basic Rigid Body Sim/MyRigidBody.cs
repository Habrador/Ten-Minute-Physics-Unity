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

    private Types type;

    //Radius if we have a sphere
    private Vector3 size;
    //Multiply velocity with this damping 
    public float damping;

    //A rigidbody has the following properties:
    //x,y,z - position, pos
    //v - velocity, vel
    //q - orientation, rot
    //omega - angular velocity
    //I - moment of inertia
    //m - mass

    //The center of mass of a rb acts like a particle with
    //Position
    private Vector3 pos;
    //Velocity
    private Vector3 vel;
    //Rotation
    private Quaternion rot;
    //Angular velocity
    //omega.magnitude -> speed of rotation in angle/seconds
    private Vector3 omega;
    //Mass (resistance to force)
    //F = m * a -> a = 1/m * F (which is why we use inverse mass)
    private float invMass;
    //Moment of inertia I (resistance to torque)
    //I ss a 3x3 matrix
    //if the body is aligned witht he x,y,z axis we can treat it as a 3 dimensional vector
    //and do calculations in local space by transforming everything to local space
    //tau = I * alpha -> alpha = 1/I * tau (which is why we use inverse inertia)
    //tau is torque, tau = r x F where r is the distance from center of mass to where the force is applied 
    //alpha is angular acceleration
    private Vector3 invInertia;

    //For simulation
    private Vector3 prevPos;
    private Quaternion prevRot;

    //dRot is some temp data holder so we dont need to create new quaternions all the time???
    private Quaternion dRot;
    private Quaternion invRot;

    //A list because we can have multiple meshes to create one rb
    //In general theres just one mesh so can maybe be simplified
    private GameObject rbObj;
    private float[] vertices;
    private int[] triIds;


    //Useful equations
    //The velocity at point a on the rb
    //r is the distance between center and point a
    //omega is angular velocity
    //v_a = omega x r (cross product)
    //If the rb moves, then
    //v_a = v + omega x r
 

    //Removed parameter scene which is a gThreeScene whish is like the sim environment
    public MyRigidBody(Types type, Vector3 size, float density, Vector3 pos, Vector3 angles, float fontSize = 0f)
    {
        this.type = type;
        this.size = new Vector3(size.x, size.y, size.z);
        this.damping = 0f;

        this.pos = new Vector3(pos.x, pos.y, pos.z);
        this.rot = new Quaternion();
        this.rot.eulerAngles = angles;
        this.vel = Vector3.zero;
        this.omega = Vector3.zero;

        this.prevPos = this.pos;
        this.prevRot = this.rot;
        this.dRot = new Quaternion();
        //Inverts this quaternion - calculates the "conjugate" according to documentation, which is the same as inverse
        this.invRot = Quaternion.Inverse(this.rot);

        this.invMass = 0f;
        this.invInertia = Vector3.zero;

        //this.rbObj = null;
        //this.vertices = null;
        //this.triIds = null;

        float mass = 0f;
        
        if (type == Types.Box)
        {
            //Create the obj we can see
            GameObject newBoxObj = GameObject.CreatePrimitive(PrimitiveType.Cube);

            newBoxObj.transform.localScale = this.size;

            this.rbObj = newBoxObj;

            //Init box data
            if (density > 0f)
            {
                //mass = volume * density
                mass = density * size.x * size.y * size.z;

                this.invMass = 1f / mass;
                
                //Box inertia
                float Ix = 1f / 12f * mass * (size.y * size.y + size.z * size.z);
                float Iy = 1f / 12f * mass * (size.x * size.x + size.z * size.z);
                float Iz = 1f / 12f * mass * (size.x * size.x + size.y * size.y);
                
                this.invInertia = new Vector3(1f / Ix, 1f / Iy, 1f / Iz);
            }

            float ex = 0.5f * size.x;
            float ey = 0.5f * size.y;
            float ez = 0.5f * size.z;

            //8 corners
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
            //Create the obj we can see
            GameObject newSphereObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);

            newSphereObj.transform.localScale = this.size.x * Vector3.one;

            this.rbObj = newSphereObj;

            //Init Sphere data
            if (density > 0f)
            {
                //mass = volume * density
                mass = 4f / 3f * Mathf.PI * size.x * size.x * size.x * density;
                
                this.invMass = 1f / mass;
                
                //Sphere inertia
                float I = 2f / 5f * mass * size.x * size.x;
                
                this.invInertia = new Vector3(1f / I, 1f / I, 1f / I);
            }
        }

        
        //Add all meshes to the scene and setup parameters
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



    //Move mesh to the simulate position and rotation
    public void UpdateMeshes()
    {
        Transform rbTrans = rbObj.transform;

        rbTrans.SetPositionAndRotation(this.pos, this.rot);

        //Maybe recalculate bounds?
        //rbTrans.GetComponent<MeshFilter>().mesh

        //if (this.textRenderer)
        //{
        //    this.textRenderer.updatePosition(this.meshes[0].position);
        //    this.textRenderer.updateRotation(gCamera.quaternion);
        //}
    }



    //Local pos to world pos and world pos to local pos
    //a is a point on the rb in local space
    //a' is the same point but in world space
    //q is the quaternion
    //x is the position of rb (center of mass)

    //a' = x + q * a 
    private Vector3 LocalToWorld(Vector3 localPos)
    {
        Vector3 worldPos = this.pos + this.rot * localPos;

        return worldPos;
    }

    //a = q^-1 * (a' - x)
    private Vector3 WorldToLocal(Vector3 worldPos)
    {
        Vector3 localPos = this.invRot * (worldPos - this.pos);

        return localPos;
    }



    //
    // Begin simulation functions
    //

    //Update vel, pos, omega, rotation by using semi-implicit Euler
    public void Integrate(float dt, Vector3 gravity)
    {
        if (this.invMass == 0f)
        {
            return;
        }

        //Linear motion
        //pos_prev = pos
        //vel = vel + dt * a
        //pos = pos + dt * vel
        this.prevPos = this.pos;

        this.vel += gravity * dt;

        this.pos += this.vel * dt;

        //Angular motion
        //q_prev = q
        //omega = omega + h * I^-1 * tau_ext, where tau_ext is external torque
        //q = q + 0.5 * h * v * [omega_x, omega_y, omega_z, 0] * q
        //where h = dt???

        this.prevRot = this.rot;

        //omega = omega because we have no external torque

        this.dRot = new Quaternion(this.omega.x, this.omega.y, this.omega.z, 0f);


        //[omega_x, omega_y, omega_z, 0] * q
        //this.dRot.multiply(this.rot);
        this.dRot *= this.rot;

        //What happened to v? Maybe it should be v[omega_x, omega_y, omega_z, 0] meaning omegas are the v???
        //this.rot.x += 0.5 * dt * this.dRot.x;
        //this.rot.y += 0.5 * dt * this.dRot.y;
        //this.rot.z += 0.5 * dt * this.dRot.z;
        //this.rot.w += 0.5 * dt * this.dRot.w;

        this.rot.x += 0.5f * dt * this.dRot.x;
        this.rot.y += 0.5f * dt * this.dRot.y;
        this.rot.z += 0.5f * dt * this.dRot.z;
        this.rot.w += 0.5f * dt * this.dRot.w;

        this.rot.Normalize();

        //Update the inverse rot with the new values
        this.invRot = Quaternion.Inverse(this.rot);
    }



    //Fix velocity and angular velocity
    public void UpdateVelocities(float dt)
    {
        if (this.invMass == 0f)
        {
            return;
        }

        //Linear motion
        //vel = (pos - pos_prev) / dt
        this.vel = (this.pos - this.prevPos) / dt;

        //Angular motion
        //delta_q = q * q_prev^-1
        //omega = 2 * (delta_q_x, delta_q_y, delta_q_z) / dt
     
        //delta_q
        this.dRot = this.rot * Quaternion.Inverse(this.prevRot);

        Vector3 delta_q = new Vector3(this.dRot.x, this.dRot.y, this.dRot.z);

        this.omega = delta_q * 2f / dt;

        if (this.dRot.w < 0f)
        {
            //Negate
            this.omega *= -1f;
        }

        //Damping
        this.vel *= Mathf.Max(1f - this.damping * dt, 0f);
    }



    //
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



    //
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



    //
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
        GameObject.Destroy(rbObj);    

        //if (this.textRenderer)
        //{
        //    this.textRenderer.dispose();
        //}
    }
    
}
