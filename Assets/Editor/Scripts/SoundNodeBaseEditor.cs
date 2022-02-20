using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;

public class SoundNodeBaseEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

[CustomPropertyDrawer(typeof(SoundNodeBase.InputPin))]
public class InputPinDrawer : PropertyDrawer
{
    int selectedInputNode = 0;
    
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        if(property != null)
        {
            EditorGUI.LabelField(position, new GUIContent(property.FindPropertyRelative("Label").stringValue));

            Rect selectorRect = new Rect(position.x + 100.0f, position.y, position.width * 0.5f, position.height);

            int newSelection = EditorGUI.Popup(selectorRect, selectedInputNode, GetInputPins(property));
            if(newSelection != selectedInputNode)
            {
                selectedInputNode = newSelection;
                DoAssignPin(property);
            }
            else
            {
                // If selection didn't change, we still want to validate that object did not modify its sound nodes, causing us to be linked with
                // a now invalid or unexpected input node.
                SoundNodeBase prevNode = GetSoundNodeAt(selectedInputNode, property);
                SoundNodeBase currNode = property.FindPropertyRelative("InputNode").objectReferenceValue as SoundNodeBase;

                // Revert back to None
                if(prevNode != currNode)
                {
                    selectedInputNode = 0;
                    DoAssignPin(property);
                }
            }
        }

        EditorGUI.EndProperty();
    }

    private string[] GetInputPins(SerializedProperty property)
    {
        SoundNodeBase node = property.serializedObject.targetObject as SoundNodeBase;

        if(node)
        {
            if(node.OwningComponent)
            {
                int size = node.OwningComponent.GetSoundNodes().IndexOf(node) + 1;
                if(size <= 0)
                {
                    return new string[0];
                }

                string[] nodeOptions = new string[size];
                nodeOptions[0] = "None";
                for(int i = 1; i < size; i++)
                {
                    nodeOptions[i] = $"[{i-1}] {node.OwningComponent.GetSoundNodes()[i-1].GetType().Name}";
                }

                return nodeOptions;
            }
        }
        return new string[0];
    }

    private void DoAssignPin(SerializedProperty property)
    {
        SoundNodeBase node = property.serializedObject.targetObject as SoundNodeBase;
        if(!node)
        {
            return;
        }

        if(!node.OwningComponent)
        {
            return;
        }

        SerializedProperty inputNodeProperty = property.FindPropertyRelative("InputNode");
        if(inputNodeProperty == null)
        {
            Debug.LogWarning("The property 'InputNode' does not exist");
            return;
        }

        // None selection, clear the mapping
        if(selectedInputNode <= 0)
        {
            inputNodeProperty.objectReferenceValue = null;
            return;
        }

        SoundNodeBase inputNode = node.OwningComponent.GetSoundNodes()[selectedInputNode -1];
        inputNodeProperty.objectReferenceValue = inputNode;
        inputNodeProperty.serializedObject.Update();
        inputNodeProperty.serializedObject.ApplyModifiedProperties();
        // Notify...
    }

    SoundNodeBase GetSoundNodeAt(int index, SerializedProperty property)
    {
        // Case of "None" selection
        if(index <= 0)
        {
            return null;
        }

        SoundNodeBase node = property.serializedObject.targetObject as SoundNodeBase;
        if(!node)
        {
            return null;
        }

        // Object modified, and we are now out of range
        if(node.OwningComponent.GetSoundNodes().Count <= index)
        {
            return null;
        }

        return node.OwningComponent.GetSoundNodes()[selectedInputNode - 1];
    }
}