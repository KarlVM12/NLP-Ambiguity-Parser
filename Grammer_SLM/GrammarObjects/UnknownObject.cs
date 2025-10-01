using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine;
using Grammar;

[DataContract]
public class UnknownObject : GrammarPhraseObject
{
    public override DataType dataType => DataType.Unknown;

    [DataMember]
    public bool _isEntity = false;


    public UnknownObject(GrammarUnit grammarPhrase, Dictionary<string, SpeechUnit> masterWordList)
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
            if (speechUnit._type == "entity" || speechUnit._type == "phonetic")
            {
                _isEntity = true;
            }

            if (speechUnit._type == "date" || speechUnit._type == "time" || speechUnit._type == "datetime" || speechUnit._type == "timeInterval")
            {
                _isEntity = true;
            }
        }

    }

}