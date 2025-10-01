using System.Collections.Generic;
using System.Data;
using Grammar;
using UnityEngine;
using System.Linq;
using Newtonsoft.Json;
using Unity.VisualScripting;

public class QuickCommandObject : BaseStoryObject
{
    public string _primaryObject;
    public string _characteristic;
    public string _updatedValue;
    public string _action;
    public bool _needsNewValue;
    public bool _containsValue;
    private ResponseManager _responseManager;
    
    public QuickCommandObject(string primaryObject, string characteristic, string updatedValue, string action, bool needsNewValue )
    {
        _containsValue = false;
        _action = action;
        if (_action == "update"){
            _primaryObject = primaryObject;
            _characteristic = characteristic;
            _updatedValue = updatedValue;

            _containsValue = needsNewValue;
        }

        if (_primaryObject == ""){
            _missingComponents.Add("primary object");
        }

        if (_characteristic == ""){
            _missingComponents.Add("characteristic");
        }

        if (_updatedValue == ""){
            if (_containsValue){
                _missingComponents.Add("updated value");
            }
        }

        _captainResponse = GetCaptainResponse();
        
    }

    private string GetCaptainResponse()
    {
        if (string.IsNullOrEmpty(_primaryObject))
        {
            return EmptyPrimaryObjectResponseQueue();
        }

        if (string.IsNullOrEmpty(_characteristic))
        {
            return EmptyCharacteristicResponseQueue();
        }

        if (string.IsNullOrEmpty(_updatedValue))
        {
            if (!_containsValue)
            {
                return CompleteResponseQueueWithoutValue();
            }
            else
            {
                return EmptyValueResponseQueue();
            }
        }
        else
        {
            return CompleteResponseQueueWithValue();
        }

    }

    public string EmptyPrimaryObjectResponseQueue()
    {
        _responseManager = new ResponseManager("quickCommand", "emptyPrimaryObject", "set1");
        _responseManager.Set("action", _action);

        return _responseManager.Output();
    }

    public string EmptyCharacteristicResponseQueue()
    {
        _responseManager = new ResponseManager("quickCommand", "emptyCharacteristic", "set1");
        _responseManager.Set("primaryObject", _primaryObject);
        return _responseManager.Output();
    }

    public string EmptyValueResponseQueue()
    {
        _responseManager = new ResponseManager("quickCommand", "emptyValue", "set1");
        _responseManager.Set("primaryObject", _primaryObject);
        _responseManager.Set("characteristic", _characteristic);
        return _responseManager.Output();
    }

    public string CompleteResponseQueueWithoutValue()
    {
        _responseManager = new ResponseManager("quickCommand", "completeWithoutValue", "set1");
        _responseManager.Set("primaryObject", _primaryObject);
        _responseManager.Set("characteristic", _characteristic);
        return _responseManager.Output();
    }

    public string CompleteResponseQueueWithValue()
    {
        _responseManager = new ResponseManager("quickCommand", "completeWithoutValue", "set1");
        _responseManager.Set("primaryObject", _primaryObject);
        _responseManager.Set("characteristic", _characteristic);
        _responseManager.Set("updatedValue", _updatedValue);
        return _responseManager.Output();
    }
         
    public override void UpdatePrompt(string prompt, GrammarManager grammarManager)
    {
        bool hasNoPrimaryObject = false;
        bool hasNoCharacteristic = false;
        bool hasNoUpdatedValue = false;
        bool needsUpdatedValue = false;

        if (_containsValue){
            needsUpdatedValue = true;
        }

 
        foreach (string missingComponent in _missingComponents)
        {
            switch (missingComponent)
            {
                case "primary object":
                    hasNoPrimaryObject = true;
                    break;
                case "characteristic":
                    hasNoCharacteristic = true;
                    break;
                case "updated value":
                    hasNoUpdatedValue = true;
                    break;
            }
        }
        Debug.Log("COMPONENTS: " + _missingComponents);

        if (hasNoPrimaryObject){
            FillPrimaryObject(grammarManager);
        }

        if (hasNoCharacteristic){
            FillCharacteristic(grammarManager);
        }

        if (hasNoUpdatedValue){
            if (needsUpdatedValue){
                FillUpdatedValue(grammarManager);
            }
        }

       _captainResponse = GetCaptainResponse();
        base.UpdatePrompt(prompt, grammarManager);
    }

    public void FillPrimaryObject(GrammarManager grammar)
{
    bool found = false;
    bool needsValue = false;

    if (_action == "update")
    {
        var updateTerms = GetKeyTerms("object", grammar._sentenceObject);

        foreach (GrammarPhraseObject objectPhrase in grammar._sentenceObject._objectPhraseList)
        {
            foreach (var speechUnit in objectPhrase._speechUnits)
            {
                foreach (var updateTerm in updateTerms)
                {
                    var key = updateTerm.Key;
                    foreach (var term in updateTerm.Value)
                    {
                        if (speechUnit._display == term || speechUnit._display == key)
                        {
                            _missingComponents.Remove("primary object");
                            _primaryObject = speechUnit._display;

                            if (key == "profile" || key == "layout" || speechUnit._type == "entity" || speechUnit._type == "phonetic")
                            {
                                _containsValue = true;
                                needsValue = true;
                                found = true;
                            }
                        }
                    }
                }
            }
        }

        if (!found)
        {
            var characteristicTerms = GetKeyTerms("characteristic", grammar._sentenceObject);

            foreach (GrammarPhraseObject objectPhrase in grammar._sentenceObject._objectPhraseList)
            {
                foreach (SpeechUnit speechUnit in objectPhrase._speechUnits)
                {
                    foreach (var characteristicTerm in characteristicTerms)
                    {
                        var key = characteristicTerm.Key;
                        foreach (var term in characteristicTerm.Value)
                        {
                            if (speechUnit._display == term)
                            {
                                _missingComponents.Remove("characteristic");
                                _characteristic = speechUnit._display;

                                if (key != "entity")
                                {
                                    _missingComponents.Remove("primary object");
                                    _primaryObject = key;
                                }

                                if (key == "profile" || key == "layout" || key == "entity")
                                {
                                    if (!_missingComponents.Contains("updated value"))
                                    {
                                        _containsValue = true;
                                        needsValue = true;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        if (needsValue)
        {
            if (!_missingComponents.Contains("updated value"))
            {
                _missingComponents.Add("updated value");
            }
        }

        _captainResponse = GetCaptainResponse();
    }
}


    public void FillCharacteristic(GrammarManager grammar)
    {
        bool needsValue = false;

        if (_action == "update")
        {

            var characteristicTerms = GetKeyTerms("characteristic", grammar._sentenceObject);

            foreach (GrammarPhraseObject objectPhrase in grammar._sentenceObject._objectPhraseList)
            {
                foreach (SpeechUnit speechUnit in objectPhrase._speechUnits)
                {
                    foreach (var characteristicTerm in characteristicTerms)
                    {
                        var key = characteristicTerm.Key;
                        foreach (var term in characteristicTerm.Value)
                        {
                            if (speechUnit._display == term)
                            {
                                _missingComponents.Remove("characteristic");
                               _characteristic = speechUnit._display;

                                if (key == "profile" || key == "layout")
                                {
                                    if (!_missingComponents.Contains("updated value"))
                                    {
                                        _containsValue = true;
                                        needsValue = true;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            if (needsValue)
            {
               _missingComponents.Add("updated value");
            }

            _captainResponse = GetCaptainResponse();
        }
    }


    public void FillUpdatedValue(GrammarManager grammar)
    {
        // Reorganize missing components
        _missingComponents = _missingComponents.ToList();

        if (_action == "update")
        {
            Sentence sentence = grammar._sentenceObject;
            string phrase = string.Empty;

            foreach (GrammarPhraseObject objectPhrase in sentence._objectPhraseList)
            {
                foreach (SpeechUnit speechUnit in objectPhrase._speechUnits)
                {
                    phrase += speechUnit._display + " ";
                }
                phrase = phrase.TrimEnd(' ');
            }

            _updatedValue = phrase;
            _missingComponents.Remove("updated value");

            _captainResponse = GetCaptainResponse();
        }
    }


    public Dictionary<string, List<string>> GetKeyTerms(string trigger, Sentence sentence)
    {
        var updateTerms = new List<string>();
        var characteristicValues = new Dictionary<string, List<string>>();
        var keyUpdateTerms = new Dictionary<string, List<string>>();

        string quickId = GrammarData.GetStoryId("quick_command");
        List<string> quickCommandUpdatables = GrammarData.GetValuesForStoryCategory(quickId, "quick_command_updatable");

        foreach (var value in quickCommandUpdatables)
        {
            updateTerms.Add(value);
        }
        updateTerms.Add("entity");

        foreach (var term in updateTerms)
        {
            List<string> identifiers = GrammarData.GetValuesForStoryCategory(quickId, $"quick_command_{term}_identifier");

            if (!keyUpdateTerms.ContainsKey(term))
            {
                keyUpdateTerms[term] = new List<string>();
            }
            keyUpdateTerms[term].AddRange(identifiers);
        }

        // Process object phrases from the sentence
        foreach (GrammarPhraseObject objectPhrase in sentence._objectPhraseList)
        {
            if (objectPhrase.dataType == GrammarPhraseObject.DataType.Noun)
            {
                NounObject nounObject = (NounObject)objectPhrase;
                if (nounObject._isEntity)
                {
                    foreach (SpeechUnit speechUnit in nounObject._speechUnits)
                    {
                        if (speechUnit._type == "entity" || speechUnit._type == "phonetic")
                        {
                            if (!keyUpdateTerms.ContainsKey("entity"))
                            {
                                keyUpdateTerms["entity"] = new List<string>();
                            }
                            keyUpdateTerms["entity"].Add(speechUnit._display);
                        }
                    }
                }
            }
            else if (objectPhrase.dataType == GrammarPhraseObject.DataType.Unknown)
            {
                UnknownObject unknownObject = (UnknownObject)objectPhrase;
                if (unknownObject._isEntity)
                {
                    foreach (var speechUnit in unknownObject._speechUnits)
                    {
                        if (speechUnit._type == "entity" || speechUnit._type == "phonetic")
                        {
                            if (!keyUpdateTerms.ContainsKey("entity"))
                            {
                                keyUpdateTerms["entity"] = new List<string>();
                            }
                            keyUpdateTerms["entity"].Add(speechUnit._display);
                        }
                    }
                }
            }
        }

        foreach (var term in updateTerms)
        {
            List<string> characteristics = GrammarData.GetValuesForStoryCategory(quickId, $"quick_command_{term}_characteristic");

            if (!characteristicValues.ContainsKey(term))
            {
                characteristicValues[term] = new List<string>();
            }
            characteristicValues[term].AddRange(characteristics);
        }

        if (trigger == "object")
        {
            return keyUpdateTerms;
        }
        else if (trigger == "characteristic")
        {
            return characteristicValues;
        }
        else
        {
            return new Dictionary<string, List<string>>();
        }
    }
    
}