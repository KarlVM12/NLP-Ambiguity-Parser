using System.Runtime.Serialization;

[DataContract]
public class DictionaryDeterminer
{
    [DataMember (Name = "term")]
    private string _term;

    [DataMember (Name = "type")]
    private string _type;
    
    public string GetTerm()
    {
        return _term;
    }

    public new string GetType()
    {
        return _type;
    }
}