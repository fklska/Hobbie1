using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MapGenerator))]
public class EditorGenerate : Editor
{
    public override void OnInspectorGUI()
    {
        MapGenerator mapGenerator = (MapGenerator)target;
        DrawDefaultInspector();
        if (GUILayout.Button("Generate"))
        {
            mapGenerator.GenerateGround();
        }

        if (GUILayout.Button("Perlin Generation"))
        {
            mapGenerator.PerlinNoiseGeneration();
        }

        if (GUILayout.Button("Clear"))
        {
            mapGenerator.ClearMap();
        }
    }
}
