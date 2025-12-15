using System;

[Serializable]
public class LogisticModelData
{
    public float[] mean;
    public float[] scale;

    public float[] coefFlat;
    public int coefRows;
    public int coefCols;

    public float[] intercept;
    public int[] classes;
}
