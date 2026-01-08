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
}