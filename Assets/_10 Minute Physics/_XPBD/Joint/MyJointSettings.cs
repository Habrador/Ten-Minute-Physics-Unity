using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace XPBD
{
    //Settings for the different joints to make the joint class less messy
    public class MyJointSettings 
    {
        public enum Types
        {
            None,
            Distance,
            Hinge, //Rotation around one axis
            Servo, //Can rotate to a specific angle and hold that position
            Motor, //Runs continuously in one direction 
            Ball, //No translation, only rotation
            Prismatic, //Movement in 1d
            Cylinder, //Similar to prismatic but the cylinder can rotate around the movement axis
            Fixed
        };

        public Types type = Types.None;

        //Distance
        public bool hasTargetDistance = false;
        public float targetDistance = 0f;
        public float distanceCompliance = 0f;
        public float distanceMin = -float.MaxValue;
        public float distanceMax = float.MaxValue;
        public float linearDampingCoeff = 0f;

        //Orientation
        public float swingMin = -float.MaxValue;
        public float swingMax = float.MaxValue;
        public float twistMin = -float.MaxValue;
        public float twistMax = float.MaxValue;
        public float targetAngle = 0f;
        public bool hasTargetAngle = false;
        public float targetAngleCompliance = 0f;
        public float angularDampingCoeff = 0f;

        //Motor
        public float velocity = 0f;


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

        //The importer uses only the first 4 parameters while the init joint uses 4 unused parameters as well?
        public void InitCylinderJoint(float distanceMin, float distanceMax, float twistMin, float twistMax)
        //public void InitCylinderJoint(float distanceMin, float distanceMax, float twistMin, float twistMax, float hasTargetDistance, float restDistance, float compliance, float damping)
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