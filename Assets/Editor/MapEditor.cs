using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(MapManager))]
public class MapEditor : Editor {
    public override void OnInspectorGUI() {
        MapManager map = target as MapManager;
        
        if (DrawDefaultInspector() || GUILayout.Button("Generate Map"))
            map.GenerateMap();
    }
}
