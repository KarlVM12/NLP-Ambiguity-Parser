using System.Runtime.Serialization;

[DataContract]
public class DictionaryDateTerm
{
    [DataMember (Name = "term")]
    private string _term;

    [DataMember (Name = "partsOfSpeech")]
    private string _partsOfSpeech;

    public string GetPartsOfSpeech()
    {
        return _partsOfSpeech;
    }

    public string GetTerm()
    {
        return _term;
    }
}