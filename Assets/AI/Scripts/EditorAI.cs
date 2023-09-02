using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Alias_AI))]
public class EditorAI : Editor
{
    public override void OnInspectorGUI()
    {
        Alias_AI ai = (Alias_AI)target;
        DrawDefaultInspector();
        if (GUILayout.Button("Find"))
        {
            ai.GoTo();
        }
    }
}