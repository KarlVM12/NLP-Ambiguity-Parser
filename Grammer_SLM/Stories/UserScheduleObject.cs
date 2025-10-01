using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using Grammar;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Unity.VisualScripting;
using UnityEngine;

public class UserScheduleObject : BaseStoryObject
{
    private SpeechUnit _mainVerb;

    private SpeechUnit _user;

    private Dictionary<string, string> _dutyOn;

    private Dictionary<string, string> _dutyOff;

    private DateTime? _dutyOnDateTime;

    private DateTime? _dutyOffDateTime;

    private ResponseManager _responseManager;

    public UserScheduleObject(SpeechUnit mainVerb, Dictionary<string, string> dutyOn, Dictionary<string, string> dutyOff, SpeechUnit user)
    {
        _mainVerb = mainVerb;
        _dutyOn = dutyOn;
        _dutyOff = dutyOff;

        if (user != null)
        {
            _user = user;
        }
        else
        {
            _missingComponents.Add("user");
        }
        
        //dutyOn and dutyOff dictionary possibilities
        //timeIndicator = morning, evening, afternoon, night, dawn, dusk, midday //TODO
        //weekTypeIndicator = week weekday weekend weeknight //TODO
        //dateTypeIndicator = month year day //TODO
        //datetime = full date without year
        //time = 16:00 9am 9:30am 12pm
        //day = 10 10th
        //month = March Mar
        //year = 2024

        if (_dutyOn.ContainsKey("datetime"))
        {
            if (_dutyOn.ContainsKey("year"))
            {
                _dutyOnDateTime = DateTimeExtension.DateFromString(_dutyOn["datetime"], _dutyOn["year"]);
            }
            else
            {
                _dutyOnDateTime = DateTimeExtension.DateFromString(_dutyOn["datetime"]);
            }
        }
        else
        {
            bool missingTime = false, missingDay = false, missingMonth = false;
            
            if (!_dutyOn.ContainsKey("time"))
            {
                missingTime = true;
            }

            if (!_dutyOn.ContainsKey("day"))
            {
                missingDay = true;
            }

            if (!_dutyOn.ContainsKey("month"))
            {
                missingMonth = true;
            }

            if (!missingTime && missingDay && missingMonth)
            {
                _dutyOnDateTime = DateTimeExtension.DateFromString(_dutyOn["time"]);
            }

            if (missingTime && missingDay)
            {
                _missingComponents.Add("dutyOn everything");
            }

            if (missingTime && !missingDay && !missingMonth)
            {
                _missingComponents.Add("dutyOn time");
            }

            if (missingTime && missingMonth && !missingDay)
            {
                _dutyOn["month"] = DateTime.Now.ToString("MMMM");
                _missingComponents.Add("dutyOn time");
            }
        }

        if (_dutyOff.ContainsKey("datetime"))
        {
            _dutyOffDateTime = DateTimeExtension.DateFromString(_dutyOff["datetime"]);
        }
        else
        {
            bool missingTime = false, missingDay = false, missingMonth = false;
            
            if (!_dutyOff.ContainsKey("time"))
            {
                missingTime = true;
            }

            if (!_dutyOff.ContainsKey("day"))
            {
                missingDay = true;
            }

            if (!_dutyOff.ContainsKey("month"))
            {
                missingMonth = true;
            }

            if (missingTime && missingDay)
            {
                _missingComponents.Add("dutyOff everything");
            }

            if (missingTime && missingMonth && !missingDay)
            {
                _dutyOn["month"] = DateTime.Now.ToString("MMMM");
                _missingComponents.Add("dutyOff time");
            }
        }

        _captainResponse = GetCaptainResponse();
    }

    private string GetCaptainResponse()
    {
        /*
        //try this for now...
        if (_dutyOnDateTime != null && _dutyOffDateTime == null && _missingComponents.Count == 0){
            if (_dutyOff["time"] != null){
                _dutyOff["date"] = _dutyOn["date"];
                // set duty off date time 
            }
        }
        */

        //Debug.Log(JsonConvert.SerializeObject(_dutyOn));
        //Debug.Log(JsonConvert.SerializeObject(_dutyOff));
        //Debug.Log(JsonConvert.SerializeObject(_dutyOnDateTime));
        //Debug.Log(JsonConvert.SerializeObject(_dutyOffDateTime));
        //Debug.Log(JsonConvert.SerializeObject(_missingComponents));
        
        if (_missingComponents.Contains("user"))
        {
            return MissingUserResponseQueue();
        }
        
        //we handle 
        //missing user "schedule somebody not in database for ... whatever"
        //no dates at all "schedule ron smith"
        //have start but no end "schedule ron smith for 9am october 18th"
        //missing start time "schedule ron smith for the october 18th" 
        //missing start time / month (can infer month) ask for time "schedule ron smith for the 18th" 
        //complete schedule "schedule ron smith for 9am october 18th until 10pm" 

        if (_missingComponents.Contains("dutyOn everything") && _missingComponents.Contains("dutyOff everything"))
        {
            return MissingStartTimeEndTimeQueue();
        }

        if (_dutyOnDateTime != null && _missingComponents.Contains("dutyOff everything") || _missingComponents.Contains("dutyOff time"))
        {
            return MissingEndTimeQueue();
        }
        
        if (_missingComponents.Contains("dutyOn time"))
        {
            return MissingStartTimeQueue();
        }

        if (!_missingComponents.Contains("user") && _dutyOnDateTime != null && _dutyOffDateTime != null)
        {
            PromptConfirm = true;
            return CompleteUserScheduleQueue();
        }

        if (!_missingComponents.Contains("user"))
        {
            return MissingStartTimeEndTimeQueue();
        }

        return string.Empty;
    }

    private string MissingUserResponseQueue()
    {
        if (_dutyOn.ContainsKey("datetime") && _dutyOff.ContainsKey("datetime"))
        {
            if (_dutyOnDateTime != null && _dutyOffDateTime != null)
            {
                DateTime start = (DateTime)_dutyOnDateTime;
                DateTime end = (DateTime)_dutyOffDateTime;

                if (!DateTimeExtension.DatesEqualNotIncludingTime(start, end))
                {
                    _responseManager = new ResponseManager("schedule", "missingUser", "set1");
                    _responseManager.Set("startDate", start.ToString("MMMM dd"));
                    _responseManager.Set("startTime", start.ToString("t"));
                    _responseManager.Set("endDate", end.ToString("MMMM dd"));
                    _responseManager.Set("endTime", end.ToString("t"));
                }
                else
                {
                    _responseManager = new ResponseManager("schedule", "missingUser", "set2");
                    _responseManager.Set("startDate", start.ToString("MMMM dd"));
                    _responseManager.Set("startTime", start.ToString("t"));
                    _responseManager.Set("endTime", end.ToString("t"));
                }

                return _responseManager.Output();
            }
        }
        
        if (_dutyOn.ContainsKey("time") && _dutyOff.ContainsKey("time"))
        {
            _responseManager = new ResponseManager("schedule", "missingUser", "set3");

            //generate these so DateTime class can handle spitting out proper time string
            string startDate = 11 + "/" + 11 + "/" + DateTime.Now.Year + _dutyOn["time"];
            string endDate = 11 + "/" + 11 + "/" + DateTime.Now.Year + _dutyOff["time"];
            DateTime start = DateTime.Parse(startDate);
            DateTime end = DateTime.Parse(endDate);

            _responseManager.Set("startTime", start.ToString("t"));
            _responseManager.Set("endTime", end.ToString("t"));

            return _responseManager.Output();
        }

        _responseManager = new ResponseManager("schedule", "missingUser", "set4");

        return _responseManager.Output();
    }

    private string MissingStartTimeEndTimeQueue()
    {
        // modified to only grab start time.. both times are not grabbed. can update post meeting
        _responseManager = new ResponseManager("schedule", "missingStartTimeEndTime", "set1");
        _responseManager.Set("display", _user._display);

        return _responseManager.Output();
    }

    private string MissingStartTimeQueue()
    {
        _responseManager = new ResponseManager("schedule", "missingStartTime", "set1");

        return _responseManager.Output();
    }

    private string MissingEndTimeQueue()
    {
        _responseManager = new ResponseManager("schedule", "missingEndTime", "set1");

        return _responseManager.Output();
    }

    private string CompleteUserScheduleQueue()
    {
        DateTime start = (DateTime)_dutyOnDateTime;
        DateTime end = (DateTime)_dutyOffDateTime;

        if (!DateTimeExtension.DatesEqualNotIncludingTime(start, end))
        {
            _responseManager = new ResponseManager("schedule", "completeUserSchedule", "set1");
            _responseManager.Set("display", _user._display);
            _responseManager.Set("startDate", start.ToString("MMMM dd"));
            _responseManager.Set("startTime", start.ToString("t"));
            _responseManager.Set("endDate", end.ToString("MMMM dd"));
            _responseManager.Set("endTime", end.ToString("t"));
        }
        else
        {
            _responseManager = new ResponseManager("schedule", "completeUserSchedule", "set2");
            _responseManager.Set("display", _user._display);
            _responseManager.Set("startDate", start.ToString("MMMM dd"));
            _responseManager.Set("startTime", start.ToString("t"));
            _responseManager.Set("endTime", end.ToString("t"));
        }

        return _responseManager.Output();
    }

    public override void UpdatePrompt(string prompt, GrammarManager grammarManager)
    {
        bool hasNoUser = false;
        bool hasNoDutyOn = false;
        bool hasNoDutyOff = false;
        bool hasNoDutyOnTime = false;
        
        foreach (string missingComponent in _missingComponents)
        {
            switch (missingComponent)
            {
                case "user":
                    hasNoUser = true;
                    break;
                case "dutyOn time":
                    hasNoDutyOnTime = true;
                    break;
                case "dutyOn everything":
                    hasNoDutyOn = true;
                    break;
                case "dutyOff time":
                case "dutyOff everything":
                    hasNoDutyOff = true;
                    break;
            }
        }

        //Debug.Log("duty OFF" + hasNoDutyOff);
        //Debug.Log("duty ON" + hasNoDutyOn);
        //Debug.Log(JsonConvert.SerializeObject(_missingComponents));

        if (hasNoUser)
        {
            FillUser(grammarManager);
            return;
        }
        if (hasNoDutyOn)
        {
            FillDutyOn(grammarManager);
            return;
        }
        if (hasNoDutyOnTime)
        {
            FillDutyOn(grammarManager);
            return;
        }
        if (hasNoDutyOff)
        {
            FillDutyOff(grammarManager);
            return;
        }
       
        _captainResponse = GetCaptainResponse();
        base.UpdatePrompt(prompt, grammarManager);
    }

    private void FillUser(GrammarManager grammar)
    {
        SpeechUnit user = null;
        foreach (GrammarPhraseObject objectPhrase in grammar._sentenceObject._objectPhraseList){
            if (objectPhrase.dataType == GrammarPhraseObject.DataType.Noun)
            {
                NounObject phraseObject = (NounObject)objectPhrase;
                if (phraseObject._isEntity){
                    user = phraseObject._mainNounUnit;
                }
            } else if (objectPhrase.dataType == GrammarPhraseObject.DataType.Unknown)
            {
                UnknownObject phraseObject = (UnknownObject)objectPhrase;
                foreach (SpeechUnit speechUnit in phraseObject._speechUnits)
                {
                    if (speechUnit._type == "entity")
                    {
                        user = speechUnit;
                    }
                }
            }
        }

        if (user != null)
        {
            _user = user;
            _missingComponents.Remove("user");
        }

        _captainResponse = GetCaptainResponse();
    }

    private void FillDutyOn(GrammarManager grammar)
    {
        List<SpeechUnit> timeIntervals = new List<SpeechUnit>();
        SpeechUnit dutyOnTime = null;
        SpeechUnit dutyOnDate = null;
        SpeechUnit dutyOnDateTime = null;
        bool dutyOnFound = false;

        foreach (GrammarPhraseObject objectPhrase in grammar._sentenceObject._objectPhraseList)
        {
            foreach (SpeechUnit speechUnit in objectPhrase._speechUnits)
            {
                if (speechUnit._type == "datetime")
                {
                    dutyOnDateTime = speechUnit;
                    dutyOnFound = true;
                    break;
                }

                if (speechUnit._type == "time")
                {
                    dutyOnTime = speechUnit;
                    dutyOnFound = true;
                    break;
                }

                if (speechUnit._type == "date")
                {
                    dutyOnDate = speechUnit;
                    dutyOnFound = true;
                    break;
                }

                if (speechUnit._type == "timeInterval")
                {
                    dutyOnFound = true;
                    timeIntervals.Add(speechUnit);
                }
            }
            
            //break so we go with first date we see in list (dutyOn) otherwise we grab last and mismatch w dutyOff
            if (dutyOnFound) break;
        }
        
        //Debug.Log(JsonConvert.SerializeObject(grammar._sentenceObject._objectPhraseList));
        //Debug.Log(JsonConvert.SerializeObject(dutyOnTime));
        
        if (dutyOnFound)
        {
            if (dutyOnTime != null)
            {
                if (_missingComponents.Contains("dutyOn time") && !_dutyOn.ContainsKey("date"))
                {
                    _dutyOnDateTime = DateTimeExtension.DateFromString(dutyOnTime._display + " " + _dutyOn["month"] + " " + _dutyOn["day"]);
                    _missingComponents.Remove("dutyOn time");
                }
                else if (_missingComponents.Contains("dutyOn time") && _dutyOn.ContainsKey("date"))
                {
                    _dutyOnDateTime = DateTimeExtension.DateFromString(dutyOnTime._display + " " + _dutyOn["date"]);
                    _missingComponents.Remove("dutyOn time");
                }
                else
                {
                    DateTime? date = DateTimeExtension.DateFromString(dutyOnTime._display);
                    if (date != null)
                    {
                        _missingComponents.Remove("dutyOn everything");
                        _dutyOnDateTime = date;
                    }   
                }
            }

            if (dutyOnDateTime != null)
            {
                DateTime? date = DateTimeExtension.DateFromString(dutyOnDateTime._display);
                if (date != null)
                {
                    _missingComponents.Remove("dutyOn everything");
                    _dutyOnDateTime = date;
                }
            }

            if (dutyOnDate != null)
            {
                _missingComponents.Remove("dutyOn everything");
                _missingComponents.Add("dutyOn time");
                _dutyOn["date"] = dutyOnDate._display;
            }
        }

        _captainResponse = GetCaptainResponse();
    }

    private void FillDutyOff(GrammarManager grammar)
    {
        List<SpeechUnit> timeIntervals = new List<SpeechUnit>();
        SpeechUnit dutyOffTime = null;
        SpeechUnit dutyOffDate = null;
        SpeechUnit dutyOffDateTime = null;
        bool dutyOffFound = false;
        
        foreach (GrammarPhraseObject objectPhrase in grammar._sentenceObject._objectPhraseList){
            foreach (SpeechUnit speechUnit in objectPhrase._speechUnits)
            {
                if (speechUnit._type == "datetime")
                {
                    dutyOffDateTime = speechUnit;
                    dutyOffFound = true;
                }
                if (speechUnit._type == "time")
                {
                    dutyOffTime = speechUnit;
                    dutyOffFound = true;
                }
                if (speechUnit._type == "date")
                {
                    dutyOffDate = speechUnit;
                    dutyOffFound = true;
                }

                if (speechUnit._type == "timeInterval")
                {
                    dutyOffFound = true;
                    timeIntervals.Add(speechUnit);
                }
            }
        }

        if (dutyOffFound)
        {
            if (dutyOffTime != null)
            {
                //TODO
                //if this time is AM and dutyOn is PM then we can assume next day
                //if this time is PM and dutyOn is AM then we can assume same day
                //if this time is PM and dutyOn is PM (and this time is before we assume next day)
                //if this time is PM and dutyOn is PM (and this time is after we assume same day)
                //if this time is AM and dutyOn is AM (and this time is before we assume
                
                if (_dutyOnDateTime != null)
                {
                    DateTime dutyOnTime = (DateTime)_dutyOnDateTime;
                    string dateString = dutyOnTime.Month + "/" + dutyOnTime.Day + "/" + dutyOnTime.Year + " " + dutyOffTime._display;
                    
                    try 
                    {
                        DateTime date = DateTime.Parse(dateString);
                        _missingComponents.Remove("dutyOff everything");
                        _dutyOffDateTime = date;
                    }
                    catch (Exception e)
                    {
                        //something went wrong parsing date
                    }
                    
                }
                else
                {
                    string dateString = DateTime.Now.Month + "/" + DateTime.Now.Day + "/" + DateTime.Now.Year + " " + dutyOffTime._display;
                    
                    try 
                    {
                        DateTime date = DateTime.Parse(dateString);
                        _missingComponents.Remove("dutyOff everything");
                        _dutyOffDateTime = date;
                    }
                    catch (Exception e)
                    {
                        //something went wrong parsing date
                    }
                }
            }

            if (dutyOffDate != null)
            {
                
            }

            if (dutyOffDateTime != null)
            {
                DateTime? date = DateTimeExtension.DateFromString(dutyOffDateTime._display);
                if (date != null)
                {
                    _missingComponents.Remove("dutyOff everything");
                    _dutyOffDateTime = date;
                }
            }
        }
        

        if (timeIntervals.Count > 0)
        {
            GetTimeFromIntervals(timeIntervals);
        }

        _captainResponse = GetCaptainResponse();
    }

    private void GetTimeFromIntervals(List<SpeechUnit> timeIntervals)
    {
        // Change all string terms to numeric for addition
        foreach (var interval in timeIntervals)
        {
            if (interval._words[0] is string str && !int.TryParse(str, out _))
            {
                interval._words[0] = WordsToNumber(str).ToString();
            }
        }

        string dutyOnTime = _dutyOn["time"];
        List<string> exactTime = new List<string>();
        string timeString = string.Empty;
        string[] dutyOnValues = Array.Empty<string>();

        if (dutyOnTime.Contains("am"))
        {
            dutyOnValues = dutyOnTime.Split(new[] { 'a' }, StringSplitOptions.RemoveEmptyEntries);
            timeString = "am";
        }
        else if (dutyOnTime.Contains("pm"))
        {
            dutyOnValues = dutyOnTime.Split(new[] { 'p' }, StringSplitOptions.RemoveEmptyEntries);
            timeString = "pm";
        }
        else
        {
            dutyOnValues = new[] { dutyOnTime };
        }

        if (dutyOnValues[0].Contains(":"))
        {
            exactTime = dutyOnValues[0].Split(':').ToList();
        }
        else
        {
            exactTime.Add(dutyOnValues[0]);
        }

        int hours = 0;
        int minutes = 0;

        if (timeIntervals.Count == 2)
        {
            if (int.TryParse(timeIntervals[0]._words[0].ToString(), out int hourValue))
            {
                hours = hourValue;
            }
            if (int.TryParse(timeIntervals[1]._words[0].ToString(), out int minuteValue))
            {
                minutes = minuteValue;
            }
        }
        else if (timeIntervals.Count == 1)
        {
            if (timeIntervals[0]._words[1] == "hour" || timeIntervals[0]._words[1] == "hours")
            {
                if (int.TryParse(timeIntervals[0]._words[0].ToString(), out int hourValue))
                {
                    hours = hourValue;
                }
            }
            else if (timeIntervals[0]._words[1] == "minute" || timeIntervals[0]._words[1] == "minutes")
            {
                if (int.TryParse(timeIntervals[0]._words[0].ToString(), out int minuteValue))
                {
                    minutes = minuteValue;
                }
            }
        }

        bool switchTime = true;
        if (exactTime[0] == "12")
        {
            switchTime = false;
        }

        minutes += int.Parse(exactTime[1]);
        if (minutes >= 60)
        {
            minutes -= 60;
            hours += 1;
        }

        hours += int.Parse(exactTime[0]);
        if (hours > 12)
        {
            hours -= 12;
            if (timeString == "pm")
            {
                if (switchTime)
                {
                    timeString = "am";
                }
            }
            else if (timeString == "am")
            {
                if (switchTime)
                {
                    timeString = "pm";
                }
            }
        }

        minutes = int.Parse(minutes.ToString("D2"));
        string times = $"{hours}:{minutes}";
        string finalTimeString = $"{times}{timeString}";

        _dutyOff["time"] = finalTimeString;
        _missingComponents.Remove("dutyOff time");
    }
    
    private static double WordsToNumber(string data)
    {
        var replacements = new Dictionary<string, string>
        {
            { "zero", "0" },
            { "a", "1" },
            { "one", "1" },
            { "two", "2" },
            { "three", "3" },
            { "four", "4" },
            { "five", "5" },
            { "six", "6" },
            { "seven", "7" },
            { "eight", "8" },
            { "nine", "9" },
            { "ten", "10" },
            { "eleven", "11" },
            { "twelve", "12" },
            { "thirteen", "13" },
            { "fourteen", "14" },
            { "fifteen", "15" },
            { "sixteen", "16" },
            { "seventeen", "17" },
            { "eighteen", "18" },
            { "nineteen", "19" },
            { "twenty", "20" },
            { "thirty", "30" },
            { "forty", "40" },
            { "fifty", "50" },
            { "sixty", "60" },
            { "seventy", "70" },
            { "eighty", "80" },
            { "ninety", "90" },
            { "hundred", "100" },
            { "thousand", "1000" },
            { "million", "1000000" },
            { "billion", "1000000000" },
            { "and", "" }
        };

        foreach (var kvp in replacements)
        {
            data = data.Replace(kvp.Key, kvp.Value);
        }

        var parts = Regex.Split(data, @"[\s-]+")
                         .Select(val => double.TryParse(val, out var number) ? number : 0)
                         .ToList();

        var stack = new Stack<double>();
        double sum = 0;
        double last = 0;

        foreach (var part in parts)
        {
            if (stack.Count > 0)
            {
                if (stack.Peek() > part)
                {
                    if (last >= 1000)
                    {
                        sum += stack.Pop();
                        stack.Push(part);
                    }
                    else
                    {
                        stack.Push(stack.Pop() + part);
                    }
                }
                else
                {
                    stack.Push(stack.Pop() * part);
                }
            }
            else
            {
                stack.Push(part);
            }

            last = part;
        }

        return sum + (stack.Count > 0 ? stack.Pop() : 0);
    }

}