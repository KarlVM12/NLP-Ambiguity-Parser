using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Grammar;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SpeechUnitFormer
{
    
    private string _sentence;

    private List<int> _possessiveIndexes = new List<int>();

    public List<string> _unknownWords = new List<string>();
    public Dictionary<int, string> _capitals = new Dictionary<int, string> ();

    public SpeechUnitFormer(string sentence, List<int> possessiveIndexes, Dictionary<int, string> capitals)
    {
        _sentence = sentence;
        Debug.Log("SENTENCE CHECK ::: " + sentence);

        _possessiveIndexes.AddRange(possessiveIndexes);

        _capitals = capitals;
    }

    public Dictionary<string, SpeechUnit> FormSpeechUnits()
    {
        Dictionary<string, SpeechUnit> masterWordList = new Dictionary<string, SpeechUnit>();

        List<string> words = _sentence.Split(" ").ToList();
        List<WordUnit> wordUnits = new List<WordUnit>();

        for (int i = 0; i < words.Count; i++)
        {
            wordUnits.Add(new WordUnit(words[i], "word"));

            if (_possessiveIndexes.Contains(i))
            {
                wordUnits[i].SetIsPossessive(true);
                wordUnits[i].SetWord(wordUnits[i].GetWord().Substring(0, wordUnits[i].GetWord().Length - 1));
            }
        }
        
        wordUnits = DateTimeIdentifier.GetDateTimeAndTimeIntervalUnits(wordUnits);

        for (int i = 0; i < wordUnits.Count; i++)
        {
            WordUnit wordUnit = wordUnits[i];
            string word = wordUnit.GetWord();

            if (wordUnit.GetIsDateTime())
            {
                //no need to convert date times to ordinals
                continue;
            }
            else
            {
                if (Regex.IsMatch(word, @"\d+(st|nd|rd|th)$"))
                {
                    string numberInWord = word.Substring(0, word.Length - 2);

                    wordUnit.SetWord(ConvertOrdinals(numberInWord));

                    continue;
                }
            }
            
            CheckPhoneticWord(wordUnit, i);

            if (StringHelper.IsNumeric(word))
            {
                //wordUnit.SetWord(NumberToWords(word)); // move this till speechUnit addition, that way display stays number and words contain NumberToWords()
                wordUnit.SetIsNumeral(true);
            }
        }
        
        wordUnits = CheckEntityProcess(wordUnits, 3);
        wordUnits = CheckEntityProcess(wordUnits, 2);
        wordUnits = CheckEntityProcess(wordUnits, 1);

        wordUnits = CheckForLocations(wordUnits, 3);
        wordUnits = CheckForLocations(wordUnits, 2);
        wordUnits = CheckForLocations(wordUnits, 1);

        wordUnits = ClearDuplicateEntities(wordUnits);

        for (int i = 0; i < wordUnits.Count; i++)
        {
            WordUnit wordUnit = wordUnits[i];

            if (wordUnit.GetSpeechUnit() != null)
            {
                SpeechUnit speechUnit = wordUnit.GetSpeechUnit();
                speechUnit.SetIndex(i);

                masterWordList[speechUnit.GetHash()] = speechUnit;

                continue;
            }

            // technically for numerals like booking numbers (2024-74560), it is critical the '-' stays
            string word = Regex.Replace(wordUnit.GetWord(), @"[^A-Za-z0-9\-]", "");

            SpeechUnit result = CheckForDictionaryInterrogatives(word, i);
            if (result != null)
            {
                masterWordList[result.GetHash()] = result;

                continue;
            }

            result = CheckForDictionaryDeterminers(word, i);
            if (result != null)
            {
                masterWordList[result.GetHash()] = result;

                continue;
            }

            result = CheckForDictionaryAuxiliaryVerbs(word, i);
            if (result != null)
            {
                masterWordList[result.GetHash()] = result;

                continue;
            }

            result = CheckForDictionaryTerm(word, i);
            if (result != null)
            {
                masterWordList[result.GetHash()] = result;
                
                continue;
            }

            // if not a date/time, still need a numeral check - ideally if not inherent date or time should always go through heres
            result = CheckForNumerals(wordUnit, i);
            if (result != null)
            {
                masterWordList[result.GetHash()] = result;

                continue;
            }

            //IMPORTANT PHP process sets this to just string
            //Here we will set as new speech unit w _notInDictionaryTermsList flagged as true so we know for logic later on
            //SpeechUnit unit = new SpeechUnit(word, "N/A", new List<string>(){"N/A"}, "N/A", i);
            //unit.SetIsNotInDictionaryTermsList(true);
            //masterWordList[AppHelper.newGuid] = unit;
            _unknownWords.Add(word);


        }
        
        CheckForDictionaryAliasTerms(3, masterWordList);
        CheckForDictionaryAliasTerms(2, masterWordList);
        CheckForDictionaryAliasTerms(1, masterWordList);

        CheckForDictionaryContractionTerms(masterWordList, 2);

        //proper index speech units
        int count = 0;
        foreach (var speechUnit in masterWordList)
        {
            speechUnit.Value._index = count;
            count++;
        }

        Debug.Log("FINAL CAPITALS LIST: " + JsonConvert.SerializeObject(_capitals));
        
        return masterWordList;
    }

    private SpeechUnit CheckForNumerals(WordUnit wordUnit, int index)
    {
        if (wordUnit.GetIsNumeral())
        {
            // possibly can add new POS "numeral", but this would become complex in the PhraseIdentifier -> handled in GrammarManager.GrammarPhraseListFinalPass()
            //SpeechUnit speechUnit = new SpeechUnit(wordUnit.GetWord(), "numeral", new List<string>((NumberToWords(wordUnit.GetWord())).Split(" ")), "numeral", index);
            SpeechUnit speechUnit = new SpeechUnit(wordUnit.GetWord(), "numeral", new List<string>((NumberToWords(wordUnit.GetWord())).Split(" ")), "noun", index);
            speechUnit._isNumeral = true;

            return speechUnit;
        }
        
        return null;
    }

    private SpeechUnit CheckForDictionaryTerm(string word, int index)
    {
        List<DictionaryTerm> dictionaryTerms = GrammarData.GetDictionaryTerms();

        foreach (DictionaryTerm dictionaryTerm in dictionaryTerms)
        {
            if (dictionaryTerm.GetTerm() == word.ToLower())
            {
                SpeechUnit speechUnit = new SpeechUnit(word, "dictionary", new List<string>() { word }, dictionaryTerm.GetPartsOfSpeech(), index);

                return speechUnit;
            }
        }
        return null;
    }
    
    private SpeechUnit CheckForDictionaryInterrogatives(string word, int index)
    {
        List<DictionaryInterrogative> dictionaryInterrogatives = GrammarData.GetDictionaryInterrogatives();

        foreach (DictionaryInterrogative dictionaryInterrogative in dictionaryInterrogatives)
        {
            if (dictionaryInterrogative.GetTerm() == word.ToLower())
            {
                SpeechUnit speechUnit = new SpeechUnit(word, "dictionary", new List<string>() { word }, dictionaryInterrogative.GetType(), index);
                speechUnit.SetIsInterrogative(true);

                return speechUnit;
            }
        }

        return null;
    }
    
    private SpeechUnit CheckForDictionaryDeterminers(string word, int index)
    {
        List<DictionaryDeterminer> dictionaryDeterminers = GrammarData.GetDictionaryDeterminers();

        foreach (DictionaryDeterminer dictionaryDeterminer in dictionaryDeterminers)
        {
            if (dictionaryDeterminer.GetTerm() == word.ToLower())
            {
                SpeechUnit speechUnit = new SpeechUnit(word, "dictionary", new List<string>() { word }, dictionaryDeterminer.GetType(), index);
                speechUnit.SetIsDeterminers(true);

                return speechUnit;
            }
        }

        return null;
    }
    
    private SpeechUnit CheckForDictionaryAuxiliaryVerbs(string word, int index)
    {
        List<DictionaryAuxiliaryVerb> dictionaryAuxiliaryVerbs = GrammarData.GetDictionaryAuxiliaryVerbs();

        foreach (DictionaryAuxiliaryVerb dictionaryAuxiliaryVerb in dictionaryAuxiliaryVerbs)
        {
            if (dictionaryAuxiliaryVerb.GetVerb() == word.ToLower())
            {
                SpeechUnit speechUnit = new SpeechUnit(word, "dictionary", new List<string>() { word }, "verb", index);
                speechUnit.SetIsAuxiliary(true);

                return speechUnit;
            }
        }

        return null;
    }

    private List<WordUnit> CheckEntityProcess(List<WordUnit> wordUnits_, int length)
    {
        int loop = length;
        List<WordUnit> wordsUnits = wordUnits_;

        List<UserNameAlias> userNameAliases = GrammarData.GetUserNameAliases();
        List<User> users = GrammarData.GetUsers();

        while (loop <= wordsUnits.Count)
        {
            string phrase = null;

            for (int x = (loop - length); x < loop; x++)
            {
                if (phrase == null)
                {
                    phrase += StringHelper.UpperCaseFirst(wordUnits_[x].GetWord());
                }
                else
                {
                    phrase += " " + StringHelper.UpperCaseFirst(wordUnits_[x].GetWord());
                }
            }

            bool entityFound = false;
            string entityId = null;
            string fullName = null;
            
            foreach (UserNameAlias userNameAlias in userNameAliases)
            {
                if (phrase != null && userNameAlias.GetAlias() == phrase)
                {
                    entityFound = true;
                    entityId = userNameAlias.GetId();
                    break;
                }
            }

            if (entityFound)
            {
                foreach (User user in users)
                {
                    if (user.GetId() == entityId)
                    {
                        fullName = user.GetFullName();

                        break;
                    }
                }

                // update list of capitals to match new indexing of terms
                if (_capitals.Count > 0)
                {
                    var keysToUpdate = _capitals.Keys.Where(key => key > (loop - length)).ToList();

                    foreach (var key in keysToUpdate)
                    {
                        int newKey = key - (length - 1);
                        var value = _capitals[key];
                        _capitals.Remove(key);
                        _capitals[newKey] = value;
                    }
                    _capitals[loop - length] = fullName;
                }

                wordsUnits[loop - length] = new WordUnit(phrase.ToLower(), "phrase");
                wordsUnits[loop - length].AddEntity(new Entity(entityId, "person", phrase.ToLower(), fullName));

                bool isPossessive = false;
                bool isDeterminers = false;
                string partsOfSpeech = null;
                
                if (EntityContainsPossessive(wordUnits_, loop, length) && fullName != null)
                {
                    wordsUnits[loop - length].SetWord(fullName.ToLower());
                    wordsUnits[loop - length].SetIsPossessive(true);
                    phrase = fullName.ToLower();
                    isPossessive = true;
                    isDeterminers = true;
                    partsOfSpeech = "possessive";
                }
                else
                {
                    partsOfSpeech = "proper noun";
                }
                
                SpeechUnit speechUnit = new SpeechUnit(fullName, "entity", phrase.Split(" ").ToList(), partsOfSpeech, loop);
                speechUnit.SetIsDeterminers(isDeterminers);
                speechUnit.SetIsPossessive(isPossessive);
                
                wordsUnits[loop - length].SetSpeechUnit(speechUnit);

                for (int x = (loop - length + 1); x < loop; x++)
                {
                    wordsUnits.RemoveAt(x);
                }
            }

            loop++;
        }

        return wordsUnits;
    }

    private bool EntityContainsPossessive(List<WordUnit> wordUnits, int loop, int length)
    {
        for (int i = loop - length; i < loop; i++)
        {
            if (wordUnits[i].GetIsPossessive())
            {
                return true;
            }
        }

        return false;
    }
    
    private void CheckForDictionaryContractionTerms(Dictionary<string, SpeechUnit> masterWordList, int length)
    {
        //NOTE --- We may not even be using this anymore, contractions are being handled in scrub process
        
        // can shift capitals up to +1 space
        List<DictionaryContraction> dictionaryContractions = GrammarData.GetDictionaryContractions();
        List<string> keysInArray = masterWordList.Keys.ToList();
        int loop = length;

        while (loop <= keysInArray.Count)
        {
            string phrase = null;

            for (int x = (loop - length); x < loop; x++)
            {
                if (phrase == null)
                {
                    phrase += masterWordList[keysInArray[x]].GetDisplay();
                }
                else
                {
                    phrase += " " + masterWordList[keysInArray[x]].GetDisplay();
                }
            }
            foreach (DictionaryContraction dictionaryContraction in dictionaryContractions)
            {
                string term = dictionaryContraction.GetTerm();

                if (term == phrase.ToLower())
                {
                    Debug.Log("HERE");
                    // update list of capitals to match new indexing of terms
                    if (_capitals.Count > 0)
                    {
                        var keysToUpdate = _capitals.Keys.Where(key => key > (loop - length)).ToList();

                        foreach (var key in keysToUpdate)
                        {
                            int newKey = key + 1;
                            var value = _capitals[key];
                            _capitals.Remove(key);
                            _capitals[newKey] = value;
                        }

                        // while the contraction begins with a capital, it has no significance for potential slang or Proper Noun so we can delete.
                        _capitals.Remove(loop - length);
                    }

                    //TODO REGEX QUICK FIX NEED UPDATE ITEMS IN DATABASE PROPER JSON
                    string definedObjectJson = dictionaryContraction.GetDefinedObject();
                    definedObjectJson = Regex.Replace(definedObjectJson, "[“”]", "\"");
                    
                    List<Dictionary<string,DefinedObject>> result = JsonConvert.DeserializeObject<List<Dictionary<string, DefinedObject>>>(definedObjectJson);
                
                    int indexOfAlias = 0;

                    for (int x = loop - length; x < loop; x++)
                    {
                        masterWordList[keysInArray[x]].UpdateFromObjectSet(result[indexOfAlias]);
                        indexOfAlias++;
                    }
                }
                
                loop++;
            }
        }
    }

    private List<WordUnit> CheckForLocations(List<WordUnit> wordUnits_, int length){

        int loop = length;
        List<WordUnit> wordsUnits = wordUnits_;

        List<Location> locations = GrammarData.GetLocations();


        while (loop <= wordsUnits.Count)
        {
            string phrase = null;

            for (int x = (loop - length); x < loop; x++)
            {
                if (phrase == null)
                {
                    phrase += wordUnits_[x].GetWord();
                }
                else
                {
                    phrase += " " + wordUnits_[x].GetWord();
                }
            }

            bool locationFound = false;
            string locationID = null;
            string specifier = null;

            phrase = phrase.ToLower();
            
            foreach (Location location in locations)
            {
                if (phrase != null && location.GetName().ToLower() == phrase)
                {
                    locationFound = true;
                    specifier = "name";
                    locationID = location.GetId();
                    break;
                }

                if (phrase != null && location.GetCityName().ToLower() == phrase)
                {
                    locationFound = true;
                    specifier = "city";
                    locationID = location.GetId();
                    break;
                }

                if (phrase != null && location.GetCountryName().ToLower() == phrase)
                {
                    locationFound = true;
                    specifier = "country";
                    locationID = location.GetId();
                    break;
                }
                if (phrase != null && location.GetRegionName().ToLower() == phrase)
                {
                    locationFound = true;
                    specifier = "region";
                    locationID = location.GetId();
                    break;
                }
            }

            if (locationFound)
            {
                if (_capitals.Count > 0)
                {
                    var keysToUpdate = _capitals.Keys.Where(key => key > (loop - length)).ToList();

                    foreach (var key in keysToUpdate)
                    {
                        int newKey = key - (length - 1);
                        var value = _capitals[key];
                        _capitals.Remove(key);
                        _capitals[newKey] = value;
                    }
                }

                wordsUnits[loop - length] = new WordUnit(phrase.ToLower(), "phrase");
                wordsUnits[loop - length].AddEntity(new Entity(locationID, "location:" + specifier, phrase.ToLower(), phrase));

                string partsOfSpeech = "proper noun";
                SpeechUnit speechUnit = null;

                speechUnit = new SpeechUnit(StringHelper.UpperCaseFirstWithSpace(phrase), "location:" + specifier, phrase.Split(" ").ToList(), partsOfSpeech, loop);
                

                speechUnit.SetIsDeterminers(false);
                speechUnit.SetIsPossessive(false);
                
                wordsUnits[loop - length].SetSpeechUnit(speechUnit);

                for (int x = (loop - length + 1); x < loop; x++)
                {
                    wordsUnits.RemoveAt(x);
                }
            }

            loop++;
        }

        return wordsUnits;
        

    }

    private void CheckForDictionaryAliasTerms(int length, Dictionary<string, SpeechUnit> masterWordList)
    {

        int loop = length;
        var keysInArray = masterWordList.Keys.ToList();

        while (loop <= keysInArray.Count)
        {
            string phraseCheck = "";

            for (int x = loop - length; x < loop; x++)
            {
                if (string.IsNullOrEmpty(phraseCheck))
                {
                    phraseCheck += masterWordList[keysInArray[x]].GetDisplay();
                }
                else
                {
                    phraseCheck += " " + masterWordList[keysInArray[x]].GetDisplay();
                }
            }

            bool aliasFound = false;

            List<DictionaryAlias> dictionaryAliases = GrammarData.GetDictionaryAliases();
            DictionaryAlias matchedAlias = null;

            foreach (DictionaryAlias dictionaryAlias in dictionaryAliases)
            {
                if (dictionaryAlias.GetAlias() == phraseCheck)
                {
                    matchedAlias = dictionaryAlias;
                    break;
                }
            }

            if (matchedAlias != null)
            {
                //TODO REGEX QUICK FIX NEED UPDATE ITEMS IN DATABASE PROPER JSON
                string definedObjectJson = matchedAlias.GetDefinedObject();
                definedObjectJson = Regex.Replace(definedObjectJson, "[“”]", "\"");
                
                List<Dictionary<string,DefinedObject>> result = JsonConvert.DeserializeObject<List<Dictionary<string, DefinedObject>>>(definedObjectJson);
                
                int indexOfAlias = 0;

                for (int x = loop - length; x < loop; x++)
                {
                    masterWordList[keysInArray[x]].UpdateFromObjectSet(result[indexOfAlias]);
                    indexOfAlias++;
                }
            }

            loop++;
        }
    }

    private List<WordUnit> ClearDuplicateEntities(List<WordUnit> wordUnits_)
    {
        // can shift capitals up to 1 space
        List<WordUnit> wordUnits = new List<WordUnit>();

        for (int i = 0; i < wordUnits_.Count; i++)
        {
            WordUnit wordUnit = wordUnits_[i];
            string speechUnitType = wordUnit.GetSpeechUnitType();
            string entityId = wordUnit.GetEntityId();

            if (speechUnitType == "entity" && i < wordUnits_.Count() - 1)
            {
                WordUnit wordUnitAfter = wordUnits_[i + 1];
                string speechUnitTypeAfter = wordUnitAfter.GetSpeechUnitType();
                string entityIdAfter = wordUnitAfter.GetEntityId();

                if (speechUnitTypeAfter == "entity" && entityId == entityIdAfter)
                {
                    continue;
                }
            }
            
            wordUnits.Add(wordUnit);
        }

        return wordUnits;
    }

    private void CheckPhoneticWord(WordUnit wordUnit, int index)
    {
        bool speechUnitIsDateTime = wordUnit.GetSpeechUnitIsDateTimeUnit();
        if (speechUnitIsDateTime) return;

        string word = wordUnit.GetWord();
        
        if (Regex.IsMatch(word, @"[A-Za-z].*[0-9]|[0-9].*[A-Za-z]"))
        {
            string adjustedWord = null;

            for (int i = 0; i < word.Length; i++)
            {
                char ch = word[i];
                string charString = ch.ToString();
                
                if (Regex.IsMatch(charString, @"[0-9]+"))
                {
                    adjustedWord += NumberToWords(charString) + " ";
                }
                else
                {
                    adjustedWord += LetterToPhonetic(ch)  + " ";
                }
            }

            if (adjustedWord != null) adjustedWord = adjustedWord.Substring(0, adjustedWord.Length - 1);
            else return;
            
            wordUnit.SetWord(adjustedWord);
            wordUnit.SetIsPhonetic(true);
            wordUnit.SetSpeechUnit(new SpeechUnit(adjustedWord, "phonetic", adjustedWord.Split(" ").ToList(), "proper noun", index + wordUnit.GetCount()));
        }
    }

    private string LetterToPhonetic(char charString)
    {
        Dictionary<char, string> phonetics = new Dictionary<char, string>()
        {
            { 'a', "alpha" }, { 'b', "bravo" }, { 'c', "charlie" },
            { 'd', "delta" }, { 'e', "echo" }, { 'f', "foxtrot" },
            { 'g', "golf" }, { 'h', "hotel" }, { 'i', "india" },
            { 'j', "juliet" }, { 'k', "kilo" }, { 'l', "lima" },
            { 'm', "mike" }, { 'n', "november" }, { 'o', "oscar" },
            { 'p', "papa" }, { 'q', "quebec" }, { 'r', "romeo" },
            { 's', "sierra" }, { 't', "tango" }, { 'u', "uniform" },
            { 'v', "victor" }, { 'w', "whiskey" }, { 'x', "xray" },
            { 'y', "yankee" }, { 'z', "zulu" }
        };

        return phonetics[charString];
    }

    private string NumberToWords(string wordNumber)
    {
        List<string> numbersLessThanTwenty = new List<string>() {"zero", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine",
            "ten", "eleven", "twelve", "thirteen", "fourteen", "fifteen", "sixteen", 
            "seventeen", "eighteen", "nineteen"};

        List<string> numbersByTenFromTwenty = new List<string>()
            { "twenty", "thirty", "forty", "fifty", "sixty", "seventy", "eighty", "ninety" };
        
        int number = int.Parse(wordNumber);
        
        if (number < 20)
        {
            return numbersLessThanTwenty[number];
        }

        if (number < 100)
        {
            return numbersByTenFromTwenty[number / 10] + 
                   (number % 10 > 0 ? " " + numbersLessThanTwenty[number % 10] : "");
        }

        if (number < 1000)
        {
            return numbersLessThanTwenty[number / 100] + " hundred" +
                   (number % 100 > 0 ? " " + NumberToWords((number % 10).ToString()) : "");
        }

        if (number < 1000000)
        {
            return NumberToWords((number / 1000).ToString()) + " thousand" +
                   (number % 1000 > 0 ? " " + NumberToWords((number % 1000).ToString()) : "");
        }

        return NumberToWords((number / 1000000).ToString()) + " million" +
               (number % 1000000 > 0 ? " " + NumberToWords((number % 1000000).ToString()) : "");
    }

    private string ConvertOrdinals(string wordNumber)
    {
        int number = int.Parse(wordNumber);
        if (number < 1) return wordNumber;
        
        List<string> numbersLessThanTwenty = new List<string>()
        {
            "first", "second", "third", "fourth", "fifth", 
            "sixth", "seventh", "eighth", "ninth", "tenth", 
            "eleventh", "twelfth", "thirteenth", "fourteenth", "fifteenth", 
            "sixteenth", "seventeenth", "eighteenth", "nineteenth", "twentieth"
        };
        
        Dictionary<char, string> tenths = new Dictionary<char, string>()
        {
            { '2', "twenty" }, { '3', "thirty" }, { '4', "forty" },
            { '5', "fifty" }, { '6', "sixty" }, { '7', "seventy" },
            { '8', "eighty" }, { '9', "ninety" }
        };
        
        Dictionary<char, string> singlesForLast = new Dictionary<char, string>()
        {
            { '1', "first" }, { '2', "second" }, { '3', "third" },
            { '4', "fourth" }, { '5', "fifth" }, { '6', "sixth" },
            { '7', "seventh" }, { '8', "eighth" }, { '9', "ninth" }
        };
        
        Dictionary<char, string> singlesForFirst = new Dictionary<char, string>()
        {
            { '3', "thirtieth" }, { '4', "fortieth" }, { '5', "fiftieth" },
            { '6', "sixtieth" }, { '7', "seventieth" }, { '8', "eightieth" },
            { '9', "ninetieth" }
        };

        if (number < 21)
        {
            return numbersLessThanTwenty[number];
        }

        if (number < 100)
        {
            char firstChar = wordNumber[0];
            char lastChar = wordNumber[1];

            if (lastChar != '0')
            {
                return tenths[firstChar] + " " + singlesForLast[lastChar];
            }
            
            return singlesForFirst[firstChar];
        }

        //TODO handle numbers greater than 100

        return wordNumber;
    }
}