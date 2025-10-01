using System.Runtime.Serialization;

[DataContract]
public class PredictiveTermWord
{
    [DataMember(Name = "id")] private string _id;
    
    [DataMember(Name = "term_token")] private string _termToken;
    
    [DataMember(Name = "word")] private string _word;
    
    [DataMember(Name = "usage_count")] private int _usageCount;

    public string GetTermToken()
    {
        return _termToken;
    }

    public int GetUsageCount()
    {
        return _usageCount;
    }

    public string GetWord()
    {
        return _word;
    }
}