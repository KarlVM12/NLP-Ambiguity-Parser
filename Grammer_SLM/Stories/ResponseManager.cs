using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;
using Random = System.Random;

public class ResponseManager
{
    private List<string> _responses = new List<string>();

    private Dictionary<string, string> _values = new Dictionary<string, string>();
    
    public ResponseManager(string storyType, string groupSet, string subgroupSet)
    {
        string response = GrammarData.GetStoryResponse(storyType, groupSet, subgroupSet);

        _responses = response.Split("<split>").ToList();
    }

    public void Set(string key, string value)
    {
        _values[key] = value;
    }

    public string Output()
    {
        Random random = new Random();
        
        List<string> filledResponses = new List<string>();

        //if no set values then we can just return a random response
        if (_values.Count == 0) return _responses[random.Next(_responses.Count)];

        foreach (string response in _responses)
        {
            string tempResponse = response;
            
            foreach(KeyValuePair<string, string> entry in _values)
            {
                string tagToReplace = "[@" + entry.Key + "]";

                tempResponse = tempResponse.Replace(tagToReplace, entry.Value);
            }
            
            filledResponses.Add(tempResponse);
        }

        if (filledResponses.Count == 0)
        {
            //something went wrong here
        }
        
        return filledResponses[random.Next(filledResponses.Count)];
    }
}