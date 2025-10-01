using System.Runtime.Serialization;

[DataContract]
public class StoryResponse
{
    [DataMember (Name = "id")]
    private string _id;

    [DataMember (Name = "story")]
    private string _story;
    
    [DataMember (Name = "group_set")]
    private string _groupSet;

    [DataMember (Name = "subgroup_set")]
    private string _subgroupSet;
    
    [DataMember (Name = "response")]
    private string _response;

    public bool HasAttributes(string story, string groupSet, string subgroupSet)
    {
        if (story == _story && groupSet == _groupSet && subgroupSet == _subgroupSet) return true;

        return false;
    }

    public string GetResponse()
    {
        return _response;
    }
}