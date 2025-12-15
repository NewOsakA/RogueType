using System.Collections.Generic;
using UnityEngine;

public class WordLoader : MonoBehaviour
{
    public List<string> wordList;

    void Awake()
    {
        // Load the text file (note: do not include the .txt extension when using Resources.Load)
        TextAsset wordFile = Resources.Load<TextAsset>("oxford3000");
        // Split by newlines and add them to the wordList
        wordList = new List<string>(wordFile.text.Split(new char[] { '\n' }, System.StringSplitOptions.RemoveEmptyEntries));
    }

    // Returns a random word from the list, trimmed and in lowercase.
    public string GetRandomWord()
    {
        int randomIndex = Random.Range(0, wordList.Count);
        return wordList[randomIndex].Trim().ToLower();
    }
}
