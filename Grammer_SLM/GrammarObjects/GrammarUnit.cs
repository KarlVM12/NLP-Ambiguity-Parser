using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Grammar;
using JetBrains.Annotations;
using Newtonsoft.Json;
using UnityEngine;

[DataContract]
public class GrammarUnit
{
    [DataMember] 
    public string _hash;
    
    [DataMember] 
    public string _phraseType;

    [DataMember] 
    public List<string> _speechHashUnits;

    [DataMember] 
    public bool _isInterrogative;

    [DataMember] 
    public SpeechUnit phraseFragment;

    [DataMember] 
    public List<List<string>> _grammarPatterns = new List<List<string>>();

    [DataMember] 
    public List<List<string>> _patternHolder = new List<List<string>>();

    public GrammarUnit(string phraseType, List<string> speechHashUnits)
    {
        _hash = AppHelper.newGuid;
        _phraseType = phraseType;
        _speechHashUnits = speechHashUnits;
    }

    public void AddToFrontOfSpeechHashUnits(string hash)
    {
        _speechHashUnits.Insert(0, hash);
    }

    public void SetIsInterrogative(bool value)
    {
        _isInterrogative = value;
    }

    public string GetPhraseType()
    {
        return _phraseType;
    }

    public string GetHash()
    {
        return _hash;
    }

    public List<string> GetSpeechHashUnits()
    {
        return _speechHashUnits;
    }

    public void MergeSpeechHashUnits(List<string> speechHashUnits)
    {
        _speechHashUnits.AddRange(speechHashUnits);
        
        //remove potential duplicate hashes
        _speechHashUnits = _speechHashUnits.Distinct().ToList();
    }

    public void SetPossibleGrammarPatterns(Dictionary<string, SpeechUnit> masterWordList)
    {
        if (_phraseType != "unknown phrase")
        {
            _grammarPatterns.Add(new List<string>(){_phraseType});
            
            return;
        }
        
        GetPatternSetSpeechUnit(new List<string>(), 0, masterWordList);

        _grammarPatterns = new List<List<string>>();

        foreach (List<string> pattern in _patternHolder)
        {
            if (!IsGrammarPatternExclusion(pattern))
            {
                _grammarPatterns.Add(pattern);
            }
        }
        
        //UnityEngine.Debug.Log("GRAMMAR PATTERNS" + JsonConvert.SerializeObject(_grammarPatterns));
    }
    
    private bool IsGrammarPatternExclusion(List<string> grammar)
    {
        //UnityEngine.Debug.Log("Grammar " + JsonConvert.SerializeObject(grammar));
        int currentPosition = 0;

        while (currentPosition < grammar.Count)
        {
            if (currentPosition + 3 < grammar.Count)
            {
                if (grammar[currentPosition] == "adjective" && grammar[currentPosition + 1] == "noun" &&
                    grammar[currentPosition + 2] == "adjective" && grammar[currentPosition + 3] == "noun")
                {
                    return true;
                } 
                if (grammar[currentPosition] == "noun" && grammar[currentPosition + 1] == "adjective" &&
                      grammar[currentPosition + 2] == "preposition" && grammar[currentPosition + 3] == "noun")
                {
                    return true;
                } 
                if (grammar[currentPosition] == "determiner" && grammar[currentPosition + 1] == "adjective" &&
                      grammar[currentPosition + 2] == "verb" && grammar[currentPosition + 3] == "adverb")
                {
                    return true;
                }
                if (grammar[currentPosition] == "determiner" && grammar[currentPosition + 1] == "adjective" &&
                      grammar[currentPosition + 2] == "adverb" && grammar[currentPosition + 3] == "verb")
                {
                    return true;
                }
            }

            if (currentPosition + 2 < grammar.Count)
            {
                if (grammar[currentPosition] == "adjective" && grammar[currentPosition + 1] == "preposition" && grammar[currentPosition + 2] == "adjective")
                {
                    return true;
                }
                if (grammar[currentPosition] == "adjective" && grammar[currentPosition + 1] == "determiner" && grammar[currentPosition + 2] == "adjective")
                {
                    return true;
                } 
                if (grammar[currentPosition] == "adjective" && grammar[currentPosition + 1] == "determiner" && grammar[currentPosition + 2] == "noun")
                {
                    return true;
                } 
                if (grammar[currentPosition] == "adjective" && grammar[currentPosition + 1] == "noun" && grammar[currentPosition + 2] == "adjective")
                {
                    return true;
                } 
                if (grammar[currentPosition] == "determiner" && grammar[currentPosition + 1] == "noun" && grammar[currentPosition + 2] == "adjective")
                {
                    return true;
                } 
                if (grammar[currentPosition] == "determiner" && grammar[currentPosition + 1] == "adverb" && grammar[currentPosition + 2] == "noun")
                {
                    return true;
                } 
                if (grammar[currentPosition] == "determiner" && grammar[currentPosition + 1] == "adjective" && grammar[currentPosition + 2] == "conjunction")
                {
                    return true;
                } 
                if (grammar[currentPosition] == "preposition" && grammar[currentPosition + 1] == "adverb" && grammar[currentPosition + 2] == "noun")
                {
                    return true;
                } 
                if (grammar[currentPosition] == "adverb" && grammar[currentPosition + 1] == "conjunction" && grammar[currentPosition + 2] == "adjective")
                {
                    return true;
                } 
                if (grammar[currentPosition] == "adverb" && grammar[currentPosition + 1] == "adverb" && grammar[currentPosition + 2] == "adverb")
                {
                    return true;
                }
                if (grammar[currentPosition] == "noun" && grammar[currentPosition + 1] == "adjective" && grammar[currentPosition + 2] == "preposition")
                {
                    return true;
                } 
                if (grammar[currentPosition] == "noun" && grammar[currentPosition + 1] == "adverb" && grammar[currentPosition + 2] == "adjective")
                {
                    return true;
                } 
                if (grammar[currentPosition] == "noun" && grammar[currentPosition + 1] == "adjective" && grammar[currentPosition + 2] == "noun")
                {
                    return true;
                } 
                if (grammar[currentPosition] == "noun" && grammar[currentPosition + 1] == "adjective" && grammar[currentPosition + 2] == "preposition")
                {
                    return true;
                } 
                if (grammar[currentPosition] == "pronoun" && grammar[currentPosition + 1] == "adverb" && grammar[currentPosition + 2] == "adjective")
                {
                    return true;
                }
                if (grammar[currentPosition] == "verb" && grammar[currentPosition + 1] == "conjunction" && grammar[currentPosition + 2] == "noun")
                {
                    return true;
                } 
                if (grammar[currentPosition] == "preposition" && grammar[currentPosition + 1] == "adjective" && grammar[currentPosition + 2] == "preposition")
                {
                    return true;
                }
            }

            if (currentPosition + 1 < grammar.Count)
            {
                if (grammar[currentPosition] == "noun" && grammar[currentPosition + 1] == "noun")
                {
                    return true;
                } 
                if (grammar[currentPosition] == "adjective" && grammar[currentPosition + 1] == "adjective")
                {
                    return true;
                } 
                if (grammar[currentPosition] == "adjective" && grammar[currentPosition + 1] == "adverb")
                {
                    return true;
                } 
                if (grammar[currentPosition] == "adjective" && grammar[currentPosition + 1] == "verb")
                {
                    return true;
                } 
                if (grammar[currentPosition] == "adjective" && grammar[currentPosition + 1] == "adverb")
                {
                    return true;
                } 
                if (grammar[currentPosition] == "adjective" && grammar[currentPosition + 1] == "interjection")
                {
                    return true;
                } 
                if (grammar[currentPosition] == "adverb" && grammar[currentPosition + 1] == "noun")
                {
                    return true;
                }
                if (grammar[currentPosition] == "noun" && grammar[currentPosition + 1] == "pronoun")
                {
                    return true;
                } 
                if (grammar[currentPosition] == "noun" && grammar[currentPosition + 1] == "interjection")
                {
                    return true;
                } 
                if (grammar[currentPosition] == "determiner" && grammar[currentPosition + 1] == "verb")
                {
                    return true;
                } 
                if (grammar[currentPosition] == "determiner" && grammar[currentPosition + 1] == "interjection")
                {
                    return true;
                } 
                if (grammar[currentPosition] == "preposition" && grammar[currentPosition + 1] == "verb")
                {
                    return true;
                } 
                if (grammar[currentPosition] == "preposition" && grammar[currentPosition + 1] == "preposition")
                {
                    return true;
                } 
                if (grammar[currentPosition] == "preposition" && grammar[currentPosition + 1] == "adverb")
                {
                    return true;
                } 
                if (grammar[currentPosition] == "pronoun" && grammar[currentPosition + 1] == "noun")
                {
                    return true;
                } 
                if (grammar[currentPosition] == "proper noun" && grammar[currentPosition + 1] == "noun")
                {
                    return true;
                } 
                if (grammar[currentPosition] == "verb" && grammar[currentPosition + 1] == "verb")
                {
                    return true;
                } 
                if (grammar[currentPosition] == "int pronoun" && grammar[currentPosition + 1] == "noun")
                {
                    return true;
                } 
                if (grammar[currentPosition] == "int determiner" && grammar[currentPosition + 1] == "verb")
                {
                    return true;
                }
            }

            currentPosition++;
        }

        return false;
    }

    public void AssessGrammarPattern(string before, string after)
    {
        UnityEngine.Debug.Log("GRAMMAR PATTERNS");
        UnityEngine.Debug.Log(before);
        UnityEngine.Debug.Log(after);
        for (int i = 0; i < _grammarPatterns.Count; i++)
        {
            List<string> patterns = _grammarPatterns[i];

            if (before == null)
            {
                bool firstPatterns = CheckAgainstFirstValues(after, patterns);
                if (!firstPatterns)
                {
                    _grammarPatterns.RemoveAt(i);
                    i--;
                }
            }else if (after == null)
            {
                bool lastPatterns = CheckAgainstLastValues(before, patterns);
                if (!lastPatterns)
                {
                    _grammarPatterns.RemoveAt(i);
                    i--;
                }
            }
            else
            {
                bool patterIsValid = CheckAgainstFirstAndLastValues(before, after, patterns);
                if (!patterIsValid)
                {
                    _grammarPatterns.RemoveAt(i);
                    i--;
                }
            }
        }
    }

    private bool CheckAgainstFirstAndLastValues(string before, string after, List<string> unknowns)
    {
        List<string> firstHolder = new List<string>
        {
            before,
            unknowns[0]
        };
        
        List<string> lastHolder = new List<string>();
        List<string> countOneHolder = new List<string>();
        int lastIndex = unknowns.Count - 1;

        if (unknowns.Count > 1)
        {
            firstHolder.Add(unknowns[1]);
            lastHolder.Add(unknowns[lastIndex - 1]);
        }else if (unknowns.Count == 1)
        {
            countOneHolder.Add(before);
            countOneHolder.Add(unknowns[0]);
            countOneHolder.Add(after);

            if (IsGrammarPatternExclusion(countOneHolder)) return false; 
            
            return true;
        }
        
        lastHolder.Add(unknowns[lastIndex]);
        lastHolder.Add(after);

        if (IsGrammarPatternExclusion(firstHolder)) return false;
        if (IsGrammarPatternExclusion(lastHolder)) return false;
        
        return true;
    }

    private bool CheckAgainstLastValues(string before, List<string> unknowns)
    {
        List<string> firstHolder = new List<string>
        {
            before,
            unknowns[0]
        };

        if (unknowns.Count > 1)
        {
            firstHolder.Add(unknowns[1]);
            if (unknowns.Count > 2)
            {
                firstHolder.Add(unknowns[2]);
            }
        }else if (unknowns.Count == 1)
        {
            if (IsGrammarPatternExclusion(firstHolder)) return false;
        }

        if (IsGrammarPatternExclusion(firstHolder)) return false;

        return true;
    }

    private bool CheckAgainstFirstValues(string after, List<string> unknowns)
    {
        List<string> lastHolder = new List<string>();
        int lastIndex = unknowns.Count - 1;
        
        if (unknowns.Count > 1)
        {
            if (unknowns.Count > 2)
            {
                lastHolder.Add(unknowns[lastIndex - 2]);
            }
            
            lastHolder.Add(unknowns[lastIndex - 1]);
            lastHolder.Add(unknowns[lastIndex]);
            lastHolder.Add(after);

            if (IsGrammarPatternExclusion(lastHolder)) return false;
        }else if (unknowns.Count == 1)
        {
            lastHolder.Add(unknowns[lastIndex]);
            lastHolder.Add(after);

            if (IsGrammarPatternExclusion(lastHolder)) return false;
        }

        return true;
    }

    public string GetLastWordGrammarOfPhrase(Dictionary<string, SpeechUnit> masterWordList)
    {
        bool endReached = false;
        return masterWordList[_speechHashUnits[^1]].GetPatternDataSet(0, ref endReached);
    }
    
    public string GetFirstWordGrammarOfPhrase(Dictionary<string, SpeechUnit> masterWordList)
    {
        bool endReached = false;
        return masterWordList[_speechHashUnits[0]].GetPatternDataSet(0, ref endReached);
    }
    
    private void GetPatternSetSpeechUnit(List<string> currentArray, int wordIndex, Dictionary<string, SpeechUnit> masterWordList)
    {
        if (wordIndex > _speechHashUnits.Count - 1)
        {
            _patternHolder.Add(JsonConvert.DeserializeObject<List<string>>(JsonConvert.SerializeObject(currentArray)));
            return;
        }
        

        int counterIndex = 0;
        int nextWordIndex = wordIndex + 1;
        bool endReached = false;

        while (!endReached)
        {
            string holderSet = masterWordList[_speechHashUnits[wordIndex]].GetPatternDataSet(counterIndex, ref endReached);
            
            if (!endReached)
            {
                currentArray.Insert(wordIndex, holderSet);
                GetPatternSetSpeechUnit(currentArray, nextWordIndex, masterWordList);
            }

            counterIndex++;
        }
    }
}