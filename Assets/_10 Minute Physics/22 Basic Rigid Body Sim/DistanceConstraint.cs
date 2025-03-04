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
    MyRigidBody body0;
    MyRigidBody body1;

    //One sided, limit motion in one direction 
    bool unilateral;

    Vector3 worldPos0;
    Vector3 worldPos1;
    Vector3 localPos0;
    Vector3 localPos1;

    float distance;
    float compliance;

    Vector3 corr;

    //A rb can be null if we want to attach the constraint to a fixed location
    //Here body1 is assumed to be the fixed one (if any exists)
    //Attachment points pos0 and pos1 are in world pos
    public DistanceConstraint(MyRigidBody body0, MyRigidBody body1, Vector3 pos0, Vector3 pos1, float distance, float compliance, bool unilateral, float width = 0.01f, float fontSize = 0f, int color = 0xff0000)
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
        
        this.distance = distance;
        this.compliance = compliance;


        //Create a cylinder for visualization
        //const geometry = new THREE.CylinderGeometry(width, width, 1, 32);
        //const material = new THREE.MeshBasicMaterial({ color: color });
        //this.cylinder = new THREE.Mesh(geometry, material);
        //this.cylinder.castShadow = true;
        //this.cylinder.receiveShadow = true;
        //scene.add(this.cylinder);
        
        
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
        //this.body0.localToWorld(this.localPos0, this.worldPos0);
        //if (this.body1 != undefined)
        //    this.body1.localToWorld(this.localPos1, this.worldPos1);
        //this.corr.subVectors(this.worldPos1, this.worldPos0);
        //let distance = this.corr.length();
        //this.corr.normalize();
        //if (this.unilateral && distance < this.distance)
        //    return;
        //this.corr.multiplyScalar(distance - this.distance);
        //let force = this.body0.applyCorrection(this.compliance, this.corr, this.worldPos0, this.body1, this.worldPos1);

        //let elongation = distance - this.distance;
        //elongation = Math.round(elongation * 100) / 100;
        //this.updateText(Math.abs(force), elongation);
    }



    public void UpdateMesh() 
    {
        //const start = this.worldPos0;
        //const end = this.worldPos1;

        //// Calculate the center point
        //const center = new THREE.Vector3().addVectors(start, end).multiplyScalar(0.5);

        //// Calculate the direction vector
        //const direction = new THREE.Vector3().subVectors(end, start);
        //const length = direction.length();

        //// Create a rotation quaternion
        //const quaternion = new THREE.Quaternion();
        //quaternion.setFromUnitVectors(new THREE.Vector3(0, 1, 0), direction.normalize());

        //// Update cylinder's transformation
        //this.cylinder.position.copy(center);
        //this.cylinder.setRotationFromQuaternion(quaternion);
        //this.cylinder.scale.set(1, length, 1);

        //// Update text position and rotation
        //if (this.textRenderer)
        //{
        //    this.textRenderer.updatePosition(center);
        //    this.textRenderer.updateRotation(gCamera.quaternion);
        //}
    }



    //private void UpdateText(force, elongation) 
    //{
    //    if (this.textRenderer)
    //    {
    //        this.textRenderer.createText(`   ${ Math.round(force)}
    //        N,  ${ elongation}
    //        m`, this.cylinder.position);
    //    }
    //}



    //private void Dispose() {
    //    if (this.cylinder)
    //    {
    //        if (this.cylinder.geometry) this.cylinder.geometry.dispose();
    //        if (this.cylinder.material) this.cylinder.material.dispose();
    //        this.scene.remove(this.cylinder);
    //    }
    //    if (this.textRenderer)
    //    {
    //        this.textRenderer.dispose();
    //    }
    //}
    
}
