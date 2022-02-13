using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SoundBankEntry
{
    public AudioClip Clip = null;
    public bool CanRepeat = false;
    [Min(0)]
    public int RepeatInterval = 2;
    [Range(0.0f, 1.0f)]
    public float Weight = 0.75f;

    public SoundBankEntry(AudioClip clip, bool canRepeat, int repeatInterval, float weight)
    {
        Clip = clip;
        CanRepeat = canRepeat;
        RepeatInterval = repeatInterval;
        Weight = weight;
    }

}

// A unit of audio assets, including clips, events, metadata. These get loaded at the game's discretion for optimizaiton
[CreateAssetMenu(fileName = "SoundBank", menuName = "Audio/SoundBank"), System.Serializable]
public class SoundBank : ScriptableObject
{
    [SerializeField] private List<SoundBankEntry> Entries = new List<SoundBankEntry>();
    private int[] RecentlyPlayed = null;
    private float TotalWeights = 0.0f;

    // If you change the size of the entries, you need to restart the editor for RecentlyPlayed to be rebuilt!
    // Idiotic that there is no PostLoad/OnLoad method.
    public void Awake()
    {
        RecentlyPlayed = new int[Entries.Count];
        Debug.LogFormat("RecentlyPlayed init with {0} elements", Entries.Count);

        foreach(SoundBankEntry entry in Entries)
        {
            TotalWeights += entry.Weight;
        }
    }

    /* Gets a random sound from the sound bank
     * @weighted: Whether the selection respects the likelyhood weights assigned to each sound.
     * @canRepeat: Whether the selection respects the repetition rules for the sound - if true, a sound is allowed to repeat consecutively
     * @meta: Selects sounds that include all metadata provided.
     * */
    public AudioClip GetSound(bool weighted = true, bool canRepeat = false, string[] meta = null)
    {
        return SelectClip(weighted, canRepeat, meta);
    }

    AudioClip SelectClip(bool weighted, bool canRepeat, string[] meta)
    {
        if(!weighted && canRepeat && meta == null)
        {
            // Easiest case, just a pure random selection
            int idx = Random.Range(0, Entries.Count);
            if(idx < Entries.Count)
            {
                return Entries[idx].Clip;
            }
        }
        else
        {
            return SearchClip(weighted, canRepeat, meta);
        }

        return null;
    }

    AudioClip SearchClip(bool weighted, bool canRepeat, string[] meta)
    {
        float weightSum = 0;
        List<int> indices = GetPossibleEntries(out weightSum, weighted, canRepeat, meta);

        if(indices.Count == 0)
        {
            return null;
        }

        if(weighted)
        {
            float rand = Random.Range(0.0f, weightSum);

            for (int i = 0; i < indices.Count; i++)
            {
                SoundBankEntry entry = Entries[indices[i]];
                if (rand < entry.Weight)
                {
                    // Choose this
                    RecentlyPlayed[indices[i]] = entry.RepeatInterval;
                    return entry.Clip;
                }

                rand -= entry.Weight;
            }
        }
        else
        {
            int idx = Random.Range(0, indices.Count);
            return Entries[indices[idx]].Clip;
        }

        return null;
    }

    AudioClip SearchClip()
    {
        return SearchClip(true, false, null);
    }

    List<int> GetPossibleEntries(out float weightSum, bool weighted, bool canRepeat, string[] meta)
    {
        weightSum = 0;
        List<int> indices = new List<int>();

        for (int i = 0; i < Entries.Count; i++)
        {
            if (!canRepeat && !Entries[i].CanRepeat)
            {
                if ((RecentlyPlayed[i]) > 0)
                {
                    --RecentlyPlayed[i];
                    continue;
                }
            }

            if(weighted)
            {
                weightSum += Entries[i].Weight;
            }
            indices.Add(i);
        }

        return indices;
    }

    List<int> GetPossibleEntries(out float weightSum)
    {
        weightSum = 0;
        List<int> indices = new List<int>();

        for (int i = 0; i < Entries.Count; i++)
        {
            if (!Entries[i].CanRepeat)
            {
                if ((RecentlyPlayed[i]) > 0)
                {
                    --RecentlyPlayed[i];
                    continue;
                }

                weightSum += Entries[i].Weight;
                indices.Add(i);
            }
        }

        return indices;
    }
}
