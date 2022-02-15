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


public static class SerializedPropertyHelpers
{
    static public SerializedProperty FindPropertyRelativeFix(this SerializedProperty sp, string name, ref SerializedObject objectToApplyChanges)
    {
        SerializedProperty result;
        if (typeof(ScriptableObject).IsAssignableFrom(sp.GetFieldType()))
        {
            if (sp.objectReferenceValue == null) return null;
            if (objectToApplyChanges == null)
                objectToApplyChanges = new SerializedObject(sp.objectReferenceValue);
            result = objectToApplyChanges.FindProperty(name);
        }
        else
        {
            objectToApplyChanges = null;
            result = sp.FindPropertyRelative(name);
        }
        return result;
    }


    static public System.Type GetFieldType(this SerializedProperty property)
    {
        if (property.serializedObject.targetObject == null) return null;
        System.Type parentType = property.serializedObject.targetObject.GetType();
        System.Reflection.FieldInfo fi = parentType.GetField(property.propertyPath);
        string path = property.propertyPath;
        if (fi == null)
            return null;

        return fi.FieldType;
    }
};

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
    public readonly int conditionValue;
    public readonly bool bitflag;
    public EditConditionAttribute(string _conditionVar)
    {
        conditionVar = _conditionVar;
        conditionValue = 1;
        bitflag = false;
        order = 999; // Draw last, since this thing needs to override the result of any other decorator.
    }

    public EditConditionAttribute(string _conditionVar, int _conditionValue, bool _bitflag = false)
    {
        conditionVar = _conditionVar;
        conditionValue = _conditionValue;
        bitflag = _bitflag;
        order = 999; // Draw last, since this thing needs to override the result of any other decorator.
    }

    // By default, condtion is met if the property is not found - i.e user error of some sort.
    public bool IsConditionMet(SerializedProperty contextProperty)
    {
        if (conditionVar == null)
        {
            return true;
        }

        if(contextProperty == null)
        {
            return true;
        }

        SerializedObject serializedObj = contextProperty.serializedObject;
        SerializedProperty conditionProperty = contextProperty.FindPropertyRelative(conditionVar);

        // The hardcore approach, assemble the full path in case of nested containers.
        if(conditionProperty == null)
        {
            string prefixPath = contextProperty.propertyPath.TrimEnd(contextProperty.name.ToCharArray());
            conditionProperty = serializedObj.FindProperty($"{prefixPath}{conditionVar}");
        }

        if(conditionProperty != null)
        {
            //Debug.LogFormat("Found property {0}, int val {1}, condition val {2}", conditionProperty.name, conditionProperty.intValue, conditionValue);
            if(!bitflag)
            {
                return (conditionProperty.intValue == conditionValue);
            }
            else
            {
                return (conditionProperty.intValue & conditionValue) != 0;
            }
        }
        else
        {
            return true;
        }
    }
}

[CustomPropertyDrawer(typeof(EditConditionAttribute))]
public class EditConditionDrawer : PropertyDrawer
{
    public override float GetPropertyHeight(SerializedProperty property,
                                            GUIContent label)
    {
        EditConditionAttribute attr = attribute as EditConditionAttribute;

        if (attr.IsConditionMet(property))
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
        //string conditionVar = ((EditConditionAttribute)attribute).conditionVar;
        //SerializedProperty conditionProp = property.serializedObject.FindProperty(conditionVar);

        EditConditionAttribute attr = attribute as EditConditionAttribute;

        //if(conditionProp == null || (conditionProp.boolValue == true))
        if(attr.IsConditionMet(property))
        {
            //GUI.enabled = false;
            EditorGUI.PropertyField(position, property, label, true);
            //GUI.enabled = true;
        }
    }
}