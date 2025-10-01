using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Grammar;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Windows;
using Directory = System.IO.Directory;
using File = System.IO.File;





public class BigramPrediction
{
    private Dictionary<string, SpeechUnit> _masterWordList;
    
    private Dictionary<string, SpeechUnit> _workingWordList;
    
    private List<GrammarUnit> _grammarPhraseList;
    
    private List<List<string>> _grammarRemainingPatterns;

    private string _theSentence;

    private List<PartOfSpeechWeighting> _partOfSpeechWeights;
    public BigramPrediction(Dictionary<string, SpeechUnit> masterWordList, Dictionary<string, SpeechUnit> workingWordList,
        List<GrammarUnit> grammarPhraseList, ref string theSentence)
    {
        _masterWordList = masterWordList;
        _workingWordList = workingWordList;
        _grammarPhraseList = grammarPhraseList;
        _theSentence = theSentence;

        TextAsset jsonText = Resources.Load<TextAsset>("bigram_model");
        string jsonContent = jsonText.text;

        _partOfSpeechWeights = JsonConvert.DeserializeObject<List<PartOfSpeechWeighting>>(jsonContent);
    }

    public List<object> Start()
    {
        UnknownProcess unknownProcess = new UnknownProcess(_masterWordList, _grammarPhraseList, _grammarRemainingPatterns, true);
        unknownProcess.SpeechUnitUnknownProcess(0);
        List<List<List<object>>> possibleTempPatterns = unknownProcess._tempPatternHolder;
        
        List<List<string>> remainingPatterns = new List<List<string>>();

        for (int i = 0; i < possibleTempPatterns.Count; i++)
        {
            List<List<object>> possibleTempPattern = possibleTempPatterns[i];
            
            foreach(List<object> obj in possibleTempPattern)
            {
                remainingPatterns[i].Add(obj[0] as string);
            }
        }


        List<string> patternPrediction = MakePredictionOnRemainingPatterns(remainingPatterns);

        // no patternPredication detected
        if (patternPrediction.Count == 0 )
        {
            return new List<object>() { false };
        }

        if (_masterWordList.ElementAt(0).Value._definiteType != "N/A")
        {
            patternPrediction.RemoveAt(0); //first word is already definite
        }

        int patternPredictionIndex = 0;
        
        Dictionary<string, SpeechUnit> masterWordList = JsonConvert.DeserializeObject<Dictionary<string, SpeechUnit>>(JsonConvert.SerializeObject(_masterWordList));

        foreach (KeyValuePair<string, SpeechUnit> entry in masterWordList)
        {
            if (!entry.Value.IsDefiniteTypeSet() && patternPredictionIndex < patternPrediction.Count)
            {
                entry.Value._definiteType = patternPrediction[patternPredictionIndex];
                patternPredictionIndex++;
            }
        }

        string theSentence = JsonConvert.DeserializeObject<string>(JsonConvert.SerializeObject(_theSentence));

        List<GrammarUnit> grammarPhraseList = new List<GrammarUnit>();
        
        PreGrammar preGrammar = new PreGrammar();
        Dictionary<string, SpeechUnit> workingWordList = preGrammar.Setup(masterWordList);
            
        PhraseIdentifier phraseIdentifier = new PhraseIdentifier(masterWordList, workingWordList, grammarPhraseList);
        phraseIdentifier.Start();    
        
        PhraseChecker phraseChecker = new PhraseChecker(masterWordList, workingWordList, grammarPhraseList, theSentence);
        List<object> result = phraseChecker.BeginAmbiguityCheck();

        theSentence = result[0] as string;
        
        if (result.Count > 2)
        {
            grammarPhraseList = (List<GrammarUnit>)result[3];
            masterWordList = (Dictionary<string, SpeechUnit>)result[4];
        }
        
        unknownProcess = new UnknownProcess(masterWordList, grammarPhraseList, new List<List<string>>());
        unknownProcess.Start();
        
        workingWordList = JsonConvert.DeserializeObject<Dictionary<string, SpeechUnit>>(JsonConvert.SerializeObject(masterWordList));
        
        phraseIdentifier = new PhraseIdentifier(masterWordList, workingWordList, grammarPhraseList);
        phraseIdentifier.Start();

        phraseChecker = new PhraseChecker(masterWordList, workingWordList, grammarPhraseList, theSentence);
        bool unknownsPresent = phraseChecker.CheckForAmbiguity();

        if (!unknownsPresent)
        {
            return new List<object>(){true, _theSentence, _workingWordList, _grammarPhraseList, _masterWordList};

            //BIGRAM SUCCESS
        }
        else
        {

            return new List<object>() { false };
            //BIGRAM FAILED 
        }
    }

    public List<string> MakePredictionOnRemainingPatterns(List<List<string>> remainingPatterns)
    {
        float maxProbability = -1;
        List<string> bestSequence = new List<string>();

        foreach (List<string> pattern in remainingPatterns)
        {
            float probability = CalculateSequenceProbability(pattern);

            if (probability > maxProbability)
            {
                maxProbability = probability;
                bestSequence = pattern;
            }
        }
        
        return bestSequence;
    }

    private float CalculateSequenceProbability(List<string> pattern)
    {
        float probability = 1.0f;

        for (int i = 0; i < pattern.Count; i++)
        {
            string currentTag = pattern[i];
            string nextTag = null;
            if (i < pattern.Count - 1) nextTag = pattern[i + 1];
            if (nextTag == null) break;

            probability += GetProbabilityFromTags(currentTag, nextTag);
        }
        
        return probability;
    }

    private float GetProbabilityFromTags(string currentTag, string nextTag)
    {
        foreach (PartOfSpeechWeighting partOfSpeechWeighting in _partOfSpeechWeights)
        {
            if (partOfSpeechWeighting._type == currentTag && partOfSpeechWeighting._values.ContainsKey(nextTag))
            {
                return (float)partOfSpeechWeighting._values[nextTag];
            }
        }
        
        return 0.0001f;
    }
}