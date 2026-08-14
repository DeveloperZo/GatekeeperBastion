using GatekeeperBastion.Game;
using UnityEditor;
using UnityEngine;

namespace GatekeeperBastion.EditorTools
{
    [CustomEditor(typeof(BastionTerminal))]
    public sealed class BastionTerminalEditor : Editor
    {
        SerializedProperty _terminalId;
        SerializedProperty _kind;
        SerializedProperty _wavesToComplete;
        SerializedProperty _unlocksDoor;
        SerializedProperty _unlockedSpawns;
        SerializedProperty _game;

        void OnEnable()
        {
            _terminalId = serializedObject.FindProperty("terminalId");
            _kind = serializedObject.FindProperty("kind");
            _wavesToComplete = serializedObject.FindProperty("wavesToComplete");
            _unlocksDoor = serializedObject.FindProperty("unlocksDoor");
            _unlockedSpawns = serializedObject.FindProperty("unlockedSpawns");
            _game = serializedObject.FindProperty("_game");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(_terminalId);
            EditorGUILayout.PropertyField(_kind);

            var kind = (BastionTerminalKind)_kind.enumValueIndex;
            if (kind == BastionTerminalKind.Field)
            {
                EditorGUILayout.Space(4f);
                EditorGUILayout.LabelField("Field unlock", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(_wavesToComplete, new GUIContent("Waves To Complete"));
                EditorGUILayout.PropertyField(_unlocksDoor, new GUIContent("Unlocks Door"));
                EditorGUILayout.PropertyField(
                    _unlockedSpawns,
                    new GUIContent(
                        "Wave Spawns",
                        "Breach points used when this terminal starts a Wave. Enabled on Interact; stay available after clear."));
            }
            else if (kind == BastionTerminalKind.Master)
            {
                EditorGUILayout.Space(4f);
                EditorGUILayout.PropertyField(
                    _unlockedSpawns,
                    new GUIContent(
                        "Wave Spawns",
                        "Control Room spawn points. Re-enabled after the door seals."));
            }

            EditorGUILayout.Space(4f);
            EditorGUILayout.PropertyField(_game);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
