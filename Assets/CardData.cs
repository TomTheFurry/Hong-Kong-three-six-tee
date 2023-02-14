using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
#endif

[CreateAssetMenu(menuName = "Game_Data/Cards")]

public class CardData : ScriptableObject
{
    public string cardName;
    public string cardDescription;


    public List<Effect> effects;

    [Serializable]
    public struct Effect
    {
        public EffectType effect;
        public int step;
    }

    public enum EffectType
    {
        MoveForward,
        MoveBack
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(CardData))]
public class CardDataBase_Editor : Editor
{
    private ReorderableList list;
    private void OnEnable()
    {
        list = new ReorderableList(
            serializedObject,
            serializedObject.FindProperty("effects"),
            true, true, true, true
        );
        list.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => {
            var element = list.serializedProperty.GetArrayElementAtIndex(index);
            rect.y += 2;
            EditorGUI.PropertyField(
                new Rect(rect.x, rect.y, 120, EditorGUIUtility.singleLineHeight),
                element.FindPropertyRelative("effect"), GUIContent.none
            );
            EditorGUI.PropertyField(
                new Rect(rect.x + 120, rect.y, rect.width - 120, EditorGUIUtility.singleLineHeight),
                element.FindPropertyRelative("step"), GUIContent.none
            );
        };
        list.drawHeaderCallback = (Rect rect) => {
            EditorGUI.LabelField(rect, "Effects");
        };
    }

    public override void OnInspectorGUI()
    {
        CardData script = (CardData)target;

        script.cardName = EditorGUILayout.TextField("Name", script.cardName);
        script.cardDescription = EditorGUILayout.TextField("Description", script.cardDescription);

        serializedObject.Update();
        list.DoLayoutList();
        serializedObject.ApplyModifiedProperties();
    }
}
#endif