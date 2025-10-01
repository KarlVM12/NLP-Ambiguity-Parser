using System.Runtime.Serialization;

[DataContract]
public class DictionaryAlias
{
    [DataMember (Name = "term")]
    private string _term;

    [DataMember (Name = "definedObject")]
    private string _definedObject;

    public string GetAlias()
    {
        return _term;
    }

    public string GetDefinedObject()
    {
        return _definedObject;
    }
}