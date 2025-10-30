using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using UnityEngine;

namespace XPBD
{
    public class MyJoint
    {
        public enum Types
        {
            None,
            Distance,
            Hinge,
            Servo,
            Motor,
            Ball,
            Prismatic,
            Cylinder,
            Fixed
        };
        //static TYPES = {
        //                NONE: 'none',
        //                DISTANCE: 'distance',
        //                HINGE: 'hinge',
        //                SERVO: 'servo',
        //                MOTOR: 'motor',
        //                BALL: 'ball',
        //                PRISMATIC: 'prismatic',
        //                CYLINDER: 'cylinder',
        //                FIXED: 'fixed',
        //            };

        Types type;

        MyRigidBody body0;
        MyRigidBody body1;

        bool disabled;

        Quaternion globalFrameRot;

        //Distance
        bool hasTargetDistance = false;
        float targetDistance = 0f;
        float distanceCompliance = 0f;
        float distanceMin = -float.MaxValue;
        float distanceMax = float.MaxValue;
        float linearDampingCoeff = 0f;

        //Orientation
        float swingMin = -float.MaxValue;
        float swingMax = float.MaxValue;
        float twistMin = -float.MaxValue;
        float twistMax = float.MaxValue;
        float targetAngle = 0f;
        bool hasTargetAngle = false;
        float targetAngleCompliance = 0f;
        float angularDampingCoeff = 0f;

        //Motor
        float velocity = 0f;

        //public Joint(MyRigidBody body0, MyRigidBody body1, Vector3 globalFramePos, Quaternion globalFrameRot = null)
        public MyJoint(MyRigidBody body0, MyRigidBody body1, Vector3 globalFramePos, Quaternion globalFrameRot)
        {
            this.type = MyJoint.Types.None;
            this.body0 = body0;
            this.body1 = body1;
            this.disabled = false;

            //if (!globalFrameRot)
            //{
            //    globalFrameRot = new THREE.Quaternion(0.0, 0.0, 0.0, 1.0);
            //}

            //this.globalPos0 = globalFramePos.clone();
            //this.globalRot0 = globalFrameRot.clone();
            //this.globalPos1 = globalFramePos.clone();
            //this.globalRot1 = globalFrameRot.clone();

            //this.localPos0 = globalFramePos.clone();
            //this.localRot0 = globalFrameRot.clone();
            //this.localPos1 = globalFramePos.clone();
            //this.localRot1 = globalFrameRot.clone();

            //this.setFrames(globalFramePos, globalFrameRot);

            //this.visFrame0 = null;
            //this.visFrame1 = null;
            //this.visDistance = null;
        }

        //private void SetFrames(Vector3 globalFramePos, Quaternion globalFrameRot = null)
        private void SetFrames(Vector3 globalFramePos, Quaternion globalFrameRot)
        {
            //if (!globalFrameRot)
            //{
            //    globalFrameRot = new THREE.Quaternion(0.0, 0.0, 0.0, 1.0);
            //}

            //if (this.body0 != null)
            //{
            //    //Store the local position relative to body0
            //    this.localPos0.subVectors(globalFramePos, this.body0.pos);
            //    this.localPos0.applyQuaternion(this.body0.invRot);

            //    //Store the local rotation relative to body0
            //    this.localRot0.copy(globalFrameRot);
            //    //Factor out the body's rotation
            //    if (globalFrameRot)
            //    {
            //        this.localRot0.premultiply(this.body0.invRot);
            //    }
            //}
            //else
            //{
            //    this.localPos0.copy(globalFramePos);
            //    this.localRot0.copy(globalFrameRot);
            //}

            //if (this.body1 != null)
            //{
            //    //Store the local position relative to body1
            //    this.localPos1.subVectors(globalFramePos, this.body1.pos);
            //    this.localPos1.applyQuaternion(this.body1.invRot);

            //    //Store the local rotation relative to body1
            //    this.localRot1.copy(globalFrameRot);
            //    //Factor out the body's rotation
            //    if (globalFrameRot)
            //    {
            //        this.localRot1.premultiply(this.body1.invRot);
            //    }
            //}
            //else
            //{
            //    this.localPos1.copy(globalFramePos);
            //    this.localRot1.copy(globalFrameRot);
            //}
        }

        private void SetVisible(bool visible)
        {
            //if (this.visFrame0 != null)
            //    this.visFrame0.setVisible(visible);
            //if (this.visFrame1 != null)
            //    this.visFrame1.setVisible(visible);
            //if (this.visDistance != null)
            //    this.visDistance.setVisible(visible);
        }

        private void SetDisabled(bool disabled)
        {
            this.disabled = disabled;
            this.SetVisible(!disabled);
        }



        //
        // Init the different joints
        //

        public void InitHingeJoint(float swingMin, float swingMax, bool hasTargetAngle, float targetAngle, float compliance, float damping)
        {
            this.type = MyJoint.Types.Hinge;
            this.hasTargetDistance = true;
            this.targetDistance = 0f;
            this.swingMin = swingMin;
            this.swingMax = swingMax;
            this.hasTargetAngle = hasTargetAngle;
            this.targetAngle = targetAngle;
            this.targetAngleCompliance = compliance;
            this.angularDampingCoeff = damping;
        }

        public void InitServo(float swingMin, float swingMax)
        {
            this.type = MyJoint.Types.Servo;
            this.hasTargetDistance = true;
            this.targetDistance = 0f;
            this.swingMin = swingMin;
            this.swingMax = swingMax;
            this.hasTargetAngle = true;
            this.targetAngle = 0f;
            this.targetAngleCompliance = 0f;
        }

        public void InitMotor(float velocity)
        {
            this.type = MyJoint.Types.Motor;
            this.hasTargetDistance = true;
            this.targetDistance = 0f;
            this.velocity = velocity;
            this.hasTargetAngle = true;
            this.targetAngle = 0f;
            this.targetAngleCompliance = 0f;
        }

        public void InitBallJoint(float swingMax, float twistMin, float twistMax, float damping)
        {
            this.type = MyJoint.Types.Ball;
            this.hasTargetDistance = true;
            this.targetDistance = 0f;
            this.swingMin = 0f;
            this.swingMax = swingMax;
            this.twistMin = twistMin;
            this.twistMax = twistMax;
            this.angularDampingCoeff = damping;
        }

        public void InitPrismaticJoint(float distanceMin, float distanceMax, float twistMin, float twistMax, bool hasTarget, float targetDistance, float targetCompliance, float damping)
        {
            this.type = MyJoint.Types.Prismatic;
            this.distanceMin = distanceMin;
            this.distanceMax = distanceMax;
            this.swingMin = 0f;
            this.swingMax = 0f;
            this.twistMin = twistMin;
            this.twistMax = twistMax;
            this.hasTargetDistance = hasTarget;
            this.targetDistance = targetDistance;
            this.distanceCompliance = targetCompliance;
            this.linearDampingCoeff = damping;
        }

        public void InitCylinderJoint(float distanceMin, float distanceMax, float twistMin, float twistMax, float hasTargetDistance, float restDistance, float compliance, float damping)
        {
            this.type = MyJoint.Types.Cylinder;
            this.distanceMin = distanceMin;
            this.distanceMax = distanceMax;
            this.swingMin = 0f;
            this.swingMax = 0f;
            this.twistMin = twistMin;
            this.twistMax = twistMax;
            this.hasTargetDistance = true;
            this.distanceCompliance = 0f;
        }

        public void InitDistanceJoint(float restDistance, float compliance, float damping)
        {
            this.type = MyJoint.Types.Distance;
            this.hasTargetDistance = true;
            this.targetDistance = restDistance;
            this.distanceCompliance = compliance;
            this.linearDampingCoeff = damping;
        }



        //
        // Move joint
        //

        private void applyTorque(float dt, float torque)
        {
            //UpdateGlobalFrames();

            //// assumng x-axis is the hinge axis
            //let corr = new THREE.Vector3(1.0, 0.0, 0.0);
            //corr.applyQuaternion(this.globalRot0);
            //corr.multiplyScalar(torque * dt);

            //this.body0.applyCorrection(0.0, corr, null, this.body1, null, true);
        }

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

        private void UpdateGlobalFrames()
        {
            //if (this.body0)
            //{
            //    this.globalPos0.copy(this.localPos0);
            //    this.globalPos0.applyQuaternion(this.body0.rot);
            //    this.globalPos0.add(this.body0.pos);
            //    this.globalRot0.multiplyQuaternions(this.body0.rot, this.localRot0);
            //}

            //if (this.body1)
            //{
            //    this.globalPos1.copy(this.localPos1);
            //    this.globalPos1.applyQuaternion(this.body1.rot);
            //    this.globalPos1.add(this.body1.pos);
            //    this.globalRot1.multiplyQuaternions(this.body1.rot, this.localRot1);
            //}
            //else
            //{
            //    this.globalPos1.copy(this.localPos1);
            //    this.globalRot1.copy(this.localRot1);
            //}
        }

        private float GetAngle(Vector3 n, Vector3 a, Vector3 b)
        {
            //const c = new THREE.Vector3().crossVectors(a, b);
            //let phi = Math.asin(c.dot(n));
            //if (a.dot(b) < 0.0)
            //    phi = Math.PI - phi;
            //if (phi > Math.PI)
            //    phi -= 2.0 * Math.PI;
            //if (phi < -Math.PI)
            //    phi += 2.0 * Math.PI;
            //return phi;

            return 0f;
        }

        private void LimitAngle(Vector3 n, Vector3 a, Vector3 b, float minAngle, float maxAngle, float compliance)
        {
            //let phi = this.getAngle(n, a, b);

            //if (minAngle <= phi && phi <= maxAngle)
            //    return;
            //phi = Math.max(minAngle, Math.min(phi, maxAngle));

            //let ra = a.clone();
            //ra.applyAxisAngle(n, phi);

            //let corr = new THREE.Vector3().crossVectors(ra, b);
            //this.body0.applyCorrection(compliance, corr, null, this.body1, null);
        }

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

        private void Solve(float dt)
        {
            SolvePosition(dt);
            SolveOrientation(dt);
        }

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

        //private void ApplyAngularDamping(float dt, float coeff = this.angularDampingCoeff)
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
        // Visuals showing where the hinges connect and the distance between the connections
        //

        //visFrame is the tiny axis???
        //visDIstance is the red line going between the visFrame axis???
        public void AddVisuals(float width = 0.004f, float size = 0.08f)
        {
            //    if (this.visFrame0 == null)
            //    {
            //        this.visFrame0 = new VisualFrame(scene, width, size);
            //        this.visFrame1 = new VisualFrame(scene, width, size);
            //    }

            //    if (this.visDistance == null)
            //    {
            //        this.visDistance = new VisualDistance(scene, width);
            //    }
            //    this.updateVisuals();
        }

        private void UpdateVisuals()
        {
            //if (this.disabled)
            //    return;

            //this.updateGlobalFrames();                    // Calculate the actual world positions for joint attachment points

            //if (this.visFrame0)
            //    this.visFrame0.updateMesh(this.globalPos0, this.globalRot0);
            //if (this.visFrame1)
            //    this.visFrame1.updateMesh(this.globalPos1, this.globalRot1);
            //if (this.visDistance != null)
            //    this.visDistance.updateMesh(this.globalPos0, this.globalPos1);
        }
    }
}