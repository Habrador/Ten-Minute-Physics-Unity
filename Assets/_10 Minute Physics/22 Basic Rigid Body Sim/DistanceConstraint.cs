using System.Collections;
using System.Collections.Generic;
using UnityEngine;


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

    private readonly GameObject displayConstraintObj;
    private readonly Transform displayConstraintTrans;

    //For displaying
    private float force;
    private float elongation;
    //Font size determines if we should display rb data on the screen
    private int fontSize;



    //Removed scene as parameter - we add the rb to the simulator when we create it
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
        GameObject newCylinderObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);

        newCylinderObj.transform.localScale = new Vector3(width, 1f, width);

        newCylinderObj.GetComponent<MeshRenderer>().material.color = UnityEngine.Color.red;

        //Remove collider because the constraint is not part of the raycasting
        newCylinderObj.GetComponent<Collider>().enabled = false;

        this.displayConstraintObj = newCylinderObj;
        this.displayConstraintTrans = newCylinderObj.transform;


        //
        this.fontSize = fontSize;


        UpdateMesh();
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



    //Move and rotate the mesh we use to display the constraint
    //so it goes between the attachment points
    public void UpdateMesh() 
    {
        Vector3 start = this.worldPos0;
        Vector3 end = this.worldPos1;

        //Depending on the number of iterations we might have to recalculate these even though we cache them in FixedUpdate
        //start = this.body0.LocalToWorld(this.localPos0);

        //if (this.body1 != null)
        //{
        //    end = this.body1.LocalToWorld(this.localPos1);
        //}

        //Debug.DrawLine(start, end, UnityEngine.Color.blue);

        //return;

        //Position

        //Calculate the center point
        Vector3 center = (start + end) * 0.5f;

        //Rotation

        //Calculate the direction vector
        Vector3 direction = end - start;
        
        //Create a rotation quaternion
        Quaternion quaternion = new Quaternion();

        quaternion = Quaternion.LookRotation(direction.normalized, Vector3.up);

        //Rotate 90 degrees to align it properly
        quaternion *= Quaternion.Euler(90f, 0f, 0f);

        //Update cylinder's transformation
        this.displayConstraintTrans.SetPositionAndRotation(center, quaternion);


        //Scale
        Vector3 currentScale = this.displayConstraintTrans.localScale;

        float length = direction.magnitude;

        //In Unity we have to multiply the length by 0.5
        length *= 0.5f;

        this.displayConstraintTrans.localScale = new Vector3(currentScale.x, length, currentScale.z);
    }



    //Display force on constraint in N and the elogation in m next to object using OnGUI 
    public void DisplayData()
    {
        if (this.fontSize == 0)
        {
            return;
        }
        
        string displayText = Mathf.RoundToInt(Mathf.Abs(this.force)) + "N" + ", " + this.elongation + "m";

        GUIStyle textStyle = new GUIStyle();

        textStyle.fontSize = this.fontSize;
        textStyle.normal.textColor = UnityEngine.Color.black;

        //The position and size of the text area
        Rect textArea = new Rect(10, 10, 200, this.fontSize);

        //From world space to screen space
        Vector3 screenPos = Camera.main.WorldToScreenPoint(this.displayConstraintTrans.position);

        //WorldToScreenPoint and GUI.Label y positions are for some reason inverted
        screenPos.y = Screen.height - screenPos.y;

        //We also want it centered
        screenPos.y -= textArea.height * 0.5f;

        //And offset it in x direction so its outside of the object
        screenPos.x += 30f;

        textArea.position = new Vector2(screenPos.x, screenPos.y);

        GUI.Label(textArea, displayText, textStyle);
    }



    public void Dispose()
    {
        if (this.displayConstraintObj)
        {
            GameObject.Destroy(this.displayConstraintObj);
        }
    }
}
