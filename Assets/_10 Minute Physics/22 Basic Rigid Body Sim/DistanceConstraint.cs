using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using UnityEngine;
using static UnityEditor.ShaderGraph.Internal.KeywordDependentCollection;
using UnityEngine.UIElements;

public class DistanceConstraint
{
    MyRigidBody body0;
    MyRigidBody body1;

    bool unilateral;

    Vector3 worldPos0;
    Vector3 worldPos1;
    Vector3 localPos0;
    Vector3 localPos1;

    float distance;
    float compliance;

    Vector3 corr;

    //A rb can be null if we want to attach the constraint to a fixed location
    public DistanceConstraint(MyRigidBody body0, MyRigidBody body1, Vector3 pos0, Vector3 pos1, float distance, float compliance, bool unilateral, float width = 0.01f, float fontSize = 0.0f, int color = 0xff0000)
    {
        //this.scene = scene;
        this.body0 = body0;
        this.body1 = body1;
        this.unilateral = unilateral;

        this.worldPos0 = pos0;
        this.worldPos1 = pos1;
        this.localPos0 = pos0;
        this.localPos1 = pos1;

        //this.body0.worldToLocal(pos0, this.localPos0);

        //if (body1 != undefined)
        //    this.body1.worldToLocal(pos1, this.localPos1);

        this.distance = distance;
        this.compliance = compliance;

        // Create a cylinder for visualization
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
            
    public void Solve() 
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
