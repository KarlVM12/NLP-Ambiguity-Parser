using System.Collections.Generic;
using System.Runtime.Serialization;

[DataContract]
public class DefinedObject
{
    [DataMember(Name = "definiteType")] 
    private string _definiteType;

    [DataMember(Name = "excludePartsOfSpeech")]
    private List<string> _excludePartsOfSpeech = new List<string>();

    [DataMember(Name = "isDeterminers")] 
    private bool _isDeterminers;

    [DataMember (Name = "isAuxiliary")]
    private bool _isAuxiliary;

    [DataMember (Name = "isInterrogative")]
    private bool _isInterrogative;

    public string GetDefiniteType()
    {
        return _definiteType;
    }
    
    public List<string> GetExcludePartsOfSpeech()
    {
        return _excludePartsOfSpeech;
    }
    
    public bool GetIsDeterminers()
    {
        return _isDeterminers;
    }
    
    public bool GetIsAuxiliary()
    {
        return _isAuxiliary;
    }
    
    public bool GetIsInterrogative()
    {
        return _isInterrogative;
    }
}