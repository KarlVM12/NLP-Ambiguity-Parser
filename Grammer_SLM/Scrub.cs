using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

using UnityEngine;
using Object = System.Object;

public class Scrub
{
    private string _theSentence;
    public Dictionary<int, string> _capitalTerms = new Dictionary<int, string>();
    public List<int> _apostropheIndexes = new List<int>();
    
    private readonly List<int> _possessiveIndexes = new List<int>();
    
    public Scrub(string sentence)
    {
        _theSentence = sentence; 
    }
    
    public List<object> Process()
    {
        List<object> results = new List<object>();

        GetCapitalWordsWithIndices(_theSentence);
        Debug.Log(JsonConvert.SerializeObject(_capitalTerms));
        if (_capitalTerms.Count > 0){
            GetContractionIndexes(_capitalTerms);
        }
        
        _theSentence = _theSentence.ToLower();

        GetPossessiveIndexes();
        
        GetContractions(); 

        RegexFilter(); 

        RemoveDuplicatesCaptainAndI();
        
        FilterSentenceForDateTimes(); 
        
        results.Add(_theSentence);
        results.Add(_possessiveIndexes);

        return results;
    }

    private void GetPossessiveIndexes()
    {
        List<string> words = _theSentence.Split(" ").ToList();

        for (int i = 0; i < words.Count; i++)
        {
            if (words[i].EndsWith("'s")){
                // if strtolower words[i] not in contractions database
                string wordLower = words[i].ToLower();
                List<DictionaryContraction> contractionsData = GrammarData.GetDictionaryContractions();

                foreach (var key in contractionsData){
                    if (key.GetTerm() != wordLower && !_possessiveIndexes.Contains(i)){
                        _possessiveIndexes.Add(i);
                    }
                }
            } 
        }
    }

    private void FilterSentenceForDateTimes()
    {
        List<string> words = _theSentence.Split(" ").ToList();

        if (words.Count == 1) return;

        for (int i = 0; i < words.Count; i++)
        {
            string word = words[i];
            string wordAfter = null;
            string wordAfterAfter = null;

            if (i < words.Count - 1) wordAfter = words[i + 1];
            if (i < words.Count - 2) wordAfterAfter = words[i + 2];

            if (StringHelper.IsNumeric(word) && StringHelper.IsNumeric(wordAfter))
            {
                double wordNumber = double.Parse(word);
                double wordAfterNumber = double.Parse(wordAfter);

                if ((word.Length == 2 && wordNumber < 24 || word.Length == 1) && wordAfter.Length == 2 && wordAfterNumber < 60)
                {
                    words[i] = word + ":" + wordAfter;
                    words.RemoveAt(i + 1);
                    
                    Debug.Log("1");
                    
                    continue;
                }
            }

            if (StringHelper.IsNumeric(word) && word.Length == 4 && (wordAfter == "am" || wordAfter == "pm"))
            {
                double wordNumber = double.Parse(word);

                if (wordNumber > 2)
                {
                    if (i < words.Count - 1)
                    {
                        words.RemoveAt(i + 1);
                    }
                }
                else
                {
                    words[i] = word.Insert(2, ":");
                    Debug.Log("2");

                }
                
                continue;
            }

            if (word.Length == 5 && word[2] == ':' && (wordAfter == "am" || wordAfter == "pm"))
            {
                double wordNumber = double.Parse(word);

                if (wordNumber > 2)
                {
                    if (i < words.Count - 1)
                    {
                        words.RemoveAt(i + 1);
                    }
                }
            }

            if (word.Length == 7 && word[2] == ':' && (word.Substring(word.Length - 2) == "am" || word.Substring(word.Length - 2) == "pm"))
            {
                words[i] = word.Substring(0, 5);
            }
        }
        
        _theSentence =  string.Join(" ", words);
    }
    
    private void GetContractions()
    {
        List<DictionaryContraction> dictionaryContractions = GrammarData.GetDictionaryContractions();
        var termPositions = new Dictionary<string, List<int>>();
        int cumulativeShift = 0;

        foreach (DictionaryContraction dictionaryContraction in dictionaryContractions)
        {
            string term = dictionaryContraction.GetTerm();
            string replacement = dictionaryContraction.GetReplacement();
            var words = _theSentence.Split(' ');

            for (int i = 0; i < words.Length; i++)
            {
                if (Regex.IsMatch(words[i], @"\b" + Regex.Escape(term) + @"\b"))
                {
                    if (!termPositions.ContainsKey(term))
                    {
                        termPositions[term] = new List<int>();
                    }
                    termPositions[term].Add(i);
                }
            }
            
            _theSentence = Regex.Replace(_theSentence, @"\b" + term + @"\b", replacement);

            //Debug.Log(JsonConvert.SerializeObject(termPositions));
            
            cumulativeShift += 1;
            var updatedCapitals = new Dictionary<int, string>();
            foreach (var capital in _capitalTerms)
            {
                int updatedIndex = capital.Key;
                if (termPositions.ContainsKey(term))
                {
                    foreach (var position in termPositions[term])
                    {
                        if (capital.Key > position)
                        {
                            updatedIndex += 1;
                        }
                    }
                }
                updatedCapitals[updatedIndex] = capital.Value;
            }
            _capitalTerms = updatedCapitals;

            //Debug.Log(JsonConvert.SerializeObject(termPositions));

        }
    }

    private void RemoveDuplicatesCaptainAndI()
    {
        List<string> words = _theSentence.Split(" ").ToList();
        
        for (int i = 0; i < words.Count; i++)
        {
            if (words[i] == "captain")
            {
                words.RemoveAt(i);
                continue;
            }

            if (words[i] == "i")
            {
                words[i] = AppHelper.User.FullName;
                continue;
            }

            if (i < words.Count - 1)
            {
                if (words[i] == words[i + 1])
                {
                    words.RemoveAt(i + 1);
                }
            }
        }

        _theSentence = string.Join(" ", words);
    }

    private void RegexFilter()
    {
        _theSentence = Regex.Replace(_theSentence, @"(\d+)(am|pm)", "$1 $2", RegexOptions.IgnoreCase);

        _theSentence = Regex.Replace(_theSentence, ":", " ");

        _theSentence = Regex.Replace(_theSentence, @"[\p{P}]", "");
    }

    private void GetCapitalWordsWithIndices(string sentence)
    {
        string[] words = sentence.Split(' ');

        for (int i = 0; i < words.Length; i++)
        {
            if (!string.IsNullOrEmpty(words[i]) && char.IsUpper(words[i][0]))
            {
                _capitalTerms[i] = words[i];
            }
        }
    }

    private void GetContractionIndexes(Dictionary<int, string> foundTerms){

        Regex apostrophe = new Regex(@"\b\w+'\w+\b");

        foreach (var keyValuePair in foundTerms)
        {
            int key = keyValuePair.Key;
            string term = keyValuePair.Value;

            if (apostrophe.IsMatch(term))
            {
                _apostropheIndexes.Add(key);
            }
        }

    }


}