using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
#endif

[CreateAssetMenu(menuName = "Game_Data/Tiles")]

public class TileData : ScriptableObject
{
    public string tileName;
    public TileType type;

    // Tradeable
    public int maxLevel;
    public List<Level> levelData;

    [Serializable]
    public struct Level
    {
        public int upgradePrice;
        public int charge;
    }

    public enum TileType
    {
        Nothing,        // No action
        Tradeable,      // Can buy
        DrawEven,       // eg draw card / random even
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(TileData))]
public class TileDataBase_Editor : Editor
{
    private ReorderableList listLevel;

    private void OnEnable()
    {
        listLevel = new ReorderableList(
            serializedObject,
            serializedObject.FindProperty("levelData"),
            true, true, false, false
        );
        listLevel.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => {
            var element = listLevel.serializedProperty.GetArrayElementAtIndex(index);
            rect.y += 2;
            EditorGUI.LabelField(new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight), $"Level-{index}");
            rect.y += EditorGUIUtility.singleLineHeight + 3;

            EditorGUI.LabelField(new Rect(rect.x, rect.y, 120, EditorGUIUtility.singleLineHeight),
                index == 0 ? "Buy Price" : "Upgrade Price");
            EditorGUI.PropertyField(
                new Rect(rect.x + 120, rect.y, rect.width - 120, EditorGUIUtility.singleLineHeight),
                element.FindPropertyRelative("upgradePrice"), GUIContent.none
            );

            rect.y += EditorGUIUtility.singleLineHeight + 3;
            EditorGUI.LabelField(new Rect(rect.x, rect.y, 120, EditorGUIUtility.singleLineHeight), "Charge");
            EditorGUI.PropertyField(
                new Rect(rect.x + 120, rect.y, rect.width - 120, EditorGUIUtility.singleLineHeight),
                element.FindPropertyRelative("charge"), GUIContent.none
            );

            rect.y += EditorGUIUtility.singleLineHeight;
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 2), new Color(0, 0, 0, 1));
        };
        listLevel.drawHeaderCallback = (Rect rect) => {
            EditorGUI.LabelField(rect, "Level Data");
        };
        listLevel.elementHeightCallback = delegate (int index) {
            var element = listLevel.serializedProperty.GetArrayElementAtIndex(index);
            var elementHeight = EditorGUI.GetPropertyHeight(element);
            var margin = EditorGUIUtility.standardVerticalSpacing;
            return elementHeight + margin + (EditorGUIUtility.singleLineHeight + 3) * 2;
        };
    }

    public override void OnInspectorGUI()
    {
        TileData script = (TileData)target;

        script.tileName = EditorGUILayout.TextField("Name", script.tileName);
        script.type = (TileData.TileType)EditorGUILayout.EnumPopup("Type", script.type);

        switch (script.type)
        {
            case TileData.TileType.Tradeable:
                if (script.levelData == null) script.levelData = new List<TileData.Level>();
                int levelNum = script.maxLevel + 1;
                if (levelNum > script.levelData.Count)
                {
                    for (int i = 0; i < levelNum; i++)
                    {
                        script.levelData.Add(new TileData.Level());
                    }
                }
                else if (levelNum < script.levelData.Count)
                {
                    int difference = script.levelData.Count - levelNum;
                    for (int i = 0; i < difference; i++)
                    {
                        script.levelData.RemoveAt(script.levelData.Count - 1);
                    }
                }
                script.maxLevel = EditorGUILayout.IntSlider("Max Level (10)", script.maxLevel, 0, 10);


                //for (int i = 0; i < script.levelData.Count; ++i)
                //{
                //    TileData.Level level = script.levelData[i];
                //    level.upgradePrice = EditorGUILayout.IntSlider(
                //        i == 0 ? "Buy Price (9999)" : $"Level-{i} Upgrade Price (9999)",
                //        level.upgradePrice, 0, 9999);
                //    level.charge = EditorGUILayout.IntSlider($"Level-{i} Charge (9999)", level.charge, 0, 9999);
                //    script.levelData[i] = level;
                //}
                serializedObject.Update();
                listLevel.DoLayoutList();
                serializedObject.ApplyModifiedProperties();
                break;
            case TileData.TileType.DrawEven:
                //script.price = EditorGUILayout.IntField("Tile Price", script.price);
                break;
        }

    }
}
#endif