using UnityEngine;
using UnityEditor;
using System.Linq;

namespace FFmpegOut
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(VideoCapture))]
    public class VideoCaptureEditor : Editor
    {
        SerializedProperty _width;
        SerializedProperty _height;
        SerializedProperty _preset;
        SerializedProperty _frameRate;
        SerializedProperty _folderPath;

        GUIContent[] _presetLabels;
        int[] _presetOptions;

        // It shows the render format options when:
        // - Editing multiple objects.
        // - No target texture is specified in the camera.
        bool ShouldShowFormatOptions => !EditorApplication.isPlaying;

        void OnEnable()
        {
            _width = serializedObject.FindProperty("_width");
            _height = serializedObject.FindProperty("_height");
            _preset = serializedObject.FindProperty("_preset");
            _frameRate = serializedObject.FindProperty("_frameRate");
            _folderPath = serializedObject.FindProperty("_folderPath");

            var presets = System.Enum.GetValues(typeof(FFmpegPreset));
            _presetLabels = presets.Cast<FFmpegPreset>().
                Select(p => new GUIContent(p.GetDisplayName())).ToArray();
            _presetOptions = presets.Cast<int>().ToArray();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            if (ShouldShowFormatOptions)
            {
                EditorGUILayout.PropertyField(_width);
                EditorGUILayout.PropertyField(_height);
                EditorGUILayout.PropertyField(_folderPath);
            }

            EditorGUILayout.IntPopup(_preset, _presetLabels, _presetOptions);
            EditorGUILayout.PropertyField(_frameRate);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
