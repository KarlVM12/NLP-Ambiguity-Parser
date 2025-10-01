using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using UnityEditor;

[DataContract]
public class Classifier
{
    [DataMember]
    public string _term;

    [DataMember]
    private int _wordCount = 0;

    [DataMember]
    private float _wordCountWeight = 0.0f;

    [DataMember]
    private float _averageWeight = 0.0f;

    [DataMember]
    public float _finalWeight = 0.0f;

    [DataMember]
    private List<string> _matchingWords = new List<string>();

    [DataMember]
    private List<float> _weightHolder = new List<float>();
    
    public Classifier(string term)
    {
        _term = term;
    }

    public string GetTerm()
    {
        return _term;
    }

    public float GetFinalWeight()
    {
        return _finalWeight;
    }

    public void AddMatchingWord(string word)
    {
        _matchingWords.Add(word);

        _wordCount++;
    }

    public void AddWeight(float weight)
    {
        _weightHolder.Add(weight);
    }

    public void SetAverageWeight()
    {
        float totalWeight = _weightHolder.Sum();

        if (_weightHolder.Count != 0)
        {
            _averageWeight = totalWeight / _weightHolder.Count;
        }
    }

    public void SetWordCountWeight(int wordCount)
    {
        if (wordCount != 0)
        {
            _wordCountWeight = _wordCount / (float)wordCount;
        }
    }

    public void SetFinalWeight()
    {
        if (_averageWeight != 0 && _wordCountWeight != 0)
        {
            float wordCountPercent = .40f * _wordCountWeight;
            float averageWeightPercent = .60f * _averageWeight;

            _finalWeight = wordCountPercent + averageWeightPercent;

            return;
        }

        if (_averageWeight != 0 && _wordCountWeight == 0.0f)
        {
            _finalWeight = _averageWeight;
            
            return;
        }

        if (_averageWeight == 0 && _wordCountWeight != 0)
        {
            _finalWeight = _wordCountWeight;

            return;
        }

        _finalWeight = 0.0f;
    }
}