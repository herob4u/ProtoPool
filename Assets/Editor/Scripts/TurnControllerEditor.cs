using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

//[CustomPropertyDrawer(typeof(TurnController.TurnStateEntry))]
public class TurnStateEntryDrawer : PropertyDrawer
{
    static string[] s_typesToStr = null;

    TurnStateEntryDrawer()
    {
        /*
        System.Type baseType = typeof(TurnState);
        Debug.Log("Building turn state types array...");

        System.Type[] allTypes = baseType.Assembly.GetTypes();
        List<string> typeStrs = new List<string>();

        foreach(System.Type type in allTypes)
        {
            if(type.IsSubclassOf(baseType) && !type.IsAbstract)
            {
                typeStrs.Add(type.Name);
            }
        }

        s_typesToStr = typeStrs.ToArray();
        Debug.Log("Build turn state types array");
        */
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        int selectedType = 0;
        //EditorGUI.BeginProperty(position, label, property);
        if (property != null)
        {
            //position = EditorGUILayout.BeginHorizontal();
            {
                EditorGUI.LabelField(position, "State");
                Rect selectorRect = GUILayoutUtility.GetLastRect();
                int newSelection = EditorGUI.Popup(selectorRect, selectedType, GetDropdownItems(property));
                if (newSelection != selectedType)
                {
                    selectedType = newSelection;
                    //DoAssignPin(property);
                }
            }
            //EditorGUILayout.EndHorizontal();
        }

        //EditorGUI.EndProperty();
    }

    protected string[] GetDropdownItems(SerializedProperty property)
    {
        return s_typesToStr;
    }
}

    /*
namespace Assets.Editor.Scripts
{
    public class TurnControllerEditor : ScriptableObject
    {
        [MenuItem("Tools/MyTool/Do It in C#")]
        static void DoIt()
        {
            EditorUtility.DisplayDialog("MyTool", "Do It in C# !", "OK", "");
        }
    }
}
    */