using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

namespace Grammar
{
    [DataContract]
    public class SpeechUnit
    {
        [DataMember] 
        public string _hash;
        
        [DataMember]
        public string _display;
        
        [DataMember]
        public string _type;

        [DataMember]
        public string _definiteType;

        [DataMember]
        public string _partsOfSpeech;

        [DataMember] 
        public string _workingPartsOfSpeech;

        [DataMember]
        public Dictionary<string, bool> _excludePartsOfSpeech = new Dictionary<string, bool>();

        [DataMember] 
        public int _index;

        [DataMember] 
        public bool _notInDictionaryTermsList;

        [DataMember]
        public bool _isDateTimeUnit;

        [DataMember] 
        public bool _isDeterminers;
        
        [DataMember] 
        public bool _isPossessive;

        [DataMember] 
        public bool _isInterrogative;

        [DataMember] 
        public bool _isAuxiliary;

        [DataMember]
        public bool _isRelative;

        [DataMember]
        public bool _isNumeral;

        [DataMember]
        public List<string> _words;

        [DataMember] 
        private List<string> _possibleSpeechPattern = new List<string>();
        
        public SpeechUnit(string display, string type, List<string> words, string partsOfSpeech, int index)
        {
            _hash = AppHelper.newGuid;
            _display = display;
            _type = type;
            _words = words;
            _partsOfSpeech = partsOfSpeech;
            _workingPartsOfSpeech = partsOfSpeech;
            _definiteType = "N/A";

            if (type == "datetime" || type == "date" || type == "time" || type == "timeInterval") _isDateTimeUnit = true;

            if (partsOfSpeech == "proper noun"){
                _definiteType = "noun";
            }
            if (partsOfSpeech == "conjunction"){
                _definiteType = "conjunction";
            }

            _isRelative = false;
            _isNumeral = false;
        }

        public bool IsDeterminerNumber()
        {
            string[] holderPOS = _workingPartsOfSpeech.Split(';');

            foreach (string POS in holderPOS)
            {
                if (POS.Trim() == "determiner")
                {
                    if (_isDateTimeUnit)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public void SetIndex(int value)
        {
            _index = value;
        }

        public bool IsVerb()
        {
            string[] possibleVerbs = _workingPartsOfSpeech.Split(';');

            foreach (string verb in possibleVerbs)
            {
                if (verb.Trim() == "verb")
                {
                    return true;
                }
            }

            return false;
        }

        public bool GetIsDeterminers()
        {
            return _isDeterminers;
        }

        public void SetIsDeterminers(bool value)
        {
            _isDeterminers = value;
        }
        
        public void SetIsInterrogative(bool value)
        {
            _isInterrogative = value;
        }

        public bool GetIsInterrogative()
        {
            return _isInterrogative;
        }

        public void SetIsPossessive(bool value)
        {
            _isPossessive = value;
        }

        public void SetIsNotInDictionaryTermsList(bool value)
        {
            _notInDictionaryTermsList = value;
        }
        
        public void SetIsAuxiliary(bool value)
        {
            _isAuxiliary = value;
        }

        public bool GetIsAuxiliary()
        {
            return _isAuxiliary;
        }

        public void SetIsRelative(bool value)
        {
            _isRelative = value;
        }

        public bool GetIsRelative()
        {
            return _isRelative;
        }

        public void SetIsNumeral(bool value)
        {
            _isNumeral = value;
        }

        public bool GetIsNumeral()
        {
            return _isNumeral;
        }

        public bool GetIsDateTimeUnit()
        {
            return _isDateTimeUnit;
        }

        public string GetHash()
        {
            return _hash;
        }

        public string GetPartsOfSpeech()
        {
            return _partsOfSpeech;
        }

        public void SetDefiniteType(string value)
        {
            _definiteType = value;
        }

        public string GetDefiniteType()
        {
            return _definiteType;
        }
        
        public string GetDisplay()
        {
            return _display;
        }

        public new string GetType()
        {
            return _type;
        }

        public string GetWorkingPartsOfSpeech()
        {
            return _workingPartsOfSpeech;
        }

        public int GetWorkingPartsOfSpeechCount()
        {
            if (_workingPartsOfSpeech == null) return 0;
            return _workingPartsOfSpeech.Split(";").ToList().Count;
        }

        public void CheckForDefiniteAgainstWorkingList()
        {
            List<string> partsOfSpeech = _workingPartsOfSpeech.Split(";").ToList();
            if (partsOfSpeech.Count == 1){
                if (partsOfSpeech[0] == "proper noun" || partsOfSpeech[0] == "noun" || partsOfSpeech[0] == "pronoun")
                {
                    SetDefiniteType("noun");
                } else {
                    SetDefiniteType(partsOfSpeech[0]);
                }

            } 
        }

        public string GetPatternDataSet(int index, ref bool endReached)
        {
            Dictionary<string, string> patternSet = GetPatternSet(index);

            if (patternSet.Count == 0)
            {
                endReached = true;
                return null;
            }

            return patternSet["type"];
        }

        public void GrammarPhraseAdjust(List<string> possibleList)
        {
            List<string> addToExclude = new List<string>();
            List<string> addToInclude = new List<string>();
            List<string> partsOfSpeech = _workingPartsOfSpeech.Split(";").ToList();

            foreach (string partOfSpeech in partsOfSpeech)
            {
                bool addTo = false;

                foreach (string possible in possibleList)
                {
                    if (possible == partOfSpeech)
                    {
                        addTo = true;
                    }
                }

                if (addTo)
                {
                    addToInclude.Add(partOfSpeech);
                }
                else
                {
                    addToExclude.Add(partOfSpeech);
                }
            }
            
            AddToExclusions(addToExclude);

            if (addToInclude.Count == 1)
            {
                SetDefiniteType(addToInclude[0]);
            }
            
            UpdateRemainingPatterns();
        }

        private Dictionary<string, string> GetPatternSet(int index = 0)
        {
            UpdateRemainingPatterns();

            if (index > _possibleSpeechPattern.Count - 1)
            {
                return new Dictionary<string, string>();
            }

            if (IsDefiniteTypeSet())
            {
                if (_isAuxiliary)
                {
                    return new Dictionary<string, string>()
                    {
                        {"display", _display},
                        {"type", "aux verb"}
                    };
                }

                if (_isDeterminers)
                {
                    return new Dictionary<string, string>()
                    {
                        {"display", _display},
                        {"type", "determiner"}
                    };
                }

                if (_isInterrogative)
                {
                    return new Dictionary<string, string>()
                    {
                        {"display", _display},
                        {"type", "int " + _definiteType}
                    };
                }
                
                return new Dictionary<string, string>()
                {
                    {"display", _display},
                    {"type", _definiteType}
                };
            }

            return new Dictionary<string, string>()
            {
                {"display", _display},
                {"type", _possibleSpeechPattern[index]}
            };
        }

        private void UpdateRemainingPatterns()
        {
            _possibleSpeechPattern = new List<string>();

            if (IsDefiniteTypeSet())
            {
                if (_isAuxiliary)
                {
                    _possibleSpeechPattern.Add("aux verb");
                    return;
                }

                if (_isDeterminers)
                {
                    _possibleSpeechPattern.Add("determiner");
                    return;
                }

                if (_isInterrogative)
                {
                    _possibleSpeechPattern.Add("int " + _definiteType);
                    return;
                }
                
                _possibleSpeechPattern.Add(_definiteType);

                return;
            }

            List<string> partsOfSpeech = _workingPartsOfSpeech.Split(";").ToList();

            if (_isInterrogative)
            {
                foreach (string partOfSpeech in partsOfSpeech)
                {
                    _possibleSpeechPattern.Add("int " + partOfSpeech);
                }
            }

            if (_isDeterminers)
            {
                _possibleSpeechPattern.Add("determiner");
            }
            else
            {
                foreach (string partOfSpeech in partsOfSpeech)
                {
                    _possibleSpeechPattern.Add(partOfSpeech);
                }
            }
        }
        
        public void UpdateFromObjectSet(Dictionary<string, DefinedObject> definedObjectSet)
        {
            DefinedObject definedObject = definedObjectSet[definedObjectSet.Keys.ToList()[0]];
            
            _definiteType = definedObject.GetDefiniteType();
            foreach (string excludedPartOfSpeech in definedObject.GetExcludePartsOfSpeech())
            {
                _excludePartsOfSpeech[excludedPartOfSpeech] = false;
            }
            _isDeterminers = definedObject.GetIsDeterminers();
            _isAuxiliary = definedObject.GetIsAuxiliary();
            _isInterrogative = definedObject.GetIsInterrogative();
        }

        public bool IsDefiniteTypeSet()
        {
            if (_definiteType != null && _definiteType != "N/A") return true;
            
            return false;
        }

        public bool IsAdverb()
        {
            List<string> partsOfSpeech = _workingPartsOfSpeech.Split(";").ToList();
            
            foreach (string partOfSpeech in partsOfSpeech)
            {
                if (partOfSpeech == "adverb") return true;
            }

            return false;
        }
        
        public bool IsAdjective()
        {
            List<string> partsOfSpeech = _workingPartsOfSpeech.Split(";").ToList();
            
            foreach (string partOfSpeech in partsOfSpeech)
            {
                if (partOfSpeech == "adjective") return true;
            }

            return false;
        }

        public bool IsNounVerbOnly()
        {
            List<string> partsOfSpeech = _workingPartsOfSpeech.Split(";").ToList();
            int verbNounCounter = 0;
            int otherPosCounter = 0;

            foreach (string partOfSpeech in partsOfSpeech)
            {
                if (partOfSpeech == "noun" || partOfSpeech == "proper noun" || partOfSpeech == "pronoun" || partOfSpeech == "verb")
                {
                    verbNounCounter++;
                }
                else
                {
                    otherPosCounter++;
                }
            }

            if (verbNounCounter > 0 && otherPosCounter == 0) return true;
            return false;
        }
        
        public bool IsVerbAdverbOnly()
        {
            List<string> partsOfSpeech = _workingPartsOfSpeech.Split(";").ToList();
            
            int count = 0;

            foreach (string partOfSpeech in partsOfSpeech)
            {
                if (partOfSpeech == "adverb" || partOfSpeech == "verb") count++;
            }

            return count == 2;
        }

        public bool IsVerbNounOrAdjective()
        {
            var possiblePOS = _workingPartsOfSpeech.Split(';');

            foreach (var pos in possiblePOS)
            {
                if (pos == "noun" || pos == "adjective" || pos == "pronoun" || pos == "verb")
                {
                    return true;
                }
            }

            return false;
        }

        public bool IsNounAdjective()
        {
            var possiblePOS = _workingPartsOfSpeech.Split(';');
            int count = 0;

            foreach (var pos in possiblePOS)
            {
                if (pos == "noun" || pos == "adjective")
                {
                    count++;
                }
            }

            return count == 2;
        }

        public bool IsVerbAdjectiveOnly()
        {
            var possiblePOS = _workingPartsOfSpeech.Split();
            int count = 0;

            foreach (var pos in possiblePOS)
            {
                if (pos == "verb" || pos == "adjective")
                {
                    count++;
                }
            }

            return count == 2;
        }


        public bool IsNounAdverbOnly()
        {
            List<string> partsOfSpeech = _workingPartsOfSpeech.Split(";").ToList();
            
            int count = 0;

            foreach (string partOfSpeech in partsOfSpeech)
            {
                if (partOfSpeech == "adverb" || partOfSpeech == "noun") count++;
            }

            return count == 2;
        }

        public bool CanOnlyBeANoun()
        {
            List<string> partsOfSpeech = _workingPartsOfSpeech.Split(";").ToList();
            int nounCounter = 0;
            int otherPosCounter = 0;
            
            foreach (string partOfSpeech in partsOfSpeech)
            {
                if (partOfSpeech == "noun" || partOfSpeech == "proper noun" || partOfSpeech == "pronoun")
                {
                    nounCounter++;
                }
                else
                {
                    otherPosCounter++;
                }
            }
            
            if (nounCounter > 0 && otherPosCounter == 0) return true;
            return false;
        }
        
        public bool IsNoun()
        {
            List<string> partsOfSpeech = _workingPartsOfSpeech.Split(";").ToList();
            
            foreach (string partOfSpeech in partsOfSpeech)
            {
                if (partOfSpeech == "noun" || partOfSpeech == "proper noun" || partOfSpeech == "pronoun") return true;
            }

            return false;
        }
        
        public bool IsPreposition()
        {
            List<string> partsOfSpeech = _workingPartsOfSpeech.Split(";").ToList();
            
            foreach (string partOfSpeech in partsOfSpeech)
            {
                if (partOfSpeech == "preposition") return true;
            }

            return false;
        }
        
        public bool IsInterjection()
        {
            List<string> partsOfSpeech = _workingPartsOfSpeech.Split(";").ToList();
            
            foreach (string partOfSpeech in partsOfSpeech)
            {
                if (partOfSpeech == "interjection") return true;
            }

            return false;
        }
        
        
        public bool IsSpecificNoun()
        {
            List<string> partsOfSpeech = _workingPartsOfSpeech.Split(";").ToList();
            
            foreach (string partOfSpeech in partsOfSpeech)
            {
                if (partOfSpeech == "noun") return true;
            }

            return false;
        }

        public bool IsNumeral()
        {
            return _definiteType == "numeral" || _workingPartsOfSpeech.Contains("numeral");
        }

        public void AddToExclusions(List<string> excludedPartsOfSpeech)
        {
            List<string> holderWorkingPartsOfSpeech = new List<string>();

            if (_workingPartsOfSpeech != null)
            {
                holderWorkingPartsOfSpeech = _workingPartsOfSpeech.Split(";").ToList();
            }
            
            foreach (string excludedPartOfSpeech in excludedPartsOfSpeech)
            {
                _excludePartsOfSpeech[excludedPartOfSpeech] = true;
                
                for (int i = 0; i < holderWorkingPartsOfSpeech.Count; i++)
                {
                    if (holderWorkingPartsOfSpeech[i] == excludedPartOfSpeech)
                    {
                        holderWorkingPartsOfSpeech.RemoveAt(i);
                    }
                }
            }

            _workingPartsOfSpeech = string.Join(";", holderWorkingPartsOfSpeech);
        }

        public void AddToExclusionsByIndex(int index)
        {
            List<string> partsOfSpeech = _workingPartsOfSpeech.Split(";").ToList();

            _excludePartsOfSpeech[partsOfSpeech[index]] = true;
            
            partsOfSpeech.RemoveAt(index);

            _workingPartsOfSpeech = string.Join(';', partsOfSpeech);
        }
    }    
}
