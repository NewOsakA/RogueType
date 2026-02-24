using System.IO;
using UnityEngine;

public class TrainingDataLogger : MonoBehaviour
{
    public static TrainingDataLogger Instance;
    private string filePath;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        string projectRoot = Application.dataPath.Replace("/Assets", "");
        string dataDir = System.IO.Path.Combine(projectRoot, "TrainingData");

        if (!System.IO.Directory.Exists(dataDir))
        {
            System.IO.Directory.CreateDirectory(dataDir);
        }

        filePath = System.IO.Path.Combine(dataDir, "training_data.csv");

        if (!File.Exists(filePath))
        {
            File.WriteAllText(filePath,
                "wpm,accuracy,mistake_count,reaction_time_avg,avg_time_per_enemy,total_enemy,label\n"
            );
        }

        // Debug.Log("Training data path: " + filePath);
    }

    public void Log(
        TypingManager tm,
        GameManager gm,
        int label
    )
    {
        string line =
            $"{tm.GetWPM():F2}," +
            $"{tm.GetAccuracy():F3}," +
            $"{tm.GetMistakeCount()}," +
            $"{tm.GetReactionTimeAvg():F3}," +
            $"{gm.GetAvgTimePerEnemy():F3}," +
            $"{gm.GetTotalEnemy()}," +
            $"{label}\n";  // dummy

        File.AppendAllText(filePath, line);
        Debug.Log("[TRAIN DATA] " + line);
    }
}
