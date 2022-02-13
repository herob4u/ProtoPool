using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
/* An audio source that picks sounds dynamically from a sound bank, with the ability to add nodes that affect the choice and processing of audio */
public class DynamicAudioComponent : MonoBehaviour
{
    // The audio source we use to output the final clip
    AudioSource AudioSource;

    [SerializeField]
    private List<SoundNodeBase> SoundNodes = new List<SoundNodeBase>();

    Dictionary<string, int> IntParameters = new Dictionary<string, int>();

    // Start is called before the first frame update
    void Start()
    {
    }

    private void Awake()
    {
        AudioSource = GetComponent<AudioSource>();
        if(!AudioSource)
        {
            Debug.LogError("DynamicAudioComponent cannot exist without an audio source - destroying self...");
            Destroy(this);

            return;
        }

        BuildParameterLists();
    }
    // Update is called once per frame
    void Update()
    {
        
    }

    // Goes through each node and builds the list of parameters that can be set on this audio component. Otherwise, the list of parameters can grow indefinitely at the mercy of the caller
    void BuildParameterLists()
    {
        foreach(SoundNodeBase node in SoundNodes)
        {
            node.SetOwningComponent(this);
            if(node is SelectorSoundNode)
            {
                string param = ((SelectorSoundNode)node).Parameter;
                if(param != null && param.Length > 0)
                {
                    IntParameters.Add(param, ((SelectorSoundNode)node).DefaultValue);
                    Debug.LogFormat("Added int param {0}", param);
                }
            }
        }
    }

    public void SetParameter(string paramName, int value)
    {
        IntParameters[paramName] = value;
    }

    public int GetIntParameter(string paramName)
    {
        if(IntParameters.ContainsKey(paramName))
        {
            return IntParameters[paramName];
        }

        return -1;
    }

    public List<SoundNodeBase> GetSoundNodes() { return SoundNodes; }

    public void PlaySound()
    {
        SoundNodeBase node = SoundNodes[SoundNodes.Count - 1];
        if (!node)
        {
            Debug.LogErrorFormat("Unexpected invalid sound node at last index, in game object{0}", gameObject.name);
            return;
        }

        PlayClip(node.GetOutput());
        /*
        for (int i = SoundNodes.Count - 1; i >= 0; --i)
        {
            SoundNodeBase node = SoundNodes[i];
            if(!node)
            {
                Debug.LogErrorFormat("Unexpected invalid sound node at idx {0}, in game object {1}", i, gameObject.name);
                return;
            }

            PlayClip(node.GetOutput());
        }
        */
    }

    protected bool PlayClip(AudioClip clip)
    {
        if(clip)
        {
            AudioSource.clip = clip;
            AudioSource.Play();

            return true;
        }

        return false;
    }
}
