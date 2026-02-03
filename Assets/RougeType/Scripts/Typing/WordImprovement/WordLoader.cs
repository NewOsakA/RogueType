using System.Collections.Generic;
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

        Debug.Log($"Loaded Words → Easy:{wordDict[Difficulty.Easy].Count} " +
                  $"Medium:{wordDict[Difficulty.Medium].Count} " +
                  $"Hard:{wordDict[Difficulty.Hard].Count}");
    }

    Difficulty ClassifyWord(string word)
    {
        if (word.Length <= 4)
            return Difficulty.Easy;

        if (word.Length <= 7)
            return Difficulty.Medium;

        return Difficulty.Hard;
    }

    public string GetRandomWord(Difficulty difficulty)
    {
        var list = wordDict[difficulty];
        if (list.Count == 0) return "";

        return list[Random.Range(0, list.Count)];
    }

    public string GetRandomWordByLengthAndZone(WordLengthBucket len, FingerZone zone)
    {
        // select difficulty base on lenght
        Difficulty diff = len switch
        {
            WordLengthBucket.Short => Difficulty.Easy,
            WordLengthBucket.Medium => Difficulty.Medium,
            WordLengthBucket.Long => Difficulty.Hard,
            _ => Difficulty.Easy
        };

        var list = wordDict[diff];
        if (list.Count == 0) return "";

        // filter by word in fingerzone
        // check that it have contain character map to that zone at least 1 character
        List<string> filtered = new List<string>();
        foreach (var w in list)
        {
            bool ok = false;
            foreach (char c in w)
            {
                if (FingerZoneMap.TryGetZone(c, out var z) && z == zone)
                {
                    ok = true;
                    break;
                }
            }
            if (ok) filtered.Add(w);
        }
        // if not find the any zone fallback to list
        var finalList = filtered.Count > 0 ? filtered : list;
        return finalList[Random.Range(0, finalList.Count)];
    }

    public string GetRandomWordMixed(float easyW = 0.6f, float mediumW = 0.3f, float hardW = 0.1f)
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