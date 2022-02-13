using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

class Pool
{
    public const int NUM_BALLS = 16;
    public const string DefaultPoolTableAssetPath = "";
    public const string DefaultPoolCueAssetPath = "";
}

/* Typed, in order by the numbers they show */
public enum EPoolBallType
{
    Cue = 0,
    Solid_Yellow,
    Solid_Blue,
    Solid_Red,
    Solid_Violet,
    Solid_Orange,
    Solid_Green,
    Solid_Maroon,
    Solid_Black,

    Stripe_Yellow,
    Stripe_Blue,
    Stripe_Red,
    Stripe_Violet,
    Stripe_Orange,
    Stripe_Green,
    Stripe_Maroon,
};

[System.Serializable]
public struct PoolBallDescriptor
{
    public Texture2D BallTexture;
    [ReadOnly] public EPoolBallType BallType;

    public PoolBallDescriptor(Texture2D texture, EPoolBallType ballType)
    {
        BallTexture = texture;
        BallType = ballType;
    }
}

[System.Serializable]
public struct PoolGameDefaults
{
    public Mesh DefaultCueMesh;
    public Material DefaultCueMaterial;
    public GameObject DefaultPoolTablePrefab;
    public GameObject DefaultPoolCuePrefab;

    public PoolGameDefaults(Mesh cueMesh, Material cueMat, GameObject poolTableObj, GameObject poolCueObj)
    {
        DefaultCueMesh = cueMesh;
        DefaultCueMaterial = cueMat;
        DefaultPoolTablePrefab = poolTableObj;
        DefaultPoolCuePrefab = poolCueObj;
    }
}

[System.Serializable]
public struct ImpactEventInfo
{
    public GameObject OtherObject;
    public GameObject Instigator;
    public Vector3 Impulse;
    public ContactPoint ImpactPoint;
}

public class ReadOnlyAttribute : PropertyAttribute
{

}

[CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
public class ReadOnlyDrawer : PropertyDrawer
{
    public override float GetPropertyHeight(SerializedProperty property,
                                            GUIContent label)
    {
        return EditorGUI.GetPropertyHeight(property, label, true);
    }

    public override void OnGUI(Rect position,
                               SerializedProperty property,
                               GUIContent label)
    {
        GUI.enabled = false;
        EditorGUI.PropertyField(position, property, label, true);
        GUI.enabled = true;
    }
}

public class EditConditionAttribute : PropertyAttribute
{
    public readonly string conditionVar;
    public EditConditionAttribute(string _conditionVar)
    {
        conditionVar = _conditionVar;
        order = 999; // Draw last, since this thing needs to override the result of any other decorator.
    }
}

[CustomPropertyDrawer(typeof(EditConditionAttribute))]
public class EditConditionDrawer : PropertyDrawer
{
    public override float GetPropertyHeight(SerializedProperty property,
                                            GUIContent label)
    {
        string conditionVar = ((EditConditionAttribute)attribute).conditionVar;
        SerializedProperty conditionProp = property.serializedObject.FindProperty(conditionVar);

        if (conditionProp == null || (conditionProp.boolValue == true))
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }
        else
        {
            return 0;
        }
    }

    public override void OnGUI(Rect position,
                               SerializedProperty property,
                               GUIContent label)
    {
        string conditionVar = ((EditConditionAttribute)attribute).conditionVar;
        SerializedProperty conditionProp = property.serializedObject.FindProperty(conditionVar);

        if(conditionProp == null || (conditionProp.boolValue == true))
        {
            //GUI.enabled = false;
            EditorGUI.PropertyField(position, property, label, true);
            //GUI.enabled = true;
        }
    }
}