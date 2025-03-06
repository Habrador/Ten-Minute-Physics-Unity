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

    //A rigidbody has the following properties:
    //Position pos
    private Vector3 pos;
    //Velocity v
    private Vector3 vel;
    //Rotation q
    private Quaternion rot;
    //Inverse rot q^-1
    private Quaternion invRot;
    //Angular velocity omega
    //omega.magnitude -> speed of rotation in angle/seconds
    private Vector3 omega;
    //Mass (resistance to force)
    //F = m * a -> a = 1/m * F (which is why we use inverse mass)
    private float invMass;
    //Moment of inertia I (resistance to torque)
    //Is a 3x3 matrix
    //if the body is aligned witht he x,y,z axis we can treat it as a 3 dimensional vector
    //and do calculations in local space by transforming everything to local space
    //tau = I * alpha -> alpha = 1/I * tau (which is why we use inverse inertia)
    //tau is torque
    //alpha is angular acceleration
    private Vector3 invInertia;

    //For simulation
    private Vector3 prevPos;
    private Quaternion prevRot;
    //Multiply velocity with this damping 
    public float damping;

    //The gameobject that represents this rigidbody
    private readonly GameObject rbObj;


    //Useful equations
    //The velocity at point a on the rb
    //r is the distance between center and point a
    //omega is angular velocity
    //v_a = omega x r (cross product)
    //If the rb moves, then
    //v_a = v + omega x r


    //Removed parameter scene which is a gThreeScene whish is like the sim environment
    //If fontSize = 0 we wont display any text
    public MyRigidBody(Types type, Vector3 size, float density, Vector3 pos, Vector3 angles, float fontSize = 0f)
    {
        this.type = type;
        //this.size = new Vector3(size.x, size.y, size.z);
        this.damping = 0f;

        this.pos = new Vector3(pos.x, pos.y, pos.z);
        this.rot = new Quaternion();
        this.rot.eulerAngles = angles;
        this.vel = Vector3.zero;
        this.omega = Vector3.zero;

        this.prevPos = this.pos;
        this.prevRot = this.rot;
        //this.dRot = new Quaternion();
        //Inverts this quaternion - calculates the "conjugate" according to documentation, which is the same as inverse
        this.invRot = Quaternion.Inverse(this.rot);

        this.invMass = 0f;
        this.invInertia = Vector3.zero;

        
        if (type == Types.Box)
        {
            //Create the obj we can see
            GameObject newBoxObj = GameObject.CreatePrimitive(PrimitiveType.Cube);

            newBoxObj.transform.localScale = size;

            this.rbObj = newBoxObj;

            //Init box data
            if (density > 0f)
            {
                //mass = volume * density
                float mass = density * size.x * size.y * size.z;

                this.invMass = 1f / mass;
                
                //Box inertia
                float Ix = 1f / 12f * mass * (size.y * size.y + size.z * size.z);
                float Iy = 1f / 12f * mass * (size.x * size.x + size.z * size.z);
                float Iz = 1f / 12f * mass * (size.x * size.x + size.y * size.y);
                
                this.invInertia = new Vector3(1f / Ix, 1f / Iy, 1f / Iz);
            }
        }
        else if (type == Types.Sphere) 
        {
            //Create the obj we can see
            GameObject newSphereObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);

            newSphereObj.transform.localScale = size.x * Vector3.one;

            this.rbObj = newSphereObj;

            //Init Sphere data
            if (density > 0f)
            {
                //mass = volume * density
                float mass = 4f / 3f * Mathf.PI * size.x * size.x * size.x * density;
                
                this.invMass = 1f / mass;
                
                //Sphere inertia
                float I = 2f / 5f * mass * size.x * size.x;
                
                this.invInertia = new Vector3(1f / I, 1f / I, 1f / I);
            }
        }


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
    public Vector3 LocalToWorld(Vector3 localPos)
    {
        Vector3 worldPos = this.pos + this.rot * localPos;

        return worldPos;
    }

    //a = q^-1 * (a' - x)
    public Vector3 WorldToLocal(Vector3 worldPos)
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

        //Cache the pos as we need it later
        this.prevPos = this.pos;

        //vel = vel + dt * a
        //pos = pos + dt * vel

        this.vel += gravity * dt;

        this.pos += this.vel * dt;


        //Angular motion

        //Cache the rot as we need it later
        this.prevRot = this.rot;

        //omega = omega + dt * I^-1 * tau_ext, where tau_ext is external torque
        //q = q + 0.5 * dt * v[omega_x, omega_y, omega_z, 0] * q
        //(sometimes you see h instead of dt)

        //omega = omega + 0 because we have no external torque

        //v[omega_x, omega_y, omega_z, 0]
        Quaternion dRot = new Quaternion(this.omega.x, this.omega.y, this.omega.z, 0f);

        //v[omega_x, omega_y, omega_z, 0] * q
        dRot *= this.rot;

        //What happened to v? Maybe it should be v[omega_x, omega_y, omega_z, 0] meaning omegas are the v???
        this.rot.x += 0.5f * dt * dRot.x;
        this.rot.y += 0.5f * dt * dRot.y;
        this.rot.z += 0.5f * dt * dRot.z;
        this.rot.w += 0.5f * dt * dRot.w;

        this.rot.Normalize();

        //Update the inverse rot with the new values
        this.invRot = Quaternion.Inverse(this.rot);
    }



    //Fix velocity and angular velocity
    //Add damping
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
        Quaternion dRot = this.rot * Quaternion.Inverse(this.prevRot);

        Vector3 delta_q = new Vector3(dRot.x, dRot.y, dRot.z);

        this.omega = delta_q * 2f / dt;

        if (dRot.w < 0f)
        {
            //Negate
            this.omega *= -1f;
        }

        //Damping
        this.vel *= Mathf.Max(1f - this.damping * dt, 0f);
    }



    //To enforce distance constraint
    //normal - direction between constraints
    //pos - where the constraint attaches to this body
    //Why can pos sometimes be undefined???
    //Generalized inverse masses w_i = m_i^-1 + (r_i x n)^T * I_i^-1 * (r_i x n)
    //m - mass
    //n - correction direction
    private float GetInverseMass(Vector3 normal, Vector3 pos, bool isPosUnedfined = false)
    {
        if (this.invMass == 0f)
        {
            return 0f;
        }

        Vector3 rn = normal;

        //Angular case
        if (isPosUnedfined)
        {
            rn = this.invRot * rn;
        }
        //Linear case
        else
        {
            rn = pos - this.pos;
            rn = Vector3.Cross(rn, normal);
            rn = this.invRot * rn;
        }

        float w =
            rn.x * rn.x * this.invInertia.x +
            rn.y * rn.y * this.invInertia.y +
            rn.z * rn.z * this.invInertia.z;

        if (isPosUnedfined)
        {
            w += this.invMass;
        }

        return w;
    }



    //Update pos and rot to enforce distance constraints
    //Corr is lambda*normal and is multiplied by -1 depending which rb we move
    public void UpdatePosAndRot(Vector3 corr, Vector3 pos)
    {
        if (this.invMass == 0f)
        {
            return;
        }

        //Linear correction
        //+- Because we move in different directions because we have two rb
        //x_i = x_i +- w_i * lambda * n
        this.pos += this.invMass * corr;

        //Angular correction
        //q_i = q_i + 0.5 * lambda * (I_i^-1 * (r_i x n), 0) * q_i

        //dOmega <- lambda * (I_i^-1 * (r_i x n), 0)
        
        //r_i
        Vector3 r = pos - this.pos;
        
        //r_i x n
        Vector3 dOmega = Vector3.Cross(r, corr);
        
        //
        dOmega = this.invRot * dOmega;

        //Scales dOmega by the inverse inertia
        //invInertia is a tensor - not a vector, so component-wise multiplication
        dOmega.x *= this.invInertia.x;
        dOmega.y *= this.invInertia.y;
        dOmega.z *= this.invInertia.z;

        //Tansforms dOmega back into the original coordinate frame after the inverse rotation was applied earlier
        dOmega = this.rot * dOmega;

        //dRot <- dOmega * q_i
        //lambda * (I_i^-1 * (r_i x n), 0)
        Quaternion dRot = new Quaternion(
            dOmega.x,
            dOmega.y,
            dOmega.z,
            0f
        );

       dRot *= this.rot;

        //q_i = q_i + 0.5 * dRot
        this.rot.x += 0.5f * dRot.x;
        this.rot.y += 0.5f * dRot.y;
        this.rot.z += 0.5f * dRot.z;
        this.rot.w += 0.5f * dRot.w;
        
        this.rot.Normalize();

        //Cache the inverse
        this.invRot = Quaternion.Inverse(this.rot);
    }



    //Fix the distance constraint between this body and the other body
    //compliance - inverse of physical stiffness (alpha in equations)
    //corr - vector between this body and another body
    //pos - where the constraint attaches to this body in world space
    //otherBody - the connected rb
    //otherPos - where the constraint attaches to the other rb in world space
    //Returns the force on this constraint
    public float ApplyCorrection(float compliance, Vector3 corr, Vector3 pos, MyRigidBody otherBody, Vector3 otherPos, float dt)
    {
        //Constraint distance C = l - l_0
        //l_0 - wanted length
        //l - current length
        //Constraint direction n = (a2 - 1)/|a2 - a1|
        //a - point on body where constraint attaches (not center!!!)
        //Generalized inverse masses w_i = m_i^-1 + (r_i x n)^T * I_i^-1 * (r_i x n)
        //r - vector from center of mass to attachment point (stored in local frame of the body)
        //Lagrange multiplyer
        //lambda = -C * (w_1 + w_2 + alpha/dt^2)^-1
        //Update pos and rot
        //x_i = x_i +- w_i * lambda * n
        //q_i = q_i + 0.5 * lambda * (I_i^-1 * (r_i x n), 0) * q_i

        if (corr.sqrMagnitude == 0f)
        {
            return 0f;
        }

        float C = corr.magnitude;

        Vector3 normal = corr.normalized;

        float w_tot = this.GetInverseMass(normal, pos);

        if (otherBody != null)
        {
            w_tot += otherBody.GetInverseMass(normal, otherPos);
        }

        if (w_tot == 0f)
        {
            return 0f;
        }

        //XPBD

        //Lagrange multiplyer
        //lambda = -C * (w_1 + w_2 + alpha/dt^2)^-1
        float lambda = -C / (w_tot + (compliance / (dt * dt)));

        //Update pos and rot
        //x_i = x_i +- w_i * lambda * n
        //q_i = q_i + 0.5 * lambda * (I_i^-1 * (r_i x n), 0) * q_i
        Vector3 lambda_normal = normal * -lambda;

        UpdatePosAndRot(lambda_normal, pos);

        if (otherBody != null)
        {
            lambda_normal *= -1f;
            otherBody.UpdatePosAndRot(lambda_normal, otherPos);
        }

        //Constraint force
        //F = (lambda * n) / dt^2
        //n is normalized
        float constraintForce = lambda / (dt * dt);

        return constraintForce;
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
