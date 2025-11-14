using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEditor.PlayerSettings;

namespace XPBD
{
    public class MyJoint
    {
        //Joint attachment points
        
        //Pos
        private Vector3 globalPos0;
        private Vector3 globalPos1;
        //Rot
        private Quaternion globalRot0;
        private Quaternion globalRot1;

        //Pos
        private Vector3 localPos0;
        private Vector3 localPos1;
        //Rot
        private Quaternion localRot0;
        private Quaternion localRot1;

        //Connected rbs
        private MyRigidBody body0;
        private MyRigidBody body1;

        private bool disabled;

        //Quaternion globalFrameRot;

        //All settings for the joint type 
        public MyJointType jointType;

        public MyJointType.Types Type() => this.jointType.type;

        //Debug objects

        //The tiny axis showing where joints attach
        private VisualFrame visFrame0;
        private VisualFrame visFrame1;
        
        //The red line going between attachment points
        private VisualDistance visDistance;



        public MyJoint(MyRigidBody body0, MyRigidBody body1, Vector3 globalFramePos) : this(body0, body1, globalFramePos, Quaternion.identity) { }

        public MyJoint(MyRigidBody body0, MyRigidBody body1, Vector3 globalFramePos, Quaternion globalFrameRot)
        {
            this.jointType = new();
        
            this.body0 = body0;
            this.body1 = body1;
            this.disabled = false;

            this.globalPos0 = globalFramePos;
            this.globalRot0 = globalFrameRot;
            this.globalPos1 = globalFramePos;
            this.globalRot1 = globalFrameRot;

            //this.localPos0 = globalFramePos;
            //this.localRot0 = globalFrameRot;
            //this.localPos1 = globalFramePos;
            //this.localRot1 = globalFrameRot;

            SetFrames(globalFramePos, globalFrameRot);
        }



        //In video he uses something called Attachment Frames (2d)
        //Which is a position p_rest and perpendicular axis a_rest, b_rest in local space
        private void SetFrames(Vector3 globalFramePos)
        {
            SetFrames(globalFramePos, Quaternion.identity, isGlobalFrameRot: false);
        }

        //We cant set globalFrameRot to null so we have to use isGlobalFrameRot
        private void SetFrames(Vector3 globalFramePos, Quaternion globalFrameRot, bool isGlobalFrameRot = true)
        {
            if (this.body0 != null)
            {
                //Store the local position relative to body0
                this.localPos0 = globalFramePos - this.body0.pos;
                this.localPos0 = this.body0.invRot * this.localPos0;

                //Store the local rotation relative to body0
                this.localRot0 = globalFrameRot;

                //Factor out the body's rotation
                if (isGlobalFrameRot)
                {
                    this.localRot0 = this.body0.invRot * this.localRot0;
                }
            }
            else
            {
                this.localPos0 = globalFramePos;
                this.localRot0 = globalFrameRot;
            }

            if (this.body1 != null)
            {
                //Store the local position relative to body1
                this.localPos1 = globalFramePos - this.body1.pos;
                this.localPos1 = this.body1.invRot * this.localPos1;

                //Store the local rotation relative to body1
                this.localRot1 = globalFrameRot;
                
                //Factor out the body's rotation
                if (isGlobalFrameRot)
                {
                    this.localRot1 = this.body1.invRot * this.localRot1;
                }
            }
            else
            {
                this.localPos1 = globalFramePos;
                this.localRot1 = globalFrameRot;
            }
        }



        //Show/hide debug objects
        private void SetVisible(bool visible)
        {
            this.visFrame0?.SetVisible(visible);
            this.visFrame1?.SetVisible(visible);

            this.visDistance?.SetVisible(visible);
        }



        //Disable the joint
        private void SetDisabled(bool disabled)
        {
            this.disabled = disabled;
            this.SetVisible(!disabled);
        }



        //
        // Move joint
        //

        //Called from FixedUpdate()
        public void Solve(float dt)
        {
            SolvePosition(dt);
            SolveOrientation(dt);
        }


        //In the video he has some building blocks that make up these joints

        //Attach Bodies
        //Attach 2 rbs at points p1 and p2, with distance d_rest between them
        //Similar to DistanceConstraint from tutorial 1 on xpbd
        //Attach(p1, p2, d_rest, alpha)
        //{
        //  d = |p2 - p1|
        //  n = (p2 - p1) / d
        //  ApplyLinearCorrection(p1, p2, -(d - d_rest) * n, alpha)
        //}

        //Restrict to axis
        //Restrict p2 to be on an axis with direction a going thorugh p1
        //RestrictToAxis(a, p1, p2, p_min, p_max, alpha)
        //{
        //  p = p2 - p1
        //  p_italics = a * p (to make it slide along axis we compute component of p along a)
        //
        //  //Clamp
        //  if p_italics < p_min: p_italics = p_min
        //  if p_italics > p_max: p_italics = p_max
        //
        //  p = p - p_italics * a
        //
        //  ApplyLinearCorrection(p1, p2, -p, alpha)
        //}

        //Align two axes
        //Make direction a1 going through p1 and direction a2 going through p2 be in the same direction
        //AlignAxes(a1, a2, alpha)
        //{
        //  ApplyAngularCorrection((-a1) cross a2, alpha) //Only valid for small angles
        //}

        //Limit angle
        //Limit the angle (phi) going between axis a1 and a2 (going from same point)
        //where n is the perpendicular axis (rotation axis) 
        //LimitAngle(n, a1, a2, phi_min, phi_max, alpha)
        //{
        //  phi = angle(n, a1, a2) //Calculate the current angle
        //
        //  if (phi < phi_min or phi > phi max) //If angle is not within bounds
        //  {
        //      phi = clamp(phi, phi_min, phi_max) //Clamp based on the limits
        //      q = roation(n, phi)
        //      a2' = q dot a1 //Rotate a1 by angle phi. This is the dir a2 should have to form the desired angle
        //
        //      ApplyAngularCorrection((-a2) cross a2', alpha) //Rotate a2 to a2'
        //  }
        //}

        //How to simulate different joints

        //Hinge
        //Attach(p1, p2, d_rest = 0, alpha = 0)
        //AlignAxes(a1, a2, alpha = 0)
        //If restriced: LimitAngle(n, a1, a2, phi_min, phi_max, alpha = 0)
        //If servo: LimitAngle(n, a1, a2, phi_servo, phi_servo, alpha = 0)
        //If motor:
        // LimitAngle(n, a1, a2, phi_motor, phi_motor, alpha = 0)
        // phi_motor = phi_motor + dt * omega_motor

        //Ball joint
        //Attach(p1, p2, d_rest = 0, alpha = 0)
        //If swing-limit:
        // n = (a1 x a2) / |a1 x a2|
        // LimitAngle(n, a1, a2, 0f, phi_swing_max, alpha = 0)
        //If twist-limit:
        // n = (a1 x a2) / |a1 x a2| //Average
        // b1' = b1 - n(b * b1)
        // b2' = b2 - n(b * b2)
        // LimitAngle(n, b1', b2', phi_twist_min, phi_twist_max, alpha = 0)

        //Prismatic 
        //RestrictToAxis(a, p1, p2, p_min, p_max, alpha)
        //AlignAxes(a1, a2, alpha = 0)
        //LimitAngle(a1, b1, b2, phi_min, phi_max, alpha)

        //Cyliner
        //RestrictToAxis(a, p1, p2, p_target, p_target, alpha)
        //AlignAxes(a1, a2, alpha = 0)
        //LimitAngle(a1, b1, b2, phi_cylinder, phi_cylinder, alpha)


        private void ApplyTorque(float dt, float torque)
        {
            UpdateGlobalFrames();

            //Assuming x-axis is the hinge axis
            Vector3 corr = new(1f, 0f, 0f);

            corr = this.globalRot0 * corr;

            corr *= torque * dt;

            //this.body0.ApplyCorrection(0f, corr, null, this.body1, null, true);

            //In the YT video:
            //ApplyAngularVelocityCorrection(tau / delta_t * a)
        }


        //Theres also an ApplyFoorce in the YT video
        private void ApplyForce(float f)
        {
            //a is the axis youn want to apply the force along
            //ApplyLinearVelocityCorrection(p1, p2, f/delta_t * a)
        }



        //Position constraint
        private void SolvePosition(float dt)
        {
            if (this.disabled || this.jointType.type == MyJointType.Types.None)
            {
                return;
            }

            //Align
            if (this.Type() == MyJointType.Types.Prismatic || this.Type() == MyJointType.Types.Cylinder)
            {
                float targetDistance = Mathf.Max(this.jointType.distanceMin, Mathf.Min(this.jointType.targetDistance, this.jointType.distanceMax));
                
                float hardCompliance = 0f;
                
                UpdateGlobalFrames();
                
                Vector3 corr = this.globalPos1 - this.globalPos0;

                corr = this.globalRot0.Conjugate() * corr;

                if (this.Type() == MyJointType.Types.Cylinder)
                {
                    corr.x -= this.jointType.targetDistance;
                }
                else if (corr.x > this.jointType.distanceMax)
                {
                    corr.x -= this.jointType.distanceMax;
                }
                else if (corr.x < this.jointType.distanceMin)
                {
                    corr.x -= this.jointType.distanceMin;
                }
                else
                {
                    corr.x = 0f;
                }

                corr = this.globalRot0 * corr;
                
                //this.body0.applyCorrection(hardCompliance, corr, this.globalPos0, this.body1, this.globalPos1);
            }

            //Solve distance
            if (this.Type() != MyJointType.Types.Cylinder && this.jointType.hasTargetDistance)
            {
                UpdateGlobalFrames();
                
                Vector3 corr = this.globalPos1 - this.globalPos0;
                
                float distance = corr.magnitude;
                
                if (distance == 0f)
                {
                    corr = new Vector3(0f, 0f, 1f);

                    corr = this.globalRot0 * corr;
                }
                else
                {
                    corr = Vector3.Normalize(corr);
                }
                    

                corr *= this.jointType.targetDistance - distance;

                corr *= -1f;
                
                //this.body0.applyCorrection(this.distanceCompliance, corr, this.globalPos0, this.body1, this.globalPos1);
            }
        }



        //Calculate the actual world positions for joint attachment points
        private void UpdateGlobalFrames()
        {
            if (this.body0 != null)
            {
                this.globalPos0 = this.body0.pos + this.body0.rot * this.localPos0;
                this.globalRot0 = this.body0.rot * this.localRot0;
            }

            if (this.body1 != null)
            {
                this.globalPos1 = this.body1.pos + this.body1.rot * this.localPos1;
                this.globalRot1 = this.body1.rot * this.localRot1;
            }
            else
            {
                this.globalPos1 = this.localPos1;
                this.globalRot1 = this.localRot1;
            }
        }



        //Algorithm 3 in the XPBD paper
        private float GetAngle(Vector3 n, Vector3 a, Vector3 b)
        {
            float phi = Mathf.Asin(Vector3.Dot(Vector3.Cross(a, b), n));

            if (Vector3.Dot(a, b) < 0f)
            {
                phi = Mathf.PI - phi;
            }
            if (phi > Mathf.PI)
            {
                phi = phi - 2f * Mathf.PI;
            }
            if (phi < -Mathf.PI)
            { 
                phi = phi + 2f * Mathf.PI;
            }

            return phi;
        }



        //Algorithm 3 in the XPBD paper
        //Limits the angle between the axes a and b of two bodies
        //to be in the interval [minAngle, maxAngle] 
        //using the common roation axis n
        private void LimitAngle(Vector3 n, Vector3 a, Vector3 b, float minAngle, float maxAngle, float compliance)
        {
            float phi = GetAngle(n, a, b);

            if (minAngle <= phi && phi <= maxAngle)
            {
                return;
            }
            
            //Clamp(phi, minAngle, maxAngle) 
            phi = Mathf.Max(minAngle, Mathf.Min(phi, maxAngle));

            //n1 = rot(n, phi) * n1
            Vector3 ra = a;

            //ra.applyAxisAngle(n, phi);
            ra = Quaternion.AngleAxis(phi * Mathf.Rad2Deg, n) * ra;

            //delta_q_limit = n1 x n2
            Vector3 corr = Vector3.Cross(ra, b);

            //this.body0.ApplyCorrection(compliance, corr, null, this.body1, null);
            AngularCorrection.Apply(compliance, corr, this.body0, this.body1);
        }



        //Orientation constraint
        private void SolveOrientation(float dt)
        {
            if (this.disabled || this.Type() == MyJointType.Types.None || this.Type() == MyJointType.Types.Distance)
            {
                return;
            }

            if (this.Type() == MyJointType.Types.Motor)
            {
                float aAngle = Mathf.Min(Mathf.Max(this.jointType.velocity * dt, -1f), 1f);

                this.jointType.targetAngle += aAngle;
            }

            float hardCompliance = 0f;

            Vector3 axis0 = new Vector3(1f, 0f, 0f);
            Vector3 axis1 = new Vector3(0f, 1f, 0f);
            
            Vector3 a0 = new Vector3();
            Vector3 a1 = new Vector3();
            Vector3 n = new Vector3();
            Vector3 corr = new Vector3();

            if (
                this.Type() == MyJointType.Types.Hinge || 
                this.Type() == MyJointType.Types.Servo || 
                this.Type() == MyJointType.Types.Motor)
            {
                //Align axes

                UpdateGlobalFrames();

                a0 = axis0;
                a0 = this.globalRot0 * a0;

                a1 = axis0;
                a1 = this.globalRot1 * a0;

                corr = Vector3.Cross(a0, a1);
                
                //this.body0.ApplyCorrection(hardCompliance, corr, null, this.body1, null);

                if (this.jointType.hasTargetAngle)
                {
                    UpdateGlobalFrames();

                    n = axis0;
                    n = this.globalRot0 * n;
                    
                    a0 = axis1;
                    a0 = this.globalRot0 * a0;

                    a1 = axis1;
                    a1 = this.globalRot1 * a1;

                    LimitAngle(n, a0, a1, this.jointType.targetAngle, this.jointType.targetAngle, this.jointType.targetAngleCompliance);
                }

                //Joint limits
                if (this.jointType.swingMin > -float.MaxValue || this.jointType.swingMax < float.MaxValue)
                {
                    UpdateGlobalFrames();

                    n = axis0;
                    n = this.globalRot0 * n;

                    a0 = axis1;
                    a0 = this.globalRot0 * a0;
                    
                    a1 = axis1;
                    a1 = this.globalRot1 * a1;
                    
                    LimitAngle(n, a0, a1, this.jointType.swingMin, this.jointType.swingMax, hardCompliance);
                }
            }
            else if (
                this.Type() == MyJointType.Types.Ball || 
                this.Type() == MyJointType.Types.Prismatic || 
                this.Type() == MyJointType.Types.Cylinder)
            {
                //Swing limit

                UpdateGlobalFrames();

                a0 = axis0;
                a0 = this.globalRot0 * a0;
                
                a1 = axis0;
                a1 = this.globalRot1 * a1;
                
                n = Vector3.Cross(a0, a1);
                n = Vector3.Normalize(n);

                LimitAngle(n, a0, a1, this.jointType.swingMin, this.jointType.swingMax, hardCompliance);


                //Twist limit

                UpdateGlobalFrames();

                a0 = axis0;
                a0 = this.globalRot0 * a0;
                
                a1 = axis0;
                a1 = this.globalRot1 * a1;
                
                n = a0 + a1;
                n = Vector3.Normalize(n);

                a0 = axis1;
                a0 = this.globalRot0 * a0;

                a1 = axis1;
                a1 = this.globalRot1 * a1;

                a0 += n * Vector3.Dot(-n, a0);
                a0 = Vector3.Normalize(a0);

                a1 += n * Vector3.Dot(-n, a1);
                a1 = Vector3.Normalize(a1);

                LimitAngle(n, a0, a1, this.jointType.twistMin, this.jointType.twistMax, hardCompliance);
            }
            else if (this.Type() == MyJointType.Types.Fixed)
            {
                //Align orientations

                UpdateGlobalFrames();

                Quaternion dq = this.globalRot0 * this.globalRot1.Conjugate();
                
                corr = new Vector3(2f * dq.x, 2f * dq.y, 2f * dq.z);

                if (dq.w > 0f)
                {
                    corr *= -1f;
                }

                //this.body0.applyCorrection(hardCompliance, corr, null, this.body1, null);
            }
        }



        //
        // Damping (called from FixedUpdate())
        //

        //Linear damping

        //From YT:
        //Damp along direction n
        //c_linear: damping coefficient
        //DampLinear(p1, p2, n, c_linear)
        //{
        //  delta_v = v2 + (p2 - x2) x omega2 - v1 - (p1 - x1) x omega1 //Relative velocity
        //  delta_v_scalar = n * delta_v //Extract vel along axis n
        //  delta_v_scalar = delta_v_scalar * min(delta_t * c_linear, 1) //Damp
        //  ApplyLinearVelocityCorrection(p1, p2, -delta_eta * n)
        //}

        public void ApplyLinearDamping(float dt)
        {
            UpdateGlobalFrames();

            Vector3 dVel = this.body0.GetVelocityAt(this.globalPos0);

            if (this.body1 != null)
            {
                dVel -= this.body1.GetVelocityAt(this.globalPos1);
            }

            //Only damp along the distance vector
            Vector3 n = this.globalPos1 - this.globalPos0;

            n.Normalize();
            
            n *= Vector3.Dot(-dVel,n);

            n *= Mathf.Min(this.jointType.linearDampingCoeff * dt, 1f);
            
            //this.body0.applyCorrection(0.0, n, this.globalPos0, this.body1, this.globalPos1, true);
        }



        //Angular damping

        //From YT:
        //Damp along rotation axis n
        //c_linear: damping coefficient
        //DampAngular(n, c_angular)
        //{
        //  delta_omega = omega2 - omega1 //Relative angular velocity
        //  delta_omega_Scalar = n * delta_omega //Extract vel along axis n
        //  delta_omega_scalar = delta_omega_scalar * min(delta_t * c_angular, 1) //Damp
        //  ApplyAngularVelocityCorrection(-delta_omega_scalar * n)
        //}

        public void ApplyAngularDamping(float dt)
        {
            ApplyAngularDamping(dt, this.jointType.angularDampingCoeff);
        }

        private void ApplyAngularDamping(float dt, float coeff)
        {
            UpdateGlobalFrames();

            Vector3 dOmega = this.body0.omega;

            if (this.body1 != null)
            {
                //dOmega.sub(this.body1.omega);
                dOmega -= this.body1.omega;
            }


            if (this.jointType.type == MyJointType.Types.Hinge)
            {
                //Damp along the hinge axis
                Vector3 n = new Vector3(1f, 0f, 0f);

                n = this.globalRot0 * n;

                n *= Vector3.Dot(dOmega, n); 
                
                dOmega = n;
            }
            if (
                this.jointType.type == MyJointType.Types.Cylinder ||
                this.jointType.type == MyJointType.Types.Prismatic ||
                this.jointType.type == MyJointType.Types.Fixed)
            {
                //Maximum damping
                dOmega *= -1f;
            }
            else
            {
                dOmega *= -Mathf.Min(this.jointType.angularDampingCoeff * dt, 1f);
            }

            //this.body0.ApplyCorrection(0f, dOmega, null, this.body1, null, true);
        }



        //
        // Visuals showing where the joints connect to the rb and the distance between the connections
        //

        public void AddVisuals(float width = 0.004f, float size = 0.08f)
        {
            if (this.visFrame0 == null)
            {
                this.visFrame0 = new VisualFrame(width, size);
                this.visFrame1 = new VisualFrame(width, size);
            }

            if (this.visDistance == null)
            {
                this.visDistance = new VisualDistance(UnityEngine.Color.red, width);
            }
            
            UpdateVisuals();
        }



        private void UpdateVisuals()
        {
            if (this.disabled)
            {
                return;
            }

            //Calculate the actual world positions for joint attachment points
            UpdateGlobalFrames();

            //If not null update meshes
            this.visFrame0?.UpdateMesh(this.globalPos0, this.globalRot0);
            this.visFrame1?.UpdateMesh(this.globalPos1, this.globalRot1);

            this.visDistance?.UpdateMesh(this.globalPos0, this.globalPos1);
        }



        //
        // End simulation methods
        //

        public void Dispose()
        {
            this.visFrame0?.Dispose();
            this.visFrame1?.Dispose();

            this.visDistance?.Dispose();
        }
    }
}