using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Grammar;

[DataContract]
public class WordUnit
{
    [DataMember]
    private string _word;

    [DataMember]
    private string _type;

    [DataMember]
    private int _count;

    [DataMember]
    private bool _isPossessive;

    [DataMember]
    private bool _isDateTime;

    [DataMember] 
    private bool _isPhonetic;

    [DataMember]
    private bool _isNumeral;

    [DataMember]
    private List<Bucket> _buckets = new List<Bucket>();

    [DataMember] 
    private List<Entity> _entities = new List<Entity>();
    
    [DataMember]
    private SpeechUnit _speechUnit;
    
    public WordUnit(string word, string type)
    {
        _word = word;
        _type = type;
        _count = word.Split(" ").ToList().Count;
    }

    public void SetIsPossessive(bool value)
    {
        _isPossessive = value;
    }
    
    public bool GetIsPossessive()
    {
        return _isPossessive;
    }

    public void SetIsPhonetic(bool value)
    {
        _isPhonetic = value;
    }
    
    public void SetIsDateTime(bool value)
    {
        _isDateTime = value;
    }

    public void SetIsNumeral(bool value)
    {
        _isNumeral = value;
    }

    public void SetWord(string word)
    {
        _word = word;

        _count = _word.Split(" ").Length;
    }

    public string GetWord()
    {
        return _word;
    }

    public void SetCount(int count)
    {
        _count = count;
    }

    public int GetCount()
    {
        return _count;
    }

    public List<Bucket> GetBuckets()
    {
        return _buckets;
    }
    
    public void AddBucket(Bucket bucket)
    {
        if (_buckets.Contains(bucket)) return;

        _buckets.Add(bucket);
    }
    
    public SpeechUnit GetSpeechUnit()
    {
        return _speechUnit;
    }

    public void SetSpeechUnit(SpeechUnit speechUnit)
    {
        _speechUnit = speechUnit;
    }

    public void AddEntity(Entity entity)
    {
        _entities.Add(entity);
    }

    public bool GetIsDateTime()
    {
        return _isDateTime;
    }

    public bool GetIsNumeral()
    {
        return _isNumeral;
    }

    public bool GetSpeechUnitIsDateTimeUnit()
    {
        if (_speechUnit == null) return false;
        return _speechUnit.GetIsDateTimeUnit();
    }

    public string GetSpeechUnitType()
    {
        if (_speechUnit == null) return null;
        return _speechUnit.GetType();
    }

    public string GetEntityId()
    {
        if (_entities.Count == 0) return null;
        return _entities[0].GetId();
    }
}