using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct RandomSoundEntry
{
    public AudioClip Clip;

    [Range(0.0f, 1.0f)]
    public float Weight;

    public bool CanRepeat;
}

[CreateAssetMenu(fileName = "RandomSoundNode", menuName = "Audio/RandomSoundNode"), System.Serializable]
public class RandomSoundNode : SoundNodeBase
{
    [SerializeField]
    public List<RandomSoundEntry> SoundEntries = new List<RandomSoundEntry>();

    /* How many turns before we can start repeating audio */
    [Min(1.0f)]
    public int RepeatInterval = 1;


    private int[] RecentlyPlayed;

    public override AudioClip GetOutput()
    {
        Debug.Log("Randomly selecting audio clip");

        if(SoundEntries.Count == 0)
        {
            return null;
        }

        return SelectClip();
    }

    protected override bool IsSourceNode()
    {
        return true;
    }

    AudioClip SelectClip()
    {
        float weightSum = 0;
        List<int> indices = GetPossibleEntries(out weightSum);

        float rand = Random.Range(0.0f, weightSum);

        for(int i = 0; i < indices.Count; i++)
        {
            RandomSoundEntry entry = SoundEntries[indices[i]];
            if (rand < entry.Weight)
            {
                // Choose this
                RecentlyPlayed[indices[i]] = RepeatInterval;
                return entry.Clip;
            }

            rand -= entry.Weight;
        }

        return null;
    }

    List<int> GetPossibleEntries(out float weightSum)
    {
        weightSum = 0;
        List<int> indices = new List<int>();

        for (int i = 0; i < SoundEntries.Count; i++)
        {
            if (!SoundEntries[i].CanRepeat)
            {
                if ((RecentlyPlayed[i]) > 0)
                {
                    --RecentlyPlayed[i];
                    continue;
                }

                weightSum += SoundEntries[i].Weight;
                indices.Add(i);
            }
        }

        return indices;
    }
    private void Awake()
    {
        RecentlyPlayed = new int[SoundEntries.Count];
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
