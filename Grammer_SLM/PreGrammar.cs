using System.Collections.Generic;
using System.Linq;
using Grammar;
using UnityEngine;
using Newtonsoft.Json;

public class PreGrammar
{
    private Dictionary<string, SpeechUnit> _workingWordList = new Dictionary<string, SpeechUnit>();
    
    public Dictionary<string, SpeechUnit> Setup(Dictionary<string, SpeechUnit> masterWordList)
    {
        _workingWordList = JsonConvert.DeserializeObject<Dictionary<string, SpeechUnit>>(JsonConvert.SerializeObject(masterWordList));

        // ??? 
        if (_workingWordList.Count == 0) return _workingWordList;
        //Debug.Log(JsonConvert.SerializeObject(_workingWordList));

        string lastWordKey = _workingWordList.Keys.Last();
        SpeechUnit lastSpeechUnit = _workingWordList[lastWordKey];

        while (ScrubLastWordCheck(lastSpeechUnit, lastWordKey) == 0 && GrammarHelper.IsLastSpeechUnit(masterWordList, lastWordKey, -1) == false)
        {
            _workingWordList.Remove(lastWordKey);
            masterWordList.Remove(lastWordKey);
            
            lastWordKey = _workingWordList.Keys.Last();
            lastSpeechUnit = _workingWordList[lastWordKey];
        }
        
        return _workingWordList;
    }

    private int ScrubLastWordCheck(SpeechUnit lastSpeechUnit, string lastSpeechUnitKey)
    {
        List<string> excludePartOfSpeechList = new List<string>();
        List<string> remainingPartOfSpeechList = new List<string>();
        List<string> partsOfSpeech = lastSpeechUnit.GetPartsOfSpeech().Split(";").ToList();

        if (partsOfSpeech.Count == 1 && partsOfSpeech[0] == "adjective") return 1;

        foreach (string partOfSpeech in partsOfSpeech)
        {
            switch (partOfSpeech)
            {
                case "preposition":
                    excludePartOfSpeechList.Add(partOfSpeech);
                    break;
                case "conjunction":
                    excludePartOfSpeechList.Add(partOfSpeech);
                    break;
                case "interjection":
                    excludePartOfSpeechList.Add(partOfSpeech);
                    break;
                case "adjective":
                    excludePartOfSpeechList.Add(partOfSpeech);
                    break;
                default:
                    remainingPartOfSpeechList.Add(partOfSpeech);
                    break;
            }
        }

        _workingWordList[lastSpeechUnitKey].AddToExclusions(excludePartOfSpeechList);

        bool determinerFlag = false;
        
        if (_workingWordList[lastSpeechUnitKey].GetIsDeterminers())
        {
            string pos = _workingWordList[lastSpeechUnitKey].GetPartsOfSpeech();
            if (pos == "demonstrative" || pos == "quantifier" || pos == "compound" || pos == "distributive" || pos == "distributive")
            {
                _workingWordList[lastSpeechUnitKey].SetDefiniteType("noun");
                _workingWordList[lastSpeechUnitKey].SetIsDeterminers(false);
                determinerFlag = true;
            }
        }

        if (!determinerFlag)
        {
            if (_workingWordList[lastSpeechUnitKey].GetWorkingPartsOfSpeechCount() == 1)
            {
                lastSpeechUnit.SetDefiniteType(lastSpeechUnit.GetWorkingPartsOfSpeech());
            }
        }

        return remainingPartOfSpeechList.Count;
    }
}