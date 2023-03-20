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
    [TextArea(5,8)]
    public string cardDescription;

    [HideInInspector]
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
        MoveBackward,
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
        list.onAddDropdownCallback = (Rect buttonRect, ReorderableList l) => {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Move/Forward"), false, clickHandler,
                new EffectCreationParams() { Type = CardData.EffectType.MoveForward });
            menu.AddItem(new GUIContent("Move/Backward"), false, clickHandler,
                new EffectCreationParams() { Type = CardData.EffectType.MoveBackward });
            menu.ShowAsContext();
        };
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        CardData script = (CardData)target;

        serializedObject.Update();
        list.DoLayoutList();
        serializedObject.ApplyModifiedProperties();
    }

    private void clickHandler(object target)
    {
        EffectCreationParams data = (EffectCreationParams)target;
        int index = list.serializedProperty.arraySize;
        list.serializedProperty.arraySize++;
        list.index = index;
        SerializedProperty element = list.serializedProperty.GetArrayElementAtIndex(index);
        element.FindPropertyRelative("effect").enumValueIndex = (int)data.Type;
        serializedObject.ApplyModifiedProperties();
    }

    private struct EffectCreationParams
    {
        public CardData.EffectType Type;
    }
}
#endif