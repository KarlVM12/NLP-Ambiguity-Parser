using System.Runtime.Serialization;

[DataContract]
public class DictionaryTerm
{
    [DataMember (Name = "term")]
    private string _term;

    [DataMember (Name = "partOfSpeech")]
    private string _partOfSpeech;

    public string GetPartsOfSpeech()
    {
        return _partOfSpeech;
    }

    public string GetTerm()
    {
        return _term;
    }

}
