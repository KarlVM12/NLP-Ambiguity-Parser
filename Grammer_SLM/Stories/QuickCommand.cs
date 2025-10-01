using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Grammar;
using Newtonsoft.Json;
using Debug = UnityEngine.Debug;

public class QuickCommand
{
    private Sentence _sentence;
    public SpeechUnit _action;
    public SpeechUnit _primaryObject;
    public SpeechUnit _characteristic;
    public string _primaryDisplay = "";
    public string _characteristicDisplay = "";
    public string _newValue = "";
    
    public Dictionary<string, List<string>> _keyUpdateTerms = new Dictionary<string, List<string>>();
    public Dictionary<string, List<string>> _characteristicValues = new Dictionary<string, List<string>>();
    public int _characteristicIndex;
    public int _primaryIndex;
    public bool _impliedPrimary;
    public bool _hasNewValue = false;
    public bool _needsNewValue = false;
    public string _commandTitle;
    public QuickCommandObject _commandObject;



        public QuickCommand(Sentence sentence, string commandTitle)
    {
        _primaryIndex = -1;
        _characteristicIndex = -1;
        _impliedPrimary = false;
        _commandTitle = commandTitle;
        _action = sentence._mainVerbObject._mainVerb;
        _sentence = sentence;

        GetKeyTerms();
    }

    public QuickCommandObject FillStory()
    {

        QuickCommandObject commandObject = null;
        Debug.Log(JsonConvert.SerializeObject(_keyUpdateTerms));
        Debug.Log(JsonConvert.SerializeObject(_characteristicValues));

        SpeechUnit mainVerb = null;

        if (_sentence._objectPhraseList[_sentence._mainVerbPhraseIndex].dataType == GrammarPhraseObject.DataType.Verb)
        {
            mainVerb = ((VerbObject)_sentence._objectPhraseList[_sentence._mainVerbPhraseIndex])._mainVerb;
        }

        if (_commandTitle == "update"){
            commandObject = updateCommandProcess();
        }

        // other quick commands, if we continue with this method

        if (commandObject != null){
            _commandObject = commandObject;
        }
 
        return new QuickCommandObject(_primaryDisplay, _characteristicDisplay, _newValue, _commandTitle, _needsNewValue);
    }

    public QuickCommandObject updateCommandProcess(){

        int toIndex = findToIndex();
        determineObjects(toIndex);

        if (_primaryObject != null){
            if (_characteristic == null){
                CheckForCharacteristics();
            }
        } else {
            checkForImpliedObject();
        }

        foreach (GrammarPhraseObject objectPhrase in _sentence._objectPhraseList)
        {
            if (objectPhrase.dataType == GrammarPhraseObject.DataType.Noun)
            {
                foreach (SpeechUnit speechUnit in objectPhrase._speechUnits)
                {
                    if (speechUnit._index != _primaryIndex && speechUnit._index != _characteristicIndex)
                    {
                        if (toIndex != -1)
                        {
                            _hasNewValue = true;
                            if (speechUnit._index > toIndex)
                            {
                                _newValue += speechUnit._display + " ";
                            }
                        }
                    }
                }

                if (_hasNewValue)
                {
                    _newValue = _newValue.TrimEnd(' ');
                }
            }
        }

        if (_primaryObject != null)
        {
            if (_primaryObject._type == "entity" || _primaryObject._type == "phonetic" || 
                _primaryObject._display == "profile" || _primaryObject._display == "layout")
            {
                _needsNewValue = true;
            }

            if (_impliedPrimary)
            {
                if (_primaryDisplay == "profile" || _primaryDisplay == "layout")
                {
                    _needsNewValue = true;
                }
            }
        }

        // Return the constructed QuickCommandObject
        return new QuickCommandObject(_primaryDisplay, _characteristicDisplay, _newValue, _commandTitle, _needsNewValue);
    }

    public int findToIndex(){
        int toIndex = -1;

        foreach(GrammarPhraseObject objectPhrase in _sentence._objectPhraseList){
            foreach(SpeechUnit speechUnit in objectPhrase._speechUnits){
                if (speechUnit._display == "to"){
                    toIndex = speechUnit._index;
                }
            }
        }
        return toIndex;
    }

    public void determineObjects(int toIndex){
        
        if (_sentence._mainObject != null){
            string mainObjectTerm = _sentence._mainObject._mainNounUnit._display;
            foreach (KeyValuePair<string, List<string>> term in _keyUpdateTerms)
            {               
                if (mainObjectTerm == term.Key){
                    checkArrayForMatch(toIndex);
                } 
                
                if (term.Value.Contains(mainObjectTerm))
                {
                    if (_sentence._mainObject._isEntity)
                    {
                        checkEntityMatch(toIndex);
                    }
                    else
                    {
                        checkArrayForMatch(toIndex);
                    }
                }
                else
                {
                    checkDirectObjectsForMatch(toIndex, term.Value);
                }
            }
        } else {
            Debug.Log("NO OBJECTS");
        }
    }

    public void CheckForCharacteristics()
    {

    if (_primaryObject._type == "entity" || _primaryObject._type == "phonetic")
        {
            foreach (GrammarPhraseObject objectPhrase in _sentence._objectPhraseList)
            {
                foreach (SpeechUnit speechUnit in objectPhrase._speechUnits)
                {
                    if (_characteristicValues["entity"].Contains(speechUnit._display))
                    {
                        _characteristic = speechUnit;
                        _characteristicDisplay = speechUnit._display;
                        _characteristicIndex = speechUnit._index;
                    }
                }
            }
        }
        else
        {
            List<string> categories = _characteristicValues[_primaryObject._display];
            foreach (string value in categories)
            {
                foreach (GrammarPhraseObject objectPhrase in _sentence._objectPhraseList)
                {
                    foreach (SpeechUnit speechUnit in objectPhrase._speechUnits)
                    {
                        if (speechUnit._display == value)
                        {
                            _characteristic = speechUnit;
                            _characteristicDisplay = speechUnit._display;
                            _characteristicIndex = speechUnit._index;
                        }
                    }
                }
            }
        }
    }


    public void checkForImpliedObject()
    {
        for (int index = 0; index < _sentence._objectPhraseList.Count; index++)
        {
            GrammarPhraseObject objectPhrase = _sentence._objectPhraseList[index];

            foreach (SpeechUnit speechUnit in objectPhrase._speechUnits)
            {
                foreach (var characteristicValuePair in _characteristicValues)
                {
                    if (characteristicValuePair.Value.Contains(speechUnit._display))
                    {
                        _primaryDisplay = characteristicValuePair.Key;
                        _characteristic = speechUnit;
                        _characteristicDisplay = speechUnit._display;
                        _characteristicIndex = index;
                        _impliedPrimary = true;
                    }
                }
            }
        }
    }


    public void checkArrayForMatch(int toIndex)
    {
        if (toIndex != -1)
        {
            if (_sentence._mainObject._mainNounUnit._index > toIndex)
            {
                _characteristic = _sentence._mainObject._mainNounUnit;
                _characteristicDisplay = _sentence._mainObject._mainNounUnit._display;
                _characteristicIndex = _sentence._mainObject._mainNounUnit._index;
            }
            else
            {
                _primaryObject = _sentence._mainObject._mainNounUnit;
                _primaryDisplay = _sentence._mainObject._mainNounUnit._display;
                _primaryIndex = _sentence._mainObject._mainNounUnit._index;
            }
        }
        else
        {
            _primaryObject = _sentence._mainObject._mainNounUnit;
            _primaryDisplay = _sentence._mainObject._mainNounUnit._display;
            _primaryIndex = _sentence._mainObject._mainNounUnit._index;
        }
    }


    public void checkEntityMatch(int toIndex)
    {
        if (toIndex != -1)
        {
            if (_sentence._mainObject._mainNounUnit._index < toIndex)
            {
                _primaryObject = _sentence._mainObject._mainNounUnit;
                _primaryDisplay = _sentence._mainObject._mainNounUnit._display;
                _primaryIndex = _sentence._mainObject._mainNounUnit._index;
            }
        }
        else
        {
            _primaryObject = _sentence._mainObject._mainNounUnit;
            _primaryDisplay = _sentence._mainObject._mainNounUnit._display;
            _primaryIndex = _sentence._mainObject._mainNounUnit._index;
        }
    }


    public void checkDirectObjectsForMatch(int toIndex, List<string> terms)
    {
        if (_sentence._directObjects != null && _sentence._directObjects.Any())
        {
            foreach (GrammarPhraseObject phraseObject in _sentence._directObjects)
            {
                if (phraseObject.dataType == GrammarPhraseObject.DataType.Noun)
                {
                    NounObject nounObject = phraseObject as NounObject;
                    if (nounObject._isEntity)
                    {
                        if (toIndex != -1)
                        {
                            if (nounObject._mainNounUnit._index < toIndex)
                            {
                                _primaryObject = nounObject._mainNounUnit;
                                _primaryDisplay = nounObject._mainNounUnit._display;
                                _primaryIndex = nounObject._mainNounUnit._index;
                                _characteristic = _sentence._mainObject._mainNounUnit;
                                _characteristicDisplay = _sentence._mainObject._mainNounUnit._display;
                                _characteristicIndex = _sentence._mainObject._mainNounUnit._index;
                            }
                        }
                        else
                        {
                            // Default case when 'toIndex' is -1
                            _primaryObject = nounObject._mainNounUnit;
                            _primaryDisplay = nounObject._mainNounUnit._display;
                            _primaryIndex = nounObject._mainNounUnit._index;
                            _characteristic = _sentence._mainObject._mainNounUnit;
                            _characteristicDisplay = _sentence._mainObject._mainNounUnit._display;
                            _characteristicIndex = _sentence._mainObject._mainNounUnit._index;
                        }
                    }
                    else
                    {
                        // nounObject._index... what is that ??? 
                        /*
                        if (terms.Contains(nounObject._mainNounUnit._display))
                        {
                            if (toIndex != -1)
                            {
                                if (nounObject._index > toIndex)
                                {
                                    _characteristic = nounObject;
                                    _characteristicIndex = nounObject._index;
                                }
                                else
                                {
                                    _primaryObject = nounObject;
                                    _primaryDisplay = nounObject._mainNounUnit._display;
                                    _primaryIndex = nounObject._index;
                                    _characteristic = _sentence._mainObject._mainNounUnit;
                                    _characteristicDisplay = _sentence._mainObject._mainNounUnit._display;
                                    _characteristicIndex = _sentence._mainObject._mainNounUnit._index;
                                }
                            }
                            else
                            {
                                // Default case when 'toIndex' is -1
                                _primaryObject = nounObject;
                                _primaryDisplay = nounObject._mainNounUnit._display;
                                _primaryIndex = nounObject._index;
                                _characteristic = _sentence._mainObject._mainNounUnit;
                                _characteristicDisplay = _sentence._mainObject._mainNounUnit._display;
                                _characteristicIndex = _sentence._mainObject._mainNounUnit._index;
                            }
                        }
                        */
                    }
                }
            }
        }
    }


     private void GetKeyTerms()
    {
        string quickID = GrammarData.GetStoryId("quick_command");

        List<string> quickCommands = GrammarData.GetValuesForStoryCategory(quickID, "quick_command_updatable");

        foreach (var command in quickCommands)
        {
            List<string> identifiers = GrammarData.GetValuesForStoryCategory(quickID, $"quick_command_{command}_identifier");
            if (!_keyUpdateTerms.ContainsKey(command))
            {
                _keyUpdateTerms[command] = new List<string>();
            }
            _keyUpdateTerms[command].AddRange(identifiers);

            List<string> characteristics = GrammarData.GetValuesForStoryCategory(quickID, $"quick_command_{command}_characteristic");
            if (!_characteristicValues.ContainsKey(command))
            {
                _characteristicValues[command] = new List<string>();
            }
            _characteristicValues[command].AddRange(characteristics);
        }

    
        foreach (GrammarPhraseObject objectPhrase in _sentence._objectPhraseList)
        {
            if (objectPhrase.dataType == GrammarPhraseObject.DataType.Noun)
                {
                NounObject phrase = objectPhrase as NounObject;
                if (phrase._isEntity){
        
                    foreach (SpeechUnit speechUnit in phrase._speechUnits)
                    {
                        if (speechUnit._type == "entity" || speechUnit._type == "phonetic")
                        {
                            if (!_keyUpdateTerms.ContainsKey("entity"))
                            {
                                _keyUpdateTerms["entity"] = new List<string>();
                            }
                            _keyUpdateTerms["entity"].Add(speechUnit._display);
                        }
                    }
                }
            } else if (objectPhrase.dataType == GrammarPhraseObject.DataType.Unknown){
                UnknownObject phrase = objectPhrase as UnknownObject;
                if (phrase._isEntity){
        
                    foreach (SpeechUnit speechUnit in phrase._speechUnits)
                    {
                        if (speechUnit._type == "entity")
                        {
                            if (!_keyUpdateTerms.ContainsKey("entity"))
                            {
                                _keyUpdateTerms["entity"] = new List<string>();
                            }
                            _keyUpdateTerms["entity"].Add(speechUnit._display);
                        }
                    }
                }
            }
        }   
    }

}