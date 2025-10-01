
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

using Grammar;
using Newtonsoft.Json;
using UnityEngine;

[DataContract]
public class ChatStory
{
    [DataMember] private Sentence _sentence;

    [DataMember] private List<SpeechUnit> _recipients = new List<SpeechUnit>();

    [DataMember] private List<string> _subjectIndicators;

    [DataMember] private List<string> _messageTerms;

    [DataMember] public List<string> _chatVerbWords;

    [DataMember] private SpeechUnit _indicator;
    
    [DataMember] private List<object> _foundIndicators = new List<object>();
    
    [DataMember] private string _messageTermUnit;

    [DataMember] private int _indicatorObjectIndex = -1;

    [DataMember] private int _firstEntityIndex = -1;
    
    [DataMember] private int _lastEntityIndex = -1;

    [DataMember] private int _messageTermStartIndex = -1;

    [DataMember] private int _messageTermEndIndex = -1;

    [DataMember] private int _messageBodyStartIndex = -1;

    [DataMember] private int _messageBodyEndIndex = -1;

    [DataMember] private bool _hasMessageKeyWord;

    [DataMember] private bool _includesSubjectTopic;

    [DataMember] private bool _includesMessageBody;

    [DataMember] private int _mainVerbIndex;

    public ChatStory(Sentence sentence)
    {
        GetKeyTerms();
        
        _sentence = sentence;
    }

    public ChatStoryObject FillStory()
    {
        _mainVerbIndex = _sentence._mainVerbPhraseIndex;
        GrammarPhraseObject phraseObject = _sentence._objectPhraseList[_mainVerbIndex];
        SpeechUnit mainVerb = null;

        if (phraseObject.dataType == GrammarPhraseObject.DataType.Verb)
        {
            mainVerb = ((VerbObject)phraseObject)._mainVerb;
        }

        CheckTermAfterMainVerb();
        GetRecipients();
        RecheckMessageTerm();
        CheckIndicators();

        if (_indicator != null)
        {
            CheckForSecondIndicator();
        }

        if (_foundIndicators.Count > 0 && _includesMessageBody)
        {
            GetMessageBodyIndexes();
        }


        if (_messageTermEndIndex != -1)
        {
            if (_messageTermStartIndex == _messageTermEndIndex)
            {
                phraseObject = _sentence._objectPhraseList[_messageTermEndIndex];

                if (phraseObject.dataType == GrammarPhraseObject.DataType.Noun)
                {
                    _messageTermUnit = ((NounObject)phraseObject)._mainNounUnit._display;
                }
            }
            else
            {
                GrammarPhraseObject phraseObjectOne = _sentence._objectPhraseList[_messageTermStartIndex];
                GrammarPhraseObject phraseObjectTwo = _sentence._objectPhraseList[_messageTermEndIndex];

                if (phraseObjectOne.dataType == GrammarPhraseObject.DataType.Noun && phraseObjectTwo.dataType == GrammarPhraseObject.DataType.Noun)
                {
                    NounObject nounObjectOne = (NounObject)phraseObjectOne;
                    NounObject nounObjectTwo = (NounObject)phraseObjectTwo;

                    _messageTermUnit += nounObjectOne._mainNounUnit._display + " " + nounObjectTwo._mainNounUnit._display;
                }
            }
        }

        GrammarPhraseObject subjectTopic = null;

        if (_includesSubjectTopic)
        {
            subjectTopic = _sentence._objectPhraseList[_indicatorObjectIndex];
        }

        Debug.Log("_messageTermUnit " + _messageTermUnit + " : "+ _messageTermStartIndex + " : " + _messageTermEndIndex);
        Debug.Log("_messageBodyStartIndex " + _messageBodyStartIndex + " : "+ _messageBodyEndIndex);

        
        return new ChatStoryObject(mainVerb, _recipients, _messageTermUnit, _messageBodyStartIndex, _messageBodyEndIndex, _sentence._objectPhraseList, subjectTopic); 
    }

    private void CheckTermAfterMainVerb()
    {
        int startIndex = _sentence._mainVerbPhraseIndex;

        GrammarPhraseObject nextPhrase = null;
        GrammarPhraseObject nextPhraseAfter = null;

        if (startIndex + 1 < _sentence._objectPhraseList.Count) nextPhrase = _sentence._objectPhraseList[startIndex + 1];
        if (startIndex + 2 < _sentence._objectPhraseList.Count) nextPhraseAfter = _sentence._objectPhraseList[startIndex + 2];


        if (nextPhrase is { dataType: GrammarPhraseObject.DataType.Noun })
        {

            if (((NounObject)nextPhrase)._isEntity)
            {
                _firstEntityIndex = startIndex + 1;
            }
            else
            {
                foreach (SpeechUnit speechUnit in nextPhrase._speechUnits)
                {
                    if (_messageTerms.Contains(speechUnit._display))
                    {
                        _hasMessageKeyWord = true;
                        _messageTermStartIndex = startIndex + 1;
                        
                    }
                }

                if (nextPhraseAfter is { dataType: GrammarPhraseObject.DataType.Noun })
                {
                    foreach (SpeechUnit speechUnit in nextPhraseAfter._speechUnits)
                    {
                        if (_messageTerms.Contains(speechUnit._display))
                        {
                            _messageTermEndIndex = startIndex + 2;
                            break;
                        }

                        _messageTermEndIndex = JsonConvert.DeserializeObject<int>(JsonConvert.SerializeObject(_messageTermStartIndex));
                    }
                }
                else
                {
                    _messageTermEndIndex = JsonConvert.DeserializeObject<int>(JsonConvert.SerializeObject(_messageTermStartIndex));
                }
            }
        }
    }

    private void GetRecipients()
    {
        if (_firstEntityIndex > _messageTermEndIndex)
        {
            GetEntities(_firstEntityIndex);

            return;
        }

        if (_messageTermEndIndex > _firstEntityIndex)
        {
            if (_messageTermEndIndex + 1 < _sentence._objectPhraseList.Count)
            {
                GrammarPhraseObject phrase = _sentence._objectPhraseList[_messageTermEndIndex + 1];

                if (phrase.dataType == GrammarPhraseObject.DataType.Noun)
            {
                NounObject nounPhrase = (NounObject)phrase;
                
                if (nounPhrase._isEntity)
                {
                    _firstEntityIndex = _messageTermEndIndex + 1;
                    GetEntities(_messageTermEndIndex + 1);
                }
                else
                {
                    CheckEndForRecipients(_sentence._objectPhraseList.Count - 1);
                }
            }else if (phrase.dataType == GrammarPhraseObject.DataType.Verb)
            {
                CheckEndForRecipients(_sentence._objectPhraseList.Count - 1);
            }

            }
            
        }
    }

    private void GetEntities(int startIndex)
    {
        int count = _sentence._objectPhraseList.Count;

        for (int i = startIndex; i < count; i++)
        {
            GrammarPhraseObject phrase = _sentence._objectPhraseList[i];

            if (phrase.dataType == GrammarPhraseObject.DataType.Unknown &&
                phrase._speechUnits[0]._definiteType == "conjunction")
            {
                continue;
            }

            if (phrase.dataType == GrammarPhraseObject.DataType.Noun)
            {
                if (!((NounObject)phrase)._isEntity)
                {
                    _lastEntityIndex = i - 1;
                    break;
                }
                
                _recipients.Add((phrase as NounObject)._mainNounUnit);
            }
        }
    }

    private void CheckEndForRecipients(int lastIndex)
    {
        bool isEntity = false;

        GrammarPhraseObject phrase = _sentence._objectPhraseList[lastIndex];
        
        if (phrase.dataType == GrammarPhraseObject.DataType.Noun)
        {
            isEntity = ((NounObject)phrase)._isEntity;
        }
        
        if (phrase.dataType == GrammarPhraseObject.DataType.Unknown)
        {
            isEntity = ((UnknownObject)phrase)._isEntity;
        }
        
        if (isEntity)
        {
            for (int i = lastIndex; i > 0; i--)
            {
                phrase = _sentence._objectPhraseList[i];

                if (phrase.dataType == GrammarPhraseObject.DataType.Unknown &&
                    phrase._speechUnits[0]._definiteType == "conjunction")
                {
                    continue;
                }

                if (phrase.dataType == GrammarPhraseObject.DataType.Noun)
                {
                    if (!((NounObject)phrase)._isEntity)
                    {
                        _lastEntityIndex = i - 1;
                        break;
                    }
                
                    _recipients.Add((phrase as NounObject)._mainNounUnit);
                }
            }
        }
    }

    private void RecheckMessageTerm()
    {
        if (_messageTermStartIndex == -1)
        {
            if (_firstEntityIndex != -1)
            {
                if (_lastEntityIndex != -1)
                {
                    GrammarPhraseObject nextPhrase = null;
                    
                    int startIndex = _lastEntityIndex + 1;
                    if (startIndex + 1 < _sentence._objectPhraseList.Count) nextPhrase = _sentence._objectPhraseList[_lastEntityIndex + 1];

                    if (nextPhrase != null)
                    {
                        foreach (SpeechUnit speechUnit in nextPhrase._speechUnits)
                        {
                            if (_messageTerms.Contains(speechUnit._display))
                            {
                                if (speechUnit._index != _mainVerbIndex)
                                {
                                    _messageTermStartIndex = _lastEntityIndex + 1;
                                }
                            }
                        }
                    }
                }
            }
        }

        if (_messageTermStartIndex != -1 && _messageTermEndIndex == -1)
        {

            GrammarPhraseObject nextPhraseAfter = _sentence._objectPhraseList[_lastEntityIndex + 2];
            if (nextPhraseAfter.dataType == GrammarPhraseObject.DataType.Noun)
            {
                foreach (SpeechUnit speechUnit in nextPhraseAfter._speechUnits)
                {
                    if (_messageTerms.Contains(speechUnit._display))
                    {
                        _messageTermEndIndex = _messageTermStartIndex + 1;
                    }
                }
            }
        }

        if (_messageTermStartIndex != -1 && _messageTermEndIndex == -1)
        {
            _messageTermEndIndex = JsonConvert.DeserializeObject<int>(JsonConvert.SerializeObject(_messageTermStartIndex));
        }
    }

    private void CheckIndicators()
    {
        if (_firstEntityIndex == -1 && _messageTermEndIndex == -1)
        {
            CheckForIndicatorsInObjectPhraseList();
            
            return;
        }
        
        if (_firstEntityIndex == -1 && _messageTermEndIndex != -1)
        {
            CheckForEntitiesOrMessageTerms(_messageTermEndIndex);
            
            return;
        }
        
        if (_firstEntityIndex != -1 && _messageTermEndIndex == -1)
        {
            FindLastEntity();
            CheckForEntitiesOrMessageTerms(_lastEntityIndex);
            
            return;
        }

        if (_firstEntityIndex != -1 && _messageTermEndIndex != -1)
        {
            FindLastEntity();
            CheckIndicatorsFromFinalNounInPreText(Math.Max(_lastEntityIndex + 1, _messageTermEndIndex + 1));
        }
    }

    private void CheckForSecondIndicator()
    {
        GrammarPhraseObject nextPhrase = null;
        
        if (_indicatorObjectIndex + 1 < _sentence._objectPhraseList.Count)
        {
            nextPhrase = _sentence._objectPhraseList[_indicatorObjectIndex + 1];
        }

        Debug.Log(JsonConvert.SerializeObject(nextPhrase));

        if (nextPhrase != null)
        {
            foreach (SpeechUnit speechUnit in nextPhrase._speechUnits)
            {
                if (_chatVerbWords.Contains(speechUnit._display))
                {
                    // check message term to ensure they are not the same ???? whats the issue ???
                    _foundIndicators.Add(nextPhrase); //TODO not sure if correct logic adding strings / speechUnit objects to this list (same as php)
                    _includesMessageBody = true;
                }
            }
        }
    }

    private void CheckForIndicatorsInObjectPhraseList()
    {
        foreach (GrammarPhraseObject phraseObject in _sentence._objectPhraseList)
        {
            foreach (SpeechUnit speechUnit in phraseObject._speechUnits)
            {
                if (_subjectIndicators.Contains(speechUnit._display))
                {
                    _indicator = speechUnit;
                    _foundIndicators.Add(phraseObject); //TODO not sure if correct logic adding strings / speechUnit objects to this list (same as php)
                    _indicatorObjectIndex = _lastEntityIndex + 1;

                    if (_messageTerms.Contains(speechUnit._display))
                    {
                        _hasMessageKeyWord = true;
                        _messageTermEndIndex = _lastEntityIndex + 1;
                    }
                }
            }
        }
    }

    private void CheckIndicatorsFromFinalNounInPreText(int index)
    {
        GrammarPhraseObject phrase = null;
        
        if (index < _sentence._objectPhraseList.Count)
        {
            phrase = _sentence._objectPhraseList[index];
        }

        if (phrase is { dataType: GrammarPhraseObject.DataType.Noun or GrammarPhraseObject.DataType.Verb or GrammarPhraseObject.DataType.Unknown })
        {
            CheckSpeechUnits(phrase, index);
        }
    }
    
    private void CheckForEntitiesOrMessageTerms(int index)
    {
        GrammarPhraseObject nextPhrase = null;
        
        if (index + 1 < _sentence._objectPhraseList.Count)
        {
            nextPhrase = _sentence._objectPhraseList[index + 1];
        }

        if (nextPhrase is { dataType: GrammarPhraseObject.DataType.Noun or GrammarPhraseObject.DataType.Verb or GrammarPhraseObject.DataType.Unknown })
        {
            CheckSpeechUnits(nextPhrase, index + 1);
        }
    }

    private void CheckSpeechUnits(GrammarPhraseObject phrase, int phraseIndex)
    {
        if (phrase.dataType == GrammarPhraseObject.DataType.Noun)
        {
            foreach (SpeechUnit speechUnit in phrase._speechUnits)
            {
                if (_subjectIndicators.Contains(speechUnit._display))
                {
                    _indicator = speechUnit;
                    _indicatorObjectIndex = phraseIndex;
                    _foundIndicators.Add(speechUnit._display); //TODO not sure if correct logic adding strings / speechUnit objects to this list (same as php)

                    if (speechUnit._definiteType == "preposition")
                    {
                        _includesSubjectTopic = true;
                    }

                    return;
                }
            }
        }
        else
        {

        if (phrase.dataType == GrammarPhraseObject.DataType.Verb){

            foreach (SpeechUnit speechUnit in phrase._speechUnits)
            {
                if (_chatVerbWords.Contains(speechUnit._display))
                    {
                        if (speechUnit._index != _mainVerbIndex){
                            _indicator = speechUnit;
                            _indicatorObjectIndex = phraseIndex;
                            _foundIndicators.Add(speechUnit._display); //TODO not sure if correct logic adding strings / speechUnit objects to this list (same as php)
                            _includesMessageBody = true;
                            
                            return;
                        }

                    }
            }
        }   
        if (phrase.dataType == GrammarPhraseObject.DataType.Unknown){
            Debug.Log("UNKNOWN FOUND");
            foreach (SpeechUnit speechUnit in phrase._speechUnits)
            {
                if (_subjectIndicators.Contains(speechUnit._display) || _chatVerbWords.Contains(speechUnit._display))
                    {
                        // UNKOWWN ---> make sure it doesn't overlap with anything else we have, set as last resort if grammar fails but 
                        // indicator still exists
                        if (speechUnit._index != _messageTermStartIndex && speechUnit._index != _messageTermEndIndex && speechUnit._index != _mainVerbIndex && speechUnit._index != _firstEntityIndex && speechUnit._index != _lastEntityIndex){
                            _indicator = speechUnit;
                            _indicatorObjectIndex = phraseIndex;
                            _foundIndicators.Add(speechUnit._display); //TODO not sure if correct logic adding strings / speechUnit objects to this list (same as php)
                            _includesMessageBody = true;
                            
                            return;
                        }

                    }
            }
        }
        GrammarPhraseObject nextPhrase = _sentence._objectPhraseList[phraseIndex + 1];

            foreach (SpeechUnit speechUnit in phrase._speechUnits)
            {
                if (_subjectIndicators.Contains(speechUnit._display))
                {
                    _indicator = speechUnit;
                    _indicatorObjectIndex = phraseIndex;
                    _foundIndicators.Add(speechUnit._display); //TODO not sure if correct logic adding strings / speechUnit objects to this list (same as php)

                    if (speechUnit._definiteType == "adverb")
                    {
                        if (nextPhrase.dataType == GrammarPhraseObject.DataType.Unknown)
                        {
                            foreach (SpeechUnit unit in nextPhrase._speechUnits)
                            {
                                if (_subjectIndicators.Contains(unit._display))
                                {
                                    _includesSubjectTopic = true;
                                    _foundIndicators.Add(unit); //TODO not sure if correct logic adding strings / speechUnit objects to this list (same as php)
                                }
                            }
                        }

                        _includesMessageBody = true;
                    }
                    
                    return;
                }
            }
        }
    }

    private void FindLastEntity()
    {
        int lastRecipientIndex = _recipients.Count - 1;
        SpeechUnit lastRecipient = _recipients[lastRecipientIndex];

        for (int i = 0; i < _sentence._objectPhraseList.Count; i++)
        {
            GrammarPhraseObject phraseObject = _sentence._objectPhraseList[i];

            if (phraseObject.dataType == GrammarPhraseObject.DataType.Noun)
            {
                NounObject nounPhrase = (NounObject)phraseObject;

                if (nounPhrase._isEntity)
                {
                    foreach (SpeechUnit speechUnit in phraseObject._speechUnits)
                    {
                        if (speechUnit._display == lastRecipient._display)
                        {
                            _lastEntityIndex = i;
                        }
                    }
                }
            }

            if (phraseObject.dataType == GrammarPhraseObject.DataType.Unknown)
            {
                UnknownObject unknownPhrase = (UnknownObject)phraseObject;

                if (unknownPhrase._isEntity)
                {
                    foreach (SpeechUnit speechUnit in phraseObject._speechUnits)
                    {
                        if (speechUnit._display == lastRecipient._display)
                        {
                            _lastEntityIndex = i;
                        }
                    }
                }
            }
        }
    }

    private void GetMessageBodyIndexes()
    {
        if (_firstEntityIndex == -1)
        {
            FindMessageBodyEndForNoEntities();
            
            return;
        }

        _messageBodyEndIndex = _sentence._objectPhraseList.Count - 1;

        if (_indicator != null && _foundIndicators.Count == 1)
        {
            _messageBodyStartIndex = _indicatorObjectIndex + 1;
        }else if (_foundIndicators.Count == 2)
        {
            _messageBodyStartIndex = _indicatorObjectIndex + 2;
        }
        else
        {
            FindMessageBodyStartForEntities();
        }
    }

    private void FindMessageBodyStartForEntities()
    {
        if (_recipients.Count > 0)
        {
            _messageBodyStartIndex = _firstEntityIndex + _recipients.Count;
        }
        else
        {
            _messageBodyStartIndex = _firstEntityIndex + 1;
        }
    }

    private void FindMessageBodyEndForNoEntities()
    {
        if (_indicatorObjectIndex == -1)
        {
            _messageBodyEndIndex = _sentence._objectPhraseList.Count - 1;

            return;
        }

        _messageBodyStartIndex = _indicatorObjectIndex + 1;

        if (_recipients.Count == 0)
        {
            _messageBodyEndIndex = _sentence._objectPhraseList.Count - 1;
        }
        else
        {
            for (int i = 0; i < _sentence._objectPhraseList.Count - 1; i++)
            {
                GrammarPhraseObject phraseObject = _sentence._objectPhraseList[i];
                
                bool isEntity = false;
                
                if (phraseObject.dataType == GrammarPhraseObject.DataType.Noun)
                {
                    isEntity = ((NounObject)phraseObject)._isEntity;
                }
        
                if (phraseObject.dataType == GrammarPhraseObject.DataType.Unknown)
                {
                    isEntity = ((UnknownObject)phraseObject)._isEntity;
                }

                if (isEntity)
                {
                    _messageBodyEndIndex = i - 1;
                    break;
                }
            }
        }
    }

    private void GetKeyTerms()
    {
        string storyId = GrammarData.GetStoryId("chat");

        _chatVerbWords = GrammarData.GetValuesForStoryCategory(storyId, "action_verb");
        _subjectIndicators = GrammarData.GetValuesForStoryCategory(storyId, "subject_indicator");
        _messageTerms = GrammarData.GetValuesForStoryCategory(storyId, "message_term");
    }
}