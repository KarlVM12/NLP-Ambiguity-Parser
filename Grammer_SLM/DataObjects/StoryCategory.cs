using System.Runtime.Serialization;

[DataContract]
public class StoryCategory
{
    [DataMember (Name = "id")]
    private string _id;

    [DataMember (Name = "story_guid")]
    public string _storyGuid;
    
    [DataMember (Name = "category")]
    public string _category;

    [DataMember (Name = "value")]
    public string _value;
}