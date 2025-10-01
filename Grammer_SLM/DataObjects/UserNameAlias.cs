using System.Dynamic;
using System.Runtime.Serialization;

[DataContract]
public class UserNameAlias
{
    [DataMember (Name = "user_id")]
    private string _id;

    [DataMember (Name = "alias")]
    private string _alias;

    public string GetId()
    {
        return _id;
    }

    public string GetAlias()
    {
        return _alias;
    }
}