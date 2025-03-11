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
    //Inverse rot q^-1 = q* / |q|^2
    private Quaternion invRot;
    //Angular velocity omega
    //omega.magnitude -> speed of rotation
    private Vector3 omega;
    //Inverse mass m^-1
    private float invMass;
    //Moment of inertia I (resistance to torque)
    //Is a 3x3 matrix
    //if the body is aligned witht he x,y,z axis we can treat it as a 3 dimensional vector
    //I = |I_xx 0    0   | -> I = (I_xx, I_yy, I_zz)
    //    |0    I_yy 0   |
    //    |0    0    I_zz|
    //and do calculations in local space by transforming everything needed to local space
    //Inverse moment of inertia I^-1
    private Vector3 invInertia;

    //For simulation
    private Vector3 prevPos;
    private Quaternion prevRot;
    //Multiply velocity with this damping 
    public float damping;

    //The gameobject that represents this rigidbody
    private readonly GameObject rbObj;
    //Faster to cache it
    private readonly Transform rbTrans;



    //Removed scene as parameter - we add the rb to the simulator when we create it
    //When we want to delete the physical object we call Dispose()
    //If fontSize = 0 we wont display any text
    //size - radius if we have a sphere, length of side if we have a box
    public MyRigidBody(Types type, Vector3 size, float density, Vector3 pos, Vector3 angles, float fontSize = 0f)
    {
        this.type = type;

        this.damping = 0f;

        this.pos = pos;

        this.rot = new Quaternion
        {
            eulerAngles = angles
        };

        //Inverts this quaternion
        this.invRot = Quaternion.Inverse(this.rot);

        this.vel = Vector3.zero;
        this.omega = Vector3.zero;

        this.prevPos = this.pos;
        this.prevRot = this.rot;
        
        //Create the object we can see
        //Calculate inverse mass and inverse moment of inertia
        if (type == Types.Box)
        {
            //Create the obj we can see
            GameObject newBoxObj = GameObject.CreatePrimitive(PrimitiveType.Cube);

            newBoxObj.transform.localScale = size;

            this.rbObj = newBoxObj;
            this.rbTrans = newBoxObj.transform;

            //Init box data
            if (density > 0f)
            {
                //mass = volume * density
                float mass = density * size.x * size.y * size.z;

                this.invMass = 1f / mass;

                //I (solid rectangular cuboid)
                //https://en.wikipedia.org/wiki/List_of_moments_of_inertia
                //h,w,d = height,width,depth
                //I_h = 1/12 * m * (w^2 + d^2)
                //I_w = 1/12 * m * (h^2 + d^2)
                //I_d = 1/12 * m * (h^2 + w^2)
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
            this.rbTrans = newSphereObj.transform;

            //Init Sphere data
            if (density > 0f)
            {
                float r = size.x;
            
                //mass = volume * density = 4/3 * pi * r^3 * density
                float mass = 4f / 3f * Mathf.PI * r * r * r * density;
                
                this.invMass = 1f / mass;

                //I (solid sphere)
                //https://en.wikipedia.org/wiki/List_of_moments_of_inertia
                //I = 2/5 * m * r^2
                float I = 2f / 5f * mass * r * r;
                
                this.invInertia = new Vector3(1f / I, 1f / I, 1f / I);
            }
        }

        //Add collider to objects for raycasting

        //Create text renderer for mass display
        //this.textRenderer = null;
        
        //if (fontSize > 0.0)
        //{
        //    this.textRenderer = new TextRenderer(scene, fontSize);
        //    this.textRenderer.loadFont().then(() => {
        //    this.textRenderer.createText(`${ mass.toFixed(1)} kg`, this.meshes[0].position);});
        //}
                    
        UpdateMesh();
    }



    //Move mesh to the simulate position and rotation
    public void UpdateMesh()
    {
        this.rbTrans.SetPositionAndRotation(this.pos, this.rot);

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

    //Update position, velocity, angular velocity, and rotation by using semi-implicit Euler
    public void Integrate(float dt, Vector3 gravity)
    {
        if (this.invMass == 0f)
        {
            return;
        }


        //Linear motion

        //Cache the pos as we need it later
        this.prevPos = this.pos;

        //Calculate the new positon and velocity after this time step
        //vel = vel + dt * a
        //pos = pos + dt * vel

        this.vel += gravity * dt;

        this.pos += this.vel * dt;


        //Angular motion

        //Cache the rot as we need it later
        this.prevRot = this.rot;

        //From the tutorial:
        //Update angular velocity:
        //omega = omega + dt * I^-1 * tau_ext, where tau_ext is external torque
        //Update rotation:
        //q = q + 0.5 * dt * v[omega_x, omega_y, omega_z, 0] * q

        //From the paper "Detailed rb simulation with xpbd":
        //Update angular velocity:
        //omega = omega + dt * I^-1 * (tau_ext - (omega x (I * omega))
        //Update rotation:
        //q = q + 0.5 * dt * [omega_x, omega_y, omega_z, 0] * q
        //In the tutorial theres a v before [omega_x, omega_y, omega_z, 0]
        //but I think it was a missprint because in the paper it doesnt exist
        //(sometimes you see h instead of dt)
        //Normalize
        //q = q / |q|

        //Derivation of the above equations:
        //tau = I * aa -> aa = I^-1 * tau (which is why we use inverse inertia)
        //where:
        // tau - torque (external), tau = r x F where r is vector from center of mass to where you apply the force
        // aa - angular acceleration
        // I - moment of inertia (resistance to torque)
        //Which is similar to F = m * a -> a = m^-1 * F
        //To update angular velocity, we do something similar as when we update velocity vel = vel + a * dt;
        //omega = omega + aa * dt = omega + I^-1 * tau * dt
        //To update rotation we do something similar as when we update position pos = pos + vel * dt
        //q = q + omega * dt
        //This works fine in 2d where the rb can only rotate around the z axis (up direction):
        //omega.z = omega.z + tau.z * I.z^-1 * dt
        //rot.z = rot.z + omega.z * dt
        //BUT in 3d it gets more complicated
        //
        //Angular velocity
        //The angular equation of motion:
        //Takes into account both external influences and the body's own rotational behavior.
        //Sum(M_cg) = dH_cg / dt = I * (domega/dt) + (omega x (I * omega))
        //-> domega/dt = I^-1 * [Sum(M) - (omega x (I * omega))]
        //Which is the equation from the paper:
        //omega = omega + dt * I^-1 * (tau_ext - (omega x (I * omega))
        //omega x (I * omega) represents the effect of the body's current rotation on its angular momentum, contributing to the overall torque.
        //I^-1 Scales the effect of the torques based on the body's resistance to changes in its rotational motion.
        //
        //Rotation
        //How a quaternion, which represents the orientation of an object, changes over time due to the angular velocity:
        //dq/dt = 0.5 * omega * q -> q_next = q + 0.5 * omega * q * dt
        //The factor of 0.5 arises from the mathematical properties of quaternions (they use half-angles)
        //and ensures that the rate of change of the quaternion correctly represents the physical rotation
        //(If omega is in body coordinates, you use dq/dt = 0.5 * q * omega)

        //omega = omega + 0 because we have no external torque
        //The tutorial is not taking into account the body's own rotational behavior (omega x (I * omega)???

        //Put the angular velocity in quaternion form so we can multiply it by a quaternion
        //This is known as a pure quaternion (a quaternion with a real part of zero: w = 0)
        //[omega_x, omega_y, omega_z, 0]
        //Some sources say it should be [0, omega_x, omega_y, omega_z],
        //but it depends on how the quaternion is implemented 
        Quaternion dRot = new Quaternion(this.omega.x, this.omega.y, this.omega.z, 0f);

        //[omega_x, omega_y, omega_z, 0] * q
        dRot *= this.rot;

        //q = q + 0.5 * dt * dRot
        this.rot.x += 0.5f * dt * dRot.x;
        this.rot.y += 0.5f * dt * dRot.y;
        this.rot.z += 0.5f * dt * dRot.z;
        this.rot.w += 0.5f * dt * dRot.w;

        //The orientation quaternion must be a unit quaternion
        //q = q / |q|
        this.rot.Normalize();

        //Update the inverse rot with the new values
        this.invRot = Quaternion.Inverse(this.rot);
    }



    //Fix velocity and angular velocity
    //The velocities calculated in Inegrate() are not the velocities we want
    //because they make the simulation unstable because we havent take into
    //consideration the constraints which changes the position
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
        //Compute the relative rotation between the two quaternions
        //The transformation that transforms the body from the frame before the solve into the frame after the solve
        //This operation is often used to determine the difference or change in orientation between two rotations
        //delta_q = q * q_prev^-1
        //Quaternions represent rotations using half-angles, so to convert back to full angles, you multiply by 2
        //Similar to vel = (pos - pos_prev) / dt
        //omega = 2 * (delta_q_x, delta_q_y, delta_q_z) / dt
        //If delta_q.w is negative, the resulting angular velocity might point in the opposite direction of the intended rotation
        //If it is negative, negating the entire angular velocity vector omega corrects the direction to be consistent with the intended rotation
        //omega = (delta_q.w >= 0) ? omega : -omega

        //delta_q = q * q_prev^-1
        Quaternion delta_q = this.rot * Quaternion.Inverse(this.prevRot);

        //Turn the transformation into an angular velocity
        //omega = 2 * (delta_q_x, delta_q_y, delta_q_z) / dt
        this.omega = new Vector3(delta_q.x, delta_q.y, delta_q.z) * 2f / dt;

        //omega = (delta_q.w >= 0) ? omega : -omega
        if (delta_q.w < 0f)
        {
            //Negate
            this.omega *= -1f;
        }

        //Damping (should maybe be in its own method)
        this.vel *= Mathf.Max(1f - this.damping * dt, 0f);
    }



    //Get the general inverse mass
    //normal - direction between constraints
    //pos - where the constraint attaches to this body
    //Why can pos sometimes be undefined???
    //Generalized inverse masses w_i = m_i^-1 + (r_i x n)^T * I_i^-1 * (r_i x n)
    //m - mass
    //n - correction direction
    //r - vector from center of mass to contact point
    //Derivation at the end of the paper "Detailed rb simulation with xpbd"
    private float GetInverseMass2(Vector3 normal, Vector3 pos, bool isPosUndefined = false)
    {
        if (this.invMass == 0f)
        {
            return 0f;
        }

        Vector3 rn = normal;

        //Angular case
        if (isPosUndefined)
        {
            rn = this.invRot * rn;
        }
        //Linear case (which is what exists in the video but in the code on github theres also this if/else)
        else
        {
            rn = pos - this.pos;
            rn = Vector3.Cross(rn, normal);
            rn = this.invRot * rn; //To be able to use the inertia vector3 instead of the tensor?
        }

        float w =
            rn.x * rn.x * this.invInertia.x +
            rn.y * rn.y * this.invInertia.y +
            rn.z * rn.z * this.invInertia.z;

        if (isPosUndefined)
        {
            w += this.invMass;
        }

        return w;
    }

    //As in the code in the video (not on github)
    private float GetInverseMass(Vector3 normal, Vector3 pos)
    {
        if (this.invMass == 0f)
        {
            return 0f;
        }

        //w_i = m_i^-1 + (r_i x n)^T * I_i^-1 * (r_i x n)

        //r_i
        Vector3 r = pos - this.pos;
        
        //(r_i x n)
        Vector3 rn = Vector3.Cross(r, normal);

        //To be able to use the inertia vector3 which is only useful in local space
        rn = this.invRot * rn;

        //(r_i x n)^T * I_i^-1 * (r_i x n)
        //rn^T * I_i^-1 * rn
        //3x3 * 3x1 = 3x1
        //|invI.x 0      0     | * |rn.x| = |rn.x * invI.x|
        //|0      invI.y 0     |   |rn.y|   |rn.y * invI.y|
        //|0      0      invI.z|   |rn.z|   |rn.z * invI.z|
        //1x3 * 3x1 = 1x1
        //|rn.x rn.y rn.z| * |rn.x * invI.x|
        //                   |rn.y * invI.y|
        //                   |rn.z * invI.z|
        float w =
            rn.x * rn.x * this.invInertia.x +
            rn.y * rn.y * this.invInertia.y +
            rn.z * rn.z * this.invInertia.z;

        //m_i^-1 + (r_i x n)^T * I_i^-1 * (r_i x n)
        w += this.invMass;

        return w;
    }



    //Update pos and rot to enforce distance constraints
    //lambda_normal is lambda*normal and is the positional impulse (also knownn as p)
    public void UpdatePosAndRot(Vector3 lambda_normal, Vector3 pos)
    {
        if (this.invMass == 0f)
        {
            return;
        }

        //Linear correction
        //+- Because we move in different directions because we have two rb
        //x_i = x_i +- w_i * lambda * n
        //lambda_normal already has this +- in it
        this.pos += this.invMass * lambda_normal;

        //Angular correction
        //q_i = q_i + 0.5 * lambda * [I_i^-1 * (r_i x n), 0] * q_i <- From tutorial
        //q_i = q_i + 0.5 * [I_i^-1 * (r_i x lambda_normal), 0] * q_i <- From paper

        //dOmega <- lambda * [I_i^-1 * (r_i x n), 0]

        //r_i
        Vector3 r = pos - this.pos;
        
        //r_i x n
        Vector3 dOmega = Vector3.Cross(r, lambda_normal);
        
        //Transform dOmega to local space so we can use the simplifed moment of inertia
        dOmega = this.invRot * dOmega;

        //Scales dOmega by the inverse inertia
        //invInertia is a tensor - not a vector, so component-wise multiplication
        dOmega.x *= this.invInertia.x;
        dOmega.y *= this.invInertia.y;
        dOmega.z *= this.invInertia.z;

        //Tansforms dOmega back into the original coordinate frame after the inverse rotation was applied earlier
        dOmega = this.rot * dOmega;

        //dRot <- dOmega * q_i
        //lambda * [I_i^-1 * (r_i x n), 0]
        Quaternion dRot = new Quaternion(
            dOmega.x,
            dOmega.y,
            dOmega.z,
            0f
        );

        //lambda * [I_i^-1 * (r_i x n), 0] * q_i
        dRot *= this.rot;

        //q_i = q_i + 0.5 * dRot
        this.rot.x += 0.5f * dRot.x;
        this.rot.y += 0.5f * dRot.y;
        this.rot.z += 0.5f * dRot.z;
        this.rot.w += 0.5f * dRot.w;
        
        //Always normnalize when we update rotation
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
        //When you pull the bodies closer you also make the bodies rotate
        //The rotations are distributed according to I^-1
    
        //Constraint distance:
        //C = l - l_0
        //l_0 - wanted length
        //l - current length
        //Constraint direction:
        //n = (a2 - 1)/|a2 - a1|
        //a - point on body where constraint attaches (not center of mass)
        //Generalized inverse masses:
        //w_i = m_i^-1 + (r_i x n)^T * I_i^-1 * (r_i x n)
        //r - vector from center of mass to attachment point (stored in local frame of the body)
        //Lagrange multiplyer:
        //lambda = -C * (w_1 + w_2 + alpha/dt^2)^-1
        //Update pos:
        //x_i = x_i +- w_i * lambda * n
        //Update rot:
        //q_i = q_i + 0.5 * lambda * [I_i^-1 * (r_i x n), 0] * q_i
        //Constraint force:
        //F = (lambda * n) / dt^2

        //In the paper you see
        //delta_lambda = (-c - (alpha_tilde * lambda)) / (w_1 + w_2 + alpha_tilde)
        //alpha_tilde = alpha / dt^2
        //lambda = lambda + delta_lambda
        //and instead of lambda * normal they use delta_lambda * normal
        //BUT according to the YT video "09 Getting ready to simulate the world with XPBD"
        //we dont need to keep track of the lagrange multiplier per constraint
        //if we iterate over the constraints just once
        //Notice that iteration and substeps are not the same
        //Some are iterating over the constraints multiple times each substep
        //But we are doing it just once because it generates a better result

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
        //q_i = q_i + 0.5 * lambda * [I_i^-1 * (r_i x n), 0] * q_i
        Vector3 lambda_normal = normal * -lambda;

        UpdatePosAndRot(lambda_normal, pos);

        if (otherBody != null)
        {
            lambda_normal *= -1f;
            otherBody.UpdatePosAndRot(lambda_normal, otherPos);
        }

        //Constraint force
        //F = (lambda * n) / dt^2
        //n is normalized -> lambda_normal.magnitude = lambda
        float constraintForce = lambda / (dt * dt);

        return constraintForce;
    }



    //
    // End simulation functions
    //

    public void Dispose() 
    {
        GameObject.Destroy(rbObj);    

        //if (this.textRenderer)
        //{
        //    this.textRenderer.dispose();
        //}
    }
    
}
