using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Grammar;
using Newtonsoft.Json;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class UserScheduleStory
{
    private Sentence _sentence;

    private SpeechUnit _user;

    private List<SpeechUnit> _allDateTimes = new List<SpeechUnit>();

    private Dictionary<string, string> _dutyOn = new Dictionary<string, string>();
    
    private Dictionary<string, string> _dutyOff = new Dictionary<string, string>();

    private int _lastDateSearchKey = -1;
    
    public UserScheduleStory(Sentence sentence)
    {
        _sentence = sentence;
    }

    public UserScheduleObject FillStory()
    {
        GetEntities();
        GetAllDateTimes();
        GetDutyOnTime();
        GetDutyOffTime();
        
        InferDutyOffTime();
        InferDutyOnTime();

        FixBackwardsTimes();

        //UnityEngine.Debug.Log(JsonConvert.SerializeObject(_dutyOn));
        //UnityEngine.Debug.Log(JsonConvert.SerializeObject(_dutyOff));
        //UnityEngine.Debug.Log(JsonConvert.SerializeObject(_allDateTimes));

        SpeechUnit mainVerb = null;

        if (_sentence._objectPhraseList[_sentence._mainVerbPhraseIndex].dataType == GrammarPhraseObject.DataType.Verb)
        {
            mainVerb = ((VerbObject)_sentence._objectPhraseList[_sentence._mainVerbPhraseIndex])._mainVerb;
        }

        return new UserScheduleObject(mainVerb, _dutyOn, _dutyOff, _user);
    }

    private void GetEntities()
    {
        foreach (GrammarPhraseObject grammarPhraseObject in _sentence._objectPhraseList)
        {
            if (grammarPhraseObject.dataType == GrammarPhraseObject.DataType.Noun)
            {
                NounObject phrase = grammarPhraseObject as NounObject;

                if (phrase._isEntity)
                {
                    _user = phrase._mainNounUnit;
                    
                    break;
                }
            }

            if (grammarPhraseObject.dataType == GrammarPhraseObject.DataType.Unknown)
            {
                UnknownObject phrase = grammarPhraseObject as UnknownObject;

                if (phrase._isEntity)
                {
                    foreach (SpeechUnit speechUnit in grammarPhraseObject._speechUnits)
                    {
                        if (speechUnit._type == "entity")
                        {
                            _user = speechUnit;

                            break;
                        }
                    }
                }
            }
        }
    }

    private void GetAllDateTimes()
    {
        List<GrammarPhraseObject> allDateTimes = new List<GrammarPhraseObject>();
        
        foreach (GrammarPhraseObject objectPhrase in _sentence._objectPhraseList)
        {
            if (objectPhrase._isDateTime)
            {
                allDateTimes.Add(objectPhrase);
            }
        }

        foreach (GrammarPhraseObject dateTime in allDateTimes)
        {
            foreach (SpeechUnit speechUnit in dateTime._speechUnits)
            {
                string type = speechUnit._type;

                if (type == "datetime" || type == "date" || type == "time")
                {
                    _allDateTimes.Add(speechUnit);
                }
            }
        }
    }

    private void GetDutyOnTime()
    {
        if (_allDateTimes.Count == 0) return;
        
        if (_allDateTimes[0]._type == "datetime")
        {
            _dutyOn["datetime"] = _allDateTimes[0]._display;
            
            return;
        }

        bool hasTime = false,
            hasDay = false,
            hasMonth = false,
            hasYear = false,
            hasWeekTypeIndicator = false,
            hasDateTypeIndicator = false;

        for (int i = 0; i < _allDateTimes.Count; i++)
        {
            if (i == 4) break;

            SpeechUnit dateTime = _allDateTimes[i];
            string phrase = dateTime._display.ToUpper();

            if (dateTime._type == "time" && hasTime)
            {
                //can logically assume we only have a time given for the start

                break;
            }

            if (dateTime._type == "time")
            {
                hasTime = true;

                if (DateTimeIdentifier._timeOfDayIndicators.Contains(phrase))
                {
                    //we have something like evening - afternoon - night

                    hasTime = false;

                    _dutyOn["timeIndicator"] = dateTime._display;

                    _lastDateSearchKey = i;
                    
                    continue;
                }

                if (phrase == "MIDNIGHT") dateTime._display = "12pm";
                if (phrase == "NOON") dateTime._display = "12am";

                _dutyOn["time"] = dateTime._display;

                _lastDateSearchKey = i;
                
                continue;
            }

            if (dateTime._type == "date")
            {
                if (DateTimeIdentifier.IsPhraseMonth(phrase))
                {
                    if (hasMonth) break;

                    hasMonth = true;

                    _dutyOn["month"] = dateTime._display;

                    _lastDateSearchKey = i;
                    
                    continue;
                }

                if (DateTimeIdentifier.IsPhraseDay(phrase))
                {
                    if (hasDay) break;

                    hasDay = true;

                    _dutyOn["day"] = dateTime._display;

                    if (DateTimeIdentifier._dateDescriptors.Contains(_dutyOn["day"].ToUpper()))
                    {
                        _dutyOn["day"] = DateTimeIdentifier.ConvertDateDescriptor(_dutyOn["day"].ToUpper());
                    }
                    
                    continue;
                }

                if (DateTimeIdentifier.IsPhraseYear(phrase))
                {
                    if (hasYear) break;

                    hasYear = true;

                    _dutyOn["year"] = dateTime._display;

                    _lastDateSearchKey = i;
                    
                    continue;
                }

                if (DateTimeIdentifier._weekTypeIndicators.Contains(phrase))
                {
                    if (hasWeekTypeIndicator) break;

                    hasWeekTypeIndicator = true;

                    _dutyOn["weekTypeIndicator"] = dateTime._display;

                    _lastDateSearchKey = i;
                    
                    continue;
                }
                
                if (DateTimeIdentifier._dateTypeIndicators.Contains(phrase))
                {
                    if (hasDateTypeIndicator) break;

                    hasDateTypeIndicator = true;

                    _dutyOn["dateTypeIndicator"] = dateTime._display;

                    _lastDateSearchKey = i;
                    
                    continue;
                }
                
                //if we get here we have a month day date
                List<string> monthDay = dateTime._display.Split(" ").ToList();

                if (monthDay.Count == 2)
                {
                    _dutyOn["month"] = monthDay[0];
                    _dutyOn["day"] = monthDay[1];

                    hasMonth = true;
                    hasDay = true;

                    _lastDateSearchKey = i;
                }
            }
        }

        if (_dutyOn.ContainsKey("day") && _dutyOn.ContainsKey("time"))
        {
            string month = DateTime.Now.ToString("MMMM");

            _dutyOn["datetime"] = _dutyOn["time"] + " " + month + " " + _dutyOn["day"];
        }
    }

    private void GetDutyOffTime()
    {
        if (_allDateTimes.Count < 1) return;

        if (_allDateTimes.Count > 1){
            if (_allDateTimes[1]._type == "datetime")
            {
                _dutyOff["datetime"] = _allDateTimes[1]._display;
            
                return;
            }
        }
       
        bool hasTime = false,
            hasDay = false,
            hasMonth = false,
            hasYear = false,
            hasWeekTypeIndicator = false,
            hasDateTypeIndicator = false;

        for (int i = 0; i < _allDateTimes.Count; i++)
        {
            if (i <= _lastDateSearchKey) continue;

            if (i - _lastDateSearchKey == 4) break;

            SpeechUnit dateTime = _allDateTimes[i];
            string phrase = dateTime._display.ToUpper();

            if (dateTime._type == "time" && hasTime)
            {
                //can logically assume we only have a time given for the start

                break;
            }

            if (dateTime._type == "time")
            {
                hasTime = true;

                if (DateTimeIdentifier._timeOfDayIndicators.Contains(phrase))
                {
                    //we have something like evening - afternoon - night

                    hasTime = false;

                    _dutyOff["timeIndicator"] = dateTime._display;

                    _lastDateSearchKey = i;
                    
                    continue;
                }

                if (phrase == "MIDNIGHT") dateTime._display = "12pm";
                if (phrase == "NOON") dateTime._display = "12am";

                _dutyOff["time"] = dateTime._display;

                _lastDateSearchKey = i;
                
                continue;
            }

            if (dateTime._type == "date")
            {
                if (DateTimeIdentifier.IsPhraseMonth(phrase))
                {
                    if (hasMonth) break;

                    hasMonth = true;

                    _dutyOff["month"] = dateTime._display;

                    _lastDateSearchKey = i;
                    
                    continue;
                }

                if (DateTimeIdentifier.IsPhraseDay(phrase))
                {
                    if (hasDay) break;

                    hasDay = true;

                    _dutyOff["day"] = dateTime._display;

                    if (DateTimeIdentifier._dateDescriptors.Contains(_dutyOff["day"].ToUpper()))
                    {
                        _dutyOff["day"] = DateTimeIdentifier.ConvertDateDescriptor(_dutyOff["day"].ToUpper());
                    }
                }

                if (DateTimeIdentifier.IsPhraseYear(phrase))
                {
                    if (hasYear) break;

                    hasYear = true;

                    _dutyOff["year"] = dateTime._display;

                    _lastDateSearchKey = i;
                    
                    continue;
                }

                if (DateTimeIdentifier._weekTypeIndicators.Contains(phrase))
                {
                    if (hasWeekTypeIndicator) break;

                    hasWeekTypeIndicator = true;

                    _dutyOff["weekTypeIndicator"] = dateTime._display;

                    _lastDateSearchKey = i;
                    
                    continue;
                }
                
                if (DateTimeIdentifier._dateTypeIndicators.Contains(phrase))
                {
                    if (hasDateTypeIndicator) break;

                    hasDateTypeIndicator = true;

                    _dutyOff["dateTypeIndicator"] = dateTime._display;

                    _lastDateSearchKey = i;
                }
            }
        }
    }

    private void InferDutyOffTime()
    {
        if (!_dutyOff.ContainsKey("datetime"))
        {
            if (_dutyOn.ContainsKey("datetime"))
            {
                if (_dutyOff.ContainsKey("time") && !_dutyOff.ContainsKey("day") && !_dutyOff.ContainsKey("month"))
                {
                    DateTime? dutyOnTime = DateTimeExtension.DateFromString(_dutyOn["datetime"]);
                    if (dutyOnTime != null)
                        _dutyOff["datetime"] = _dutyOff["time"] + " " + ((DateTime)dutyOnTime).ToString("MMMM") + " " + ((DateTime)dutyOnTime).Day;
                }

                if (_dutyOff.ContainsKey("time") && _dutyOff.ContainsKey("day"))
                {
                    DateTime? dutyOnTime = DateTimeExtension.DateFromString(_dutyOn["datetime"]);
                    if (dutyOnTime != null)
                        _dutyOff["datetime"] = _dutyOff["time"] + " " + ((DateTime)dutyOnTime).ToString("MMMM") + " " +
                                               _dutyOff["day"];
                }
            }

            if (_dutyOff.ContainsKey("time") && _dutyOff.ContainsKey("day"))
            {
                string month = DateTime.Now.ToString("MMMM");

                _dutyOff["datetime"] = _dutyOff["time"] + " " + month + " " + _dutyOff["day"];
            }
        }
    }

    private void InferDutyOnTime()
    {
        if (!_dutyOn.ContainsKey("datetime"))
        {
            if (_dutyOff.ContainsKey("datetime"))
            {
                if (_dutyOn.ContainsKey("time") && !_dutyOn.ContainsKey("day") && !_dutyOn.ContainsKey("month"))
                {
                    DateTime? dutyOffTime = DateTimeExtension.DateFromString(_dutyOff["datetime"]);
                    if (dutyOffTime != null)
                        _dutyOn["datetime"] = _dutyOn["time"] + " " + ((DateTime)dutyOffTime).ToString("MMMM") + " " + ((DateTime)dutyOffTime).Day;
                }

                if (_dutyOn.ContainsKey("time") && _dutyOn.ContainsKey("day"))
                {
                    DateTime? dutyOffTime = DateTimeExtension.DateFromString(_dutyOff["datetime"]);
                    if (dutyOffTime != null)
                        _dutyOn["datetime"] = _dutyOn["time"] + " " + ((DateTime)dutyOffTime).ToString("MMMM") + " " +
                                              _dutyOn["day"];           
                }
            }
        }
    }

    private void FixBackwardsTimes()
    {
        if (_dutyOn.ContainsKey("datetime") && _dutyOff.ContainsKey("datetime"))
        {
            DateTime? dutyOnTime = DateTimeExtension.DateFromString(_dutyOn["datetime"]);
            DateTime? dutyOffTime = DateTimeExtension.DateFromString(_dutyOff["datetime"]);

            if (dutyOnTime != null && dutyOffTime != null)
            {
                DateTime dutyOn = (DateTime)dutyOnTime;
                DateTime dutyOff = (DateTime)dutyOffTime;

                if (dutyOn > dutyOff && dutyOn.Day == dutyOff.Day && !_dutyOn.ContainsKey("day"))
                {
                    _dutyOn["datetime"] = _dutyOn["time"] + " " + dutyOn.ToString("MMMM") + " " + dutyOn.Day;
                }
            }
        }
    }
}