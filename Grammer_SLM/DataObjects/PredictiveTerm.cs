using System.Runtime.Serialization;

[DataContract]
public class PredictiveTerm
{
    [DataMember(Name = "token")] private string _token;
    
    [DataMember(Name = "term")] private string _term;
    
    [DataMember(Name = "tally")] private int _tally;

    public string GetToken()
    {
        return _token;
    }

    public string GetTerm()
    {
        return _term;
    }

    public int GetTally()
    {
        return _tally;
    }
}