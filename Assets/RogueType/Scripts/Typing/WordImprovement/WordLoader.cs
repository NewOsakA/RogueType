using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class WordLoader : MonoBehaviour
{
    public enum Difficulty
    {
        Easy,
        Medium,
        Hard
    }

    public Dictionary<Difficulty, List<string>> wordDict;

    void Awake()
    {
        wordDict = new Dictionary<Difficulty, List<string>>()
        {
            { Difficulty.Easy, new List<string>() },
            { Difficulty.Medium, new List<string>() },
            { Difficulty.Hard, new List<string>() }
        };

        TextAsset wordFile = Resources.Load<TextAsset>("oxford3000");

        foreach (string raw in wordFile.text.Split('\n'))
        {
            string word = raw.Trim().ToLower();
            if (string.IsNullOrEmpty(word)) continue;

            Difficulty diff = ClassifyWord(word);
            wordDict[diff].Add(word);
        }

        Debug.Log(
            $"Loaded Words → Easy:{wordDict[Difficulty.Easy].Count} | " +
            $"Medium:{wordDict[Difficulty.Medium].Count} | " +
            $"Hard:{wordDict[Difficulty.Hard].Count}"
        );
    }

    Difficulty ClassifyWord(string word)
    {
        if (word.Length <= 4)
            return Difficulty.Easy;

        if (word.Length <= 7)
            return Difficulty.Medium;

        return Difficulty.Hard;
    }


    public string GetAdaptiveWord(
        float[] zoneWeights,
        (float s, float m, float l) lenMix)
    {
        WordLengthBucket len = ChooseLength(lenMix);

        FingerZone chosenZone = ChooseZone(zoneWeights);

        return GetRandomWordByLengthAndZone(len, chosenZone);
    }

    WordLengthBucket ChooseLength((float s, float m, float l) lenMix)
    {
        float total = lenMix.s + lenMix.m + lenMix.l;

        if (total <= 0.0001f)
            return WordLengthBucket.Short;

        float r = Random.value * total;

        if (r < lenMix.s)
            return WordLengthBucket.Short;

        if (r < lenMix.s + lenMix.m)
            return WordLengthBucket.Medium;

        return WordLengthBucket.Long;
    }

    FingerZone ChooseZone(float[] zoneWeights)
    {
        if (zoneWeights == null || zoneWeights.Length == 0)
            return FingerZone.LeftPinky;

        float total = zoneWeights.Sum();

        if (total <= 0.0001f)
            return (FingerZone)Random.Range(0, zoneWeights.Length);

        float r = Random.value * total;
        float acc = 0f;

        for (int i = 0; i < zoneWeights.Length; i++)
        {
            acc += zoneWeights[i];
            if (r <= acc)
                return (FingerZone)i;
        }

        return (FingerZone)Random.Range(0, zoneWeights.Length);
    }

    public string GetRandomWordByLengthAndZone(
        WordLengthBucket len,
        FingerZone zone)
    {
        Difficulty diff = len switch
        {
            WordLengthBucket.Short => Difficulty.Easy,
            WordLengthBucket.Medium => Difficulty.Medium,
            WordLengthBucket.Long => Difficulty.Hard,
            _ => Difficulty.Easy
        };

        var list = wordDict[diff];
        if (list.Count == 0)
            return "";

        List<(string word, float score)> scored =
            new List<(string, float)>();

        foreach (var w in list)
        {
            int count = 0;

            foreach (char c in w)
            {
                if (FingerZoneMap.TryGetZone(c, out var z) && z == zone)
                    count++;
            }

            float score = (float)count / w.Length;

            if (score > 0f)
                scored.Add((w, score));
        }

        if (scored.Count == 0)
            return list[Random.Range(0, list.Count)];

        float totalWeight = scored.Sum(x => x.score);
        float r = Random.value * totalWeight;
        float acc = 0f;

        foreach (var item in scored)
        {
            acc += item.score;
            if (r <= acc)
                return item.word;
        }

        return scored[Random.Range(0, scored.Count)].word;
    }

    public string GetRandomWord(Difficulty difficulty)
    {
        var list = wordDict[difficulty];
        if (list.Count == 0) return "";

        return list[Random.Range(0, list.Count)];
    }

    public string GetRandomWordMixed(
        float easyW = 0.6f,
        float mediumW = 0.3f,
        float hardW = 0.1f)
    {
        float r = Random.value;

        Difficulty diff =
            r < easyW ? Difficulty.Easy :
            r < easyW + mediumW ? Difficulty.Medium :
            Difficulty.Hard;

        var list = wordDict[diff];
        if (list.Count == 0) return "";

        return list[Random.Range(0, list.Count)];
    }
}