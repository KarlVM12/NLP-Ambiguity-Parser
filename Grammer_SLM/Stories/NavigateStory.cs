using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Grammar;
using Newtonsoft.Json;

[DataContract]
public class NavigateStory
{
    [DataMember] private Sentence _sentence;

    [DataMember] public Dictionary<string, List<string>> _interfaces = new Dictionary<string, List<string>>();
    [DataMember] public List<string> _allSubscreens = new List<string>();
    [DataMember] public Dictionary<string, List<string>> _screenIdentifiers = new Dictionary<string, List<string>>();

    [DataMember] public string _mainScreen;

    [DataMember] public string _subScreen;

    [DataMember] private bool _subScreenFound;

    [DataMember] private bool _mainScreenFound;

    
    public NavigateStory(Sentence sentence)
    {
        GetKeyTerms();
        
        _sentence = sentence;

    }


    public NavigateStoryObject FillStory()
    {
        SearchForMainScreen();
        if (_mainScreenFound)
        {
            SearchForSubScreen();
        }
        else
        {
            SearchAllSubscreens();
        }

        int mainVerbIndex = _sentence._mainVerbPhraseIndex;
        GrammarPhraseObject phraseObject = _sentence._objectPhraseList[mainVerbIndex];
        SpeechUnit mainVerb = null;

        if (phraseObject.dataType == GrammarPhraseObject.DataType.Verb)
        {
            mainVerb = ((VerbObject)phraseObject)._mainVerb;
        }

        return new NavigateStoryObject(mainVerb, _mainScreen, _subScreen);
    }

    private void SearchForMainScreen()
    {
        foreach (GrammarPhraseObject objectPhrase in _sentence._objectPhraseList)
        {
            foreach (SpeechUnit speechUnit in objectPhrase._speechUnits)
            {
                foreach (string key in _interfaces.Keys)
                {
                    if (speechUnit._display == key)
                    {
                        _mainScreenFound = true;
                        _mainScreen = key;
                        return;
                    }
                }
            }
        }

        foreach (GrammarPhraseObject objectPhrase in _sentence._objectPhraseList)
        {
            foreach (SpeechUnit speechUnit in objectPhrase._speechUnits)
            {
                if (_screenIdentifiers.TryGetValue(speechUnit._display, out var screenIdentifier))
                {
                    _mainScreen = screenIdentifier.FirstOrDefault();
                    _mainScreenFound = true;
                    return;
                }
            }
        }
    }

    private void SearchForSubScreen()
    {
        if (_interfaces.TryGetValue(_mainScreen, out var interfaceList))
        {
            foreach (GrammarPhraseObject objectPhrase in _sentence._objectPhraseList)
            {
                foreach (SpeechUnit speechUnit in objectPhrase._speechUnits)
                {
                    if (interfaceList.Contains(speechUnit._display))
                    {
                        _subScreenFound = true;
                        _subScreen = speechUnit._display;
                        return;
                    }
                }
            }
        }
    }

    private void SearchAllSubscreens()
    {
        foreach (GrammarPhraseObject objectPhrase in _sentence._objectPhraseList)
        {
            foreach (SpeechUnit speechUnit in objectPhrase._speechUnits)
            {
                if (_allSubscreens.Contains(speechUnit._display))
                {
                    _subScreen = speechUnit._display;
                    _subScreenFound = true;
                    break;
                }
            }
        }

        if (_subScreenFound)
        {
            foreach (var pair in _interfaces)
            {
                if (pair.Value.Contains(_subScreen))
                {
                    _mainScreenFound = true;
                    _mainScreen = pair.Key;
                    return;
                }
            }
        }
    }


    private void GetKeyTerms()
    {
        string navID = GrammarData.GetStoryId("navigate");

        List<string> mainScreens = GrammarData.GetValuesForStoryCategory(navID, "interface");

        //UnityEngine.Debug.Log(JsonConvert.SerializeObject(mainScreens));

        foreach (var screen in mainScreens)
        {

            List<string> subscreens = GrammarData.GetValuesForStoryCategory(navID, $"{screen}_subscreen");
            if (!_interfaces.ContainsKey(screen))
            {
                _interfaces[screen] = new List<string>();
            }
            _interfaces[screen].AddRange(subscreens);
            _allSubscreens.AddRange(subscreens);

            List<string> screenIdentifiersList = GrammarData.GetValuesForStoryCategory(navID, $"{screen}_identifier");
            if (!_screenIdentifiers.ContainsKey(screen))
            {
                _screenIdentifiers[screen] = new List<string>();
            }
            _screenIdentifiers[screen].AddRange(screenIdentifiersList);
        }

    }

}
