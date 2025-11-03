using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace XPBD
{

    //Distance between two rbs or one rb and fixed point
    public class DistanceConstraint
    {
        //Null if attachment point is fixed
        private MyRigidBody body0;
        private MyRigidBody body1;

        //One sided, limit motion in one direction 
        private bool unilateral;

        public Vector3 worldPos0;
        //Public so we can access it when we drag with mouse 
        public Vector3 worldPos1;
        private Vector3 localPos0;
        private Vector3 localPos1;

        //The rest distance 
        private readonly float wantedDistance;
        //Inverse of physical stiffness (alpha in equations) [m/N]
        private readonly float compliance;

        //For displaying the distance with a red line
        private readonly VisualDistance displayConstraintObj;

        //For displaying constraint data
        private float force;
        private float elongation;
        //Font size determines if we should display rb data on the screen
        private readonly int fontSize;



        //When we want to delete the physical object we call Dispose()
        //A rb can be null if we want to attach the constraint to a fixed location
        //Here body1 is assumed to be the fixed one (if any exists)
        //Attachment points pos0 and pos1 are in world pos
        public DistanceConstraint(MyRigidBody body0, MyRigidBody body1, Vector3 pos0, Vector3 pos1, float distance, float compliance, bool unilateral, float width = 0.01f, int fontSize = 0)
        {
            this.body0 = body0;
            this.body1 = body1;

            this.unilateral = unilateral;

            this.worldPos0 = pos0;
            this.worldPos1 = pos1;
            this.localPos0 = pos0;
            this.localPos1 = pos1;

            this.localPos0 = this.body0.WorldToLocal(pos0);

            if (body1 != null)
            {
                this.localPos1 = this.body1.WorldToLocal(pos1);
            }

            this.wantedDistance = distance;
            this.compliance = compliance;


            //Create a cylinder for visualization
            this.displayConstraintObj = new VisualDistance(UnityEngine.Color.red, width);

            UpdateMesh();

            //
            this.fontSize = fontSize;
        }



        //Make sure the constraint has the correct length
        //a - attachment points are defined relative to a rb's center of mass
        //r - vector from center of mass to a 
        //l_0 - wanted length
        //l - current length
        //Move each rb delta_x which is proportional to m^-1 and I^-1
        //n = (a_2 - a_1) / |a_2 - a_1|
        //C = l - l_0
        //Compute generalized inverse mass for rb i
        //w_i = m_i^-1 * (r_i x n)^T * I_i^-1 * (r_i x n)
        //Compute Lagrange multiplier
        //lambda = -C * (w_1 + w_2 + alpha / dt^2)^-1 where 
        //alpha is physical inverse stiffness
        //Update pos and rot
        //x_i = x_i +- w_i * lambda * n
        //q_i = q_i + 0.5 * lambda * (I_i^-1 * (r_i x n), 0) * q_i
        //Constraint force
        //F = (lambda * n) / dt^2
        public void Solve(float dt)
        {
            //Local -> global
            this.worldPos0 = this.body0.LocalToWorld(this.localPos0);

            if (this.body1 != null)
            {
                this.worldPos1 = this.body1.LocalToWorld(this.localPos1);
            }

            //Constraint distance C = l - l_0
            Vector3 corr = this.worldPos1 - this.worldPos0;

            float distance = corr.magnitude;

            corr = corr.normalized;

            if (this.unilateral && distance < this.wantedDistance)
            {
                return;
            }

            corr *= distance - this.wantedDistance;

            this.force = this.body0.ApplyCorrection(this.compliance, corr, this.worldPos0, this.body1, this.worldPos1, dt);


            //Data for display purposes
            float elongation = distance - this.wantedDistance;

            this.elongation = Mathf.Round(elongation * 100f) / 100f;
        }



        //Move, rotate, adns cale the mesh we use to display the constraint
        //so it goes between the attachment points
        public void UpdateMesh()
        {
            this.displayConstraintObj.UpdateMesh(this.worldPos0, this.worldPos1);
        }



        //Display force on constraint in N and the elogation in m next to object using OnGUI 
        public void DisplayData()
        {
            if (this.fontSize == 0)
            {
                return;
            }

            string displayText = Mathf.RoundToInt(Mathf.Abs(this.force)) + "N" + ", " + this.elongation + "m";

            BasicRBGUI.DisplayDataNextToRB(displayText, this.fontSize, this.displayConstraintObj.Pos);
        }



        public void Dispose()
        {
            if (this.displayConstraintObj != null)
            {
                this.displayConstraintObj.Dispose();
            }
        }
    }
}