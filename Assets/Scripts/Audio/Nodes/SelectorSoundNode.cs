using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* Selects a node by switching on an int/enum */
public class SelectorSoundNode : SoundNodeBase
{
    public string Parameter;
    public int DefaultValue = -1;

    public override AudioClip GetOutput()
    {
        int selector = ParseParameter();
        if(selector < 0)
        {
            selector = DefaultValue;
        }

        SoundNodeBase inputNode = null;
        if(InputPins != null && selector >= 0 && selector < InputPins.Count)
        {
            inputNode = InputPins[selector].InputNode;
        }

        if(inputNode)
        {
            return inputNode.GetOutput();
        }

        return null;
    }

    protected override bool IsSourceNode()
    {
        return false;
    }

    protected override void InitPins()
    {
        base.InitPins();

        AddPin("0");
    }

    int ParseParameter()
    {
        if(Parameter == null) { return -1; }

        return OwningComponent.GetIntParameter(Parameter);
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
