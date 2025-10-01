using System.Runtime.Serialization;

[DataContract]
public class StoryTitle
{
    [DataMember (Name = "story_id")]
    public string _storyId;

    [DataMember (Name = "story_name")]
    public string _storyName;
}