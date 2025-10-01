using System;
using System.Collections.Generic;
using System.Linq;
using Grammar;
using Newtonsoft.Json;
using UnityEngine;
using System.Runtime.Versioning;
using Unity.VisualScripting;
using UnityEditor;

public class GrammarManager
{
    private List<int> _possessiveIndexes = new List<int>();

    public Dictionary<string, SpeechUnit> _masterWordList;

    private Dictionary<string, SpeechUnit> _workingWordList;

    public List<GrammarUnit> _grammarPhraseList = new List<GrammarUnit>();

    private List<Tuple<SpeechUnit, int>> _phraseFragments;

    private List<List<string>> _grammarRemainingPatterns = new List<List<string>>();

    public Sentence _sentenceObject;
    private List<string> _unknownWords = new List<string>();

    private string _theSentence;

    public bool CouldNotProcess;

    public GrammarManager(string sentence)
    {
        CouldNotProcess = false;

        //scrub sentence
        Scrub scrub = new Scrub(sentence);
        List<object> results = scrub.Process();

        // new object from scrub process -- returns all indexes of words with capital letters.

        _theSentence = results[0] as string;
        _possessiveIndexes = results[1] as List<int>;
        Dictionary<int, string> capitals = scrub._capitalTerms;

        //Debug.Log("<START>");
        //Debug.Log(JsonConvert.SerializeObject(_masterWordList));
        //Debug.Log(JsonConvert.SerializeObject(_grammarPhraseList));
        //Debug.Log("</START>");

        //form speech units
        SpeechUnitFormer speechUnitFormer = new SpeechUnitFormer(_theSentence, _possessiveIndexes, capitals);
        //Debug.Log("before");
        _masterWordList = speechUnitFormer.FormSpeechUnits();
        //Debug.Log("after");

        /*
        if(_masterWordList.Count < 2)
        {
            CouldNotProcess = true;
            return;
            //we need to bail....
        }
        */

        //grab all words not recognized in dictionary
        _unknownWords = speechUnitFormer._unknownWords;

        //setup base grammar holder
        PreGrammar preGrammar = new PreGrammar();
        _workingWordList = preGrammar.Setup(_masterWordList);
        _masterWordList = preGrammar.Setup(_masterWordList);

        //Debug.Log("<START>");
        //Debug.Log(JsonConvert.SerializeObject(_masterWordList));
        //Debug.Log(JsonConvert.SerializeObject(_grammarPhraseList));
        //Debug.Log("</START>");

        //process phrases | recursion
        PhraseIdentifier phraseIdentifier = new PhraseIdentifier(_masterWordList, _workingWordList, _grammarPhraseList);
        phraseIdentifier.Start();

        // automate?? using a few times..
        foreach (SpeechUnit speechUnit in _masterWordList.Values)
        {
            SetWorkingPartsOfSpeechIfDefinite(speechUnit);
        }

        Debug.Log(JsonConvert.SerializeObject(_workingWordList));
        Debug.Log(JsonConvert.SerializeObject(_masterWordList));

        //check for ambiguity
        PhraseChecker phraseChecker = new PhraseChecker(_masterWordList, _workingWordList, _grammarPhraseList, _theSentence);
        List<object> result = phraseChecker.BeginAmbiguityCheck();



        _theSentence = result[0] as string;
        _phraseFragments = result[1] as List<Tuple<SpeechUnit, int>>;

        if (result.Count > 2)
        {
            _workingWordList = (Dictionary<string, SpeechUnit>)result[2];
            _grammarPhraseList = (List<GrammarUnit>)result[3];
            _masterWordList = (Dictionary<string, SpeechUnit>)result[4];
        }


        //check for patterns
        UnknownProcess unknownProcess = new UnknownProcess(_masterWordList, _grammarPhraseList, _grammarRemainingPatterns);
        unknownProcess.Start();


        Debug.Log(JsonConvert.SerializeObject(_masterWordList));
        foreach (SpeechUnit speechUnit in _masterWordList.Values)
        {
            SetWorkingPartsOfSpeechIfDefinite(speechUnit);
        }

        //identify phrases -- reset new working list to current master
        _workingWordList = JsonConvert.DeserializeObject<Dictionary<string, SpeechUnit>>(JsonConvert.SerializeObject(_masterWordList));

        phraseIdentifier = new PhraseIdentifier(_masterWordList, _workingWordList, _grammarPhraseList);
        phraseIdentifier.Start();

        foreach (SpeechUnit speechUnit in _masterWordList.Values)
        {
            SetWorkingPartsOfSpeechIfDefinite(speechUnit);
        }

        //if we have remaining patterns, attempt to use bigram model to choose most common pattern

        if (_grammarRemainingPatterns.Count > 1)
        {
            // current issue in bigram for this input - show me the first flight of today
            BigramPrediction bigramPrediction = new BigramPrediction(_masterWordList, _workingWordList, _grammarPhraseList, ref _theSentence);
            result = bigramPrediction.Start();

            if ((bool)result[0])
            {
                _theSentence = result[1] as string;
                _workingWordList = result[2] as Dictionary<string, SpeechUnit>;
                _grammarPhraseList = result[3] as List<GrammarUnit>;
                _masterWordList = result[4] as Dictionary<string, SpeechUnit>;
            }
        }

        if (_phraseFragments is { Count: > 0 })
        {
            ReinsertPhraseFragments();

            unknownProcess = new UnknownProcess(_masterWordList, _grammarPhraseList, _grammarRemainingPatterns);
            unknownProcess.Start();

            _workingWordList = JsonConvert.DeserializeObject<Dictionary<string, SpeechUnit>>(JsonConvert.SerializeObject(_masterWordList));
            phraseIdentifier = new PhraseIdentifier(_masterWordList, _workingWordList, _grammarPhraseList);
            phraseIdentifier.Start();

            foreach (SpeechUnit speechUnit in _masterWordList.Values)
            {
                SetWorkingPartsOfSpeechIfDefinite(speechUnit);
            }

            if (_grammarRemainingPatterns.Count > 1)
            {
                BigramPrediction bigramPrediction = new BigramPrediction(_masterWordList, _workingWordList, _grammarPhraseList, ref _theSentence);
                result = bigramPrediction.Start();

                if ((bool)result[0])
                {
                    _theSentence = result[1] as string;
                    _workingWordList = result[2] as Dictionary<string, SpeechUnit>;
                    _grammarPhraseList = result[3] as List<GrammarUnit>;
                    _masterWordList = result[4] as Dictionary<string, SpeechUnit>;
                }

            }
        }

        foreach (SpeechUnit speechUnit in _masterWordList.Values)
        {
            SetWorkingPartsOfSpeechIfDefinite(speechUnit);
        }


        // combine independent verb phrases if first is aux, this is a bandaid to the issue not a fix
        GrammarPhraseListFinalPass();

        _sentenceObject = new Sentence(_masterWordList, _grammarPhraseList);

        _sentenceObject.Start();

    }
    private void ReinsertPhraseFragments()
    {
        foreach (Tuple<SpeechUnit, int> phraseFragment in _phraseFragments)
        {
            bool wordReinserted = false;
            Dictionary<string, SpeechUnit> reinsertedMasterWordList = new Dictionary<string, SpeechUnit>();

            foreach (KeyValuePair<string, SpeechUnit> entry in _masterWordList)
            {
                if (!wordReinserted && entry.Value._index == phraseFragment.Item1._index)
                {
                    reinsertedMasterWordList[phraseFragment.Item1._hash] = phraseFragment.Item1;
                    wordReinserted = true;
                }

                reinsertedMasterWordList[entry.Key] = entry.Value;

                //increase index of words after inserted words
                if (wordReinserted)
                {
                    reinsertedMasterWordList[entry.Key]._index++;
                }
            }

            _masterWordList = reinsertedMasterWordList;

            _grammarPhraseList = new List<GrammarUnit>();

            _theSentence = ConstructSentenceFromMasterWordList();

            PreGrammar preGrammar = new PreGrammar();
            _workingWordList = preGrammar.Setup(_masterWordList);

            PhraseIdentifier phraseIdentifier = new PhraseIdentifier(_masterWordList, _workingWordList, _grammarPhraseList, true);
            phraseIdentifier.Start();

            //TODO do we need to call unknownPatterns SpeechUnitUnknownProcess() again here? Only seems needed for debug
        }
    }

    private string ConstructSentenceFromMasterWordList()
    {
        string sentence = "";

        foreach (var speechUnit in _masterWordList)
        {
            sentence += speechUnit.Value._display + " ";
        }

        return sentence.Trim();
    }

    private void SetWorkingPartsOfSpeechIfDefinite(SpeechUnit speechUnit)
    {
        if (speechUnit._definiteType != "N/A")
        {
            speechUnit._workingPartsOfSpeech = speechUnit._definiteType;
        }
    }

    private void GrammarPhraseListFinalPass()
    {
        for (int i = 0; i < _grammarPhraseList.Count; i++)
        {
            // combine any adjacent verb phrases - [ ..., ["is" -> verb phrase], ["flying" -> verb phrase], ...] -> [..., ["is flying" -> verb phrase], ...]
            if (_grammarPhraseList[i]._phraseType == "verb phrase" && _grammarPhraseList[i]._speechHashUnits.Count == 1 && _masterWordList[_grammarPhraseList[i]._speechHashUnits[0]]._isAuxiliary && i != (_grammarPhraseList.Count - 1) && _grammarPhraseList[i + 1]._phraseType == "verb phrase")
            {
                _grammarPhraseList[i]._speechHashUnits.AddRange(_grammarPhraseList[i + 1]._speechHashUnits);
                _grammarPhraseList.RemoveAt(i + 1);

                i--;
                continue;
            }

            // clean up numeral phrases when separated
            //  [] if we have a noun/prep phrase and next phrase contains only a numeral, combine them
            if (i + 1 < _grammarPhraseList.Count && (_grammarPhraseList[i]._phraseType == "prepositional phrase" || _grammarPhraseList[i]._phraseType == "noun phrase") && _grammarPhraseList[i + 1]._speechHashUnits.Count == 1 && _masterWordList[_grammarPhraseList[i + 1]._speechHashUnits[0]]._isNumeral)
            {
                // maybe also assign POS to numeral for nummod in DependencyParser so obj of phrase stays obj 
                _grammarPhraseList[i]._speechHashUnits.AddRange(_grammarPhraseList[i + 1]._speechHashUnits);
                _grammarPhraseList.RemoveAt(i + 1);

                if (i != 0)
                {
                    i--;
                }
            }

            // clean up numeral phrase when combined in one phrase with numeral as obj of phrase - numeral should just modify it unless its the only noun in the phrase
            if ((_grammarPhraseList[i]._phraseType == "prepositional phrase" || _grammarPhraseList[i]._phraseType == "noun phrase") && _grammarPhraseList.Count > 2)
            {
                // covers both numerals and datetimes
                SpeechUnit numeralSpeechUnit = _masterWordList.Values.FirstOrDefault(word => (word._isNumeral || word._isDateTimeUnit) && _grammarPhraseList[i]._speechHashUnits.Contains(word._hash));
                if (numeralSpeechUnit != null)
                {
                    SpeechUnit firstPossibleNoun = _masterWordList.Values.FirstOrDefault(word => word._partsOfSpeech.Contains("noun") && _grammarPhraseList[i]._speechHashUnits.Contains(word._hash));
                    if (firstPossibleNoun._hash != numeralSpeechUnit._hash)
                    {
                        if (!firstPossibleNoun._definiteType.Contains("noun"))
                        {
                            // pronoun vs noun?
                            firstPossibleNoun._definiteType = "noun";
                            // maybe also assign POS to numeral for nummod in DependencyParser so obj of phrase stays obj 
                        }
                    }
                }


            }

            // any other grammarPhraseList cleanup we might need to do
        }

        CheckRelativePronouns();



        // one more _masterWordList pass to get rid of any words still with "N/A" - even if its wrong, better than sending an unknown to the Stories/Dependency Parser because the user can correct it - transactional language model
        // -> called in DependencyParser constructor with FinalBigramPass


    }

    private void CheckRelativePronouns()
    {
        List<string> relativePronouns = new List<string> { "that", "which", "who", "whom", "whose", "where", "when", "why", "what" };

        // check each phrase in the grammarPhraseList, we aren't changing the grammarPhraseList here, just the masterWordList definite type
        for (int i = 0; i < _grammarPhraseList.Count - 1; i++)
        {
            var currentPhrase = _grammarPhraseList[i];
            var currentPhraseIndex = 0;
            var currentPhraseHash = currentPhrase._speechHashUnits[0];
            var masterListIndex = 0;
            var wordDefiniteType = "N/A";
            var containsRelativePronoun = false;
            foreach (var hash in currentPhrase._speechHashUnits)
            {
                if (relativePronouns.Contains(_masterWordList[hash]._display))
                {
                    masterListIndex = _masterWordList[hash]._index;
                    wordDefiniteType = _masterWordList[hash]._definiteType;
                    currentPhraseHash = hash;

                    containsRelativePronoun = true;
                    break;
                }
                currentPhraseIndex += 1;
            }

            // conjunction are usually marked correctly
            if (!containsRelativePronoun || wordDefiniteType == "conjunction")
            {
                continue;
            }

            // a lot of relative pronouns get marked as adverbs on accident
            //      [] if it is an adverb and is a grammar phrase by itself as well as being in the middle of a sentence, it is actually a relative pronoun
            if (currentPhrase._phraseType == "adverb" && currentPhrase._speechHashUnits.Count == 1 && relativePronouns.Contains(_masterWordList[currentPhraseHash]._display))
            {
                // if in middle of sentence
                if (_masterWordList[currentPhraseHash]._index > 1 && _masterWordList[currentPhraseHash]._index < _masterWordList.Count - 1)
                {
                    _masterWordList[currentPhraseHash]._isRelative = true;
                    _masterWordList[currentPhraseHash]._definiteType = "pronoun";
                    return;
                }
            }

            // any other relative clause cases

            // replace potential relative pronoun with "it" and if the structure of the sentence stays the same -> regular pronoun, etc.., else -> relative pronoun
            // can't do it here though because it will loop in on itself infinitely --> in DependencyParser()
        }
    }

    // another pass through bigram because of N/A words not being assigned unknown phrases in the grammar list - called from DependencyParser constructor
    // can also use this with existing unknown phrases by missing = false
    public void FinalBigramPassMissingUnknownPhrase(bool missing = true)
    {
        // have to check for any words that are in the masterWordList that aren't represented as a phrase in the grammarPhraseList - i don't know why this happens, but it does
        List<string> hashesInGrammarList = _grammarPhraseList.SelectMany(phrase => phrase._speechHashUnits).ToList();
        List<SpeechUnit> hashesNotInGrammarList = new List<SpeechUnit>();

        if (missing)
        {
            hashesNotInGrammarList = _masterWordList.Values.Where(word => !hashesInGrammarList.Contains(word._hash)).ToList();
        }
        // can also use the same functionality for unidentified unknown phrases, just have to remove them first since they are not yet missing
        else
        {
            // get hashes inside unknown phrase, add them to hashesNotIn and delete that phrase from the grammarPhraseList
            List<GrammarUnit> unknownPhrases = _grammarPhraseList.Where(phrase => phrase._phraseType == "unknown phrase").ToList();
            foreach (GrammarUnit phrase in unknownPhrases)
            {
                foreach (string hash in phrase._speechHashUnits)
                {
                    hashesNotInGrammarList.Add(_masterWordList[hash]);
                }

                _grammarPhraseList.Remove(phrase);
            }

            unknownPhrases.Clear();
        }


        if (hashesNotInGrammarList.Count == 0)
        {
            return;
        }

        // now have to place unknown phrases into the _grammarPhraseList so that we can call the bigram - or even just send through regular process
        _grammarRemainingPatterns.Clear();
        _grammarRemainingPatterns = new List<List<string>>();
        foreach (var word in hashesNotInGrammarList)
        {
            GrammarUnit newUnknownPhrase = new GrammarUnit("unknown phrase", new List<string> { word._hash });
            var listOfPartsOfSpeech = word._partsOfSpeech.Split(";").ToList();
            newUnknownPhrase._grammarPatterns = new List<List<string>>();

            // populate grammarRemainingPatterns with all new possible patterns
            foreach (var POS in listOfPartsOfSpeech)
            {
                newUnknownPhrase._grammarPatterns.Add(new List<string> { POS });

                // doing all patterns at once so we can add N/A possibilities along the way
                var patterns = new List<List<string>> { new List<string>() };
                foreach (var hash in _masterWordList)
                {
                    if (hash.Value._index != word._index)
                    {
                        // if "N/A", create variations of the current patterns with each possible POS
                        if (hash.Value._definiteType == "N/A")
                        {
                            var newPatterns = new List<List<string>>();
                            foreach (var pattern in patterns)
                            {
                                var unknownListOfPartsOfSpeech = hash.Value._partsOfSpeech.Split(";").ToList();
                                foreach (var unknownPOS in unknownListOfPartsOfSpeech)
                                {
                                    // copy current pattern and add possiblePOS
                                    var newPattern = new List<string>(pattern) { unknownPOS };
                                    newPatterns.Add(newPattern);
                                }
                            }

                            patterns = newPatterns;
                        }
                        else
                        {
                            foreach (var pattern in patterns)
                            {
                                pattern.Add(hash.Value._definiteType);
                            }
                        }
                    }
                    else
                    {
                        foreach (var pattern in patterns)
                        {
                            pattern.Add(POS);
                        }
                    }
                }

                _grammarRemainingPatterns.AddRange(patterns);
            }


            // now insert it in the correct position
            for (int i = 0; i < _grammarPhraseList.Count; i++)
            {

                if (_masterWordList[_grammarPhraseList[i]._speechHashUnits[0]]._index < word._index)
                {
                    continue;
                }
                else if (_masterWordList[_grammarPhraseList[i]._speechHashUnits[0]]._index - 1 >= word._index)
                {
                    _grammarPhraseList.Insert(i, newUnknownPhrase);
                    break;
                }
            }
        }

        var tempGrammarPhraseList = JsonConvert.DeserializeObject<List<GrammarUnit>>(JsonConvert.SerializeObject(_grammarPhraseList));

        // run through phraseChecker and unknownProcess to get rid of any N/A types
        PhraseChecker phraseChecker = new PhraseChecker(_masterWordList, _workingWordList, _grammarPhraseList, _theSentence);
        List<object> result = phraseChecker.BeginAmbiguityCheck();

        _theSentence = result[0] as string;
        _phraseFragments = result[1] as List<Tuple<SpeechUnit, int>>;

        if (result.Count > 2)
        {
            _workingWordList = (Dictionary<string, SpeechUnit>)result[2];
            _grammarPhraseList = (List<GrammarUnit>)result[3];
            _masterWordList = (Dictionary<string, SpeechUnit>)result[4];
        }

        UnknownProcess unknownProcess = new UnknownProcess(_masterWordList, _grammarPhraseList, _grammarRemainingPatterns);
        unknownProcess.Start();

        foreach (SpeechUnit speechUnit in _masterWordList.Values)
        {
            SetWorkingPartsOfSpeechIfDefinite(speechUnit);
        }

        // don't use the cleared grammarPhraseList, use the one we created before
        // attempt to identify unknown using phraseIdentifier first
        _grammarPhraseList = JsonConvert.DeserializeObject<List<GrammarUnit>>(JsonConvert.SerializeObject(tempGrammarPhraseList));
        PhraseIdentifier phraseIdentifier = new PhraseIdentifier(_masterWordList, _workingWordList, _grammarPhraseList, false, true);
        phraseIdentifier.Start();

        foreach (SpeechUnit speechUnit in _masterWordList.Values)
        {
            SetWorkingPartsOfSpeechIfDefinite(speechUnit);
        }

        // there will most likely be remaining patterns since the regular process missed it before - meaning we approach the bigram differently as well
        if (_grammarRemainingPatterns.Count > 1)
        {


            // call the prediction directly because the unknownProcess at the top of BigramPredication.Start() erases all the progress up till here because it didn't catch it before so it just erases it - i don't know why that does that
            BigramPrediction _bigramPrediction = new BigramPrediction(_masterWordList, _workingWordList, _grammarPhraseList, ref _theSentence);
            List<string> highestProbabilitySequence = _bigramPrediction.MakePredictionOnRemainingPatterns(_grammarRemainingPatterns);

            // assign POS directly from best sequence if the phraseChecker and unknownProcess don't catch it
            for (int i = 0; i < _masterWordList.Count; i++)
            {
                if (_masterWordList.ElementAt(i).Value._definiteType == "N/A" || _masterWordList.ElementAt(i).Value._definiteType == "")
                {
                    _masterWordList.ElementAt(i).Value._definiteType = highestProbabilitySequence[i];
                }
            }

            // have to just assign one word phrases because its better than having it erased and unknown for the DependencyParser
            List<GrammarUnit> unknownPhrases = _grammarPhraseList.Where(phrase => phrase._phraseType == "unknown phrase").ToList();
            foreach (var speechUnit in hashesNotInGrammarList)
            {
                foreach (var phrase in unknownPhrases)
                {
                    if (phrase._speechHashUnits.Contains(speechUnit._hash))
                    {
                        // making sure its just one word unknown phrases
                        if (phrase._speechHashUnits.Count == 1)
                        {
                            switch (speechUnit._definiteType)
                            {
                                case "verb":
                                    phrase._phraseType = "verb phrase";
                                    break;
                                case "adverb":
                                    phrase._phraseType = "adverb";
                                    break;
                                case "pronoun":
                                    if (!speechUnit._isRelative)
                                    {
                                        phrase._phraseType = "noun phrase";
                                    }
                                    break;
                                case "noun":
                                    phrase._phraseType = "noun phrase";
                                    break;
                                case "preposition":
                                    phrase._phraseType = "prepositional phrase";
                                    break;
                                case "adjective":
                                    phrase._phraseType = "adjective";
                                    break;
                                case "possessive":
                                    phrase._phraseType = "possessive";
                                    break;
                                case "determiner":
                                    phrase._phraseType = "determiner";
                                    break;
                                default:
                                    break;
                            }
                        }
                    }
                }
            }
        }
    }

}