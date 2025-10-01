using System.Runtime.Serialization;

[DataContract]
public class DictionaryAuxiliaryVerb
{
    [DataMember (Name = "verb")]
    private string _verb;

    [DataMember (Name = "type")]
    private string _type;
    
    public string GetVerb()
    {
        return _verb;
    }

    public new string GetType()
    {
        return _type;
    }
}