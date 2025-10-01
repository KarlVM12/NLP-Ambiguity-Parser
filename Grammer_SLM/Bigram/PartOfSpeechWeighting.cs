using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

[DataContract]
public class PartOfSpeechWeighting
{
    [DataMember(Name = "type")] public string _type;

    [DataMember(Name = "values")] public Dictionary<string, double> _values;
}