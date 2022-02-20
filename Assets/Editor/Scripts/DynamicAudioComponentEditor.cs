using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

[CustomEditor(typeof(DynamicAudioComponent))]
[CanEditMultipleObjects]
public class DynamicAudioComponentEditor : Editor
{
    List<System.Type> SoundNodeTypes;
    string[] SoundNodeTypesStr;

    void OnEnable()
    {
        //SoundNodeTypes = typeof(SoundNodeBase).GetNestedTypes();
        var types = typeof(SoundNodeBase).Assembly.GetTypes().Where(t => t.BaseType == typeof(SoundNodeBase));
        SoundNodeTypes = new List<System.Type>();

        foreach(System.Type type in types)
        {
            if(type.IsSubclassOf(typeof(SoundNodeBase)))
            {
                SoundNodeTypes.Add(type);
            }
        }

        SoundNodeTypesStr = new string[SoundNodeTypes.Count];
        for(int i = 0; i < SoundNodeTypes.Count; i++)
        {
            SoundNodeTypesStr[i] = SoundNodeTypes[i].Name;
        }
    }

    private void OnDisable()
    {
        SoundNodeTypes = null;
        SoundNodeTypesStr = null;
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        // Add our custom button
        GenericMenu menu = new GenericMenu();
        foreach(string typeStr in SoundNodeTypesStr)
        {
            menu.AddItem(new GUIContent(typeStr), false, OnTypeSelected, typeof(SoundNodeBase).Assembly.GetType(typeStr));
        }

        if(EditorGUILayout.DropdownButton(new GUIContent("Add Sound Node"), FocusType.Passive))
        {
            menu.ShowAsContext();
        }

        serializedObject.ApplyModifiedProperties();
    }

    void OnTypeSelected(object selection)
    {
        System.Type type = (System.Type)selection;
        if (type != null)
        {
            SerializedProperty property = serializedObject.FindProperty("SoundNodes");
            if(!property.isArray)
            {
                return;
            }

            property.arraySize++;

            serializedObject.ApplyModifiedProperties();

            SerializedProperty elementProperty = property.GetArrayElementAtIndex(property.arraySize - 1);
            ScriptableObject instancedNode = CreateInstance(type); // Instanced variable??

            const string tmpDir = "Assets/Audio/Temp";
            if(!AssetDatabase.IsValidFolder(tmpDir))
            {
                AssetDatabase.CreateFolder("Assets/Audio", "Temp");
            }
            string qualifiedName = $"{tmpDir}/{serializedObject.targetObject.GetType().Name}_{serializedObject.targetObject.GetInstanceID()}_{type.Name}_{property.arraySize - 1}.asset";

            // Don't use a unique name because we want to be able to replace/update existing.
            AssetDatabase.CreateAsset(instancedNode, qualifiedName);
            elementProperty.objectReferenceValue = instancedNode;
            ((SoundNodeBase)instancedNode).SetOwningComponent(serializedObject.targetObject as DynamicAudioComponent);

            serializedObject.ApplyModifiedProperties();
        }

    }

    string[] GetDropdownContent()
    {
        return SoundNodeTypesStr;
    }
}