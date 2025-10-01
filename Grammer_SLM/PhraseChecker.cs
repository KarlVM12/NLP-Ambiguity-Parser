using System;
using System.Collections.Generic;
using System.Linq;
using Grammar;
using Newtonsoft.Json;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

public class PhraseChecker
{
    private Dictionary<string, SpeechUnit> _masterWordList;
    
    private Dictionary<string, SpeechUnit> _workingWordList;
    
    private Dictionary<string, List<SpeechUnit>> _ambiguousUnits;

    private List<GrammarUnit> _grammarPhraseList;

    private string _theSentence;
    
    public PhraseChecker(Dictionary<string, SpeechUnit> masterWordList, Dictionary<string, SpeechUnit> workingWordList,
        List<GrammarUnit> grammarPhraseList, string theSentence)
    {
        _masterWordList = masterWordList;
        _workingWordList = workingWordList;
        _grammarPhraseList = grammarPhraseList;
        _theSentence = theSentence;

        _ambiguousUnits = new Dictionary<string, List<SpeechUnit>>();
    }

    public List<object> BeginAmbiguityCheck()
    {
        bool unknown = CheckForAmbiguity();
        
        List<Tuple<SpeechUnit, int>> phraseFragments = new List<Tuple<SpeechUnit, int>>();

        if (unknown)
        {
            CreateUnknownArray();
            
            List<object> result = IdentifyUnknownPrepNounPhraseAttempt();

            if ((bool)result[0])
            {
                _theSentence = (string)result[1];
                _workingWordList = (Dictionary<string, SpeechUnit>)result[2];
                _grammarPhraseList = (List<GrammarUnit>)result[3];
                _masterWordList = (Dictionary<string, SpeechUnit>)result[4];

                phraseFragments.Add(new Tuple<SpeechUnit, int>((SpeechUnit)result[5], (int)result[6]));
                
                return new List<object>(){_theSentence, phraseFragments, _workingWordList, _grammarPhraseList, _masterWordList};
            }

            if (_ambiguousUnits.Count > 0 && !(bool)result[0])
            {
                MergeCheckStart();
            }
        }
        
        return new List<object>(){_theSentence, phraseFragments};
    }

    public bool CheckForAmbiguity()
    {
        foreach (GrammarUnit grammarUnit in _grammarPhraseList)
        {
            if (grammarUnit.GetPhraseType() == "unknown phrase") return true;
        }

        return false;
    }

    private void CreateUnknownArray()
    {
        foreach (GrammarUnit grammarUnit in _grammarPhraseList)
        {
            if (grammarUnit.GetPhraseType() == "unknown phrase")
            {
                List<SpeechUnit> ambiguousUnitHolder = new List<SpeechUnit>();
                string grammarUnitHash = grammarUnit.GetHash();

                List<string> speechHashUnits = grammarUnit.GetSpeechHashUnits();

                if (speechHashUnits.Count == 1)
                {
                    foreach (SpeechUnit speechUnit in _masterWordList.Values)
                    {
                        if (speechHashUnits[0] == speechUnit.GetHash())
                        {
                            ambiguousUnitHolder.Add(speechUnit);
                        }
                    }
                }
                else
                {
                    foreach (string speechHash in speechHashUnits)
                    {
                        foreach (SpeechUnit speechUnit in _masterWordList.Values)
                        {
                            if (speechHash == speechUnit.GetHash())
                            {
                                ambiguousUnitHolder.Add(speechUnit);

                            }
                        }
                    }
                }
                
                if (ambiguousUnitHolder.Count > 0)
                {
                    _ambiguousUnits[grammarUnitHash] = ambiguousUnitHolder;
                }
            }
        }

        
    }

    private void MergeCheckStart()
    {
        bool unitsAreAdjacent = CheckAdjacent();

        List<SpeechUnit> lastUnits = new List<SpeechUnit>();
        List<SpeechUnit> firstUnits = new List<SpeechUnit>();

        if (unitsAreAdjacent)
        {
            foreach (KeyValuePair<string, List<SpeechUnit>> pair in _ambiguousUnits)
            {
                SpeechUnit lastUnit = pair.Value.Last();
                if (lastUnit != null) lastUnits.Add(lastUnit);

                SpeechUnit firstUnit = pair.Value.First();
                if (firstUnit != null) firstUnits.Add(firstUnit);
            }

            if (firstUnits.Count > 1)
            {
                firstUnits.RemoveAt(0);
            }

            if (lastUnits.Count > 1)
            {
                lastUnits.RemoveAt(lastUnits.Count - 1);
            }

            if (lastUnits.Count == firstUnits.Count)
            {
                CheckForMerge(firstUnits, lastUnits);
            }
        }
    }

    private void CheckForMerge(List<SpeechUnit> firstUnits, List<SpeechUnit> lastUnits)
    {
        for (int i = 0; i < lastUnits.Count; i++)
        {
            if (lastUnits[i].IsPreposition() && (firstUnits[i].GetIsDeterminers() || firstUnits[i].IsAdjective() || firstUnits[i].IsNoun()))
            {
                MergeUnknownArray(firstUnits[i].GetHash(), lastUnits[i].GetHash());
                continue;
            }

            if (lastUnits[i].GetDefiniteType() == "adjective")
            {
                MergeUnknownArray(firstUnits[i].GetHash(), lastUnits[i].GetHash());
                continue;
            }

            if (lastUnits[i].IsAdverb() && (firstUnits[i].IsVerb() || firstUnits[i].IsAdjective()))
            {
                MergeUnknownArray(firstUnits[i].GetHash(), lastUnits[i].GetHash());
                continue;
            }

            if (lastUnits[i].IsAdjective() && firstUnits[i].IsNoun())
            {
                MergeUnknownArray(firstUnits[i].GetHash(), lastUnits[i].GetHash());
            }
        }
    }

    private void MergeUnknownArray(string firstWordHash, string lastWordHash)
    {
        string firstGrammarUnitHash = null;
        string secondGrammarUnitHash = null;

        foreach (GrammarUnit grammarUnit in _grammarPhraseList)
        {
            string currentHash = grammarUnit.GetHash();
            if (grammarUnit.GetPhraseType() == "unknown phrase")
            {
                foreach (string hashUnit in grammarUnit.GetSpeechHashUnits())
                {
                    if (hashUnit == lastWordHash)
                    {
                        firstGrammarUnitHash = currentHash;
                    }

                    if (hashUnit == firstWordHash)
                    {
                        secondGrammarUnitHash = currentHash;
                    }
                }
            }
        }

        List<string> secondUnitHashHolder = new List<string>();

        if (secondGrammarUnitHash != null)
        {
            foreach (GrammarUnit grammarUnit in _grammarPhraseList)
            {
                if (grammarUnit.GetHash() == secondGrammarUnitHash)
                {
                    foreach (string hashUnit in grammarUnit.GetSpeechHashUnits())
                    {
                        secondUnitHashHolder.Add(hashUnit);
                    }
                }
            }
        }

        if (firstGrammarUnitHash != null)
        {
            foreach (GrammarUnit grammarUnit in _grammarPhraseList)
            {
                if (grammarUnit.GetHash() == firstGrammarUnitHash)
                {
                    grammarUnit.MergeSpeechHashUnits(secondUnitHashHolder);
                    break;
                }
            }

            for (int i = 0; i < _grammarPhraseList.Count; i++)
            {
                GrammarUnit grammarUnit = _grammarPhraseList[i];
                if (grammarUnit.GetHash() == secondGrammarUnitHash)
                {
                    _grammarPhraseList.RemoveAt(i);
                }
            }
        }
    }

    private bool CheckAdjacent()
    {
        if (_ambiguousUnits.Count > 1)
        {
            for (int i = 0; i < _grammarPhraseList.Count; i++)
            {
                foreach (KeyValuePair<string, List<SpeechUnit>> pair in _ambiguousUnits)
                {
                    if (_grammarPhraseList[i].GetHash() == pair.Key)
                    {
                        string nextHash = null;
                        if (GrammarHelper.FindAmbiguousHashOf(ref nextHash, _ambiguousUnits, pair.Key, 1))
                        {
                            return _grammarPhraseList[i + 1].GetHash() == nextHash;
                        }
                    }
                }
            }
        }
        
        return false;
    }

    private List<object> IdentifyUnknownPrepNounPhraseAttempt()
    {
        List<GrammarUnit> phraseList = _grammarPhraseList;
        Dictionary<string, SpeechUnit> masterList = _masterWordList;
        int oldUnknownPhraseIndex = 0;



        GrammarUnit unknownPhrase = null;

        for (int i = 0; i < phraseList.Count; i++)
        {
            if (phraseList[i].GetPhraseType() == "unknown phrase")
            {
                oldUnknownPhraseIndex = i;
                unknownPhrase = phraseList[i];
                break;
            }
        }
        
        string partsOfSpeech = null;
        
        if (unknownPhrase != null)
        {
            var speechHashUnits = unknownPhrase.GetSpeechHashUnits();
    
            if (speechHashUnits != null && speechHashUnits.Count > 0)
            {
                partsOfSpeech = masterList[speechHashUnits[0]].GetPartsOfSpeech();
            }
        }
        
        List<Tuple<SpeechUnit, int>> unknownPhraseSpeechUnitsNoDefiniteType = new List<Tuple<SpeechUnit, int>>();
        int maxIndex = 0;

        if (partsOfSpeech != null && !partsOfSpeech.Contains("verb"))
        {
            foreach (string hash in unknownPhrase.GetSpeechHashUnits())
            {
                SpeechUnit speechUnit = masterList[hash];

                if (!speechUnit.IsDefiniteTypeSet())
                {
                    int partsOfSpeechCount = speechUnit.GetPartsOfSpeech().Split(';').Length;
                    unknownPhraseSpeechUnitsNoDefiniteType.Add(new Tuple<SpeechUnit, int>(speechUnit, partsOfSpeechCount));

                    if (partsOfSpeechCount > unknownPhraseSpeechUnitsNoDefiniteType[maxIndex].Item2 ||
                        unknownPhraseSpeechUnitsNoDefiniteType.Count == 1)
                    {
                        maxIndex = unknownPhraseSpeechUnitsNoDefiniteType.Count - 1;
                    }
                }
            }
        }
        
        while (unknownPhraseSpeechUnitsNoDefiniteType.Count > 1)
        {
            int maxValIndex = maxIndex;
            SpeechUnit currentRemovedSpeechUnit = unknownPhraseSpeechUnitsNoDefiniteType[maxIndex].Item1;

            string theSentence = _theSentence.Replace(currentRemovedSpeechUnit._words[0] + " ", "");

            Scrub scrub = new Scrub(theSentence);
            List<object> results = scrub.Process(); 
            Dictionary<int, string> capitals = scrub._capitalTerms;
            List<int> apostrophes = scrub._apostropheIndexes;

            theSentence = results[0].ToString();
            List<int> possessiveIndexes = (List<int>)results[1];
        
            SpeechUnitFormer speechUnitFormer = new SpeechUnitFormer(theSentence, possessiveIndexes, capitals);
            Dictionary<string, SpeechUnit> masterWordList = speechUnitFormer.FormSpeechUnits(); 
            
            PreGrammar preGrammar = new PreGrammar();
            Dictionary<string, SpeechUnit> workingWordList = preGrammar.Setup(masterWordList);
        
            List<GrammarUnit> grammarPhraseList = new List<GrammarUnit>();
            PhraseIdentifier phraseIdentifier = new PhraseIdentifier(masterWordList, workingWordList, grammarPhraseList);
            phraseIdentifier.Start();

            PhraseChecker phraseChecker = new PhraseChecker(masterWordList, workingWordList, grammarPhraseList, theSentence);
            bool unknown = phraseChecker.CheckForAmbiguity();

            if (!unknown)
            {
                //Debug.Log(oldUnknownPhraseIndex + " : " + phraseIdentifier._grammarPhraseList.Count + " : " + _grammarPhraseList.Count);
                //phraseIdentifier._grammarPhraseList[oldUnknownPhraseIndex].phraseFragment = currentRemovedSpeechUnit;
                if (oldUnknownPhraseIndex >= 0 && oldUnknownPhraseIndex < grammarPhraseList.Count)
                    {
                        grammarPhraseList[oldUnknownPhraseIndex].phraseFragment = currentRemovedSpeechUnit;
                    }
                
                return new List<object>()
                {
                    true, theSentence, workingWordList, grammarPhraseList, masterWordList, currentRemovedSpeechUnit,
                    oldUnknownPhraseIndex
                };
            }
            else
            {
                unknownPhraseSpeechUnitsNoDefiniteType.RemoveAt(maxValIndex);

                if (unknownPhraseSpeechUnitsNoDefiniteType.Count == 1) break;
                if (unknownPhraseSpeechUnitsNoDefiniteType.Count == 2) maxIndex = 0;
                else
                {
                    maxValIndex = unknownPhraseSpeechUnitsNoDefiniteType[0].Item2;

                    for (int i = 0; i < unknownPhraseSpeechUnitsNoDefiniteType.Count; i++)
                    {

                        if (unknownPhraseSpeechUnitsNoDefiniteType[i].Item2 > maxValIndex)
                        {
                            maxValIndex = unknownPhraseSpeechUnitsNoDefiniteType[i].Item2;
                        }
                    }

                    maxIndex = maxValIndex;
                }
            }
        }
        
        return new List<object>(){false};
    } 
}