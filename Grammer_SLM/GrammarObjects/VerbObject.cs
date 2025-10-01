using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine;
using Grammar;
using System.Linq;

[DataContract]
public class VerbObject : GrammarPhraseObject
{
    public override DataType dataType => DataType.Verb;

    [DataMember]
    public SpeechUnit _mainVerb;

    [DataMember]
    public List<SpeechUnit> _descriptors = new List<SpeechUnit>();

    [DataMember]
    public bool _isOnlyAdverb = false;

    [DataMember]
    public bool _isInterrogative = false;

    [DataMember]
    public bool _isOnlyAuxiliary = false;

    
    [DataMember]
    public bool _hasActionVerb = false;

    public VerbObject(GrammarUnit grammarPhrase, Dictionary<string, SpeechUnit> masterWordList)
    {
        _grammarPhrase = grammarPhrase;

        foreach (var word in masterWordList)
        {
            foreach (var unit in _grammarPhrase._speechHashUnits)
            {
                if (word.Value._hash == unit)
                {
                    _speechUnits.Add(word.Value);
                }
            }
        }

        if (_speechUnits.Count == 1 && _speechUnits.First()._isAuxiliary)
        {
            _isOnlyAuxiliary = true;
        }

        int verbCount = 0;

        foreach (var speechUnit in _speechUnits)
        {
            if (speechUnit._definiteType == "verb")
            {
                verbCount++;
                if (!speechUnit._isAuxiliary)
                {
                    _mainVerb = speechUnit;
                    _hasActionVerb = true;
                }
            }

            if (speechUnit._isInterrogative)
            {
                _isInterrogative = true;
            }

            if (speechUnit._isDateTimeUnit)
            {
                _isDateTime = true;
            }

            if (verbCount > 0)
            {
                _isOnlyAdverb = false;
            } else
            {
                _isOnlyAuxiliary = true;
            }

        }

        if (_hasActionVerb)
        {
            foreach (var speechUnit in _speechUnits)
            {
                if (speechUnit._definiteType == "adverb" || speechUnit._isAuxiliary)
                {
                    _descriptors.Add(speechUnit);
                }
            }
        }
        else
        {
            foreach (var speechUnit in _speechUnits)
            {
                if (speechUnit._definiteType == "adverb")
                {
                    _descriptors.Add(speechUnit);
                }
                if (speechUnit._isAuxiliary)
                {
                    _mainVerb = speechUnit;
                }
            }
        }
    }
}