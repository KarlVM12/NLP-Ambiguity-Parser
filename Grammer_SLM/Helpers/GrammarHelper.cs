using System.Collections.Generic;
using System.Linq;
using Grammar;
using Newtonsoft.Json;
using UnityEngine;

public class GrammarHelper
{
    public static bool FindHashOf(ref string foundHash, Dictionary<string, SpeechUnit> speechUnitList, string currentHash, int direction)
    {
        foundHash = "";

        List<string> keys = speechUnitList.Keys.ToList();
        int index = keys.IndexOf(currentHash) + direction;

        if (index >= 0 && index < keys.Count)
        {
            foundHash = keys[index];
            return true;
        }

        return false;
    }
    
    //allows us to find hash using ambiguousUnits
    public static bool FindAmbiguousHashOf(ref string foundHash, Dictionary<string, List<SpeechUnit>> speechUnitList, string currentHash, int direction)
    {
        foundHash = "";

        List<string> keys = speechUnitList.Keys.ToList();
        int index = keys.IndexOf(currentHash) + direction;

        if (index >= 0 && index < keys.Count)
        {
            foundHash = keys[index];
            return true;
        }

        return false;
    }

    public static bool IsGrammarPatternExclusion(List<string> patternTypes, bool depthOnlyOne = false)
    {
        int currentPosition = 0;

        Debug.LogWarning(JsonConvert.SerializeObject(patternTypes));

        while (currentPosition < patternTypes.Count)
        {
            if (currentPosition + 3 < patternTypes.Count)
            {
                if (patternTypes[currentPosition] == "adjective" && patternTypes[currentPosition + 1] == "noun" && patternTypes[currentPosition + 2] == "adjective" && patternTypes[currentPosition + 3] == "noun")
                {
                    return true;
                }
            }

            if (currentPosition + 2 < patternTypes.Count)
            {
                if (patternTypes[currentPosition] == "adjective" && patternTypes[currentPosition + 1] == "preposition" && patternTypes[currentPosition + 2] == "adjective")
                {
                    return true;
                }
                if (patternTypes[currentPosition] == "adjective" && patternTypes[currentPosition + 1] == "preposition" && patternTypes[currentPosition + 2] == "noun")
                {
                    return true;
                }
                if (patternTypes[currentPosition] == "adjective" && patternTypes[currentPosition + 1] == "noun" && patternTypes[currentPosition + 2] == "adjective")
                {
                    return true;
                }
                if (patternTypes[currentPosition] == "determiner" && patternTypes[currentPosition + 1] == "noun" && patternTypes[currentPosition + 2] == "adjective")
                {
                    return true;
                }
                if (patternTypes[currentPosition] == "preposition" && patternTypes[currentPosition + 1] == "adverb" && patternTypes[currentPosition + 2] == "noun")
                {
                    return true;
                }
                if (patternTypes[currentPosition] == "adverb" && patternTypes[currentPosition + 1] == "conjunction" && patternTypes[currentPosition + 2] == "adjective")
                {
                    return true;
                }
                if (patternTypes[currentPosition] == "adverb" && patternTypes[currentPosition + 1] == "adverb" && patternTypes[currentPosition + 2] == "adverb")
                {
                    return true;
                }
                if (patternTypes[currentPosition] == "noun" && patternTypes[currentPosition + 1] == "adjective" && patternTypes[currentPosition + 2] == "preposition")
                {
                    return true;
                }
                if (patternTypes[currentPosition] == "noun" && patternTypes[currentPosition + 1] == "adverb" && patternTypes[currentPosition + 2] == "adjective")
                {
                    return true;
                }
                if (patternTypes[currentPosition] == "noun" && patternTypes[currentPosition + 1] == "conjunction" && patternTypes[currentPosition + 2] == "verb")
                {
                    return true;
                }
                if (patternTypes[currentPosition] == "noun" && patternTypes[currentPosition + 1] == "adjective" && patternTypes[currentPosition + 2] == "noun")
                {
                    return true;
                }
                if (patternTypes[currentPosition] == "pronoun" && patternTypes[currentPosition + 1] == "adverb" && patternTypes[currentPosition + 2] == "adjective")
                {
                    return true;
                }
                if (patternTypes[currentPosition] == "verb" && patternTypes[currentPosition + 1] == "conjunction" && patternTypes[currentPosition + 2] == "noun")
                {
                    return true;
                }
            }

            if (currentPosition + 1 < patternTypes.Count)
            {
                if (patternTypes[currentPosition] == "noun" && patternTypes[currentPosition + 1] == "noun")
                {
                    return true;
                }
                if (patternTypes[currentPosition] == "adjective" && patternTypes[currentPosition + 1] == "adjective")
                {
                    return true;
                }
                if (patternTypes[currentPosition] == "adjective" && patternTypes[currentPosition + 1] == "adverb")
                {
                    return true;
                }
                if (patternTypes[currentPosition] == "adjective" && patternTypes[currentPosition + 1] == "verb")
                {
                    return true;
                }
                if (patternTypes[currentPosition] == "adjective" && patternTypes[currentPosition + 1] == "adverb")
                {
                    return true;
                }
                if (patternTypes[currentPosition] == "adjective" && patternTypes[currentPosition + 1] == "interjection")
                {
                    return true;
                }
                if (patternTypes[currentPosition] == "adverb" && patternTypes[currentPosition + 1] == "noun")
                {
                    return true;
                }
                if (patternTypes[currentPosition] == "noun" && patternTypes[currentPosition + 1] == "pronoun")
                {
                    return true;
                }
                if (patternTypes[currentPosition] == "noun" && patternTypes[currentPosition + 1] == "interjection")
                {
                    return true;
                }
                if (patternTypes[currentPosition] == "determiner" && patternTypes[currentPosition + 1] == "verb")
                {
                    return true;
                }
                if (patternTypes[currentPosition] == "determiner" && patternTypes[currentPosition + 1] == "interjection")
                {
                    return true;
                }
                if (patternTypes[currentPosition] == "preposition" && patternTypes[currentPosition + 1] == "verb")
                {
                    return true;
                }
                if (patternTypes[currentPosition] == "preposition" && patternTypes[currentPosition + 1] == "preposition")
                {
                    return true;
                }
                if (patternTypes[currentPosition] == "preposition" && patternTypes[currentPosition + 1] == "adverb")
                {
                    return true;
                }
                if (patternTypes[currentPosition] == "pronoun" && patternTypes[currentPosition + 1] == "noun")
                {
                    return true;
                }
                if (patternTypes[currentPosition] == "proper noun" && patternTypes[currentPosition + 1] == "noun")
                {
                    return true;
                }
                if (patternTypes[currentPosition] == "verb" && patternTypes[currentPosition + 1] == "verb")
                {
                    return true;
                }
                if (patternTypes[currentPosition] == "int pronoun" && patternTypes[currentPosition + 1] == "noun")
                {
                    return true;
                }
                if (patternTypes[currentPosition] == "int determiner" && patternTypes[currentPosition + 1] == "verb")
                {
                    return true;
                }
            }

            currentPosition++;
            if (depthOnlyOne)
            {
                currentPosition = patternTypes.Count;
            }
        }

        return false;
    }

    public static bool IsLastSpeechUnit(Dictionary<string, SpeechUnit> masterWordList, string currentKey, int direction)
    { 
        List<string> keys = masterWordList.Keys.ToList();
        int index = keys.IndexOf(currentKey) + direction;

        if (index >= 0 && index < keys.Count)
        {
            return false;
        }

        return true;
    }
}