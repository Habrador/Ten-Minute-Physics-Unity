using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace XPBD
{
    public class MyJointType 
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

        Types type = Types.None;

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
        public float angularDampingCoeff = 0f;

        //Motor
        float velocity = 0f;


        //
        // Init the different joints
        //

        public void InitHingeJoint(float swingMin, float swingMax, bool hasTargetAngle, float targetAngle, float compliance, float damping)
        {
            this.type = Types.Hinge;
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
            this.type = Types.Servo;
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
            this.type = Types.Motor;
            this.hasTargetDistance = true;
            this.targetDistance = 0f;
            this.velocity = velocity;
            this.hasTargetAngle = true;
            this.targetAngle = 0f;
            this.targetAngleCompliance = 0f;
        }

        public void InitBallJoint(float swingMax, float twistMin, float twistMax, float damping)
        {
            this.type = Types.Ball;
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
            this.type = Types.Prismatic;
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
            this.type = Types.Cylinder;
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
            this.type = Types.Distance;
            this.hasTargetDistance = true;
            this.targetDistance = restDistance;
            this.distanceCompliance = compliance;
            this.linearDampingCoeff = damping;
        }
    }
}