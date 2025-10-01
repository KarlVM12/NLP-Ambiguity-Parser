using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using Newtonsoft.Json;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

public class PredictiveWeighting
{
    public List<Classifier> CreateClassifiers()
    {
        List<Classifier> classifiers = new List<Classifier>();

        classifiers.Add(new Classifier("schedule"));
        classifiers.Add(new Classifier("navigate"));
        classifiers.Add(new Classifier("query"));
        classifiers.Add(new Classifier("chat"));
        classifiers.Add(new Classifier("quick_command"));
        classifiers.Add(new Classifier("exit"));

        return classifiers;
    }

    public List<string> CreateInputWords(GrammarManager grammarManager, string sentence, bool useOnlyObjects = true)
    {
        List<string> words = new List<string>();
        
        if (!useOnlyObjects)
        {
            sentence = sentence.ToLower();
            words = sentence.Split(" ").ToList();

            for (int i = 0; i < words.Count; i++)
            {
                words[i] = Regex.Replace(words[i], @"[^A-Za-z0-9\-]", "");
            }

            return words;
        }

        //GrammarManager grammarManager = new GrammarManager(sentence);

        if (grammarManager._sentenceObject._mainVerbObject != null)
        {
            words.Add(grammarManager._sentenceObject._mainVerbObject._speechUnits[0]._display);
        }

        if (grammarManager._sentenceObject._mainObject != null)
        {
            words.Add(grammarManager._sentenceObject._mainObject._speechUnits[0]._display);
        }

        if (grammarManager._sentenceObject._subject != null)
        {
            words.Add(grammarManager._sentenceObject._subject._speechUnits[0]._display);
        }

        foreach (NounObject directObject in grammarManager._sentenceObject._directObjects)
        {
            words.Add(directObject._mainNounUnit._display);
        }

        return words;
    }

    public void WeightingProcedure(List<Classifier> classifiers, List<string> words)
    {
        foreach (Classifier classifier in classifiers)
        {
            List<WeightRow> weightRows = GetWeights(classifier);

            foreach (WeightRow weightRow in weightRows)
            {
                if (words.Contains(weightRow.GetWord()))
                {
                    classifier.AddMatchingWord(weightRow.GetWord());
                    classifier.AddWeight(weightRow.GetPercent());
                }
            }

            classifier.SetAverageWeight();
            classifier.SetWordCountWeight(words.Count);
            classifier.SetFinalWeight();
        }
    }

    private List<WeightRow> GetWeights(Classifier classifier)
    {
        Tuple<string, int> tokenTally = GrammarData.GetTokenTallyForClassifierTerm(classifier.GetTerm());
        string token = tokenTally.Item1;
        int tally = tokenTally.Item2;
        
        if (tally != 0)
        {
            return GrammarData.GetWeightRowsForPredictiveTermWord(token);
        }
        
        return null;
    }

    public Tuple<string, float> GuessClassifierWithWeight(List<Classifier> classifiers)
    {
        float finalWeight = 0.0f;
        string leader = "";

        foreach (Classifier classifier in classifiers)
        {
            if (classifier.GetFinalWeight() > finalWeight)
            {
                finalWeight = classifier.GetFinalWeight();
                leader = classifier.GetTerm();
            }
        }

        Debug.Log("Leader "+leader);

        return new Tuple<string, float>(leader, finalWeight);
    }
}