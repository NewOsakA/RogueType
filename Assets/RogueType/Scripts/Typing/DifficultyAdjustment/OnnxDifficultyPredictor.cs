using UnityEngine;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using Microsoft.ML.OnnxRuntime.Unity;
using System.Linq;

public class OnnxDifficultyPredictor : MonoBehaviour
{
    [Header("ONNX Model")]
    public OrtAsset modelAsset;

    private InferenceSession session;
    private string inputName;

    void Awake()
    {
        if (modelAsset == null)
        {
            Debug.LogError("ONNX model not assigned!");
            return;
        }

        session = new InferenceSession(modelAsset.bytes);
        inputName = session.InputMetadata.Keys.First();

        Debug.Log("ONNX model loaded successfully");
    }

    public int Predict(
        float wpm,
        float accuracy,
        float mistakeCount,
        float reactionTime,
        float avgTimePerEnemy)
    {
        if (session == null)
            return 1;

        float[] inputData =
        {
            wpm,
            accuracy,
            mistakeCount,
            reactionTime,
            avgTimePerEnemy
        };

        var tensor = new DenseTensor<float>(inputData, new[] { 1, 5 });

        using var results = session.Run(new[]
        {
            NamedOnnxValue.CreateFromTensor(inputName, tensor)
        });

        var output = results.First().AsEnumerable<long>().ToArray();

        return (int)output[0];
    }
}
