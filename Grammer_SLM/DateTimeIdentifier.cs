using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using Grammar;
using Newtonsoft.Json;
using Unity.VisualScripting;
using UnityEngine;

public class DateTimeIdentifier
{
    private static List<string> _timeIndicators = new List<string>() {"AM", "PM", "UTC", ":", "OCLOCK"};

    private static List<string> _dateIndicators = new List<string>() {"TOMORROW", "TODAY",
        "SUNDAY", "MONDAY", "TUESDAY", "WEDNESDAY", "THURSDAY", "FRIDAY", "SATURDAY",
        "SUN", "MON", "TUE", "WED", "THUR", "FRIDAY", "SAT", "TUES", "THURS",
        "JANUARY", "FEBRUARY", "MARCH", "APRIL", "MAY", "JUNE", "JULY", "AUGUST", "SEPTEMBER", "OCTOBER", "NOVEMBER", "DECEMBER",
        "JAN", "FEB", "MAR", "APR", "JUN", "JUL", "AUG", "SEP", "SEPT", "OCT", "NOV", "DEC"};

    public static List<string> _weekTypeIndicators = new List<string>() {"WEEK", "WEEKDAY", "WEEKEND", "WEEKNIGHT"};

    public static List<string> _dateDescriptors = new List<string>() {"TOMORROW", "TODAY", "YESTERDAY", "NOW"};

    public static List<string> _timeOfDayIndicators = new List<string>() {"MORNING", "EVENING", "AFTERNOON", "NIGHT", "DAWN", "DUSK", "MIDDAY"};

    private static List<string> _wordTimeIndicators = new List<string>() {"MIDNIGHT", "NOON"};
    
    public static List<string> _dateTypeIndicators = new List<string>() {"MONTH", "YEAR", "DAY"};

    private static List<string> _dayIndicators = new List<string>()
    {
        "SUNDAY", "MONDAY", "TUESDAY", "WEDNESDAY",
        "THURSDAY", "FRIDAY", "SATURDAY",
        "SUN", "MON", "TUE", "WED", "THUR", "FRIDAY", "SAT", "TUES", "THURS"
    };

    private static List<string> _ordinalIndicators = new List<string>() { "ST", "ND", "RD", "TH" };

    public static List<WordUnit> GetDateTimeAndTimeIntervalUnits(List<WordUnit> wordUnits_)
    {
        List<WordUnit> wordUnits = wordUnits_;
        
        foreach (WordUnit wordUnit in wordUnits)
        {
            SetTimeBucket(wordUnit);
            SetDateBucket(wordUnit);
        }

        wordUnits = GetTimeIntervalUnits(wordUnits);
        wordUnits = GetDateTimeUnits(wordUnits);

        return wordUnits;
    }

    private static void SetTimeBucket(WordUnit wordUnit)
    {
        string word = wordUnit.GetWord().ToUpper();

        foreach (string indicator in _timeIndicators)
        {
            if (word.Contains(indicator) || (word.Length == 2 && StringHelper.IsNumeric(word)))
            {
                wordUnit.AddBucket(new Bucket("time"));

                break;
            }
        }

        foreach (string indicator in _timeOfDayIndicators)
        {
            if (word.Contains(indicator))
            {
                wordUnit.AddBucket(new Bucket("time"));

                break;
            }
        }
    }

    private static void SetDateBucket(WordUnit wordUnit)
    {
        string word = wordUnit.GetWord().ToUpper();

        if (word == "NOW")
        {
            wordUnit.AddBucket(new Bucket("date"));

            return;
        }
        
        foreach (string indicator in _dateIndicators)
        {
            if (word.Contains(indicator))
            {
                wordUnit.AddBucket(new Bucket("date"));

                break;
            }
        }
        
        foreach (string indicator in _ordinalIndicators)
        {
            if (word.Equals(indicator))
            {
                wordUnit.AddBucket(new Bucket("date"));

                break;
            }
        }
    }

    private static List<WordUnit> GetTimeIntervalUnits(List<WordUnit> wordUnits_)
    {
        List<WordUnit> wordUnits = new List<WordUnit>();
        
        for (int i = 0; i < wordUnits_.Count; i++)
        {
            WordUnit wordUnit = wordUnits_[i];
            WordUnit wordUnitAfter = null;
            WordUnit wordUnitAfterAfter = null;

            if (i < wordUnits_.Count - 1) wordUnitAfter = wordUnits_[i + 1];
            if (i < wordUnits_.Count - 2) wordUnitAfterAfter = wordUnits_[i + 2];
            
            if (IsNumber(wordUnit))
            {
                string number = wordUnit.GetWord();

                if (IsNumber(wordUnitAfter))
                {
                    string numberTwo = wordUnitAfter.GetWord();

                    if (IsUnitOfTime(wordUnitAfterAfter))
                    {
                        string timeUnit = wordUnitAfterAfter.GetWord();

                        string timeInterval = number + " " + numberTwo + " " + timeUnit;
                        
                        wordUnits.Add(BuildWordUnit(wordUnit, new List<string>(){wordUnitAfter.GetWord(), wordUnitAfterAfter.GetWord()}, timeInterval, "timeInterval", "noun", i));

                        i += 2;
                        
                        continue;
                    }
                }

                if (IsUnitOfTime(wordUnitAfter))
                {
                    string timeUnit = wordUnitAfter.GetWord();

                    string timeInterval = number + " " + timeUnit;
                    
                    wordUnits.Add(BuildWordUnit(wordUnit, new List<string>(){wordUnitAfter.GetWord()}, timeInterval, "timeInterval", "noun", i));

                    i++;
                    
                    continue;
                }
            }
            
            wordUnits.Add(wordUnit);
        }

        return wordUnits;
    }

    private static List<WordUnit> GetDateTimeUnits(List<WordUnit> wordUnits_)
    {
        List<WordUnit> wordUnits = new List<WordUnit>();

        for (int i = 0; i < wordUnits_.Count; i++)
        {
            WordUnit wordUnitBefore = null;
            WordUnit wordUnit = wordUnits_[i];
            WordUnit wordUnitAfter = null;
            WordUnit wordUnitAfterAfter = null;
            WordUnit wordUnitAfterAfterAfter = null;
            WordUnit wordUnitAfterAfterAfterAfter = null;
            
            string word = wordUnit.GetWord().ToUpper();

            if (i > 0) wordUnitBefore = wordUnits_[i - 1];
            if (i < wordUnits_.Count - 1) wordUnitAfter = wordUnits_[i + 1];
            if (i < wordUnits_.Count - 2) wordUnitAfterAfter = wordUnits_[i + 2];
            if (i < wordUnits_.Count - 3) wordUnitAfterAfterAfter = wordUnits_[i + 3];
            if (i < wordUnits_.Count - 4) wordUnitAfterAfterAfterAfter = wordUnits_[i + 4];
            
            if (wordUnit.GetSpeechUnit() != null)
            {
                wordUnits.Add(wordUnit);
                
                continue;
            }

            string dateString = null;

            if (word.Length == 4 && StringHelper.IsNumeric(word))
            {
                double wordNumber = double.Parse(word);

                if (wordNumber > 2020 && wordNumber < 2050)
                {
                    wordUnit.SetSpeechUnit(BuildSpeechUnit(wordUnit.GetWord(), "date", "noun", wordUnit, i));
                    wordUnit.SetIsDateTime(true);
                    
                    wordUnits.Add(wordUnit);
                    
                    continue;
                }
            }

            if (_weekTypeIndicators.Contains(word) || _dateTypeIndicators.Contains(word))
            {
                wordUnit.SetSpeechUnit(BuildSpeechUnit(wordUnit.GetWord(), "date", "noun", wordUnit, i));
                wordUnit.SetIsDateTime(true);
                    
                wordUnits.Add(wordUnit);
                    
                continue;
            }

            if (_wordTimeIndicators.Contains(word))
            {
                wordUnit.SetSpeechUnit(BuildSpeechUnit(wordUnit.GetWord(), "time", "noun", wordUnit, i));
                wordUnit.SetIsDateTime(true);
                    
                wordUnits.Add(wordUnit);
                
                continue;
            }

            if (_timeOfDayIndicators.Contains(word))
            {
                string partsOfSpeech = GetPartsOfSpeech(word);
                
                wordUnit.SetSpeechUnit(BuildSpeechUnit(wordUnit.GetWord(), "time", partsOfSpeech, wordUnit, i));
                wordUnit.SetIsDateTime(true);
                
                wordUnits.Add(wordUnit);
                
                continue;
            }

            if (_dayIndicators.Contains(word) || _dateDescriptors.Contains(word))
            {
                string day = StringHelper.UpperCaseFirst(word);

                if (day == "Now")
                {
                    wordUnit.SetSpeechUnit(BuildSpeechUnit(wordUnit.GetWord(), "date", "noun", wordUnit, i));
                    wordUnit.SetIsDateTime(true);
                
                    wordUnits.Add(wordUnit);
                    
                    continue;
                }

                wordUnit.SetSpeechUnit(BuildSpeechUnit(wordUnit.GetWord(), "date", "noun", wordUnit, i));
                wordUnit.SetIsDateTime(true);
                
                wordUnits.Add(wordUnit);
                
                continue;
            }

            if (IsDay(wordUnit, wordUnitBefore, wordUnitAfter))
            {
                string day = StringHelper.UpperCaseFirst(wordUnit.GetWord());

                if (IsMonth(wordUnitAfter))
                {
                    string month = StringHelper.UpperCaseFirst(wordUnitAfter.GetWord());

                    if (IsTime(wordUnitAfterAfter, wordUnitAfter, wordUnitAfterAfterAfter))
                    {
                        string time = wordUnitAfterAfter.GetWord();

                        if (IsTime(wordUnitAfterAfterAfter, wordUnitAfterAfter, wordUnitAfterAfterAfterAfter))
                        { 
                            time += wordUnitAfterAfterAfter.GetWord();
                            
                            dateString = time + " " + month + " " + day;
                            
                            wordUnits.Add(BuildWordUnit(wordUnit, new List<string>(){wordUnitAfter.GetWord(), wordUnitAfterAfter.GetWord(), wordUnitAfterAfterAfter.GetWord()}, dateString, "datetime", "noun", i));

                            i += 3;
                            
                            continue;
                        }
                        dateString = time + " " + month + " " + day;
                        
                        wordUnits.Add(BuildWordUnit(wordUnit, new List<string>(){wordUnitAfter.GetWord(), wordUnitAfterAfter.GetWord()}, dateString, "datetime", "noun", i));

                        i += 2;
                        
                        continue;
                    }

                    dateString = month + day;
                    
                    wordUnits.Add(BuildWordUnit(wordUnit, new List<string>(){wordUnitAfter.GetWord()}, dateString, "date", "noun", i));

                    i += 1;
                    
                    continue;
                }

                if (IsTime(wordUnitAfter, wordUnit, wordUnitAfterAfter))
                {
                    string time = wordUnitAfter.GetWord();

                    if (IsTime(wordUnitAfterAfter, wordUnitAfter, wordUnitAfterAfterAfter))
                    {
                        time += wordUnitAfterAfter.GetWord();

                        if (IsMonth(wordUnitAfterAfterAfter))
                        {
                            string month = wordUnitAfterAfterAfter.GetWord();

                            dateString = time + " " + month + " " + day;
                            
                            wordUnits.Add(BuildWordUnit(wordUnit, new List<string>(){wordUnitAfter.GetWord(), wordUnitAfterAfter.GetWord(), wordUnitAfterAfterAfter.GetWord()}, dateString, "datetime", "noun", i));

                            i += 3;
                            
                            continue;
                        }
                    }

                    if (IsMonth(wordUnitAfterAfter))
                    {
                        string month = wordUnitAfterAfter.GetWord();

                        dateString = time + " " + month + " " + day;
                        
                        wordUnits.Add(BuildWordUnit(wordUnit, new List<string>(){wordUnitAfter.GetWord(), wordUnitAfterAfter.GetWord()}, dateString, "datetime", "noun", i));

                        i += 2;
                        
                        continue;
                    }
                }
                
                wordUnit.SetSpeechUnit(BuildSpeechUnit(wordUnit.GetWord(), "date", "noun;determiner", wordUnit, i));
                wordUnit.SetIsDateTime(true);
                
                wordUnits.Add(wordUnit);
                
                continue;
            }

            if (IsMonth(wordUnit))
            {
                string month = wordUnit.GetWord();

                if (IsDay(wordUnitAfter, wordUnit, wordUnitAfterAfter))
                {
                    string day = StringHelper.UpperCaseFirst(wordUnitAfter.GetWord());

                    if (IsTime(wordUnitAfterAfter, wordUnitAfter, wordUnitAfterAfterAfter))
                    {
                        string time = wordUnitAfterAfter.GetWord();

                        if (IsTime(wordUnitAfterAfterAfter, wordUnitAfterAfter, wordUnitAfterAfterAfterAfter))
                        {
                            time += wordUnitAfterAfterAfter.GetWord();

                            dateString = time + " " + month + " " + day;
                            
                            wordUnits.Add(BuildWordUnit(wordUnit, new List<string>(){wordUnitAfter.GetWord(), wordUnitAfterAfter.GetWord(), wordUnitAfterAfterAfter.GetWord()}, dateString, "datetime", "noun", i));

                            i += 3;
                            
                            continue;
                        }

                        dateString = time + " " + month + " " + day;
                            
                        wordUnits.Add(BuildWordUnit(wordUnit, new List<string>(){wordUnitAfter.GetWord(), wordUnitAfterAfter.GetWord()}, dateString, "datetime", "noun", i));

                        i += 2;
                            
                        continue;
                    }

                    dateString = month + " " + day;
                    
                    wordUnits.Add(BuildWordUnit(wordUnit, new List<string>(){wordUnitAfter.GetWord()}, dateString, "date", "noun", i));

                    i++;
                    
                    continue;
                }

                if (IsTime(wordUnitAfter, wordUnit, wordUnitAfterAfter))
                {
                    string time = wordUnitAfter.GetWord();

                    if (IsTime(wordUnitAfterAfter, wordUnitAfter, wordUnitAfterAfterAfter))
                    {
                        time += wordUnitAfterAfter.GetWord();

                        if (IsDay(wordUnitAfterAfterAfter, wordUnitAfterAfter, wordUnitAfterAfterAfterAfter))
                        {
                            string day = StringHelper.UpperCaseFirst(wordUnitAfterAfterAfter.GetWord());

                            dateString = time + " " + month + " " + day;
                            
                            wordUnits.Add(BuildWordUnit(wordUnit, new List<string>(){wordUnitAfter.GetWord(), wordUnitAfterAfter.GetWord(), wordUnitAfterAfterAfter.GetWord()}, dateString, "datetime", "noun", i));

                            i += 3;
                            
                            continue;
                        }
                    }

                    if (IsDay(wordUnitAfterAfter, wordUnitAfter, wordUnitAfterAfterAfter))
                    {
                        string day = StringHelper.UpperCaseFirst(wordUnitAfterAfter.GetWord());

                        dateString = time + " " + month + " " + day;
                            
                        wordUnits.Add(BuildWordUnit(wordUnit, new List<string>(){wordUnitAfter.GetWord(), wordUnitAfterAfter.GetWord()}, dateString, "datetime", "noun", i));

                        i += 2;
                            
                        continue;
                    }
                }
                
                wordUnit.SetSpeechUnit(BuildSpeechUnit(wordUnit.GetWord(), "date", "noun", wordUnit, i));
                wordUnit.SetIsDateTime(true);
                
                wordUnits.Add(wordUnit);
                
                continue;
            }

            if (IsTime(wordUnit, wordUnitBefore, wordUnitAfter))
            {
                string time = wordUnit.GetWord();

                if (IsTime(wordUnitAfter, wordUnit, wordUnitAfterAfter))
                {
                    time += wordUnitAfter.GetWord();

                    if (IsDay(wordUnitAfterAfter, wordUnitAfter, wordUnitAfterAfterAfter))
                    {
                        string day = wordUnitAfterAfter.GetWord();

                        if (IsMonth(wordUnitAfterAfterAfter))
                        {
                            string month = wordUnitAfterAfterAfter.GetWord();
                            
                            dateString = time + " " + month + " " + day;
                            
                            wordUnits.Add(BuildWordUnit(wordUnit, new List<string>(){wordUnitAfter.GetWord(), wordUnitAfterAfter.GetWord(), wordUnitAfterAfterAfter.GetWord()}, dateString, "datetime", "noun", i));

                            i += 3;
                            
                            continue;
                        }
                    }

                    if (IsMonth(wordUnitAfterAfter))
                    {
                        string month = wordUnitAfterAfter.GetWord();

                        if (IsDay(wordUnitAfterAfterAfter, wordUnitAfterAfter, wordUnitAfterAfterAfterAfter))
                        {
                            string day = StringHelper.UpperCaseFirst(wordUnitAfterAfterAfter.GetWord());
                            
                            dateString = time + " " + month + " " + day;
                            
                            wordUnits.Add(BuildWordUnit(wordUnit, new List<string>(){wordUnitAfter.GetWord(), wordUnitAfterAfter.GetWord(), wordUnitAfterAfterAfter.GetWord()}, dateString, "datetime", "noun", i));

                            i += 3;
                            
                            continue;
                        }
                    }
                    
                    wordUnits.Add(BuildWordUnit(wordUnit, new List<string>(){wordUnitAfter.GetWord()}, time, "time", "noun", i));

                    i++;
                    
                    continue;
                }

                if (IsDay(wordUnitAfter, wordUnit, wordUnitAfterAfter))
                {
                    string day = StringHelper.UpperCaseFirst(wordUnitAfter.GetWord());

                    if (IsMonth(wordUnitAfterAfter))
                    {
                        string month = wordUnitAfterAfter.GetWord();
                        
                        dateString = time + " " + month + " " + day;
                            
                        wordUnits.Add(BuildWordUnit(wordUnit, new List<string>(){wordUnitAfter.GetWord(), wordUnitAfterAfter.GetWord()}, dateString, "datetime", "noun", i));

                        i += 2;
                            
                        continue;
                    }
                }

                if (IsMonth(wordUnitAfter))
                {
                    string month = wordUnitAfter.GetWord();

                    if (IsDay(wordUnitAfterAfter, wordUnitAfter, wordUnitAfterAfterAfter))
                    {
                        string day = wordUnitAfterAfter.GetWord();

                        dateString = time + " " + month + " " + day;
                            
                        wordUnits.Add(BuildWordUnit(wordUnit, new List<string>(){wordUnitAfter.GetWord(), wordUnitAfterAfter.GetWord()}, dateString, "datetime", "noun", i));

                        i += 2;
                            
                        continue;
                    }
                }
                
                wordUnit.SetSpeechUnit(BuildSpeechUnit(wordUnit.GetWord(), "time", "noun", wordUnit, i));
                wordUnit.SetIsDateTime(true);
                
                wordUnits.Add(wordUnit);
                
                continue;
            }
            
            wordUnits.Add(wordUnit);
        }
        
        return wordUnits;
    }

    private static string GetPartsOfSpeech(string word)
    {
        List<DictionaryDateTerm> dictionaryDateTerms = GrammarData.GetDictionaryDateTerms();

        foreach (DictionaryDateTerm dictionaryDateTerm in dictionaryDateTerms)
        {
            if (word.ToLower() == dictionaryDateTerm.GetTerm())
            {
                return dictionaryDateTerm.GetPartsOfSpeech();
            }
        }

        return "noun";
    }

    public static bool IsTime(WordUnit wordUnit, WordUnit wordUnitBefore, WordUnit wordUnitAfter)
    {
        if (wordUnit == null) return false;

        string word = wordUnit.GetWord().ToUpper();

        if (_dateDescriptors.Contains(word)) return false;

        if (_timeOfDayIndicators.Contains(word)) return false;

        if (word.Contains("TH") || word.Contains("ST") || word.Contains("ND") || word.Contains("RD")) return false;

        if (HasBucketType(wordUnit, "time") || (HasBucketType(wordUnit, "date") && StringHelper.IsNumeric(word) && word.Length == 4))
        {
            if (HasBucketType(wordUnitBefore, "date") || HasBucketType(wordUnitAfter, "date")) return true;
            
            if (HasBucketType(wordUnitBefore, "time") || HasBucketType(wordUnitAfter, "time")) return true;
        }

        if (StringHelper.IsNumeric(word) && word.Length < 3)
        {
            if (wordUnitAfter != null)
            {
                string wordAfter = wordUnitAfter.GetWord().ToUpper();

                if (wordAfter == "AM" || wordAfter == "PM") return true;

                if (StringHelper.IsNumeric(word) && double.Parse(word) < 13 && wordAfter == "oclock") return true;
            }
        }

        if (word == "AM" || word == "PM") return true;

        if (HasBucketType(wordUnit, "time") ||
            HasBucketType(wordUnit, "date") && !IsDay(wordUnit, null, null) && !IsMonth(wordUnit))
        {
            return true;
        }

        return false;
    }

    private static bool IsDay(WordUnit wordUnit, WordUnit wordUnitBefore, WordUnit wordUnitAfter)
    {
        if (wordUnit == null) return false;
        
        string word = wordUnit.GetWord();

        if (word.Contains("th") || word.Contains("st") || word.Contains("nd") || word.Contains("rd"))
        {
            if (!Regex.IsMatch(word, @"\d+")) return false;

            if (HasBucketType(wordUnitBefore, "date") || HasBucketType(wordUnitAfter, "date")) return true;
            
            if (HasBucketType(wordUnitBefore, "time") || HasBucketType(wordUnitAfter, "time")) return true;

            string firstTwoChars = word.Substring(0, 2);

            if (word.Length <= 4 && StringHelper.IsNumeric(firstTwoChars) && double.Parse(firstTwoChars) < 32) return true;

            string firstChar = word.Substring(0, 1);
            
            if (word.Length <= 3 && StringHelper.IsNumeric(firstChar)) return true;
        }

        if (word.Length <= 2 && StringHelper.IsNumeric(word) && double.Parse(word) < 32)
        {
            if (HasBucketType(wordUnitBefore, "date") || HasBucketType(wordUnitAfter, "date"))
            {
                string wordAfter = wordUnitAfter.GetWord();

                if (wordAfter != "am" && wordAfter != "pm") return true;
            }
            
            if (wordUnitAfter != null && HasBucketType(wordUnitBefore, "time") || HasBucketType(wordUnitAfter, "time"))
            {
                string wordAfter = wordUnitAfter.GetWord();

                if (wordAfter != "am" && wordAfter != "pm" && wordAfter != "oclock") return true;
            }
        }

        return false;
    }

    public static bool IsPhraseDay(string phrase)
    {
        if (_dateDescriptors.Contains(phrase)) return true;
        if (_dayIndicators.Contains(phrase)) return true;
        
        if (phrase.Contains("TH") || phrase.Contains("ST") || phrase.Contains("ND") || phrase.Contains("RD"))
        {
            if (!Regex.IsMatch(phrase, @"\d+")) return false;

            string firstTwoChars = phrase.Substring(0, 2);

            if (phrase.Length <= 4 && StringHelper.IsNumeric(firstTwoChars) && double.Parse(firstTwoChars) < 32) return true;

            string firstChar = phrase.Substring(0, 1);
            
            if (phrase.Length <= 3 && StringHelper.IsNumeric(firstChar)) return true;
        }

        if (phrase.Length < 3 && StringHelper.IsNumeric(phrase) && double.Parse(phrase) < 32) return true;

        return false;
    }

    private static bool IsMonth(WordUnit wordUnit)
    {
        if (wordUnit == null) return false;

        List<string> months = new List<string>() {"JANUARY", "FEBRUARY", "MARCH", "APRIL", "MAY", "JUNE", "JULY", "AUGUST", "SEPTEMBER", "OCTOBER", "NOVEMBER", "DECEMBER",
            "JAN", "FEB", "MAR", "APR", "JUN", "JUL", "AUG", "SEP", "SEPT", "OCT", "NOV", "DEC"};

        if (months.Contains(wordUnit.GetWord().ToUpper())) return true;

        return false;
    }

    public static bool IsPhraseMonth(string phrase)
    {
        List<string> months = new List<string>() {"JANUARY", "FEBRUARY", "MARCH", "APRIL", "MAY", "JUNE", "JULY", "AUGUST", "SEPTEMBER", "OCTOBER", "NOVEMBER", "DECEMBER",
            "JAN", "FEB", "MAR", "APR", "JUN", "JUL", "AUG", "SEP", "SEPT", "OCT", "NOV", "DEC"};

        if (months.Contains(phrase)) return true;

        return false;
    }

    public static bool IsPhraseYear(string phrase)
    {
        if (phrase.Length == 4 && StringHelper.IsNumeric(phrase) && double.Parse(phrase) < 2100) return true;

        return false;
    } 

    public static string ConvertDateDescriptor(string dateDescriptor)
    {
        string result = "";

        if (dateDescriptor == "NOW" || dateDescriptor == "TODAY")
        {
            DateTime date = DateTime.Today;
            result = date.Day.ToString();
        }

        if (dateDescriptor == "TOMORROW")
        {
            DateTime date = DateTime.Today.AddDays(1);
            result = date.Day.ToString();
        }

        if (dateDescriptor == "YESTERDAY")
        {
            DateTime date = DateTime.Today.AddDays(-1);
            result = date.Day.ToString();
        }

        return result;
    }

    private static bool IsNumber(WordUnit wordUnit)
    {
        if (wordUnit == null) return false;

        string word = wordUnit.GetWord();
        
        if (StringHelper.IsNumeric(word)) return true;
        if (StringHelper.IsNumber(word)) return true;

        return false;
    }

    private static bool IsUnitOfTime(WordUnit wordUnit)
    {
        if (wordUnit == null) return false;

        string word = wordUnit.GetWord();

        if (word == "minutes" || word == "minute" || word == "hours" || word == "hour") return true;

        return false;
    }

    private static bool HasBucketType(WordUnit wordUnit, string type)
    {
        if (wordUnit == null) return false;
        
        foreach (Bucket bucket in wordUnit.GetBuckets())
        {
            if (bucket.GetType() == type) return true;
        }

        return false;
    }
    
    private static WordUnit BuildWordUnit(WordUnit wordUnit, List<string> words, string display, string type, string partsOfSpeech, int index)
    {
        wordUnit.SetWord(wordUnit.GetWord() + " " + string.Join(" ", words));
        wordUnit.SetIsDateTime(true);
        wordUnit.SetSpeechUnit(BuildSpeechUnit(display, type, partsOfSpeech, wordUnit, index));

        return wordUnit;
    }

    private static SpeechUnit BuildSpeechUnit(string display, string type, string partsOfSpeech, WordUnit wordUnit, int index)
    {
        string word = wordUnit.GetWord();

        if (wordUnit.GetCount() == 1 && word == "today" || word == "tomorrow") partsOfSpeech = "noun;adverb";

        return new SpeechUnit(display, type, wordUnit.GetWord().Split(" ").ToList(), partsOfSpeech, index);
    }
}