using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using System.Runtime.Serialization;
using Grammar;
using UnityEngine;
using Newtonsoft.Json;

[DataContract]
public class Sentence
{
    [DataMember]
    private Dictionary<string, SpeechUnit> _masterWordList;

    [DataMember]
    public List<GrammarUnit> _grammarPhraseList;

    [DataMember]
    public List<GrammarPhraseObject> _objectPhraseList = new List<GrammarPhraseObject>();

    [DataMember]
    public string _sentenceType;

    [DataMember]
    public NounObject _subject;

    [DataMember]
    public VerbObject _mainVerbObject;

    [DataMember]
    public NounObject _mainObject;

    [DataMember]
    public List<NounObject> _directObjects = new List<NounObject>();

    [DataMember]
    public bool _isInterrogative = false;

    [DataMember]
    public string _interrogativePOS;

    [DataMember]
    public int _countVerbObjects = 0;

    [DataMember]
    public int _countNounObjects = 0;

    [DataMember]
    public int _mainVerbPhraseIndex = 0;

    [DataMember]
    public int _subjectIndex = 0;

    [DataMember]
    public int _mainObjectIndex = 0;

    [DataMember]
    public int _mainVerbIndex = 0;

    [DataMember]
    public bool _captainIsSubject = false;



    public Sentence(Dictionary<string, SpeechUnit> masterWordList, List<GrammarUnit> grammarPhraseList)
    {

        // SENTENCE TYPE EDIT FOR SHORT PROMPTS


        _masterWordList = masterWordList;
        _grammarPhraseList = grammarPhraseList;

        foreach (var grammarPhrase in _grammarPhraseList)
        {
            if (grammarPhrase._phraseType == "noun phrase" || grammarPhrase._phraseType == "prepositional phrase")
            {
                _objectPhraseList.Add(new NounObject(grammarPhrase, _masterWordList));
            }
            else if (grammarPhrase._phraseType == "verb phrase" || grammarPhrase._phraseType == "adverb")
            {
                _objectPhraseList.Add(new VerbObject(grammarPhrase, _masterWordList));
            }
            else
            {
                _objectPhraseList.Add(new UnknownObject(grammarPhrase, _masterWordList));
            }
        }


    }

    public void Start()
    {
        //UnityEngine.Debug.Log(JsonConvert.SerializeObject(_objectPhraseList));

        foreach (GrammarPhraseObject value in _objectPhraseList)
        {
            if (value.dataType == GrammarPhraseObject.DataType.Noun)
            {
                _countNounObjects++;
            }
            if (value.dataType == GrammarPhraseObject.DataType.Verb)
            {
                if (!((VerbObject)value)._isOnlyAdverb)
                {
                    _countVerbObjects++;
                }
            }
        }

        SetSentenceType();
        SetMainVerb();
        // break if one word prompt
        if (_masterWordList.Count == 1)
        {
            return;
        }
        SetSubject();
        // break if two word prompt
        SetObjects();

        //UnityEngine.Debug.Log(JsonConvert.SerializeObject(_mainVerbObject));
        //UnityEngine.Debug.Log(_countNounObjects);
        //UnityEngine.Debug.Log(_countVerbObjects);
    }

    public void SetSubject()
    {

        if (_sentenceType == "imperative")
        {
            _captainIsSubject = true;
            _subjectIndex = -1;
        }
        else if (_sentenceType == "interrogative")
        {
            if (_mainVerbIndex != 0)
            {
                if (_interrogativePOS == "aux verb")
                {
                    if (_objectPhraseList[1].dataType == GrammarPhraseObject.DataType.Noun)
                    {
                        _subject = (NounObject)_objectPhraseList[1];
                        _subjectIndex = 1;
                    }
                }

                else if (_interrogativePOS == "adverb")
                {
                    if (_objectPhraseList[_mainVerbIndex - 1].dataType == GrammarPhraseObject.DataType.Noun)
                    {
                        if (((NounObject)_objectPhraseList[_mainVerbIndex - 1])._isInterrogative)
                        {
                            if (_objectPhraseList[_mainVerbIndex + 1].dataType == GrammarPhraseObject.DataType.Noun)
                            {
                                _subjectIndex = _mainVerbIndex + 1;
                                _subject = (NounObject)_objectPhraseList[_subjectIndex];
                            } else
                            {
                                _subjectIndex = _mainVerbIndex - 1;
                                _subject = (NounObject)_objectPhraseList[_subjectIndex];
                            }
                        }
                        else
                        {
                            _subjectIndex = _mainVerbIndex - 1;
                            _subject = (NounObject)_objectPhraseList[_subjectIndex];
                        }
                    } else
                    {
                        if (_objectPhraseList[_mainVerbIndex + 1].dataType == GrammarPhraseObject.DataType.Noun)
                        {
                            _subjectIndex = _mainVerbIndex + 1;
                            _subject = (NounObject)_objectPhraseList[_subjectIndex];

                        }
                    }
                    
                }

                else if (_interrogativePOS == "determiner")
                {
                    if (_countVerbObjects == 1)
                    {
                       if(_objectPhraseList[_mainVerbIndex + 1].dataType == GrammarPhraseObject.DataType.Noun)
                        {
                            _subjectIndex = _mainVerbIndex + 1;
                            _subject = (NounObject)_objectPhraseList[_subjectIndex];
                        }
                    } else if(_countVerbObjects > 1)
                    {
                        if (!_mainVerbObject._isOnlyAuxiliary)
                        {
                            if (_objectPhraseList[_mainVerbIndex - 1].dataType == GrammarPhraseObject.DataType.Noun)
                            {
                                _subjectIndex = _mainVerbIndex - 1;
                                _subject = (NounObject)_objectPhraseList[_subjectIndex];
                            }
                        } else
                        {
                            if (_objectPhraseList[_mainVerbIndex + 1].dataType == GrammarPhraseObject.DataType.Noun)
                            {
                                _subjectIndex = _mainVerbIndex + 1;
                                _subject = (NounObject)_objectPhraseList[_subjectIndex];
                            }
                        }
                    }

                }

                else if (_interrogativePOS == "pronoun")
                {
                    if (_objectPhraseList[0]._speechUnits[0]._display == "who")
                    {
                        _subject = (NounObject)_objectPhraseList[0];
                    } else
                    {
                        if (_countVerbObjects == 1)
                        {
                            if (_objectPhraseList[1].dataType == GrammarPhraseObject.DataType.Noun)
                            {
                                _subjectIndex = _mainVerbIndex + 1;
                                _subject = (NounObject)_objectPhraseList[_subjectIndex];
                            }
                        }
                        else if (_countVerbObjects > 1)
                        {
                            if (!_mainVerbObject._isOnlyAuxiliary)
                            {
                                if (_objectPhraseList[_mainVerbIndex - 1].dataType == GrammarPhraseObject.DataType.Noun)
                                {
                                    _subjectIndex = _mainVerbIndex - 1;
                                    _subject = (NounObject)_objectPhraseList[_subjectIndex];
                                }
                            }
                            else
                            {
                                if (_objectPhraseList[_mainVerbIndex + 1].dataType == GrammarPhraseObject.DataType.Noun)
                                {
                                    _subjectIndex = _mainVerbIndex + 1;
                                    _subject = (NounObject)_objectPhraseList[_subjectIndex];
                                }
                            }
                        }
                    }
                }
            } else
            {
                if (_objectPhraseList[1].dataType == GrammarPhraseObject.DataType.Noun)
                {
                    _subjectIndex = 1;
                    _subject = (NounObject)_objectPhraseList[1];
                }
            }
        }
    }

    public void SetMainVerb()
    {
        if (_objectPhraseList.Count < 1)
            return;

        if (_sentenceType == "imperative")
        {
            for (int key = 0; key < _objectPhraseList.Count; key++)
            {
                
                if (_objectPhraseList[key].dataType == GrammarPhraseObject.DataType.Verb)
                {
                    VerbObject objectPhrase = (VerbObject)_objectPhraseList[key];
                    if (objectPhrase._isOnlyAuxiliary)
                    {
                        continue;
                    }
                    if (!objectPhrase._isOnlyAdverb)
                    {
                        _mainVerbObject = objectPhrase;//TODO might need to copy this might be a memory link
                        _mainVerbPhraseIndex = key;
                        break;
                    }
                }
            }
        }
        else
        {
            if (_objectPhraseList[0].dataType == GrammarPhraseObject.DataType.Verb)
            {
                VerbObject TheVerbObject = (VerbObject)_objectPhraseList[0];

                if (TheVerbObject._isOnlyAuxiliary)
                {

                    _interrogativePOS = "aux verb";
                    if (_countVerbObjects == 1)
                    {
                        _mainVerbObject = TheVerbObject;//object list 0
                        _mainVerbIndex = 0;
                        return;
                    }
                    else if (_countVerbObjects > 1)
                    {
                        for (int key = 0; key < _objectPhraseList.Count; key++)
                        {
                            if (_objectPhraseList[key].dataType == GrammarPhraseObject.DataType.Verb)
                            {
                                if (((VerbObject)_objectPhraseList[key])._hasActionVerb)
                                {
                                    _mainVerbObject = (VerbObject)_objectPhraseList[key];
                                    _mainVerbIndex = key;
                                    return;
                                }

                            }
                        }
                        //_mainVerbObject = firstVerbObject;
                        //_mainVerbIndex = 0;
                        return;
                    }

                }

                if (TheVerbObject._isOnlyAdverb)
                {
                    if (_objectPhraseList[1].dataType == GrammarPhraseObject.DataType.Verb && ((VerbObject)_objectPhraseList[1])._isOnlyAuxiliary)
                    {
                        _interrogativePOS = "adverb";
                        if (_countVerbObjects == 1)
                        {
                            _mainVerbObject = (VerbObject)_objectPhraseList[1];
                            _mainVerbIndex = 1;
                            return;
                        }
                        else if (_countVerbObjects > 1)
                        {
                            for (int key = 0; key < _objectPhraseList.Count; key++)
                            {
                                if (_objectPhraseList[key].dataType == GrammarPhraseObject.DataType.Verb && ((VerbObject)_objectPhraseList[key])._hasActionVerb)
                                {
                                    _mainVerbObject = (VerbObject)_objectPhraseList[key];
                                    _mainVerbIndex = key;
                                    return;
                                }
                            }
                            _mainVerbObject = (VerbObject)_objectPhraseList[1];
                            _mainVerbIndex = 1;
                            return;
                        }
                    }
                }
           
                if (TheVerbObject._mainVerb._isAuxiliary && TheVerbObject._speechUnits[0].IsAdverb())
                {
                    _interrogativePOS = "adverb";
                    if (_countVerbObjects == 1)
                    {
                        _mainVerbObject = TheVerbObject;
                        _mainVerbIndex = 0;
                        return;
                    }
                    else if (_countVerbObjects > 1)
                    {
                        for (int key = 0; key < _objectPhraseList.Count; key++)
                        {
                            if (_objectPhraseList[key].dataType == GrammarPhraseObject.DataType.Verb && ((VerbObject)_objectPhraseList[key])._hasActionVerb)
                            {
                                _mainVerbObject = (VerbObject)_objectPhraseList[key];
                                _mainVerbIndex = key;
                                return;
                            }
                        }
                        //TODO check this might be issue
                        //_mainVerbObject = (VerbObject)_objectPhraseList[key];
                        //_mainVerbIndex = 0;
                        return;
                    }
                }
            }







            if (_objectPhraseList[0].dataType == GrammarPhraseObject.DataType.Noun)
            {
                if (_objectPhraseList[0]._speechUnits[0]._definiteType == "pronoun" &&
                    _objectPhraseList[0]._speechUnits[0]._isInterrogative)
                {
                    _interrogativePOS = "pronoun";
                    if (_countVerbObjects == 1)
                    {
                        if (_objectPhraseList[1].dataType == GrammarPhraseObject.DataType.Verb)
                        {
                            if (!((VerbObject)_objectPhraseList[1])._isOnlyAdverb)
                            {
                                _mainVerbObject = (VerbObject)_objectPhraseList[1];
                                _mainVerbIndex = 1;
                                return;
                            }
                        }
                    }
                    else if (_countVerbObjects > 1)
                    {
                        for (int key = 0; key < _objectPhraseList.Count; key++)
                        {
                            if (_objectPhraseList[key].dataType == GrammarPhraseObject.DataType.Verb)
                            {
                                if(!((VerbObject)_objectPhraseList[key])._isOnlyAdverb && !((VerbObject)_objectPhraseList[key])._isOnlyAuxiliary)
                                {
                                    _mainVerbObject = (VerbObject)_objectPhraseList[key];
                                    _mainVerbIndex = key;
                                    return;
                                }
                            }
                        }

                        for (int key = 0; key < _objectPhraseList.Count; key++)
                        {
                            if (_objectPhraseList[key].dataType == GrammarPhraseObject.DataType.Verb)
                            {
                                if (((VerbObject)_objectPhraseList[key])._isOnlyAuxiliary)
                                {
                                    _mainVerbObject = (VerbObject)_objectPhraseList[key];
                                    _mainVerbIndex = key;
                                    return;
                                }
                            }
                        }
                    }
                }
            }

            if (_objectPhraseList[0]._speechUnits[0]._isDeterminers && _objectPhraseList[0]._speechUnits[0]._isInterrogative)
            {
                _interrogativePOS = "determiner";
                if (_countVerbObjects == 1)
                {
                    if (_objectPhraseList[1].dataType == GrammarPhraseObject.DataType.Verb)
                    {
                        if (!((VerbObject)_objectPhraseList[1])._isOnlyAdverb)
                        {
                            _mainVerbObject = (VerbObject)_objectPhraseList[1];
                            _mainVerbIndex = 1;
                            return;
                        }
                    }
                }
                else if (_countVerbObjects > 1)
                {
                    for (int key = 0; key < _objectPhraseList.Count; key++)
                    {
                        if (_objectPhraseList[key].dataType == GrammarPhraseObject.DataType.Verb)
                        {
                            if (!((VerbObject)_objectPhraseList[key])._isOnlyAdverb && !((VerbObject)_objectPhraseList[key])._isOnlyAuxiliary)
                            {
                                _mainVerbObject = (VerbObject)_objectPhraseList[key];
                                _mainVerbIndex = key;
                                return;
                            }
                        }
                    }

                    for (int key = 0; key < _objectPhraseList.Count; key++)
                    {
                        if (_objectPhraseList[key].dataType == GrammarPhraseObject.DataType.Verb) {
                            if (((VerbObject)_objectPhraseList[key])._isOnlyAuxiliary)
                            {
                                _mainVerbObject = (VerbObject)_objectPhraseList[key];
                                _mainVerbIndex = key;
                                return;
                            }
                        }
                    }
                }
            }

            if (_objectPhraseList[0]._speechUnits[0].IsAdverb() && _objectPhraseList[0]._speechUnits[0]._isInterrogative &&
                _objectPhraseList[0]._speechUnits[1].IsAdjective() && _objectPhraseList[0]._speechUnits[1].IsNoun())
            {
                _interrogativePOS = "adverb";
                if (_countVerbObjects == 1)
                {
                    if (_objectPhraseList[1].dataType == GrammarPhraseObject.DataType.Verb)
                    {
                        if (!((VerbObject)_objectPhraseList[1])._isOnlyAdverb)
                        {
                            _mainVerbObject = (VerbObject)_objectPhraseList[1];
                            _mainVerbIndex = 1;
                            return;
                        }
                    }
                }
                else if (_countVerbObjects > 1)
                {
                    for (int key = 0; key < _objectPhraseList.Count; key++)
                    {
                        if (_objectPhraseList[key].dataType == GrammarPhraseObject.DataType.Verb)
                        {
                            if (!((VerbObject)_objectPhraseList[key])._isOnlyAdverb && !((VerbObject)_objectPhraseList[key])._isOnlyAuxiliary)
                            {
                                _mainVerbObject = (VerbObject)_objectPhraseList[key];
                                _mainVerbIndex = key;
                                return;
                            }
                        }
                    }

                    for (int key = 0; key < _objectPhraseList.Count; key++)
                    {
                        if (_objectPhraseList[key].dataType == GrammarPhraseObject.DataType.Verb)
                        {
                            if (((VerbObject)_objectPhraseList[key])._isOnlyAuxiliary)
                            {
                                _mainVerbObject = (VerbObject)_objectPhraseList[key];
                                _mainVerbIndex = key;
                                return;
                            }
                        }
                    }
                }
            }
        }
        
    }


    public void SetObjects()
    {
        if (_sentenceType == "imperative")
        {
            int nextIndexFromVerbPhrase = _mainVerbPhraseIndex + 1;
            if (nextIndexFromVerbPhrase < _objectPhraseList.Count)
            {
                if (_objectPhraseList[nextIndexFromVerbPhrase].dataType == GrammarPhraseObject.DataType.Noun)
            {
                _mainObject = (NounObject)_objectPhraseList[nextIndexFromVerbPhrase];
                _mainObjectIndex = nextIndexFromVerbPhrase;
            }
            foreach (GrammarPhraseObject objectPhrase in _objectPhraseList)
            {
                if (objectPhrase.dataType == GrammarPhraseObject.DataType.Noun)
                {
                    if (_objectPhraseList.IndexOf(objectPhrase) != _mainObjectIndex)
                    {
                        _directObjects.Add((NounObject)objectPhrase);
                    }
                }

            }
            }
            
        } else
        {
            foreach (GrammarPhraseObject objectPhrase in _objectPhraseList)
            {
                if (objectPhrase.dataType == GrammarPhraseObject.DataType.Noun)
                {
                    if (_objectPhraseList.IndexOf(objectPhrase) != _subjectIndex)
                    {
                        _directObjects.Add((NounObject)objectPhrase);
                    }
                }

            }
        }
    }

    public void SetSentenceType()
    {
        if (_objectPhraseList.Count == 0)
            return;
        //UnityEngine.Debug.Log(JsonConvert.SerializeObject(_objectPhraseList[0].dataType));
        if (_objectPhraseList[0].dataType == GrammarPhraseObject.DataType.Verb || _objectPhraseList[0].dataType == GrammarPhraseObject.DataType.Noun)
        {
            _sentenceType = "imperative";

            if (_objectPhraseList[0].dataType == GrammarPhraseObject.DataType.Verb)
            {
                if (((VerbObject)_objectPhraseList[0])._isInterrogative)
                {
                    _sentenceType = "interrogative";
                }
            }

            if (_objectPhraseList[0].dataType == GrammarPhraseObject.DataType.Noun)
            {
                if (((NounObject)_objectPhraseList[0])._isInterrogative)
                {
                    _sentenceType = "interrogative";
                }
            }

        }

    }
}
