#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CameraPanRoundTrigger))]
public class CameraPanRoundTriggerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GUILayout.Space(15);

        CameraPanRoundTrigger trigger =
            (CameraPanRoundTrigger)target;


        if (GUILayout.Button("BUILD / REBUILD CAMERA SPLINES"))
        {
            trigger.BuildAllSplines();

            EditorUtility.SetDirty(trigger);

            Debug.Log("Camera splines rebuilt.");
        }
    }
}

#endif