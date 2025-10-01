using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine;
using Grammar;

[DataContract]
public class NounObject : GrammarPhraseObject
{
    public override DataType dataType => DataType.Noun;

    [DataMember]
    public SpeechUnit _mainNounUnit;

    [DataMember]
    public List<SpeechUnit> _descriptors = new List<SpeechUnit>();

    [DataMember]
    public bool _isPrepositional = false;

    [DataMember]
    public bool _isInterrogative = false;

    [DataMember]
    public bool _isEntity = false;



    public NounObject(GrammarUnit grammarPhrase, Dictionary<string, SpeechUnit> masterWordList)
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

        foreach (var speechUnit in _speechUnits)
        {
            if (speechUnit._definiteType == "noun" || speechUnit._definiteType == "pronoun" || speechUnit._partsOfSpeech == "proper noun")
            {
                _mainNounUnit = speechUnit;
            }

            if (speechUnit._definiteType == "adjective" || speechUnit._definiteType == "adverb" || speechUnit._partsOfSpeech == "quantifier" || speechUnit._partsOfSpeech == "possessive")
            {
                _descriptors.Add(speechUnit);
            }

            if (speechUnit._definiteType == "preposition")
            {
                _isPrepositional = true;
                _isEntity = false;
            }

            if (speechUnit._isInterrogative)
            {
                _isInterrogative = true;
            }

            if (speechUnit._type == "entity" || speechUnit._type == "phonetic")
            {
                _isEntity = true;
            }

            if (speechUnit._type == "datetime" || speechUnit._type == "date" || speechUnit._type == "time" || speechUnit._type == "timeInterval")
            {
                _isDateTime = true;
            }
        }
    }

}
