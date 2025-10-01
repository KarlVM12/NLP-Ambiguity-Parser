using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using Grammar;
using UnityEngine;
using Newtonsoft.Json;

public class PhraseIdentifier
{
    private Dictionary<string, SpeechUnit> _masterWordList;

    private Dictionary<string, SpeechUnit> _workingWordList;

    public List<GrammarUnit> _grammarPhraseList;

    private bool _noCheckAgainstWorkingWordList = false;

    public PhraseIdentifier(Dictionary<string, SpeechUnit> masterWordList, Dictionary<string, SpeechUnit> workingWordList, List<GrammarUnit> grammarPhraseList, bool onlyGrammarPhraseList = false, bool noCheckAgainstWorkingWordList = false)
    {
        if (onlyGrammarPhraseList)
        {
            //useful for reinsertPhraseFragments function that calls this but only modifies grammarPhraseList
            _masterWordList =  JsonConvert.DeserializeObject<Dictionary<string, SpeechUnit>>(JsonConvert.SerializeObject(masterWordList));
            _workingWordList =  JsonConvert.DeserializeObject<Dictionary<string, SpeechUnit>>(JsonConvert.SerializeObject(workingWordList));
            _grammarPhraseList = grammarPhraseList;
        }
        else
        {
            _masterWordList = masterWordList;
            _workingWordList = workingWordList;
            _grammarPhraseList = grammarPhraseList;   
        }

        _noCheckAgainstWorkingWordList = noCheckAgainstWorkingWordList;
    }

    public void Start()
    {
        // check all terms in master list.. assign single types
        foreach (var key in _masterWordList.Keys)
        {
            _masterWordList[key].CheckForDefiniteAgainstWorkingList();
            if (!_noCheckAgainstWorkingWordList)
            {
                _workingWordList[key].CheckForDefiniteAgainstWorkingList();
            }
        }

        //UnityEngine.Debug.Log(JsonConvert.SerializeObject(_masterWordList));

        int loop = 20;

        while (loop > 0 && _workingWordList.Count > 0)
        {
            if (_workingWordList.Count == 1)
            {

                CheckFirstWordTest();
            }
            else
            {
                IdentifyPhrase();
            }
            loop--;
        }


        //UnityEngine.Debug.Log(JsonConvert.SerializeObject(_masterWordList));
        //UnityEngine.Debug.Log(JsonConvert.SerializeObject(_workingWordList));
    }
    private void IdentifyPhrase()
    {
        int check = IdentifyLastWordOfThePhrase();

        if (check == 0)
        {
            return;
        }

        string lastWordKey = _workingWordList.Keys.ToList().Last();
        int lastWordTrigger = -1;

        if (_workingWordList[lastWordKey].IsDefiniteTypeSet() && _workingWordList[lastWordKey].GetDefiniteType() == "verb")
        {
            lastWordTrigger = 1;
        }

        if (_workingWordList[lastWordKey].IsDefiniteTypeSet() && _workingWordList[lastWordKey].GetDefiniteType() == "noun")
        {
            lastWordTrigger = 2;
        }

        if (_workingWordList[lastWordKey].IsDefiniteTypeSet() && _workingWordList[lastWordKey].GetDefiniteType() == "adverb")
        {
            lastWordTrigger = 3;
        }


        switch (lastWordTrigger)
        {
            case 1:
                VerbPhraseCheckStart();
                break;
            case 2:
                NounPhraseCheckStart();
                break;
            case 3:
                AdverbCheckStart();
                break;
            default:
                UnknownCheckStart();
                break;
        }
    }

    private void CheckFirstWordTest()
    {
        string firstWordKey = _workingWordList.Keys.First();
        if (_workingWordList.Count == 1)
        {
            if (_workingWordList[firstWordKey].GetDefiniteType() == "verb")
            {
                SetPhraseType(firstWordKey, "verb phrase", 0);

                return;
            }

            // one word, check for match to known command terms
            List<string> verbTerms = GrammarData.GetActionVerbTerms();
            if (verbTerms.Contains(_workingWordList[firstWordKey]._display))
            {
                _masterWordList[firstWordKey].SetDefiniteType("verb");
                _workingWordList[firstWordKey].SetDefiniteType("verb");
                SetPhraseType(firstWordKey, "verb phrase", 0);
                return;
            }
        }


        string wordToRightOfHash = "";
        if (GrammarHelper.FindHashOf(ref wordToRightOfHash, _masterWordList, firstWordKey, 1))
        {


            _masterWordList[firstWordKey].CheckForDefiniteAgainstWorkingList();

            if (_masterWordList[wordToRightOfHash].IsDefiniteTypeSet() &&
                _masterWordList[wordToRightOfHash].GetDefiniteType() == "noun" &&
                !_masterWordList[wordToRightOfHash].GetIsInterrogative())
            {
                if (_masterWordList[firstWordKey].IsVerb())
                {
                    _workingWordList[firstWordKey].SetDefiniteType("verb");
                    _masterWordList[firstWordKey].SetDefiniteType("verb");
                    SetPhraseType(firstWordKey, "verb phrase", 0);
                    return;
                }
            }

            if (_masterWordList[wordToRightOfHash].GetIsDeterminers() &&
                _masterWordList[wordToRightOfHash].GetIsInterrogative())
            {
                _workingWordList[firstWordKey].SetDefiniteType("verb");
                _masterWordList[firstWordKey].SetDefiniteType("verb");
                SetPhraseType(firstWordKey, "verb phrase", 0);
                return;
            }

            if (_masterWordList[wordToRightOfHash].GetIsDeterminers() &&
                !_workingWordList[firstWordKey].IsVerbAdjectiveOnly())
            {
                _workingWordList[firstWordKey].SetDefiniteType("verb");
                _masterWordList[firstWordKey].SetDefiniteType("verb");
                SetPhraseType(firstWordKey, "verb phrase", 0);
                return;
            }

            if (_workingWordList[firstWordKey].GetDefiniteType() == "adverb" &&
                _masterWordList[wordToRightOfHash].GetDefiniteType() == "adverb")
            {
                _grammarPhraseList[0].AddToFrontOfSpeechHashUnits(firstWordKey);
                _grammarPhraseList[0].SetIsInterrogative(true);
                _workingWordList[firstWordKey].SetDefiniteType("adverb");
                _masterWordList[firstWordKey].SetDefiniteType("adverb");
                _workingWordList.Remove(firstWordKey);
                return;
            }


            if (_workingWordList[firstWordKey].IsDefiniteTypeSet())
            {

                if (_workingWordList[firstWordKey].GetDefiniteType() == "proper noun" ||
                    _workingWordList[firstWordKey].GetDefiniteType() == "noun")
                {
                    SetPhraseType(firstWordKey, "noun phrase", 0);
                    return;
                }

                
                if (_workingWordList[firstWordKey].GetDefiniteType() == "adverb" &&
                    _workingWordList[firstWordKey].GetIsInterrogative())
                {
                    SetPhraseType(firstWordKey, "adverb", 0);
                    return;//TODO had to add this to prevent crashing
                }
                

                if (_workingWordList[firstWordKey].GetIsInterrogative() &&
                    _workingWordList[firstWordKey].GetDefiniteType() == "pronoun")
                {
                    SetPhraseType(firstWordKey, "noun phrase", 0);
                    return;
                }
            }

            if (_workingWordList.Count == 1)
            {
                if (_workingWordList[firstWordKey].IsVerb() &&
                    _workingWordList[firstWordKey]._definiteType == "N/A")
                {
                    _workingWordList[firstWordKey].SetDefiniteType("verb");
                    _masterWordList[firstWordKey].SetDefiniteType("verb");
                    SetPhraseType(firstWordKey, "verb phrase", 0);
                }
            }

        }
        else
        {
            SetPhraseType(firstWordKey, "unknown phrase", 0);
        }

    }

    private void SetPhraseType(string startingHashKey, string phraseType, int depth)
    {
        if (depth == 0)
        {
            _grammarPhraseList.Insert(0, new GrammarUnit(phraseType, new List<string>() { startingHashKey }));
            _workingWordList.Remove(startingHashKey);

            return;
        }

        List<string> speechUnitHashList = new List<string>();
        List<string> keys = _workingWordList.Keys.ToList();

        while (depth > -1)
        {
            speechUnitHashList.Add(_workingWordList[keys[keys.Count - 1 - depth]].GetHash());

            _workingWordList.Remove(keys[keys.Count - 1 - depth]);

            depth--;
        }

        _grammarPhraseList.Insert(0, new GrammarUnit(phraseType, speechUnitHashList));
    }


    private void VerbPhraseCheckStart()
    {
        string lastWordKey = _workingWordList.Keys.ToList().Last();

        int depth = 0;

        if (GrammarHelper.IsLastSpeechUnit(_workingWordList, lastWordKey, 1))
        {
            depth = VerbPhraseRecursion(lastWordKey, depth);
        }

        string phrase = "";
        int loop = depth;
        List<string> keys = _workingWordList.Keys.ToList();

        while (loop > -1)
        {
            int keyIndex = keys.Count - 1 - loop;
            if (keyIndex >= 0)
            {
                phrase += _workingWordList[keys[keyIndex]]._display + " ";
            }

            loop--;
        }

        SetPhraseType(lastWordKey, "verb phrase", depth);
    }

    private int VerbPhraseRecursion(string currentWordHash, int depth)
    {
        string wordToTheLeftOfHash = "";
        string secondWordToTheLeft = "";
        int returnDepth = depth;

        if (GrammarHelper.FindHashOf(ref wordToTheLeftOfHash, _workingWordList, currentWordHash, -1))
        {
            SpeechUnit leftWordSpeechUnit = _workingWordList[wordToTheLeftOfHash];
            SpeechUnit leftWordMasterUnit = _masterWordList[wordToTheLeftOfHash];

            if (GrammarHelper.FindHashOf(ref secondWordToTheLeft, _workingWordList, currentWordHash, -2))
            {
                if (_workingWordList[secondWordToTheLeft].GetIsInterrogative() &&
                    _workingWordList[secondWordToTheLeft].IsAdverb())
                {
                    if (leftWordSpeechUnit.IsAdverb() && _workingWordList[currentWordHash].GetIsAuxiliary())
                    {
                        _workingWordList[wordToTheLeftOfHash].SetDefiniteType("adverb");
                        _masterWordList[wordToTheLeftOfHash].SetDefiniteType("adverb");

                        return returnDepth;
                    }
                }
            }

            if (leftWordSpeechUnit.GetIsInterrogative())
            {
                if (leftWordSpeechUnit.GetDisplay() == "what")
                {
                    leftWordSpeechUnit.SetDefiniteType("pronoun");
                    leftWordMasterUnit.SetDefiniteType("pronoun");
                }
            }

            if (leftWordSpeechUnit.IsInterjection())
            {
                leftWordSpeechUnit.AddToExclusions(new List<string>() { "interjection" });
                leftWordMasterUnit.AddToExclusions(new List<string>() { "interjection" });
            }

            if (leftWordSpeechUnit.GetDefiniteType() != "adverb")
            {
                if (!leftWordSpeechUnit.IsNoun() && !leftWordSpeechUnit.IsVerb())
                {
                    returnDepth = VerbPhraseRecursion(wordToTheLeftOfHash, depth + 1);
                }
                else
                {
                    return depth;
                }
            }
            else
            {
                if (leftWordSpeechUnit.GetDisplay() == "to")
                {
                    returnDepth = VerbPhraseRecursion(wordToTheLeftOfHash, depth + 1);
                }
            }
        }

        return returnDepth;
    }


    private void NounPhraseCheckStart()
    {
        string lastWordKey = _workingWordList.Keys.ToList().Last();

        var returnArray = new Dictionary<string, object>();
        int depth = 0;
        if (GrammarHelper.IsLastSpeechUnit(_workingWordList, lastWordKey, 1))
        {
            returnArray = NounPhraseRecursion(lastWordKey, depth, "noun phrase");
        }

        depth = (int)returnArray["depth"];
        string phraseType = (string)returnArray["phraseType"];

        var phrase = "";
        int loop = depth;
        var keys = _workingWordList.Keys.ToList();
        while (loop > -1)
        {
            var keyIndex = keys.Count - 1 - loop;
            if (keyIndex >= 0)
            {
                phrase += _workingWordList[keys[keyIndex]]._display + " ";
            }
            loop--;
        }

        SetPhraseType(lastWordKey, phraseType, depth);
    }

    private Dictionary<string, object> NounPhraseRecursion(string currentWordHash, int depth, string phraseType)
    {
        string wordToTheLeftOfHash = "";
        int returnDepth = depth;
        string returnPhraseType = phraseType;

        var currentWord = _workingWordList[currentWordHash];
        var currentMaster = _masterWordList[currentWordHash];

        if (GrammarHelper.FindHashOf(ref wordToTheLeftOfHash, _workingWordList, currentWordHash, -1))
        {
            var leftWordSpeechUnit = _workingWordList[wordToTheLeftOfHash];
            var leftWordMasterUnit = _masterWordList[wordToTheLeftOfHash];

            if (currentWord._display == "i")
            {
                return new Dictionary<string, object>
                {
                    { "depth", returnDepth },
                    { "phraseType", returnPhraseType },
                    { "lastSpeechUnit", leftWordSpeechUnit }
                };
            }

            if (leftWordSpeechUnit._isInterrogative)
            {
                if (leftWordSpeechUnit._display == "what")
                {
                    leftWordSpeechUnit._isDeterminers = true;
                    leftWordSpeechUnit.AddToExclusions(new List<string> { "pronoun" });
                    leftWordMasterUnit._isDeterminers = true;
                    leftWordMasterUnit.AddToExclusions(new List<string> { "pronoun" });
                    leftWordSpeechUnit.CheckForDefiniteAgainstWorkingList();
                    leftWordMasterUnit.CheckForDefiniteAgainstWorkingList();
                }
            }

            if (currentWord._display == "long")
            {
                if (leftWordSpeechUnit._isInterrogative && leftWordSpeechUnit._display == "how")
                {
                    currentWord.SetDefiniteType("adverb");
                    currentMaster.SetDefiniteType("adverb");
                }
            }

            if (leftWordSpeechUnit._isDeterminers)
            {
                var returnArray = NounPhraseRecursion(wordToTheLeftOfHash, depth + 1, returnPhraseType);
                return new Dictionary<string, object>
                {
                    { "depth", returnArray["depth"] },
                    { "phraseType", returnArray["phraseType"] },
                    { "lastSpeechUnit", leftWordSpeechUnit }
                };
            }

            if (currentWord._isDeterminers && leftWordSpeechUnit.IsPreposition())
            {
                leftWordSpeechUnit.SetDefiniteType("preposition");
                leftWordMasterUnit.SetDefiniteType("preposition");
                currentMaster.SetDefiniteType("determiner");
                return new Dictionary<string, object>
                {
                    { "depth", returnDepth + 1 },
                    { "phraseType", "prepositional phrase" },
                    { "lastSpeechUnit", leftWordSpeechUnit }
                };
            }

            if (currentWord._isDeterminers && leftWordSpeechUnit.IsAdjective())
            {
                leftWordSpeechUnit.AddToExclusions(new List<string> { "adjective" });
                return new Dictionary<string, object>
                {
                    { "depth", returnDepth },
                    { "phraseType", returnPhraseType },
                    { "lastSpeechUnit", leftWordSpeechUnit }
                };
            }

            if (currentWord._definiteType == "noun" && leftWordSpeechUnit._definiteType == "adjective")
            {
                var returnArray = NounPhraseRecursion(wordToTheLeftOfHash, depth + 1, returnPhraseType);
                return new Dictionary<string, object>
                {
                    { "depth", returnArray["depth"] },
                    { "phraseType", returnArray["phraseType"] },
                    { "lastSpeechUnit", leftWordSpeechUnit }
                };
            }

            if (currentWord.IsAdjective() && leftWordSpeechUnit._definiteType == "adjective")
            {
                leftWordSpeechUnit.AddToExclusions(new List<string> { "adjective" });
                leftWordMasterUnit.AddToExclusions(new List<string> { "adjective" });

                return new Dictionary<string, object>
                {
                    { "depth", returnDepth },
                    { "phraseType", returnPhraseType },
                    { "lastSpeechUnit", leftWordSpeechUnit }
                };
            }
            else
            {
                if (leftWordSpeechUnit.IsAdjective() && leftWordSpeechUnit._definiteType != "adverb")
                {
                    var returnArray = NounPhraseRecursion(wordToTheLeftOfHash, depth + 1, returnPhraseType);
                    return new Dictionary<string, object>
                    {
                        { "depth", returnArray["depth"] },
                        { "phraseType", returnArray["phraseType"] },
                        { "lastSpeechUnit", leftWordSpeechUnit }
                    };
                }
            }

            if (leftWordSpeechUnit.CanOnlyBeANoun())
            {
                return new Dictionary<string, object>
                {
                    { "depth", returnDepth },
                    { "phraseType", returnPhraseType },
                    { "lastSpeechUnit", leftWordSpeechUnit }
                };
            }

            if (leftWordSpeechUnit.IsPreposition() &&
                leftWordSpeechUnit._definiteType != "adverb" &&
                leftWordSpeechUnit._definiteType != "adjective")
            {
                string secondWordLeft = "";
                if (GrammarHelper.FindHashOf(ref secondWordLeft, _workingWordList, currentWordHash, -2))
                {
                    if (_workingWordList[secondWordLeft]._isDeterminers && leftWordSpeechUnit.IsAdverb())
                    {
                        leftWordSpeechUnit.SetDefiniteType("adverb");
                        leftWordMasterUnit.SetDefiniteType("adverb");
                    }
                    else
                    {
                        leftWordSpeechUnit.SetDefiniteType("preposition");
                        leftWordMasterUnit.SetDefiniteType("preposition");
                        return new Dictionary<string, object>
                    {
                        { "depth", returnDepth + 1 },
                        { "phraseType", "prepositional phrase" },
                        { "lastSpeechUnit", leftWordSpeechUnit }
                    };
                    }
                }
            }
       
            if (leftWordSpeechUnit._isDeterminers)
            {
                var returnArray = NounPhraseRecursion(wordToTheLeftOfHash, depth + 1, returnPhraseType);
                if (returnArray["lastSpeechUnit"] is SpeechUnit lastSpeechUnit)
                {
                    if (lastSpeechUnit._definiteType.Contains("adjective"))
                    {
                        return new Dictionary<string, object>
                        {
                            { "depth", returnArray["depth"] },
                            { "phraseType", returnArray["phraseType"] },
                            { "lastSpeechUnit", leftWordSpeechUnit }
                        };
                    }

                }
            }

            if (leftWordSpeechUnit.IsAdverb())
            {
                if (leftWordSpeechUnit._isInterrogative)
                {
                    return NounPhraseRecursion(wordToTheLeftOfHash, depth + 1, returnPhraseType);
                }

                var returnArray = NounPhraseRecursion(wordToTheLeftOfHash, depth + 1, returnPhraseType);
                string secondWordLeft = "";
                if (GrammarHelper.FindHashOf(ref secondWordLeft, _workingWordList, currentWordHash, -2))
                {
                    if (_workingWordList[secondWordLeft]._isDeterminers)
                    {
                        return new Dictionary<string, object>
                    {
                        { "depth", returnArray["depth"] },
                        { "phraseType", returnArray["phraseType"] },
                        { "lastSpeechUnit", leftWordSpeechUnit }
                    };
                    }
                }

                if (returnArray.ContainsKey("lastType") && returnArray["lastType"].ToString() == "determiner")
                {
                    if (leftWordSpeechUnit._isDeterminers)
                    {
                        return NounPhraseRecursion(wordToTheLeftOfHash, depth + 1, returnPhraseType);
                    }
                }




                return new Dictionary<string, object>
                {
                    { "depth", returnDepth },
                    { "phraseType", returnPhraseType },
                    { "lastSpeechUnit", leftWordSpeechUnit }
                };

            }
        }

        return new Dictionary<string, object>
        {
            { "depth", returnDepth },
            { "phraseType", returnPhraseType },
            { "lastSpeechUnit", null }
        };
    }


    private void AdverbCheckStart()
    {
        string lastWordKey = _workingWordList.Keys.ToList().Last();
        
        int depth = 0;
        if (GrammarHelper.IsLastSpeechUnit(_workingWordList, lastWordKey, 1))
        {
            depth = AdverbHandler(lastWordKey, depth);
        }

        string phrase = "";
        int loop = depth;
        List<string> keys = _workingWordList.Keys.ToList();

        while (loop > -1)
        {
            int keyIndex = keys.Count - 1 - loop;
            if (keyIndex >= 0)
            {
                phrase += _workingWordList[keys[keyIndex]]._display + " ";
            }

            loop--;
        }

        if (depth == 0)
        {
            SetPhraseType(lastWordKey, "adverb", depth);
        }
        else
        {
            SetPhraseType(lastWordKey, "verb phrase", depth);
        }
    }

    private int AdverbHandler(string lastWordKey, int depth)
    {
        string wordToTheLeftOfHash = "";
        int returnDepth = depth;

        if (GrammarHelper.FindHashOf(ref wordToTheLeftOfHash, _workingWordList, lastWordKey, -1))
        {
            SpeechUnit leftWordSpeechUnit = _workingWordList[wordToTheLeftOfHash];

            if (!leftWordSpeechUnit.IsVerb()) return 0;

            if (leftWordSpeechUnit.GetDefiniteType() == "verb" && leftWordSpeechUnit.IsDefiniteTypeSet())
            {
                return VerbPhraseRecursion(wordToTheLeftOfHash, depth + 1);
            }

            if (leftWordSpeechUnit.GetIsAuxiliary())
            {
                return VerbPhraseRecursion(wordToTheLeftOfHash, depth + 1);
            }
        }

        return returnDepth;
    }
        
    private int IdentifyLastWordOfThePhrase()
    {
        if (_workingWordList.Count < 1)
        {
            return 0;
        }

        string lastWordKey = _workingWordList.Keys.ToList().Last();
        var lastSpeechUnit = _workingWordList[lastWordKey];

        if (CheckLastWordForNoun(lastSpeechUnit, lastWordKey))
        {
            return _workingWordList.Count;
        }

        if (lastSpeechUnit.IsDefiniteTypeSet())
        {
            return _workingWordList.Count;
        }

        if (CheckLastWordForAdverb(lastSpeechUnit, lastWordKey))
        {
            return _workingWordList.Count;
        }

        if (lastSpeechUnit.IsDefiniteTypeSet())
        {
            return _workingWordList.Count;
        }

        if (CheckLastWordForVerb(lastSpeechUnit, lastWordKey))
        {
            return _workingWordList.Count;
        }

        if (lastSpeechUnit.IsDefiniteTypeSet())
        {
            return _workingWordList.Count;
        }

        if (CheckLastWordForNounAgainstVerb(lastSpeechUnit, lastWordKey))
        {
            return _workingWordList.Count;
        }

        if (lastSpeechUnit.IsDefiniteTypeSet())
        {
            return _workingWordList.Count;
        }

        return 1;
    }

    private bool CheckLastWordForNoun(SpeechUnit lastSpeechUnit, string lastSpeechUnitKey)
    {
        List<string> remainingPartOfSpeechList = new List<string>();
        List<string> foundTypeList = new List<string>();

        string[] POS = lastSpeechUnit._workingPartsOfSpeech.Split(';');
        foreach (var part in POS)
        {
            switch (part)
            {
                case "noun":
                    foundTypeList.Add("noun");
                    break;
                case "proper noun":
                    foundTypeList.Add("proper noun");
                    break;
                case "pronoun":
                    foundTypeList.Add("pronoun");
                    break;
                default:
                    remainingPartOfSpeechList.Add(part);
                    break;
            }
        }

        if (remainingPartOfSpeechList.Count == 0 && foundTypeList.Count > 0)
        {
            _workingWordList[lastSpeechUnitKey].SetDefiniteType("noun");
            _masterWordList[lastSpeechUnitKey].SetDefiniteType("noun");

            string wordToTheLeftOfHash = string.Empty;
            string secondWordLeft = string.Empty;

            if (GrammarHelper.FindHashOf(ref wordToTheLeftOfHash, _workingWordList, lastSpeechUnitKey, -1))
            {
 
                if (_workingWordList[wordToTheLeftOfHash]._isDeterminers)
                {
                    _workingWordList[wordToTheLeftOfHash].SetDefiniteType("determiner");
                    _masterWordList[wordToTheLeftOfHash].SetDefiniteType("determiner");

                }
                
                if (GrammarHelper.FindHashOf(ref secondWordLeft, _workingWordList, lastSpeechUnitKey, -2))
                {

                    if (_workingWordList[lastSpeechUnitKey]._definiteType == "noun" && _workingWordList[wordToTheLeftOfHash].IsAdjective())
                    {
                        if (_workingWordList[secondWordLeft]._isInterrogative)
                        {
                            _workingWordList[wordToTheLeftOfHash].SetDefiniteType("adjective");
                            _masterWordList[wordToTheLeftOfHash].SetDefiniteType("adjective");

                            if (_workingWordList[lastSpeechUnitKey].IsNoun())
                            {
                                _workingWordList[lastSpeechUnitKey].SetDefiniteType("noun");
                                _masterWordList[lastSpeechUnitKey].SetDefiniteType("noun");
                            }
                        }
                    }

                    if (_workingWordList[wordToTheLeftOfHash]._isDeterminers && _workingWordList[secondWordLeft]._isDateTimeUnit)
                    {
                        if (_workingWordList[secondWordLeft].IsDeterminerNumber())
                        {
                            _workingWordList[secondWordLeft].SetDefiniteType("determiner");
                            _masterWordList[secondWordLeft].SetDefiniteType("determiner");
                        }
                    }

                    if (_workingWordList[secondWordLeft]._isInterrogative && _workingWordList[secondWordLeft]._display == "how")
                    {
                        if (_workingWordList[lastSpeechUnitKey]._isAuxiliary)
                        {
                            if (_workingWordList[wordToTheLeftOfHash].IsAdverb())
                            {
                                _workingWordList[wordToTheLeftOfHash].SetDefiniteType("adverb");
                                _masterWordList[wordToTheLeftOfHash].SetDefiniteType("adverb");
                            }
                        }
                        else
                        {
                            if (_workingWordList[lastSpeechUnitKey].IsNoun() && _workingWordList[wordToTheLeftOfHash].IsAdjective())
                            {
                                _workingWordList[wordToTheLeftOfHash].SetDefiniteType("adjective");
                                _masterWordList[lastSpeechUnitKey].SetDefiniteType("adjective");
                                _workingWordList[lastSpeechUnitKey].SetDefiniteType("noun");
                                _masterWordList[lastSpeechUnitKey].SetDefiniteType("noun");
                            }
                        }
                    }

                    if (_workingWordList[lastSpeechUnitKey]._definiteType == "noun" && _workingWordList[secondWordLeft]._isDeterminers)
                    {
                        _workingWordList[secondWordLeft].SetDefiniteType("determiner");
                        _masterWordList[secondWordLeft].SetDefiniteType("determiner");
                    }

                    if (_workingWordList[lastSpeechUnitKey]._definiteType == "noun" && _workingWordList[wordToTheLeftOfHash].IsVerb() && _workingWordList[secondWordLeft]._display == "to")
                    {
                        _workingWordList[wordToTheLeftOfHash].SetDefiniteType("verb");
                        _masterWordList[wordToTheLeftOfHash].SetDefiniteType("verb");
                        _workingWordList[secondWordLeft].SetDefiniteType("adverb");
                        _masterWordList[wordToTheLeftOfHash].SetDefiniteType("adverb");
                    }

                    if (_workingWordList[lastSpeechUnitKey]._definiteType == "noun" && _workingWordList[wordToTheLeftOfHash].IsAdjective() && _workingWordList[secondWordLeft]._isDeterminers)
                    {
                        _workingWordList[wordToTheLeftOfHash].SetDefiniteType("adjective");
                        _masterWordList[wordToTheLeftOfHash].SetDefiniteType("adjective");
                        _workingWordList[secondWordLeft].SetDefiniteType("determiner");
                        _masterWordList[secondWordLeft].SetDefiniteType("determiner");

                    }

                    if (_workingWordList[lastSpeechUnitKey]._definiteType == "noun" && _workingWordList[secondWordLeft]._definiteType == "preposition")
                    {
                        if (_workingWordList[wordToTheLeftOfHash].IsAdjective())
                        {
                            _workingWordList[wordToTheLeftOfHash].SetDefiniteType("adjective");
                            _masterWordList[wordToTheLeftOfHash].SetDefiniteType("adjective");
                        }
                    }
                }
            } 
            return true;
        }

        return false;
    }

    private bool CheckLastWordForAdverb(SpeechUnit lastSpeechUnit, string lastSpeechUnitKey)
    {
        if (!lastSpeechUnit.IsAdverb()) return false;

        if (lastSpeechUnit.IsPreposition())
        {
            _workingWordList[lastSpeechUnitKey].AddToExclusions(new List<string>(){"preposition"});
            _masterWordList[lastSpeechUnitKey].AddToExclusions(new List<string>() { "preposition" });
        }

        string wordLeftKey = "";

        if (GrammarHelper.FindHashOf(ref wordLeftKey, _workingWordList, lastSpeechUnitKey, -1))
        {
            if (_workingWordList[wordLeftKey].IsInterjection())
            {
                _workingWordList[lastSpeechUnitKey].AddToExclusions(new List<string>(){"interjection"});
                _masterWordList[lastSpeechUnitKey].AddToExclusions(new List<string>() {"interjection"});
            }

            if (_workingWordList[wordLeftKey].GetIsInterrogative())
            {
                if (!_workingWordList[lastSpeechUnitKey].IsSpecificNoun() && !_workingWordList[lastSpeechUnitKey].IsVerb())
                {
                    if (_workingWordList[wordLeftKey].GetDefiniteType() == "adverb")
                    {
                        _workingWordList[lastSpeechUnitKey].SetDefiniteType("adverb");
                        _masterWordList[lastSpeechUnitKey].SetDefiniteType("adverb");
                    }
                }
            }

            if (_workingWordList[wordLeftKey].GetIsDeterminers())
            {
                _workingWordList[wordLeftKey].SetDefiniteType("determiner");
                _masterWordList[wordLeftKey].SetDefiniteType("determiner");
                _workingWordList[lastSpeechUnitKey].CheckForDefiniteAgainstWorkingList();
                _masterWordList[lastSpeechUnitKey].CheckForDefiniteAgainstWorkingList();

                return false;
            }

            if (_workingWordList[wordLeftKey].IsPreposition() && _workingWordList[wordLeftKey].IsAdverb())
            {
                _workingWordList[lastSpeechUnitKey].AddToExclusions(new List<string>(){"adverb"});
                _masterWordList[lastSpeechUnitKey].AddToExclusions(new List<string>() { "adverb" });
                _workingWordList[lastSpeechUnitKey].CheckForDefiniteAgainstWorkingList();
                _masterWordList[lastSpeechUnitKey].CheckForDefiniteAgainstWorkingList();

                return false;
            }
        }

        string secondWordHash = "";

        if (GrammarHelper.FindHashOf(ref secondWordHash, _workingWordList, lastSpeechUnitKey, -2))
        {
            
            if (_workingWordList[secondWordHash].IsInterjection())
            {
                _workingWordList[secondWordHash].AddToExclusions(new List<string>(){"interjection"});
                _masterWordList[secondWordHash].AddToExclusions(new List<string>() {"interjection"});
            }

            if (_workingWordList[wordLeftKey].IsVerb() && _workingWordList[wordLeftKey].IsAdjective())
            {
                if (!_workingWordList[wordLeftKey].IsNoun())
                {
                    if (_workingWordList[secondWordHash].IsNounVerbOnly())
                    {
                        _workingWordList[wordLeftKey].SetDefiniteType("verb");
                        _workingWordList[secondWordHash].SetDefiniteType("noun");
                        _masterWordList[wordLeftKey].SetDefiniteType("verb");
                        _masterWordList[secondWordHash].SetDefiniteType("noun");
                    }
                }
            }

            if (_workingWordList[wordLeftKey].IsNounVerbOnly())
            {
                _workingWordList[lastSpeechUnitKey].SetDefiniteType("adverb");
                _masterWordList[lastSpeechUnitKey].SetDefiniteType("adverb");

                if (_workingWordList[secondWordHash].GetIsDeterminers())
                {
                    _workingWordList[secondWordHash].SetDefiniteType("determiner");
                    _masterWordList[secondWordHash].SetDefiniteType("determiner");

                    if (_workingWordList[wordLeftKey].IsNoun())
                    {
                        _workingWordList[wordLeftKey].SetDefiniteType("noun");
                        _masterWordList[wordLeftKey].SetDefiniteType("noun");
                    }

                }

                if (_workingWordList[secondWordHash].CanOnlyBeANoun())
                {
                    _workingWordList[secondWordHash].SetDefiniteType("noun");
                    _masterWordList[secondWordHash].SetDefiniteType("noun");

                    if (_workingWordList[wordLeftKey].IsVerb())
                    {
                        _workingWordList[wordLeftKey].SetDefiniteType("verb");
                        _masterWordList[wordLeftKey].SetDefiniteType("verb");
                    }
                }

                if (_workingWordList[wordLeftKey].IsVerb())
                {
                    if (_workingWordList[secondWordHash].GetIsAuxiliary())
                    {
                        _workingWordList[wordLeftKey].SetDefiniteType("verb");
                        _masterWordList[wordLeftKey].SetDefiniteType("verb");
                    }

                    if (_workingWordList[secondWordHash].GetDisplay() == "to")
                    {
                        _workingWordList[wordLeftKey].SetDefiniteType("verb");
                        _workingWordList[secondWordHash].SetDefiniteType("adverb");
                        _masterWordList[wordLeftKey].SetDefiniteType("verb");
                        _masterWordList[secondWordHash].SetDefiniteType("adverb");
                    }

                    if (_workingWordList[secondWordHash].GetDefiniteType() == "noun")
                    {
                        if (_workingWordList[wordLeftKey].GetDefiniteType() != "noun")
                        {
                            _workingWordList[wordLeftKey].SetDefiniteType("verb");
                            _masterWordList[wordLeftKey].SetDefiniteType("verb");
                        }
                    }
                }

                return true;
            }

            if (_workingWordList[lastSpeechUnitKey].IsVerbAdverbOnly())
            {
                if (_workingWordList[wordLeftKey].IsNounAdverbOnly())
                {
                    if (_workingWordList[secondWordHash].IsPreposition())
                    {
                        _workingWordList[wordLeftKey].SetDefiniteType("noun");
                        _workingWordList[secondWordHash].SetDefiniteType("preposition");
                        _masterWordList[wordLeftKey].SetDefiniteType("noun");
                        _masterWordList[secondWordHash].SetDefiniteType("preposition");
                    }
                }

                return true;
            }
        }

        if (_workingWordList[wordLeftKey].IsNounVerbOnly())
        {
            _workingWordList[lastSpeechUnitKey].SetDefiniteType("adverb");
            _masterWordList[lastSpeechUnitKey].SetDefiniteType("adverb");

            return true;
        }

        if (_workingWordList[lastSpeechUnitKey].GetWorkingPartsOfSpeechCount() == 1)
        {
            _workingWordList[lastSpeechUnitKey].SetDefiniteType("adverb");
            _masterWordList[lastSpeechUnitKey].SetDefiniteType("adverb");

            return true;
        }
        
        return false;
    }

    private bool CheckLastWordForVerb(SpeechUnit lastSpeechUnit, string lastSpeechUnitKey)
    {
        if (!lastSpeechUnit.IsVerb()) return false;

        string wordLeftKey = "";

        if (GrammarHelper.FindHashOf(ref wordLeftKey, _workingWordList, lastSpeechUnitKey, -1))
        {
            string secondWordLeftHash = "";
            if (GrammarHelper.FindHashOf(ref secondWordLeftHash, _workingWordList, lastSpeechUnitKey, -2))
            {
                if (lastSpeechUnit.GetIsAuxiliary() && _workingWordList[wordLeftKey].IsNoun() && _workingWordList[secondWordLeftHash].GetIsInterrogative())
                {
                    if (!_workingWordList[secondWordLeftHash].IsAdverb() && _workingWordList[secondWordLeftHash].GetDisplay() != "who")
                    {
                        _workingWordList[wordLeftKey].SetDefiniteType("noun");
                        _masterWordList[wordLeftKey].SetDefiniteType("noun");
                    }
                }
            }

            if (_workingWordList[lastSpeechUnitKey].IsVerb() && _workingWordList[lastSpeechUnitKey].IsAdjective())
            {
                if (!_workingWordList[lastSpeechUnitKey].IsNoun())
                {
                    if (_workingWordList[wordLeftKey].IsNounVerbOnly())
                    {
                        _workingWordList[lastSpeechUnitKey].SetDefiniteType("verb");
                        _workingWordList[wordLeftKey].SetDefiniteType("noun");
                        _masterWordList[lastSpeechUnitKey].SetDefiniteType("verb");
                        _masterWordList[wordLeftKey].SetDefiniteType("noun");
                    }
                }
            }

            if (_workingWordList[wordLeftKey].IsInterjection())
            {
                _workingWordList[wordLeftKey].AddToExclusions(new List<string>(){"interjection"});
                _masterWordList[wordLeftKey].AddToExclusions(new List<string>() {"interjection"});
            }

            if (_workingWordList[wordLeftKey].GetIsDeterminers())
            {
                _workingWordList[wordLeftKey].SetDefiniteType("determiner");
                _workingWordList[lastSpeechUnitKey].AddToExclusions(new List<string>(){"verb"});
                _workingWordList[lastSpeechUnitKey].CheckForDefiniteAgainstWorkingList();

                _masterWordList[wordLeftKey].SetDefiniteType("determiner");
                _masterWordList[lastSpeechUnitKey].AddToExclusions(new List<string>() { "verb" });
                _masterWordList[lastSpeechUnitKey].CheckForDefiniteAgainstWorkingList();

                return false;
            }

            if (_workingWordList[wordLeftKey].GetDefiniteType() == "adverb")
            {
                _workingWordList[lastSpeechUnitKey].SetDefiniteType("verb");
                _masterWordList[lastSpeechUnitKey].SetDefiniteType("verb");

                return true;
            }

            if (_workingWordList[wordLeftKey].IsPreposition())
            {
                if (!_workingWordList[wordLeftKey].IsAdverb())
                {
                    _workingWordList[lastSpeechUnitKey].AddToExclusions(new List<string>(){"verb"});
                    _masterWordList[lastSpeechUnitKey].AddToExclusions(new List<string>() { "verb" });

                    if (!_workingWordList[secondWordLeftHash].GetIsDeterminers())
                    {
                        _workingWordList[wordLeftKey].SetDefiniteType("preposition");
                        _workingWordList[lastSpeechUnitKey].CheckForDefiniteAgainstWorkingList();

                        _masterWordList[wordLeftKey].SetDefiniteType("preposition");
                        _masterWordList[lastSpeechUnitKey].CheckForDefiniteAgainstWorkingList();

                        return false;
                    }
                }
                else
                {
                    if (_workingWordList[secondWordLeftHash].GetIsDeterminers())
                    {
                        if (_workingWordList[wordLeftKey].IsAdjective() && _workingWordList[lastSpeechUnitKey].IsNoun())
                        {
                            _workingWordList[wordLeftKey].SetDefiniteType("adjective");
                            _workingWordList[lastSpeechUnitKey].SetDefiniteType("noun");

                            _masterWordList[wordLeftKey].SetDefiniteType("adjective");
                            _masterWordList[lastSpeechUnitKey].SetDefiniteType("noun");

                            return false;
                        }
                    }
                    else
                    {
                        _workingWordList[lastSpeechUnitKey].SetDefiniteType("verb");
                        _workingWordList[wordLeftKey].SetDefiniteType("adverb");

                        _masterWordList[lastSpeechUnitKey].SetDefiniteType("verb");
                        _masterWordList[wordLeftKey].SetDefiniteType("adverb");

                        return true;
                    }
                }
            }

            if (_workingWordList[wordLeftKey].GetIsAuxiliary())
            {
                _workingWordList[lastSpeechUnitKey].SetDefiniteType("verb");
                _masterWordList[lastSpeechUnitKey].SetDefiniteType("verb");
                return true;
            }

            if (_workingWordList[wordLeftKey].IsAdverb() && _workingWordList[wordLeftKey].IsVerb() &&
                !_workingWordList[wordLeftKey].IsAdjective() && !_workingWordList[wordLeftKey].IsNoun())
            {
                _workingWordList[lastSpeechUnitKey].SetDefiniteType("verb");
                _masterWordList[lastSpeechUnitKey].SetDefiniteType("verb");
                return true;
            }

            if (_workingWordList[wordLeftKey].GetDefiniteType() == "adjective")
            {
                _workingWordList[lastSpeechUnitKey].AddToExclusions(new List<string>(){"verb"});
                _workingWordList[lastSpeechUnitKey].CheckForDefiniteAgainstWorkingList();

                _masterWordList[lastSpeechUnitKey].AddToExclusions(new List<string>() { "verb" });
                _masterWordList[lastSpeechUnitKey].CheckForDefiniteAgainstWorkingList();


                return false;
            }

            if (_workingWordList[wordLeftKey].GetDefiniteType() == "verb" &&
                !_workingWordList[wordLeftKey].GetIsAuxiliary())
            {
                _workingWordList[lastSpeechUnitKey].AddToExclusions(new List<string>(){"verb"});
                _workingWordList[lastSpeechUnitKey].CheckForDefiniteAgainstWorkingList();

                _masterWordList[lastSpeechUnitKey].AddToExclusions(new List<string>() { "verb" });
                _masterWordList[lastSpeechUnitKey].CheckForDefiniteAgainstWorkingList();

                return false;
            }

            if (_workingWordList[wordLeftKey].CanOnlyBeANoun())
            {
                _workingWordList[lastSpeechUnitKey].SetDefiniteType("verb");
                _masterWordList[lastSpeechUnitKey].SetDefiniteType("verb");
                return true;
            }

            if (lastSpeechUnit.GetWorkingPartsOfSpeechCount() == 1)
            {
                _workingWordList[lastSpeechUnitKey].SetDefiniteType("verb");
                _masterWordList[lastSpeechUnitKey].SetDefiniteType("verb");

                return true;
            }
        }
        
        return false;
    }

    public bool CheckLastWordForNounAgainstVerb(SpeechUnit lastSpeechUnit, string lastSpeechUnitKey)
    {
        string wordLeftKey = "";
        string firstWordKey = _workingWordList.Keys.First();

        // hard check for two word prompt
        string wordToRightOfHash = "";
        if (GrammarHelper.FindHashOf(ref wordToRightOfHash, _masterWordList, firstWordKey, 1))
        {
            if (_masterWordList.Count == 2){
                if (_workingWordList[firstWordKey].GetDefiniteType() == "N/A" && _masterWordList[wordToRightOfHash].GetDefiniteType() == "N/A")
                {
                    List<string> verbTerms = GrammarData.GetActionVerbTerms();
                    if (verbTerms.Contains(_workingWordList[firstWordKey]._display))
                    {
                        _masterWordList[firstWordKey].SetDefiniteType("verb");
                        _workingWordList[firstWordKey].SetDefiniteType("verb");

                        if (_masterWordList[wordToRightOfHash].IsNoun()){
                            _masterWordList[wordToRightOfHash].SetDefiniteType("noun");
                        }
                    }
                }
            }
        }


        if (GrammarHelper.FindHashOf(ref wordLeftKey, _workingWordList, lastSpeechUnitKey, -1))
        {
            if (_workingWordList[lastSpeechUnitKey].IsNoun() && _workingWordList[wordLeftKey]._definiteType == "preposition")
            {
                _workingWordList[lastSpeechUnitKey].SetDefiniteType("noun");
                _masterWordList[lastSpeechUnitKey].SetDefiniteType("noun");
            }

            if (_workingWordList[wordLeftKey]._isDeterminers)
            {
                _workingWordList[wordLeftKey].SetDefiniteType("determiner");
                _workingWordList[lastSpeechUnitKey].SetDefiniteType("noun");

                _masterWordList[wordLeftKey].SetDefiniteType("determiner");
                _masterWordList[lastSpeechUnitKey].SetDefiniteType("noun");

                return true;
            }

            if (_workingWordList[wordLeftKey]._definiteType == "conjunction")
            {
                string wordToRight = "";
                if (GrammarHelper.FindHashOf(ref wordToRight, _masterWordList, lastSpeechUnitKey, 1))
                {
                    if (_masterWordList[wordToRight]._definiteType == "noun")
                    {
                        _workingWordList[lastSpeechUnitKey].AddToExclusions(new List<string> { "noun" });
                        _masterWordList[lastSpeechUnitKey].AddToExclusions(new List<string> { "noun" });

                        return false;
                    }
                }
            }

            if (_workingWordList[wordLeftKey]._definiteType == "adjective")
            {
                _workingWordList[lastSpeechUnitKey].SetDefiniteType("noun");
                _masterWordList[lastSpeechUnitKey].SetDefiniteType("noun");
                return true;
            }

            if (_workingWordList[wordLeftKey]._isInterrogative)
            {
                if (!_workingWordList[lastSpeechUnitKey].IsAdverb())
                {
                    _workingWordList[lastSpeechUnitKey].SetDefiniteType("noun");
                    _workingWordList[wordLeftKey].SetDefiniteType("determiner");

                    _masterWordList[lastSpeechUnitKey].SetDefiniteType("noun");
                    _masterWordList[wordLeftKey].SetDefiniteType("determiner");
                }
            }

            if (_workingWordList[wordLeftKey].IsVerb() && _workingWordList[wordLeftKey].IsAdverb() && !_workingWordList[wordLeftKey].IsNoun())
            {
                _workingWordList[lastSpeechUnitKey].SetDefiniteType("noun");
                _masterWordList[lastSpeechUnitKey].SetDefiniteType("noun");
                return true;
            }

            if (_workingWordList[wordLeftKey]._definiteType == "verb" && !_workingWordList[wordLeftKey]._isAuxiliary)
            {
                if (_workingWordList[lastSpeechUnitKey].IsNoun())
                {
                    _workingWordList[lastSpeechUnitKey].SetDefiniteType("noun");
                    _masterWordList[lastSpeechUnitKey].SetDefiniteType("noun");
                    return true;
                }
            }

            if (_workingWordList[wordLeftKey]._isAuxiliary)
            {
                _workingWordList[lastSpeechUnitKey].AddToExclusions(new List<string> { "noun" });
                _workingWordList[lastSpeechUnitKey].CheckForDefiniteAgainstWorkingList();

                _masterWordList[lastSpeechUnitKey].AddToExclusions(new List<string> { "noun" });
                _masterWordList[lastSpeechUnitKey].CheckForDefiniteAgainstWorkingList();

                return false;
            }

            if (_workingWordList[wordLeftKey].IsPreposition() && !_workingWordList[wordLeftKey].IsVerbNounOrAdjective())
            {
                if (_workingWordList[wordLeftKey]._display == "to")
                {
                    if (!_workingWordList[lastSpeechUnitKey].IsVerb())
                    {
                        _workingWordList[lastSpeechUnitKey].SetDefiniteType("noun");
                        _workingWordList[wordLeftKey].SetDefiniteType("preposition");

                        _masterWordList[lastSpeechUnitKey].SetDefiniteType("noun");
                        _masterWordList[wordLeftKey].SetDefiniteType("preposition");

                        return true;
                    }
                }
            }

            string secondWordLeftKey = "";
            if (GrammarHelper.FindHashOf(ref secondWordLeftKey, _workingWordList, lastSpeechUnitKey, -2))
            {
                if (_workingWordList[secondWordLeftKey]._isInterrogative && _workingWordList[secondWordLeftKey]._display == "how")
                {
                    _workingWordList[secondWordLeftKey].SetDefiniteType("adverb");
                    _masterWordList[secondWordLeftKey].SetDefiniteType("adverb");

                    if (_workingWordList[lastSpeechUnitKey]._isAuxiliary)
                    {
                        if (_workingWordList[wordLeftKey].IsAdverb())
                        {
                            _workingWordList[wordLeftKey].SetDefiniteType("adverb");
                            _masterWordList[wordLeftKey].SetDefiniteType("adverb");
                        }
                    }
                    else
                    {
                        if (_workingWordList[lastSpeechUnitKey].IsNoun() && _workingWordList[wordLeftKey].IsAdjective())
                        {
                            _workingWordList[wordLeftKey].SetDefiniteType("adjective");
                            _masterWordList[wordLeftKey].SetDefiniteType("adjective");
                            _workingWordList[lastSpeechUnitKey].SetDefiniteType("noun");
                            _masterWordList[lastSpeechUnitKey].SetDefiniteType("noun");

                        }
                    }
                }

                    if (_workingWordList[secondWordLeftKey]._isDeterminers){
                    
                        _workingWordList[secondWordLeftKey].SetDefiniteType("determiner");
                        _masterWordList[secondWordLeftKey].SetDefiniteType("determiner");

                        if (_workingWordList[wordLeftKey].IsNounVerbOnly()){
                            if (_workingWordList[lastSpeechUnitKey].IsNounVerbOnly()){
                                
                                _workingWordList[wordLeftKey].SetDefiniteType("noun");
                                _masterWordList[wordLeftKey].SetDefiniteType("noun");

                                _workingWordList[lastSpeechUnitKey].SetDefiniteType("verb");
                                _masterWordList[lastSpeechUnitKey].SetDefiniteType("verb");
                            }
                        }
                    }

                if (_workingWordList[wordLeftKey]._definiteType == "adjective")
                {
                    if (_workingWordList[secondWordLeftKey].IsPreposition() && _workingWordList[wordLeftKey].IsNounVerbOnly() && _workingWordList[secondWordLeftKey]._display != "to")
                    {
                        if (_workingWordList[lastSpeechUnitKey].IsAdverb())
                        {
                            _workingWordList[secondWordLeftKey].SetDefiniteType("preposition");
                            _workingWordList[wordLeftKey].SetDefiniteType("noun");
                            _workingWordList[lastSpeechUnitKey].AddToExclusions(new List<string> { "noun" });
                            _workingWordList[lastSpeechUnitKey].CheckForDefiniteAgainstWorkingList();

                            _masterWordList[secondWordLeftKey].SetDefiniteType("preposition");
                            _masterWordList[wordLeftKey].SetDefiniteType("noun");
                            _masterWordList[lastSpeechUnitKey].AddToExclusions(new List<string> { "noun" });
                            _masterWordList[lastSpeechUnitKey].CheckForDefiniteAgainstWorkingList();

                            return false;
                        }
                    }

                    if (_workingWordList[secondWordLeftKey].IsPreposition() && _workingWordList[wordLeftKey].IsAdjective())
                    {
                        if (!_workingWordList[lastSpeechUnitKey].IsAdverb())
                        {
                            if (_workingWordList[lastSpeechUnitKey].IsVerb() && _workingWordList[lastSpeechUnitKey].IsNoun() &&
                                _workingWordList[wordLeftKey].IsVerb() && _workingWordList[wordLeftKey].IsNoun())
                            {
                                return false;
                            }
                            else
                            {
                                _workingWordList[secondWordLeftKey].SetDefiniteType("preposition");
                                _workingWordList[wordLeftKey].SetDefiniteType("adjective");
                                _workingWordList[lastSpeechUnitKey].SetDefiniteType("noun");

                                _masterWordList[secondWordLeftKey].SetDefiniteType("preposition");
                                _masterWordList[wordLeftKey].SetDefiniteType("adjective");
                                _masterWordList[lastSpeechUnitKey].SetDefiniteType("noun");

                                return true;
                            }
                        }
                    }
                }

                if (_workingWordList[secondWordLeftKey].IsPreposition() && _workingWordList[wordLeftKey].IsNounVerbOnly() && _workingWordList[lastSpeechUnitKey].IsNounVerbOnly())
                {
                    _workingWordList[lastSpeechUnitKey].SetDefiniteType("noun");
                    _masterWordList[lastSpeechUnitKey].SetDefiniteType("noun");
                }
                
                if (_workingWordList[lastSpeechUnitKey].IsNoun() && _workingWordList[wordLeftKey].IsVerbAdjectiveOnly() && _workingWordList[secondWordLeftKey]._isDeterminers)
                {
                    _workingWordList[lastSpeechUnitKey].SetDefiniteType("noun");
                    _workingWordList[wordLeftKey].SetDefiniteType("adjective");
                    _workingWordList[secondWordLeftKey].SetDefiniteType("determiner");

                    _masterWordList[lastSpeechUnitKey].SetDefiniteType("noun");
                    _masterWordList[wordLeftKey].SetDefiniteType("adjective");
                    _masterWordList[secondWordLeftKey].SetDefiniteType("determiner");
                }


                if (_workingWordList[lastSpeechUnitKey].IsNounVerbOnly() && _workingWordList[wordLeftKey].IsNounVerbOnly())
                {
                    if (_workingWordList[secondWordLeftKey]._isInterrogative)
                    {
                        if (!_workingWordList[secondWordLeftKey].IsDefiniteTypeSet())
                        {
                            _workingWordList[secondWordLeftKey].SetDefiniteType("determiner");
                            _workingWordList[wordLeftKey].SetDefiniteType("noun");
                            _workingWordList[lastSpeechUnitKey].SetDefiniteType("verb");

                            _masterWordList[secondWordLeftKey].SetDefiniteType("determiner");
                            _masterWordList[wordLeftKey].SetDefiniteType("noun");
                            _masterWordList[lastSpeechUnitKey].SetDefiniteType("verb");
                        }
                    }
                }

                if (_workingWordList[lastSpeechUnitKey].IsNoun() && _workingWordList[wordLeftKey]._isDeterminers && _workingWordList[secondWordLeftKey]._definiteType == "preposition")
                {
                    _workingWordList[lastSpeechUnitKey].SetDefiniteType("noun");
                    _masterWordList[lastSpeechUnitKey].SetDefiniteType("noun");
                }

                if (_workingWordList[lastSpeechUnitKey].IsNounVerbOnly() && _workingWordList[wordLeftKey].IsNoun())
                {
                    if (_workingWordList[secondWordLeftKey]._isInterrogative)
                    {
                        if (_workingWordList[secondWordLeftKey]._definiteType == "pronoun")
                        {
                            _workingWordList[wordLeftKey].SetDefiniteType("verb");
                            _workingWordList[lastSpeechUnitKey].SetDefiniteType("noun");

                            _masterWordList[wordLeftKey].SetDefiniteType("verb");
                            _masterWordList[lastSpeechUnitKey].SetDefiniteType("noun");
                        }

                        if (_workingWordList[secondWordLeftKey]._definiteType == "determiner")
                        {
                            _workingWordList[wordLeftKey].SetDefiniteType("noun");
                            _workingWordList[lastSpeechUnitKey].SetDefiniteType("verb");

                            _masterWordList[wordLeftKey].SetDefiniteType("noun");
                            _masterWordList[lastSpeechUnitKey].SetDefiniteType("verb");
                        }
                    }
                }

                if (_workingWordList[lastSpeechUnitKey].IsNoun() && _workingWordList[wordLeftKey].IsAdjective())
                {
                    if (_workingWordList[secondWordLeftKey]._isInterrogative && _workingWordList[secondWordLeftKey]._definiteType == "adjective")
                    {
                        _workingWordList[lastSpeechUnitKey].SetDefiniteType("noun");
                        _workingWordList[wordLeftKey].SetDefiniteType("adjective");

                        _masterWordList[lastSpeechUnitKey].SetDefiniteType("noun");
                        _masterWordList[wordLeftKey].SetDefiniteType("adjective");
                    }
                }

                if (_workingWordList[lastSpeechUnitKey].IsNounVerbOnly() && _workingWordList[wordLeftKey].IsNounAdjective())
                {
                    if (_workingWordList[secondWordLeftKey]._isInterrogative)
                    {
                        if (!_workingWordList[secondWordLeftKey].IsDefiniteTypeSet())
                        {
                            _workingWordList[secondWordLeftKey].SetDefiniteType("determiner");
                            _workingWordList[wordLeftKey].SetDefiniteType("noun");
                            _workingWordList[lastSpeechUnitKey].SetDefiniteType("verb");

                            _masterWordList[secondWordLeftKey].SetDefiniteType("determiner");
                            _masterWordList[wordLeftKey].SetDefiniteType("noun");
                            _masterWordList[lastSpeechUnitKey].SetDefiniteType("verb");
                        }
                    }
                }

                if (_workingWordList[lastSpeechUnitKey].IsAdverb() || _workingWordList[lastSpeechUnitKey].IsVerb())
                {
                    if (_workingWordList[secondWordLeftKey]._isInterrogative && _workingWordList[wordLeftKey].IsNounVerbOnly())
                    {
                        if (_workingWordList[secondWordLeftKey]._display == "which")
                        {
                            _workingWordList[lastSpeechUnitKey].SetDefiniteType("verb");
                            _workingWordList[wordLeftKey].SetDefiniteType("noun");
                            _workingWordList[secondWordLeftKey].SetDefiniteType("determiner");

                            _masterWordList[lastSpeechUnitKey].SetDefiniteType("verb");
                            _masterWordList[wordLeftKey].SetDefiniteType("noun");
                            _masterWordList[secondWordLeftKey].SetDefiniteType("determiner");
                        }
                    }
                }
            }
        }
        
        return true;
    }

    private void UnknownCheckStart()
    {
        string lastWordKey = _workingWordList.Keys.ToList().Last();

        int depth = 0;
        if (GrammarHelper.IsLastSpeechUnit(_workingWordList, lastWordKey, 1))
        {
            depth = UnknownPhraseRecursion(lastWordKey, depth);
        }

        string phrase = "";
        int loop = depth;
        List<string> keys = _workingWordList.Keys.ToList();
        
        while (loop > -1)
        {
            int keyIndex = keys.Count - 1 - loop;
            if (keyIndex >= 0)
            {
                phrase += _workingWordList[keys[keyIndex]]._display + " ";
            }

            loop--;
        }

        SetPhraseType(lastWordKey, "unknown phrase", depth);
    }

    private int UnknownPhraseRecursion(string currentWordHash, int depth)
    {
        string wordToTheLeftOfHash = "";
        int returnDepth = depth;

        if (GrammarHelper.FindHashOf(ref wordToTheLeftOfHash, _workingWordList, currentWordHash, -1))
        {
            var leftWordSpeechUnit = _workingWordList[wordToTheLeftOfHash];
            var leftWordMasterUnit = _masterWordList[wordToTheLeftOfHash];

            string secondWordLeftHash = "";
            if (GrammarHelper.FindHashOf(ref secondWordLeftHash, _workingWordList, currentWordHash, -2))
            {
                if (_workingWordList[currentWordHash]._isDeterminers)
                {
                    if (leftWordSpeechUnit.IsAdjective())
                    {
                        leftWordSpeechUnit.AddToExclusions(new List<string>() { "adjective" });
                        leftWordMasterUnit.AddToExclusions(new List<string>() { "adjective" });
                    }
                    if (leftWordSpeechUnit.IsAdverb())
                    {
                        if (!_workingWordList[secondWordLeftHash].IsPreposition())
                        {
                            return returnDepth;
                        }
                    }
                }

                if (_workingWordList[currentWordHash].IsNoun() && leftWordSpeechUnit._isDeterminers)
                {
                    var firstWordKeyInRemainingList = _workingWordList.Keys.First();
                    if (firstWordKeyInRemainingList == _workingWordList[secondWordLeftHash]._hash)
                    {
                        if (!_workingWordList[secondWordLeftHash]._isInterrogative && _workingWordList[secondWordLeftHash].IsVerb())
                        {
                            _workingWordList[secondWordLeftHash].SetDefiniteType("verb");
                            _masterWordList[secondWordLeftHash].SetDefiniteType("verb");

                            if (!_workingWordList[currentWordHash].IsAdjective())
                            {
                                _workingWordList[currentWordHash].SetDefiniteType("noun");
                                _masterWordList[currentWordHash].SetDefiniteType("noun");

                                return returnDepth - 1;
                            }
                        }
                    }
                }
            }

            if (_workingWordList[currentWordHash]._display == "much" || _workingWordList[currentWordHash]._display == "many")
            {
                if (_workingWordList[wordToTheLeftOfHash]._isInterrogative && _workingWordList[wordToTheLeftOfHash]._display == "how")
                {
                    string nextMasterHash = "";
                    if (GrammarHelper.FindHashOf(ref nextMasterHash, _masterWordList, currentWordHash, 1))
                    {
                        if (_masterWordList[nextMasterHash]._isAuxiliary)
                        {
                            _workingWordList[currentWordHash].SetDefiniteType("adverb");
                            _masterWordList[currentWordHash].SetDefiniteType("adverb");
                        }
                        else
                        {
                            _workingWordList[currentWordHash].SetDefiniteType("adjective");
                            _masterWordList[currentWordHash].SetDefiniteType("adjective");

                            if (_masterWordList[nextMasterHash].IsNoun())
                            {
                                _masterWordList[nextMasterHash].SetDefiniteType("noun");
                            }
                        }
                    }
                }
            }

            if (_workingWordList[currentWordHash]._display == "long")
            {
                if (_workingWordList[wordToTheLeftOfHash]._isInterrogative && _workingWordList[wordToTheLeftOfHash]._display == "how")
                {
                    _workingWordList[currentWordHash].SetDefiniteType("adverb");
                    _masterWordList[currentWordHash].SetDefiniteType("adverb");

                }
            }

            if (leftWordSpeechUnit._definiteType == "conjunction")
            {
                returnDepth = UnknownPhraseRecursion(wordToTheLeftOfHash, depth + 1);
            }

            if (leftWordSpeechUnit._isAuxiliary && _workingWordList[currentWordHash]._definiteType == "adjective")
            {
                return returnDepth;
            }

            if (leftWordSpeechUnit._definiteType == "N/A" && !leftWordSpeechUnit._isDeterminers && !leftWordSpeechUnit._isAuxiliary)
            {
                returnDepth = UnknownPhraseRecursion(wordToTheLeftOfHash, depth + 1);
            }

            if (_workingWordList[currentWordHash]._isDeterminers && !leftWordSpeechUnit.IsPreposition())
            {
                var firstWordKeyInRemainingList = _workingWordList.Keys.First();
                if (firstWordKeyInRemainingList == leftWordSpeechUnit._hash)
                {
                    if (!leftWordSpeechUnit._isInterrogative && leftWordSpeechUnit.IsVerb())
                    {
                        leftWordSpeechUnit.SetDefiniteType("verb");
                        leftWordMasterUnit.SetDefiniteType("verb");
                        return returnDepth - 1;
                    }
                }
                return returnDepth;
            }

            if (leftWordSpeechUnit._isDeterminers)
            {
                returnDepth = UnknownPhraseRecursion(wordToTheLeftOfHash, depth + 1);
                return returnDepth;
            }
        }

        return returnDepth;
    }




}


