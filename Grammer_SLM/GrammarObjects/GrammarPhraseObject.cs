using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine;
using Grammar;

[DataContract]
public class GrammarPhraseObject 
{
    public enum DataType
    {
        none = -1,
        Verb,
        Noun,
        Unknown
    }
    

    public virtual DataType dataType
    {
        get
        {
            return DataType.none;
        }
    }



    [DataMember]
    public List<SpeechUnit> _speechUnits = new List<SpeechUnit>();

    [DataMember]
    public GrammarUnit _grammarPhrase;

    [DataMember]
    public bool _isDateTime = false;
}
