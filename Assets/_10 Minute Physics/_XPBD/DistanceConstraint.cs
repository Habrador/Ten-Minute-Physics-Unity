using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace XPBD
{

    //Distance between two rbs or one rb and fixed point
    //Aka Position constraint
    public class DistanceConstraint
    {
        //Null if attachment point is fixed (always body1 is attachment point)
        private readonly MyRigidBody body0;
        private readonly MyRigidBody body1;

        //One sided, limit motion in one direction 
        private bool unilateral;

        //Attachment points
        //Public so we can access it when we drag with mouse 
        //Why do we need to chache world pos? Becomes confusing...
        //If fixed attachment point???
        public Vector3 worldPos0;
        public Vector3 worldPos1;
        private Vector3 localPos0;
        private Vector3 localPos1;

        //The rest distance (length)
        private readonly float wantedLength;
        //Inverse of physical stiffness (alpha in equations) [m/N]
        private readonly float compliance;

        //For displaying the distance with a red cylinder
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

            this.wantedLength = distance;
            this.compliance = compliance;

            this.fontSize = fontSize;

            //Create a cylinder for visualization
            this.displayConstraintObj = new VisualDistance(UnityEngine.Color.red, width);

            UpdateMesh();
        }



        //Make sure the constraint has the correct length
        //We have:
        // - a: attachment points are defined relative to a rb's center of mass
        // - r: vector from center of mass to a 
        // - l_0: wanted length
        // - l: current length
        //
        //Distance constraint:
        // n = (a2 - a1) / |a2 - a1|
        // C = l - l_0
        //
        //Compute generalized inverse mass for each rb
        // w = m^-1 * (r x n)^T * I^-1 * (r x n)
        //
        //Compute Lagrange multiplier
        // lambda = -C * (w_1 + w_2 + alpha / dt^2)^-1
        // where
        // - alpha: physical inverse stiffness
        //
        //Update pos and rot (+- because we have two rbs and we use + for one and - for the other)
        // x = x +- w * lambda * n (w = 1/m and not the generalized inverse mass as it is in the paper???)
        // q = q +- 0.5 * lambda * (I^-1 * (r x n), 0) * q
        //
        //Constraint force (only needed for display purposes)
        // F = (lambda * n) / dt^2
        //
        //From "Detailed rigid body simulation with xpbd"
        //The difference is that they compute the Lagrange multipler updates
        // delta_lambda = (-c - alpha_tilde * lambda) / (w1 + w2 + alpha_tilde)
        //where
        // - alpha_tilde = alpha / dt^2
        // lambda = lambda + delta_lambda
        //BUT according to the YT video "09 Getting ready to simulate the world with XPBD" (7:15)
        //we dont need to keep track of the lagrange multiplier per constraint
        //if we iterate over the constraints just once
        //Notice that iteration and substeps are not the same
        //Some are iterating over the constraints multiple times each substep
        //But we are doing it just once because it generates a better result
        //
        //Update pos and rot
        // x = x +- p / m
        // q = q +- 0.5 * (I^-1 * (r x p), 0) * q
        // where
        // p = delta_lambda * n
        //BUT we dont use delta_lambda so we get p = lambda * n
        public void Solve(float dt)
        {
            //Local -> global
            this.worldPos0 = this.body0.LocalToWorld(this.localPos0);

            if (this.body1 != null)
            {
                this.worldPos1 = this.body1.LocalToWorld(this.localPos1);
            }

            //Distance constraint so we need to calculate:
            // n = (a2 - a1) / |a2 - a1|
            // C = l - l_0

            Vector3 a2_minus_a1 = this.worldPos1 - this.worldPos0;

            float currentLength = a2_minus_a1.magnitude;

            //Why do we ignore this if currentLength < wantedLength?
            //Whats the meaning of unilateral?
            if (this.unilateral && currentLength < this.wantedLength)
            {
                return;
            }

            Vector3 n = a2_minus_a1.normalized;

            float C = currentLength - this.wantedLength;

            //n * C
            Vector3 corr = n * C;

            this.force = this.body0.ApplyCorrection(this.compliance, corr, this.worldPos0, this.body1, this.worldPos1, dt);


            //Data for display purposes
            float elongation = currentLength - this.wantedLength;

            this.elongation = Mathf.Round(elongation * 100f) / 100f;
        }



        //Transform the mesh we use to display the constraint so it goes between the attachment points
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
            this.displayConstraintObj?.Dispose();
        }
    }
}