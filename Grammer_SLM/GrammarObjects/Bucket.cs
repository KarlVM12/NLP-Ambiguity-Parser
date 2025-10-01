using System.Runtime.Serialization;

[DataContract]
public class Bucket
{
    [DataMember]
    private string _type;
    
    public Bucket(string type)
    {
        _type = type;
    }

    public new string GetType()
    {
        return _type;
    }
}