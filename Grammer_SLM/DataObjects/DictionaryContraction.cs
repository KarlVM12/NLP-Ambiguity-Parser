using System.Runtime.Serialization;

[DataContract]
public class DictionaryContraction
{
    [DataMember (Name = "term")]
    private string _term;

    [DataMember (Name = "definedObject")]
    private string _definedObject;
    
    [DataMember (Name = "replacement")]
    private string _replacement;

    public string GetTerm()
    {
        return _term;
    }

    public string GetReplacement()
    {
        return _replacement;
    }

    public string GetDefinedObject()
    {
        return _definedObject;
    }
}