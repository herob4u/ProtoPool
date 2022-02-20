using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class ReadOnlyAttribute : PropertyAttribute
{

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
}