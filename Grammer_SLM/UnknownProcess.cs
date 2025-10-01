using System.Collections.Generic;
using System.Linq;
using Grammar;
using Newtonsoft.Json;
using Unity.VisualScripting;
using UnityEngine;

public class UnknownProcess
{
    private Dictionary<string, SpeechUnit> _masterWordList;

    private List<GrammarUnit> _grammarPhraseList;

    private List<string> _masterWordListKeys;

    private List<string> _tempMasterWordListKeys = new List<string>();

    private List<List<string>> _grammarRemainingPatterns;

    public List<List<List<object>>> _tempPatternHolder = new List<List<List<object>>>();
    
    public UnknownProcess(Dictionary<string, SpeechUnit> masterWordList, List<GrammarUnit> grammarPhraseList, List<List<string>> grammarRemainingPatterns, bool dontModify = false)
    {
        if (dontModify)
        {
            _masterWordList =  JsonConvert.DeserializeObject<Dictionary<string, SpeechUnit>>(JsonConvert.SerializeObject(masterWordList));
            _grammarPhraseList =  JsonConvert.DeserializeObject<List<GrammarUnit>>(JsonConvert.SerializeObject(grammarPhraseList));
            _grammarRemainingPatterns = JsonConvert.DeserializeObject<List<List<string>>>(JsonConvert.SerializeObject(grammarRemainingPatterns));
        }
        else
        {
            _masterWordList = masterWordList;
            _grammarPhraseList = grammarPhraseList;
            _grammarRemainingPatterns = grammarRemainingPatterns;    
        }
        
        _masterWordListKeys = _masterWordList.Keys.ToList();
    }

    public void Start()
    {

        if (_masterWordList.Count == 1){
            return;
        }

        SpeechUnitUnknownProcess(0);

        foreach (GrammarUnit grammarUnit in _grammarPhraseList)
        {
            grammarUnit.SetPossibleGrammarPatterns(_masterWordList);
        }

        for (int i = 0; i < _grammarPhraseList.Count; i++)
        {
            GrammarUnit grammarUnit = _grammarPhraseList[i];
            
            if (grammarUnit._grammarPatterns.Count > 1)
            {
                GrammarUnit unitBefore = null;
                GrammarUnit unitAfter = null;

                if (i > 1)
                {
                    unitBefore = _grammarPhraseList[i - 1];
                }

                if (i < _grammarPhraseList.Count - 1)
                {
                    unitAfter = _grammarPhraseList[i + 1];
                }

                if (unitBefore != null && unitAfter != null)
                {
                    grammarUnit.AssessGrammarPattern(unitBefore.GetLastWordGrammarOfPhrase(_masterWordList), unitAfter.GetFirstWordGrammarOfPhrase(_masterWordList));
                }else if (unitBefore != null)
                {
                    grammarUnit.AssessGrammarPattern(unitBefore.GetLastWordGrammarOfPhrase(_masterWordList), null);
                }else if (unitAfter != null)
                {
                    grammarUnit.AssessGrammarPattern(null, unitAfter.GetFirstWordGrammarOfPhrase(_masterWordList));
                }
            }
        }


        foreach (GrammarUnit grammarUnit in _grammarPhraseList)
        {
            if (grammarUnit._phraseType == "unknown phrase")
            {
                List<List<string>> grammarPossible = new List<List<string>>();

                int count = 0;

                foreach (List<string> patterns in grammarUnit._grammarPatterns)
                {
                    for (int i = 0; i < patterns.Count; i++)
                    {
                        string pattern = patterns[i];

                        if (i == 0)
                        {
                            grammarPossible.Insert(count, new List<string>());
                        }

                        if (!grammarPossible[count].Contains(pattern))
                        {
                            grammarPossible[count].Add(pattern);
                        }
                    }

                    count++;
                }

                int fff = System.Math.Min(grammarPossible.Count, grammarUnit._speechHashUnits.Count);
                for (int i = 0; i < fff; i++)
                {
                    List<string> grammar = grammarPossible[i];
                    string speechKeyHash = grammarUnit._speechHashUnits[i];
                    _masterWordList[speechKeyHash].GrammarPhraseAdjust(grammar);
                }

                _grammarRemainingPatterns = grammarUnit._grammarPatterns;
            }
        }

        _grammarPhraseList.Clear();

        
        UnknownProcess unknownProcess = new UnknownProcess(_masterWordList, _grammarPhraseList, _grammarRemainingPatterns);
        unknownProcess.SpeechUnitUnknownProcess(0);
    }

    public void SpeechUnitUnknownProcess(int wordIndex)
    {
        ProcessUnknownGrammar();
        
        SetupSpeechUnitUnknownProcess();


        foreach (List<List<object>> patterns in _tempPatternHolder)
        {
            List<string> holder = TakeUnknownPatternArrayAndGetType(patterns);

            if (GrammarHelper.IsGrammarPatternExclusion(holder, true))
            {
                if (wordIndex >= 0 && wordIndex < patterns.Count && patterns[wordIndex].Count > 2)
                {
                    string key = (string)patterns[wordIndex][2];
                    if (_masterWordList.ContainsKey(key) && _masterWordList[key].GetWorkingPartsOfSpeechCount() > 1)
                    {
                        if (patterns[wordIndex].Count > 1)
                        {
                            _masterWordList[key].AddToExclusionsByIndex((int)patterns[wordIndex][1]);
                        }
                    }
                    else
                    {
                        wordIndex++;
                    }
                    SpeechUnitUnknownProcess(wordIndex);
                    break;
                }
            }
        }
    }

    private void SetupSpeechUnitUnknownProcess()
    {
        List<List<string>> unknownPatternHashes = GetUnknownPatterns();

        if (unknownPatternHashes.Count > 0)
        {
            _tempMasterWordListKeys.AddRange(unknownPatternHashes[0]);
            GetUnknownWordPatternArray(new List<List<object>>(), 0);
        }

    }

    private void GetUnknownWordPatternArray(List<List<object>> currentArray, int wordIndex)
    {
        if (wordIndex > _tempMasterWordListKeys.Count - 1)
        {
            _tempPatternHolder.Add(currentArray);
            return;
        }

        int counterIndex = wordIndex; // fix: was 0 before - caused many sentences to crash and give N/A types since recursion would not update it in cases
        int nextWordIndex = wordIndex + 1;
        bool endReached = false;

        while (!endReached)
        {
            List<object> holderSet = new List<object>();
            holderSet.Add(_masterWordList[_tempMasterWordListKeys[wordIndex]].GetPatternDataSet(counterIndex, ref endReached));
            holderSet.Add(counterIndex);
            holderSet.Add(_tempMasterWordListKeys[wordIndex]);

            if (!endReached)
            {
                currentArray.Add(holderSet);
                GetUnknownWordPatternArray(currentArray, nextWordIndex);
            }

            counterIndex++;
        }
    }

    private List<string> TakeUnknownPatternArrayAndGetType(List<List<object>> patterns)
    {
        List<string> result = new List<string>();

        foreach (List<object> unknowns in patterns)
        {
            result.Add((string)unknowns[0]);
        }

        return result;
    }

    private List<List<string>> GetUnknownPatterns()
    {
        List<List<string>> unknownList = new List<List<string>>();
        List<string> currentList = new List<string>();
        bool foundUnknown = false;
        int loop = 0;

        while (loop < _masterWordListKeys.Count)
        {
            if (_masterWordList[_masterWordListKeys[loop]].IsDefiniteTypeSet())
            {
                if (foundUnknown)
                {
                    currentList.Add(_masterWordListKeys[loop]);
                }
                else
                {
                    foundUnknown = true;
                    currentList = new List<string>();
                    if (loop - 1 >= 0)
                    {
                        currentList.Add(_masterWordListKeys[loop - 1]);
                        currentList.Add(_masterWordListKeys[loop]);
                    }
                }
            }
            else
            {
                if (foundUnknown)
                {
                    currentList.Add(_masterWordListKeys[loop]);
                    unknownList.Add(currentList);
                    foundUnknown = false;
                }
            }

            loop++;
        }

        return unknownList;
    }

    private void ProcessUnknownGrammar()
    {
        List<int> unknownPhraseIndexes = new List<int>();

        for (int i = 0; i < _grammarPhraseList.Count; i++)
        {
            GrammarUnit grammarUnit = _grammarPhraseList[i];

            if (grammarUnit.GetPhraseType() == "unknown phrase")
            {
                unknownPhraseIndexes.Add(i);
            }
        }

        foreach (int index in unknownPhraseIndexes)
        {
            ProcessUnknownGrammarPhrases(index);
        }
    }

    private void ProcessUnknownGrammarPhrases(int index)
    {
        string phraseBefore = null;
        string phraseAfter = null;

        if (index - 1 > -1)
        {
            phraseBefore = _grammarPhraseList[index - 1].GetPhraseType();
        }

        if (index + 1 < _grammarPhraseList.Count)
        {
            phraseAfter = _grammarPhraseList[index + 1].GetPhraseType();
        }
        
        //useful debug statement here
    }
}