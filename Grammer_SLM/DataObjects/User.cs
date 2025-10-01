using System.Dynamic;
using System.Runtime.Serialization;

[DataContract]
public class User
{
    [DataMember (Name = "id")]
    private string _id;

    [DataMember (Name = "full_name")]
    private string _fullName;

    public string GetId()
    {
        return _id;
    }

    public string GetFullName()
    {
        return _fullName;
    }
}