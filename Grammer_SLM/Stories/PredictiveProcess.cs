using System;
using System.Collections.Generic;
using UnityEngine;
using Grammar;
using Newtonsoft.Json;

public class PredictiveProcess {
    public string _prompt;
    public GrammarManager _grammar;
    public List<Classifier> _classifierList = new List<Classifier>();
    public string _guessedClassifierTerm;
    public float _guessedClassifierWeight;

    public PredictiveProcess(string prompt, GrammarManager grammar){
        _prompt = prompt;
        _grammar = grammar;
    }

    public void process(){
        PredictiveWeighting weighting = new PredictiveWeighting();
        
        _classifierList = weighting.CreateClassifiers();

        bool useOnlyObjects = true;
        if (_grammar._masterWordList.Count == 1){
            useOnlyObjects = false;
        }
        // use only objects if there is ambiguity
        foreach (GrammarUnit grammarUnit in _grammar._grammarPhraseList){

            if (grammarUnit._phraseType == "unknown phrase"){
                useOnlyObjects = false;
            }
        }
        List<string> words = weighting.CreateInputWords(_grammar,_prompt, useOnlyObjects);

        Debug.Log(JsonConvert.SerializeObject(words));

        

        weighting.WeightingProcedure(_classifierList, words);

        Tuple<string, float> result = weighting.GuessClassifierWithWeight(_classifierList);

        Debug.Log(":" + JsonConvert.SerializeObject(_classifierList) + " : " + JsonConvert.SerializeObject(result));

        _guessedClassifierTerm = result.Item1;
        _guessedClassifierWeight = result.Item2;
    }

    public float checkExitStatus(){
        float exitWeight = 0.0f;
        foreach (Classifier classifier in _classifierList){
            if (classifier._term == "exit"){
                exitWeight = classifier._finalWeight;
            }
        }
        return exitWeight;
    }


}

