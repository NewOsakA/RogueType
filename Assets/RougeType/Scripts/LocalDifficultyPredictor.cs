using UnityEngine;

public class LocalDifficultyPredictor : MonoBehaviour
{
    private LogisticModelData model;
    private bool modelReady = false;

    void Awake()
    {
        LoadModel();
    }

    void LoadModel()
    {
        TextAsset json = Resources.Load<TextAsset>("difficulty_model");

        if (json == null)
        {
            Debug.LogError("difficulty_model.json not found");
            return;
        }

        model = JsonUtility.FromJson<LogisticModelData>(json.text);

        // Validate structure
        if (model == null ||
            model.mean == null || model.scale == null ||
            model.coefFlat == null ||
            model.intercept == null ||
            model.classes == null ||
            model.coefRows <= 0 || model.coefCols <= 0 ||
            model.coefFlat.Length != model.coefRows * model.coefCols)
        {
            Debug.LogError("Model loaded but structure invalid");
            return;
        }

        modelReady = true;
        Debug.Log("Local AI model READY");
    }

    public int Predict(PlayerData data)
    {
        if (!modelReady)
        {
            Debug.LogWarning("AI not ready → fallback difficulty");
            return 1; // default = medium
        }

        // Build feature vector (order must match training)
        float[] x = {
            data.wpm,
            data.combo_length,
            data.mistake_count,
            data.recent_accuracy,
            data.wave_number
        };

        // Standardize
        for (int i = 0; i < x.Length; i++)
        {
            x[i] = (x[i] - model.mean[i]) / model.scale[i];
        }

        // Compute logits
        float[] logits = new float[model.classes.Length];

        for (int c = 0; c < model.classes.Length; c++)
        {
            float sum = model.intercept[c];

            for (int i = 0; i < x.Length; i++)
            {
                // index in flattened coef
                int coefIndex = c * model.coefCols + i;
                sum += model.coefFlat[coefIndex] * x[i];
            }

            logits[c] = sum;
        }

        // ArgMax → class
        return ArgMax(logits);
    }

    int ArgMax(float[] values)
    {
        int bestIndex = 0;
        float bestValue = values[0];

        for (int i = 1; i < values.Length; i++)
        {
            if (values[i] > bestValue)
            {
                bestValue = values[i];
                bestIndex = i;
            }
        }

        return model.classes[bestIndex];
    }
}
