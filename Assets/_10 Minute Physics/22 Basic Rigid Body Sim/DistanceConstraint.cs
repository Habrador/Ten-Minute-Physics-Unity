using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using UnityEngine;
using static UnityEditor.ShaderGraph.Internal.KeywordDependentCollection;
using UnityEngine.UIElements;

//Distance between two rbs or one rb and fixed point
public class DistanceConstraint
{
    //If one of these are null, then the attachment point is fixed
    private MyRigidBody body0;
    private MyRigidBody body1;

    //One sided, limit motion in one direction 
    private bool unilateral;

    private Vector3 worldPos0;
    private Vector3 worldPos1;
    private Vector3 localPos0;
    private Vector3 localPos1;

    //The rest distance 
    private float wantedDistance;
    //Inverse of physical stiffness (alpha in equations)
    private float compliance;

    //private Vector3 corr;

    private GameObject displayConstraintObj;

    //A rb can be null if we want to attach the constraint to a fixed location
    //Here body1 is assumed to be the fixed one (if any exists)
    //Attachment points pos0 and pos1 are in world pos
    public DistanceConstraint(MyRigidBody body0, MyRigidBody body1, Vector3 pos0, Vector3 pos1, float distance, float compliance, bool unilateral, float width = 0.01f, float fontSize = 0f)
    {
        //this.scene = scene;
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

        newCylinderObj.transform.localScale = new Vector3(width, width, 1f);

        newCylinderObj.GetComponent<MeshRenderer>().material.color = UnityEngine.Color.red;

        this.displayConstraintObj = newCylinderObj;


        //Create text renderer for force display
        //this.textRenderer = null;
        //if (fontSize > 0.0) 
        //{
        //    this.textRenderer = new TextRenderer(scene, fontSize);
        //    this.textRenderer.loadFont().then(() => {
        //        this.updateText(0, 1);
        //        this.updateMesh();
        //    });
        //}


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
        
        float force = this.body0.ApplyCorrection(this.compliance, corr, this.worldPos0, this.body1, this.worldPos1, dt);

        float elongation = distance - this.wantedDistance;
        
        elongation = Mathf.Round(elongation * 100f) / 100f;
        
        //UpdateText(Mathf.Abs(force), elongation);
    }



    //Move and rotate the mesh we use to display the constraint
    //so it goes between the attachment points
    public void UpdateMesh() 
    {
        Vector3 start = this.worldPos0;
        Vector3 end = this.worldPos1;

        //Calculate the center point
        Vector3 center = (start + end) * 0.5f;

        //Calculate the direction vector
        Vector3 direction = end - start;
        
        float length = direction.magnitude;

        ///Create a rotation quaternion
        Quaternion quaternion = new Quaternion();

        //quaternion.setFromUnitVectors(new THREE.Vector3(0, 1, 0), direction.normalize());
        //Not sure if correct???
        quaternion = Quaternion.FromToRotation(new Vector3(0f, 1f, 0f), direction.normalized);

        //Update cylinder's transformation
        Transform cylinderTrans = displayConstraintObj.transform;

        cylinderTrans.SetPositionAndRotation(center, quaternion);
        cylinderTrans.localScale = new Vector3(1f, length, 1f);


        //Update text position and rotation
        //if (this.textRenderer)
        //{
        //    this.textRenderer.updatePosition(center);
        //    this.textRenderer.updateRotation(gCamera.quaternion);
        //}
    }



    private void UpdateText(float force, float elongation)
    {
        //if (this.textRenderer)
        //{
        //    this.textRenderer.createText(`   ${ Math.round(force)}
        //    N,  ${ elongation}
        //    m`, this.cylinder.position);
        //}
    }



    private void Dispose()
    {
        if (this.displayConstraintObj)
        {
            GameObject.Destroy(this.displayConstraintObj);
        }

        //if (this.textRenderer)
        //{
        //    this.textRenderer.dispose();
        //}
    }

}
