using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace XPBD
{
    public class MyJoint
    {
        //Joint attachment points
        
        //Pos
        private Vector3 globalPos1;
        private Vector3 globalPos2;
        //Rot
        private Quaternion globalRot1;
        private Quaternion globalRot2;

        //Pos
        private Vector3 localPos1;
        private Vector3 localPos2;
        //Rot
        private Quaternion localRot1;
        private Quaternion localRot2;

        //Connected rbs
        private readonly MyRigidBody body1;
        private readonly MyRigidBody body2;

        private bool disabled;

        //Quaternion globalFrameRot;

        //All settings for the joint type 
        public MyJointSettings settings;

        public MyJointSettings.Types JointType => this.settings.type;

        //Debug objects

        //The tiny axis showing where joints attach
        private VisualFrame visFrame0;
        private VisualFrame visFrame1;
        
        //The red line going between attachment points
        private VisualDistance visDistance;

        //For joint sim calculations
        private Vector3 axis0 = new Vector3(1f, 0f, 0f);
        private Vector3 axis1 = new Vector3(0f, 1f, 0f);

        //Infinite stiffness (compliance is the inverse if stiffness)
        private readonly float hardCompliance = 0f;



        public MyJoint(MyRigidBody body1, MyRigidBody body2, Vector3 globalFramePos) : this(body1, body2, globalFramePos, Quaternion.identity) { }

        public MyJoint(MyRigidBody body1, MyRigidBody body2, Vector3 globalFramePos, Quaternion globalFrameRot)
        {
            this.settings = new();
        
            this.body1 = body1;
            this.body2 = body2;
            this.disabled = false;

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
            if (this.body1 != null)
            {
                //Store the local position relative to body0
                this.localPos1 = globalFramePos - this.body1.pos;
                this.localPos1 = this.body1.invRot * this.localPos1;

                //Store the local rotation relative to body0
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

            if (this.body2 != null)
            {
                //Store the local position relative to body1
                this.localPos2 = globalFramePos - this.body2.pos;
                this.localPos2 = this.body2.invRot * this.localPos2;

                //Store the local rotation relative to body1
                this.localRot2 = globalFrameRot;
                
                //Factor out the body's rotation
                if (isGlobalFrameRot)
                {
                    this.localRot2 = this.body2.invRot * this.localRot2;
                }
            }
            else
            {
                this.localPos2 = globalFramePos;
                this.localRot2 = globalFrameRot;
            }
        }



        //Calculate the actual world positions for joint attachment points
        private void UpdateGlobalFrames()
        {
            if (this.body1 != null)
            {
                this.globalPos1 = this.body1.pos + this.body1.rot * this.localPos1;
                this.globalRot1 = this.body1.rot * this.localRot1;
            }

            if (this.body2 != null)
            {
                this.globalPos2 = this.body2.pos + this.body2.rot * this.localPos2;
                this.globalRot2 = this.body2.rot * this.localRot2;
            }
            else
            {
                this.globalPos2 = this.localPos2;
                this.globalRot2 = this.localRot2;
            }
        }



        //Show/hide debug objects
        public void SetVisible(bool visible)
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
        //
        //Cylinders from left to right in the demo scene:
        // - Cylinder joint where we control the offset
        // - Servo joint where we control the angle
        // - Motor joint where we can control motor speed
        // - Hinge joint with angle limits
        // - Hinge joint with angle limits but damped
        // - Ball and socket (spherical) joint with swing and twist limits
        // - Prismatic joint with target offset and stiffness
        // - Prismatic joint with target offset and stiffness but damped
        public void Solve(float dt)
        {
            if (this.disabled || this.settings.type == MyJointSettings.Types.None)
            {
                return;
            }


            if (this.JointType == MyJointSettings.Types.Hinge)
            {
                HingeJoint();
            }
            else if (this.JointType == MyJointSettings.Types.Servo)
            {
                ServoJoint();
            }
            else if (this.JointType == MyJointSettings.Types.Motor)
            {
                MotorJoint(dt);
            }
            else if (this.JointType == MyJointSettings.Types.Ball)
            {
                BallJoint();
            }
            else if (this.JointType == MyJointSettings.Types.Prismatic)
            {
                PrismaticJoint();
            }
            else if (this.JointType == MyJointSettings.Types.Cylinder)
            {
                CylinderJoint();
            }
            else if (this.JointType == MyJointSettings.Types.Fixed)
            {
                FixedJoint();
            }
        }



        //Called from Update()
        public void UpdateMesh()
        {
            UpdateVisuals();

            //The meshes connected to the rb are not updated here!
        }



        //
        // Building blocks used to simulate all joints
        //

        //Attach Bodies
        //Attach 2 rbs at points p1 and p2, with distance d_rest between them
        //Similar to DistanceConstraint from tutorial 1 on xpbd
        //Attach(p1, p2, d_rest, alpha)
        //{
        //  d = |p2 - p1|
        //  n = (p2 - p1) / d
        //  ApplyLinearCorrection(p1, p2, -(d - d_rest) * n, alpha)
        //}
        private void Attach(Vector3 p1, Vector3 p2, float d_rest, float alpha)
        {
            Vector3 corr = p2 - p1;

            float d = corr.magnitude;

            if (d == 0f)
            {
                corr = new Vector3(0f, 0f, 1f);

                corr = this.globalRot1 * corr;
            }
            else
            {
                corr = Vector3.Normalize(corr);
            }

            corr = -(d - d_rest) * corr * -1f;

            PositionalCorrection.Apply(alpha, corr, this.body1, p1, this.body2, p2);
        }



        //Restrict to axis
        //Restrict p2 to be on an axis with direction a going thorugh p1
        //We can also provide a lower and upper limit for the offset
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
        private void RestrictToAxis(Quaternion a, Vector3 p1, Vector3 p2, float p_min, float p_max, float alpha)
        {
            Vector3 corr = p2 - p1;

            corr = a.Conjugate() * corr;

            //Clamp
            if (corr.x > p_max)
            {
                corr.x -= p_max;
            }
            else if (corr.x < p_min)
            {
                corr.x -= p_min;
            }
            else
            {
                corr.x = 0f;
            }

            corr = a * corr;

            PositionalCorrection.Apply(alpha, corr, this.body1, p1, this.body2, p2);
        }



        //Align two axes
        //Make direction a1 going through p1 and direction a2 going through p2 be in the same direction
        //AlignAxes(a1, a2, alpha)
        //{
        //  ApplyAngularCorrection((-a1) cross a2, alpha) //Only valid for small angles
        //}
        private void AlignAxes(Vector3 a1, Vector3 a2, float alpha)
        {
            //What happened to the minus sign?
            Vector3 corr = Vector3.Cross(a1, a2);

            AngularCorrection.Apply(alpha, corr, this.body1, this.body2);
        }



        //Algorithm 3 in the XPBD paper
        //Limits the angle between the axes a and b of two bodies
        //to be in the interval [minAngle, maxAngle] 
        //using the common roation axis n
        //From YT video:
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
        private void LimitAngle(Vector3 n, Vector3 a, Vector3 b, float minAngle, float maxAngle, float alpha)
        {
            float phi = GetAngle(n, a, b);

            //If angle is within the bounds
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

            AngularCorrection.Apply(alpha, corr, this.body1, this.body2);
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



        //
        // Simulate joints by using the building blocks
        //

        //p1 and p2 are positions in world space where a constraint is attached
        //alpha = 0 means infinite stiffness (hard compliance)

        //Hinge joint
        private void HingeJoint()
        {
            //Attach(p1, p2, d_rest = 0, alpha = 0);
            UpdateGlobalFrames();

            Vector3 p1 = this.globalPos1;
            Vector3 p2 = this.globalPos2;

            Attach(p1, p2, d_rest: 0f, alpha: 0f);


            //AlignAxes(a1, a2, alpha = 0)
            UpdateGlobalFrames();

            Vector3 a1 = this.globalRot1 * axis0;
            Vector3 a2 = this.globalRot2 * axis0;

            AlignAxes(a1, a2, alpha: 0f);


            //LimitAngle(a1, b1, b2, phi_min, phi_max, alpha = 0)
            //Limit angle so it cant spin 360 degrees if needed
            if (this.settings.swingMin > -float.MaxValue || this.settings.swingMax < float.MaxValue)
            {
                UpdateGlobalFrames();

                Vector3 n = this.globalRot1 * axis0;

                Vector3 b1 = this.globalRot1 * axis1;
                Vector3 b2 = this.globalRot2 * axis1;

                LimitAngle(n, b1, b2, this.settings.swingMin, this.settings.swingMax, alpha: 0f);
            }
        }



        //Hinge joint that where we can control the angle with x slider
        private void ServoJoint()
        {
            //Attach(p1, p2, d_rest = 0, alpha = 0);
            UpdateGlobalFrames();

            Vector3 p1 = this.globalPos1;
            Vector3 p2 = this.globalPos2;

            Attach(p1, p2, d_rest: 0f, alpha: 0f);


            //AlignAxes(a1, a2, alpha = 0)
            UpdateGlobalFrames();

            Vector3 a1 = this.globalRot1 * axis0;
            Vector3 a2 = this.globalRot2 * axis0;

            AlignAxes(a1, a2, alpha: 0f);


            //LimitAngle(a1, b1, b2, phi_servo, phi_servo, alpha = 0)
            if (this.settings.hasTargetAngle)
            {
                UpdateGlobalFrames();

                Vector3 n = this.globalRot1 * axis0;

                Vector3 b1 = this.globalRot1 * axis1;
                Vector3 b2 = this.globalRot2 * axis1;

                LimitAngle(n, b1, b2, this.settings.targetAngle, this.settings.targetAngle, this.settings.targetAngleCompliance);
            }

            //Joint limits
            if (this.settings.swingMin > -float.MaxValue || this.settings.swingMax < float.MaxValue)
            {
                UpdateGlobalFrames();

                Vector3 n = this.globalRot1 * axis0;

                Vector3 b1 = this.globalRot1 * axis1;
                Vector3 b2 = this.globalRot2 * axis1;

                LimitAngle(n, b1, b2, this.settings.swingMin, this.settings.swingMax, alpha: 0f);
            }
        }



        //Hinge joint that spins endlessly 
        private void MotorJoint(float dt)
        {
            //Attach(p1, p2, d_rest = 0, alpha = 0);
            UpdateGlobalFrames();

            Vector3 p1 = this.globalPos1;
            Vector3 p2 = this.globalPos2;

            Attach(p1, p2, d_rest: 0f, alpha: 0f);


            //AlignAxes(a1, a2, alpha = 0)
            UpdateGlobalFrames();

            Vector3 a1 = this.globalRot1 * axis0;
            Vector3 a2 = this.globalRot2 * axis0;

            AlignAxes(a1, a2, alpha: 0f);


            //LimitAngle(a1, b1, b2, phi_motor, phi_motor, alpha = 0)
            UpdateGlobalFrames();

            Vector3 n = this.globalRot1 * axis0;

            Vector3 b1 = this.globalRot1 * axis1;
            Vector3 b2 = this.globalRot2 * axis1;

            LimitAngle(n, b1, b2, this.settings.targetAngle, this.settings.targetAngle, this.settings.targetAngleCompliance);


            //phi_motor = phi_motor + dt * omega_motor
            float aAngle = Mathf.Min(Mathf.Max(this.settings.velocity * dt, -1f), 1f);

            this.settings.targetAngle += aAngle;
        }



        //Ball-and-socket joint (or spheroid joint) where a ball-shaped surface of one rounded bone fits into the cup-like depression of another bone
        private void BallJoint()
        {
            //Attach(p1, p2, d_rest = 0, alpha = 0);
            UpdateGlobalFrames();

            Vector3 p1 = this.globalPos1;
            Vector3 p2 = this.globalPos2;

            Attach(p1, p2, d_rest: 0f, alpha: 0f);


            //Swing limit
            //n = (a1 x a2) / |a1 x a2|
            //LimitAngle(n, a1, a2, 0, phi_swing_max, alpha = 0)
            UpdateGlobalFrames();

            Vector3 a1 = this.globalRot1 * axis0;
            Vector3 a2 = this.globalRot2 * axis0;

            Vector3 n = Vector3.Cross(a1, a2).normalized;

            LimitAngle(n, a1, a2, this.settings.swingMin, this.settings.swingMax, 0f);


            //Twist limit
            //n = (a1 + a2) / |a1 + a2|
            //b1' = b1 - n(n dot b1)
            //b2' = b2 - n(n dot b2)
            //LimitAngle(n, b1', b2', phi_twist_max, phi_twist_max, alpha = 0)
            UpdateGlobalFrames();

            a1 = this.globalRot1 * axis0;
            a2 = this.globalRot2 * axis0;

            n = (a1 + a2).normalized;

            Vector3 b1 = this.globalRot1 * axis1;
            Vector3 b2 = this.globalRot2 * axis1;

            Vector3 b1_prim = b1 - n * (Vector3.Dot(n, b1));
            Vector3 b2_prim = b2 - n * (Vector3.Dot(n, b2));

            b1_prim = b1_prim.normalized;
            b2_prim = b2_prim.normalized;

            LimitAngle(n, b1_prim, b2_prim, this.settings.twistMin, this.settings.twistMax, alpha: 0f);
        }



        //Only linear motion
        private void PrismaticJoint()
        {
            //RestrictToAxis(a1, p1, p2, p_min, p_max, alpha)
            UpdateGlobalFrames();

            Quaternion a1 = this.globalRot1;

            Vector3 p1 = this.globalPos1;
            Vector3 p2 = this.globalPos2;

            float p_min = this.settings.distanceMin;
            float p_max = this.settings.distanceMax;

            RestrictToAxis(a1, p1, p2, p_min, p_max, alpha: 0f);


            //AlignAxes(a1, a2, alpha = 0)
            UpdateGlobalFrames();

            Vector3 a11 = this.globalRot1 * axis0;
            Vector3 a22 = this.globalRot2 * axis0;

            AlignAxes(a11, a22, alpha: 0f);


            //LimitAngle(a1, b1, b2, phi_min, phi_max, alpha)
            UpdateGlobalFrames();

            Vector3 n = this.globalRot1 * axis0;

            Vector3 b1 = this.globalRot1 * axis1;
            Vector3 b2 = this.globalRot2 * axis1;

            LimitAngle(n, b1, b2, 0f, 0f, alpha: 0f);
        }



        //Like a prismatic joint but we can control the movement like the servo
        private void CylinderJoint()
        {
            //RestrictToAxis(a1, p1, p2, p_target, p_target, alpha = 0)
            UpdateGlobalFrames();

            Quaternion a1 = this.globalRot1;

            Vector3 p1 = this.globalPos1;
            Vector3 p2 = this.globalPos2;

            float p_target = this.settings.targetDistance;

            RestrictToAxis(a1, p1, p2, p_target, p_target, alpha: 0f);


            //AlignAxes(a1, a2, alpha = 0)
            UpdateGlobalFrames();

            Vector3 a11 = this.globalRot1 * axis0;
            Vector3 a22 = this.globalRot2 * axis0;

            AlignAxes(a11, a22, alpha: 0f);


            //LimitAngle(a1, b1, b2, phi_cylinder, phi_cylinder, alpha)
            UpdateGlobalFrames();

            Vector3 n = this.globalRot1 * axis0;

            Vector3 b1 = this.globalRot1 * axis1;
            Vector3 b2 = this.globalRot2 * axis1;

            LimitAngle(n, b1, b2, 0f, 0f, alpha: 0f);
        }



        //Fixed (untested)
        private void FixedJoint()
        {
            //Align orientations

            UpdateGlobalFrames();

            Quaternion dq = this.globalRot1 * this.globalRot2.Conjugate();

            Vector3 corr = new Vector3(2f * dq.x, 2f * dq.y, 2f * dq.z);

            if (dq.w > 0f)
            {
                corr *= -1f;
            }

            AngularCorrection.Apply(alpha: 0f, corr, this.body1, this.body2);
        }



        //
        // Torque, force, and damping
        //

        //ApplyTorque (not used here but was in the YT video)
        //ApplyAngularVelocityCorrection(tau/dt * a)
        //where a is the main axis
        private void ApplyTorque(float torque, float dt)
        {
            UpdateGlobalFrames();

            //Assuming x-axis is the hinge axis
            Vector3 corr = new(1f, 0f, 0f);

            corr = this.globalRot1 * corr;

            corr *= torque * dt;

            //this.body0.ApplyCorrection(0f, corr, null, this.body1, null, true);

            //In the YT video:
            //ApplyAngularVelocityCorrection(tau / delta_t * a)
        }



        //ApplyForce (not used here but was in the YT video)
        //a is the axis youn want to apply the force along
        //ApplyLinearVelocityCorrection(p1, p2, f/dt * a)
        //where a is the main axis
        private void ApplyForce(float f, float dt)
        {
            
        }



        //Linear damping (called from FixedUpdate())

        //From YT:
        //Damp along direction n
        //c_linear: damping coefficient
        //DampLinear(p1, p2, n, c_linear)
        //{
        //  delta_v = v2 + (p2 - x2) x omega2 - v1 - (p1 - x1) x omega1 //Relative velocity
        //  delta_v_scalar = n * delta_v //Extract vel along axis n
        //  delta_v_scalar = delta_v_scalar * min(delta_t * c_linear, 1) //Damp and make it stable
        //  ApplyLinearVelocityCorrection(p1, p2, -delta_eta * n)
        //}

        public void ApplyLinearDamping(float dt)
        {
            UpdateGlobalFrames();

            Vector3 dVel = this.body1.GetVelocityAt(this.globalPos1);

            if (this.body2 != null)
            {
                dVel -= this.body2.GetVelocityAt(this.globalPos2);
            }

            //Only damp along the distance vector
            Vector3 n = this.globalPos2 - this.globalPos1;

            n.Normalize();
            
            n *= Vector3.Dot(-dVel,n);

            n *= Mathf.Min(this.settings.linearDampingCoeff * dt, 1f);
            
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
            ApplyAngularDamping(dt, this.settings.angularDampingCoeff);
        }

        private void ApplyAngularDamping(float dt, float coeff)
        {
            UpdateGlobalFrames();

            Vector3 dOmega = this.body1.omega;

            if (this.body2 != null)
            {
                //dOmega.sub(this.body1.omega);
                dOmega -= this.body2.omega;
            }


            if (this.settings.type == MyJointSettings.Types.Hinge)
            {
                //Damp along the hinge axis
                Vector3 n = new Vector3(1f, 0f, 0f);

                n = this.globalRot1 * n;

                n *= Vector3.Dot(dOmega, n); 
                
                dOmega = n;
            }
            if (
                this.settings.type == MyJointSettings.Types.Cylinder ||
                this.settings.type == MyJointSettings.Types.Prismatic ||
                this.settings.type == MyJointSettings.Types.Fixed)
            {
                //Maximum damping
                dOmega *= -1f;
            }
            else
            {
                dOmega *= -Mathf.Min(this.settings.angularDampingCoeff * dt, 1f);
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
            this.visFrame0?.UpdateMesh(this.globalPos1, this.globalRot1);
            this.visFrame1?.UpdateMesh(this.globalPos2, this.globalRot2);

            this.visDistance?.UpdateMesh(this.globalPos1, this.globalPos2);
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