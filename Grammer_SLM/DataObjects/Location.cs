using System.Runtime.Serialization;

[DataContract]
public class Location
{
    [DataMember(Name = "id")] private string _id;
    [DataMember(Name = "name")] private string _name;
    [DataMember(Name = "country_name")] private string _countryName;
    [DataMember(Name = "region_name")] private string _regionName;
    [DataMember(Name = "city_name")] private string _cityName;

    public string GetId()
    {
        return _id;
    }

    public string GetName()
    {
        return _name;
    }

    public string GetCountryName()
    {
        return _countryName;
    }

    public string GetRegionName()
    {
        return _regionName;
    }

    public string GetCityName()
    {
        return _cityName;
    }
}