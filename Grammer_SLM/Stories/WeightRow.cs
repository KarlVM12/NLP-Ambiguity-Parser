public class WeightRow
{
    private string _word;

    private float _percent;

    public WeightRow(string word, float percent)
    {
        _word = word;
        _percent = percent;
    }

    public string GetWord()
    {
        return _word;
    }

    public float GetPercent()
    {
        return _percent;
    }
}