using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XPBD;

namespace XPBD
{
    public class MyJointTemp
    {
        //
        // Simulate joints by combining the building blocks in two large methods making it messy
        //

        //Position constraint
        //private void SolvePosition(float dt)
        //{
        //    if (this.disabled || this.settings.type == MyJointSettings.Types.None)
        //    {
        //        return;
        //    }

        //    //Align
        //    if (this.JointType == MyJointSettings.Types.Prismatic || this.JointType == MyJointSettings.Types.Cylinder)
        //    {
        //        float targetDistance = Mathf.Max(this.settings.distanceMin, Mathf.Min(this.settings.targetDistance, this.settings.distanceMax));

        //        float hardCompliance = 0f;

        //        UpdateGlobalFrames();

        //        Vector3 corr = this.globalPos1 - this.globalPos0;

        //        corr = this.globalRot0.Conjugate() * corr;

        //        if (this.JointType == MyJointSettings.Types.Cylinder)
        //        {
        //            corr.x -= this.settings.targetDistance;
        //        }
        //        else if (corr.x > this.settings.distanceMax)
        //        {
        //            corr.x -= this.settings.distanceMax;
        //        }
        //        else if (corr.x < this.settings.distanceMin)
        //        {
        //            corr.x -= this.settings.distanceMin;
        //        }
        //        else
        //        {
        //            corr.x = 0f;
        //        }

        //        corr = this.globalRot0 * corr;

        //        //this.body0.applyCorrection(hardCompliance, corr, this.globalPos0, this.body1, this.globalPos1);
        //        PositionalCorrection.Apply(hardCompliance, corr, this.body0, this.globalPos0, this.body1, this.globalPos1);
        //    }

        //    //Solve distance
        //    if (this.JointType != MyJointSettings.Types.Cylinder && this.settings.hasTargetDistance)
        //    {
        //        UpdateGlobalFrames();

        //        Vector3 corr = this.globalPos1 - this.globalPos0;

        //        float distance = corr.magnitude;

        //        if (distance == 0f)
        //        {
        //            corr = new Vector3(0f, 0f, 1f);

        //            corr = this.globalRot0 * corr;
        //        }
        //        else
        //        {
        //            corr = Vector3.Normalize(corr);
        //        }


        //        corr *= this.settings.targetDistance - distance;

        //        corr *= -1f;

        //        //this.body0.ApplyCorrection(this.distanceCompliance, corr, this.globalPos0, this.body1, this.globalPos1);
        //        PositionalCorrection.Apply(hardCompliance, corr, this.body0, this.globalPos0, this.body1, this.globalPos1);
        //    }
        //}



        //Orientation constraint
        //private void SolveOrientation(float dt)
        //{
        //    if (this.disabled || this.JointType == MyJointSettings.Types.None || this.JointType == MyJointSettings.Types.Distance)
        //    {
        //        return;
        //    }

        //    if (this.JointType == MyJointSettings.Types.Motor)
        //    {
        //        float aAngle = Mathf.Min(Mathf.Max(this.settings.velocity * dt, -1f), 1f);

        //        this.settings.targetAngle += aAngle;
        //    }

        //    float hardCompliance = 0f;

        //    Vector3 axis0 = new Vector3(1f, 0f, 0f);
        //    Vector3 axis1 = new Vector3(0f, 1f, 0f);

        //    Vector3 a0 = new Vector3();
        //    Vector3 a1 = new Vector3();
        //    Vector3 n = new Vector3();
        //    Vector3 corr = new Vector3();

        //    if (
        //        this.JointType == MyJointSettings.Types.Hinge ||
        //        this.JointType == MyJointSettings.Types.Servo ||
        //        this.JointType == MyJointSettings.Types.Motor)
        //    {
        //        //Align axes

        //        UpdateGlobalFrames();

        //        a0 = axis0;
        //        a0 = this.globalRot0 * a0;

        //        a1 = axis0;
        //        a1 = this.globalRot1 * a0;

        //        corr = Vector3.Cross(a0, a1);

        //        //this.body0.ApplyCorrection(hardCompliance, corr, null, this.body1, null);
        //        AngularCorrection.Apply(hardCompliance, corr, this.body0, this.body1);

        //        if (this.settings.hasTargetAngle)
        //        {
        //            UpdateGlobalFrames();

        //            n = axis0;
        //            n = this.globalRot0 * n;

        //            a0 = axis1;
        //            a0 = this.globalRot0 * a0;

        //            a1 = axis1;
        //            a1 = this.globalRot1 * a1;

        //            LimitAngle(n, a0, a1, this.settings.targetAngle, this.settings.targetAngle, this.settings.targetAngleCompliance);
        //        }

        //        //Joint limits
        //        if (this.settings.swingMin > -float.MaxValue || this.settings.swingMax < float.MaxValue)
        //        {
        //            UpdateGlobalFrames();

        //            n = axis0;
        //            n = this.globalRot0 * n;

        //            a0 = axis1;
        //            a0 = this.globalRot0 * a0;

        //            a1 = axis1;
        //            a1 = this.globalRot1 * a1;

        //            LimitAngle(n, a0, a1, this.settings.swingMin, this.settings.swingMax, hardCompliance);
        //        }
        //    }
        //    else if (
        //        this.JointType == MyJointSettings.Types.Ball ||
        //        this.JointType == MyJointSettings.Types.Prismatic ||
        //        this.JointType == MyJointSettings.Types.Cylinder)
        //    {
        //        //Swing limit

        //        UpdateGlobalFrames();

        //        a0 = axis0;
        //        a0 = this.globalRot0 * a0;

        //        a1 = axis0;
        //        a1 = this.globalRot1 * a1;

        //        n = Vector3.Cross(a0, a1);
        //        n = Vector3.Normalize(n);

        //        LimitAngle(n, a0, a1, this.settings.swingMin, this.settings.swingMax, hardCompliance);


        //        //Twist limit

        //        UpdateGlobalFrames();

        //        a0 = axis0;
        //        a0 = this.globalRot0 * a0;

        //        a1 = axis0;
        //        a1 = this.globalRot1 * a1;

        //        n = a0 + a1;
        //        n = Vector3.Normalize(n);

        //        a0 = axis1;
        //        a0 = this.globalRot0 * a0;

        //        a1 = axis1;
        //        a1 = this.globalRot1 * a1;

        //        a0 += n * Vector3.Dot(-n, a0);
        //        a0 = Vector3.Normalize(a0);

        //        a1 += n * Vector3.Dot(-n, a1);
        //        a1 = Vector3.Normalize(a1);

        //        LimitAngle(n, a0, a1, this.settings.twistMin, this.settings.twistMax, hardCompliance);
        //    }
        //    else if (this.JointType == MyJointSettings.Types.Fixed)
        //    {
        //        //Align orientations

        //        UpdateGlobalFrames();

        //        Quaternion dq = this.globalRot0 * this.globalRot1.Conjugate();

        //        corr = new Vector3(2f * dq.x, 2f * dq.y, 2f * dq.z);

        //        if (dq.w > 0f)
        //        {
        //            corr *= -1f;
        //        }

        //        //this.body0.applyCorrection(hardCompliance, corr, null, this.body1, null);
        //        AngularCorrection.Apply(hardCompliance, corr, this.body0, this.body1);
        //    }
        //}
    }
}