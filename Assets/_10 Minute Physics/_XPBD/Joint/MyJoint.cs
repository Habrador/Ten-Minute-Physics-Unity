using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace XPBD
{
    public class MyJoint
    {
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
        public MyJointType type;

        //Debug objects

        //The tiny axis showing where joints attach
        private VisualFrame visFrame0;
        private VisualFrame visFrame1;
        
        //The red line going between attachment points
        private VisualDistance visDistance;



        public MyJoint(MyRigidBody body0, MyRigidBody body1, Vector3 globalFramePos) : this(body0, body1, globalFramePos, Quaternion.identity) { }

        public MyJoint(MyRigidBody body0, MyRigidBody body1, Vector3 globalFramePos, Quaternion globalFrameRot)
        {
            this.type = new();
        
            this.body0 = body0;
            this.body1 = body1;
            this.disabled = false;

            this.globalPos0 = globalFramePos;
            this.globalRot0 = globalFrameRot;
            this.globalPos1 = globalFramePos;
            this.globalRot1 = globalFrameRot;

            this.localPos0 = globalFramePos;
            this.localRot0 = globalFrameRot;
            this.localPos1 = globalFramePos;
            this.localRot1 = globalFrameRot;

            SetFrames(globalFramePos, globalFrameRot);
        }



        //Whats happening here???
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

        private void Solve(float dt)
        {
            SolvePosition(dt);
            SolveOrientation(dt);
        }



        private void ApplyTorque(float dt, float torque)
        {
            UpdateGlobalFrames();

            //Assuming x-axis is the hinge axis
            Vector3 corr = new(1f, 0f, 0f);

            corr = this.globalRot0 * corr;

            corr *= torque * dt;

            //this.body0.ApplyCorrection(0f, corr, null, this.body1, null, true);
        }



        //Position constraint
        private void SolvePosition(float dt)
        {
            //let hardCompliance = 0.0;

            //if (this.disabled || this.type == Joint.TYPES.NONE)
            //    return;

            //let corr = new THREE.Vector3();

            //// align

            //if (this.type == Joint.TYPES.PRISMATIC || this.type == Joint.TYPES.CYLINDER)
            //{
            //    this.targetDistance = Math.max(this.distanceMin, Math.min(this.targetDistance, this.distanceMax));
            //    let hardCompliance = 0.0;
            //    this.updateGlobalFrames();
            //    corr.subVectors(this.globalPos1, this.globalPos0);

            //    corr.applyQuaternion(this.globalRot0.clone().conjugate());
            //    if (this.type == Joint.TYPES.CYLINDER)
            //        corr.x -= this.targetDistance;
            //    else if (corr.x > this.distanceMax)
            //        corr.x -= this.distanceMax;
            //    else if (corr.x < this.distanceMin)
            //        corr.x -= this.distanceMin;
            //    else
            //        corr.x = 0.0;

            //    corr.applyQuaternion(this.globalRot0);
            //    this.body0.applyCorrection(hardCompliance, corr, this.globalPos0, this.body1, this.globalPos1);
            //}

            //// solve distance

            //if (this.type != Joint.TYPES.CYLINDER && this.hasTargetDistance)
            //{
            //    this.updateGlobalFrames();
            //    corr.subVectors(this.globalPos1, this.globalPos0);
            //    let distance = corr.length();
            //    if (distance == 0.0)
            //    {
            //        corr.set(0.0, 0.0, 1.0);
            //        corr.applyQuaternion(this.globalRot0);
            //    }
            //    else
            //        corr.normalize();

            //    corr.multiplyScalar(this.targetDistance - distance);
            //    corr.multiplyScalar(-1.0);
            //    this.body0.applyCorrection(this.distanceCompliance, corr, this.globalPos0, this.body1, this.globalPos1);
            //}
        }



        //Calculate the actual world positions for joint attachment points
        private void UpdateGlobalFrames()
        {
            if (this.body0 != null)
            {
                this.globalPos0 = this.localPos0;
                this.globalPos0 = this.body0.rot * this.globalPos0;
                this.globalPos0 += this.body0.pos;
                
                this.globalRot0 = this.body0.rot * this.localRot0;
            }

            if (this.body1 != null)
            {
                this.globalPos1 = this.localPos1;
                this.globalPos1 = this.body1.rot * this.globalPos1;
                this.globalPos1 += this.body1.pos;

                this.globalRot1 = this.body1.rot * this.localRot0;
            }
            else
            {
                this.globalPos1 = this.localPos1;
                this.globalRot1 = this.localRot1;
            }
        }



        private float GetAngle(Vector3 n, Vector3 a, Vector3 b)
        {
            Vector3 c = Vector3.Cross(a, b);

            float phi = Mathf.Asin(Vector3.Dot(c, n));

            if (Vector3.Dot(a, b) < 0f)
            {
                phi = Mathf.PI - phi;
            }
            if (phi > Mathf.PI)
            {
                phi -= 2f * Mathf.PI;
            }
            if (phi < -Mathf.PI)
            { 
                phi += 2f * Mathf.PI;
            }

            return phi;
        }



        private void LimitAngle(Vector3 n, Vector3 a, Vector3 b, float minAngle, float maxAngle, float compliance)
        {
            float phi = GetAngle(n, a, b);

            if (minAngle <= phi && phi <= maxAngle)
            {
                return;
            }
            
            phi = Mathf.Max(minAngle, Mathf.Min(phi, maxAngle));

            Vector3 ra = a;

            //ra.applyAxisAngle(n, phi);
            ra = Quaternion.AngleAxis(phi * Mathf.Rad2Deg, n) * ra;

            Vector3 corr = Vector3.Cross(ra, b);
            
            //this.body0.ApplyCorrection(compliance, corr, null, this.body1, null);
        }



        //Orientation constraint
        private void SolveOrientation(float dt)
        {
            //if (this.disabled || this.type == Joint.TYPES.NONE || this.type == Joint.TYPES.DISTANCE)
            //{
            //    return;
            //}

            //if (this.type == Joint.TYPES.MOTOR)
            //{
            //    let aAngle = Math.min(Math.max(this.velocity * dt, -1.0), 1.0);
            //    this.targetAngle += aAngle;
            //}

            //let hardCompliance = 0.0;
            //let axis0 = new THREE.Vector3(1.0, 0.0, 0.0);
            //let axis1 = new THREE.Vector3(0.0, 1.0, 0.0);
            //let a0 = new THREE.Vector3();
            //let a1 = new THREE.Vector3();
            //let n = new THREE.Vector3();
            //let corr = new THREE.Vector3();

            //if (this.type == Joint.TYPES.HINGE || this.type == Joint.TYPES.SERVO || this.type == Joint.TYPES.MOTOR)
            //{
            //    // align axes

            //    this.updateGlobalFrames();

            //    a0.copy(axis0);
            //    a0.applyQuaternion(this.globalRot0);
            //    a1.copy(axis0);
            //    a1.applyQuaternion(this.globalRot1);
            //    corr.crossVectors(a0, a1);
            //    this.body0.applyCorrection(hardCompliance, corr, null, this.body1, null);

            //    if (this.hasTargetAngle)
            //    {
            //        this.updateGlobalFrames();
            //        n.copy(axis0);
            //        n.applyQuaternion(this.globalRot0);
            //        a0.copy(axis1);
            //        a0.applyQuaternion(this.globalRot0);
            //        a1.copy(axis1);
            //        a1.applyQuaternion(this.globalRot1);
            //        this.limitAngle(n, a0, a1, this.targetAngle, this.targetAngle, this.targetAngleCompliance);
            //    }

            //    // joint limits

            //    if (this.swingMin > -Number.MAX_VALUE || this.swingMax < Number.MAX_VALUE)
            //    {
            //        this.updateGlobalFrames();

            //        n.copy(axis0);
            //        n.applyQuaternion(this.globalRot0);
            //        a0.copy(axis1);
            //        a0.applyQuaternion(this.globalRot0);
            //        a1.copy(axis1);
            //        a1.applyQuaternion(this.globalRot1);
            //        this.limitAngle(n, a0, a1, this.swingMin, this.swingMax, hardCompliance);
            //    }
            //}
            //else if (this.type == Joint.TYPES.BALL || this.type == Joint.TYPES.PRISMATIC || this.type == Joint.TYPES.CYLINDER)
            //{
            //    // swing limit

            //    this.updateGlobalFrames();

            //    a0.copy(axis0);
            //    a0.applyQuaternion(this.globalRot0);
            //    a1.copy(axis0);
            //    a1.applyQuaternion(this.globalRot1);
            //    n.crossVectors(a0, a1);
            //    n.normalize();
            //    this.limitAngle(n, a0, a1, this.swingMin, this.swingMax, hardCompliance);

            //    // twist limit

            //    this.updateGlobalFrames();

            //    a0.copy(axis0);
            //    a0.applyQuaternion(this.globalRot0);
            //    a1.copy(axis0);
            //    a1.applyQuaternion(this.globalRot1);
            //    n.addVectors(a0, a1);
            //    n.normalize();

            //    a0.copy(axis1);
            //    a0.applyQuaternion(this.globalRot0);
            //    a1.copy(axis1);
            //    a1.applyQuaternion(this.globalRot1);

            //    a0.addScaledVector(n, -n.dot(a0));
            //    a0.normalize();
            //    a1.addScaledVector(n, -n.dot(a1));
            //    a1.normalize();
            //    this.limitAngle(n, a0, a1, this.twistMin, this.twistMax, hardCompliance);
            //}
            //else if (this.type == Joint.TYPES.FIXED)
            //{
            //    // align orientations

            //    this.updateGlobalFrames();

            //    let dq = new THREE.Quaternion();
            //    dq.multiplyQuaternions(this.globalRot0, this.globalRot1.conjugate());
            //    corr.set(2.0 * dq.x, 2.0 * dq.y, 2.0 * dq.z);
            //    if (dq.w > 0.0)
            //        corr.multiplyScalar(-1.0);

            //    this.body0.applyCorrection(hardCompliance, corr, null, this.body1, null);
            //}
        }



        //Linear damping
        private void ApplyLinearDamping(float dt)
        {
            //this.updateGlobalFrames();

            //let dVel = this.body0.getVelocityAt(this.globalPos0);
            //if (this.body1 != null)
            //    dVel.sub(this.body1.getVelocityAt(this.globalPos1));

            //// only damp along the distance vector

            //let n = new THREE.Vector3();
            //n.subVectors(this.globalPos1, this.globalPos0);
            //n.normalize();
            //n.multiplyScalar(-dVel.dot(n));
            //n.multiplyScalar(Math.min(this.linearDampingCoeff * dt, 1.0));
            //this.body0.applyCorrection(0.0, n, this.globalPos0, this.body1, this.globalPos1, true);
        }



        //Angular damping
        private void ApplyAngularDamping(float dt)
        {
            ApplyAngularDamping(dt, this.type.angularDampingCoeff);
        }

        private void ApplyAngularDamping(float dt, float coeff)
        {
            //this.updateGlobalFrames();

            //let dOmega = this.body0.omega.clone();
            //if (this.body1 != null)
            //    dOmega.sub(this.body1.omega);

            //if (this.type == Joint.TYPES.HINGE)
            //{
            //    // damp along the hinge axis
            //    let n = new THREE.Vector3(1.0, 0.0, 0.0);
            //    n.applyQuaternion(this.globalRot0);
            //    n.multiplyScalar(dOmega.dot(n));
            //    dOmega.copy(n);
            //}
            //if (this.type == Joint.TYPES.CYLINDER || this.type == Joint.TYPES.PRISMATIC || this.type == Joint.TYPES.FIXED)
            //    dOmega.multiplyScalar(-1.0); // maximum damping
            //else
            //    dOmega.multiplyScalar(-Math.min(this.angularDampingCoeff * dt, 1.0));
            //this.body0.applyCorrection(0.0, dOmega, null, this.body1, null, true);
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
    }
}