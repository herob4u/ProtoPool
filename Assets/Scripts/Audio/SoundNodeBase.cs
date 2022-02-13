using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class SoundNodeBase : ScriptableObject
{
    [System.Serializable]
    // For nodes that require an input from a preceding node.
    public struct InputPin
    {
        public SoundNodeBase InputNode;
        public string Label;

        public InputPin(string label, SoundNodeBase inputNode)
        {
            Label = label;
            InputNode = inputNode;
        }

    }

    // Set at runtime
    [SerializeField]
    protected List<InputPin> InputPins = new List<InputPin>();

    public DynamicAudioComponent OwningComponent { get; private set; }

    public abstract AudioClip GetOutput();

    protected abstract bool IsSourceNode();

    protected virtual void InitPins() { }

    public SoundNodeBase()
    {
        InitPins();
    }

    private void OnValidate()
    {
        if(IsSourceNode())
        {
            InputPins.Clear();
        }
    }

    public void SetOwningComponent(DynamicAudioComponent component)
    {
        OwningComponent = component;
    }

    protected void AddPin(string label = null)
    {
        if(label == null)
        {
            label = InputPins.Count.ToString();
        }

        InputPins.Add(new InputPin(label, null));
    }

    protected void RemovePin()
    {
        if(InputPins.Count > 0)
        {
            InputPins.RemoveAt(InputPins.Count - 1);
        }
    }

    protected void RemovePin(int idx)
    {
        if(InputPins.Count > 0)
        {
            if(idx >= 0 && idx < InputPins.Count)
            {
                InputPins.RemoveAt(idx);
            }
        }
    }
}
