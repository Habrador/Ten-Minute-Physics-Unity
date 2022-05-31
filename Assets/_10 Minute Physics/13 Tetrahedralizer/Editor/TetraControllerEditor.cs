using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(TetraController))]
public class TetraControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        TetraController tetraScript = (TetraController)target;

        if (GUILayout.Button("Tetrahedralize Mesh"))
        {
            tetraScript.TetrahedralizeMesh();
        }
    }


}
