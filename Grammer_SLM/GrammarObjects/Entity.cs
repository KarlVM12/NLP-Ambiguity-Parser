using System.Runtime.Serialization;

[DataContract]
public class Entity
{
    [DataMember] private string _id;

    [DataMember] private string _type;
    
    [DataMember] private string _word;
    
    [DataMember] private string _display;

    public Entity(string id, string type, string word, string display)
    {
        _id = id;
        _type = type;
        _word = word;
        _display = display;
    }

    public string GetId()
    {
        return _id;
    }

    public new string GetType()
    {
        return _type;
    }

    public string GetWord()
    {
        return _word;
    }

    public string GetDisplay()
    {
        return _display;
    }
}