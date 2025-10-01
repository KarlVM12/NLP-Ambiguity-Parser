using System;
using System.Collections.Generic;

using DT7.Data;
using JetBrains.Annotations;
using Newtonsoft.Json;
using UnityEngine;

public class GrammarData
{
    public delegate void DelegateOnDataLoadComplete();

    private static DelegateOnDataLoadComplete _onDataLoadComplete;
    
    private static DataManager _dataManager;

    private static List<User> _users = new List<User>();
    
    private static List<UserNameAlias> _userNameAliases = new List<UserNameAlias>();

    private static List<DictionaryAlias> _dictionaryAliases = new List<DictionaryAlias>();

    private static List<DictionaryAuxiliaryVerb> _dictionaryAuxiliaryVerbs = new List<DictionaryAuxiliaryVerb>();

    private static List<DictionaryContraction> _dictionaryContractions = new List<DictionaryContraction>();

    private static List<DictionaryDateTerm> _dictionaryDateTerms = new List<DictionaryDateTerm>();

    private static List<DictionaryDeterminer> _dictionaryDeterminers = new List<DictionaryDeterminer>();

    private static List<DictionaryInterrogative> _dictionaryInterrogatives = new List<DictionaryInterrogative>();

    private static List<DictionaryTerm> _dictionaryTerms = new List<DictionaryTerm>();

    private static List<StoryTitle> _storyTitles = new List<StoryTitle>();

    private static List<StoryResponse> _storyResponses = new List<StoryResponse>();

    private static List<StoryCategory> _storyCategories = new List<StoryCategory>();

    private static List<PredictiveTerm> _predictiveTerms = new List<PredictiveTerm>();

    private static List<PredictiveTermWord> _predictiveTermWords = new List<PredictiveTermWord>();
    private static List<Location> _locations = new List<Location>();
    
    public static void LoadData(DelegateOnDataLoadComplete onDataLoadComplete)
    {
        _onDataLoadComplete = onDataLoadComplete;
        
        AppHelper.LoadAuthorizedUser();
        _dataManager = DataManager.instance;
        
        List<DataRequestRead> dataRequestReads = new List<DataRequestRead>();

        dataRequestReads.Add(new DataRequestRead("SELECT `term`, `definedObject` FROM `dictionary_alias`", "dictionaryAlias"));
        dataRequestReads.Add(new DataRequestRead("SELECT `verb`, `type` FROM `dictionary_auxiliary_verbs`", "dictionaryAuxiliaryVerbs"));
        dataRequestReads.Add(new DataRequestRead("SELECT `term`, `definedObject`, `replacement` FROM `dictionary_contractions`", "dictionaryContractions"));
        dataRequestReads.Add(new DataRequestRead("SELECT `term`, `partsOfSpeech` FROM `dictionary_date_term`", "dictionaryDateTerms"));
        dataRequestReads.Add(new DataRequestRead("SELECT `term`, `type` FROM `dictionary_determiners`", "dictionaryDeterminers"));
        dataRequestReads.Add(new DataRequestRead("SELECT `term`, `type` FROM `dictionary_interrogatives`", "dictionaryInterrogatives"));
        dataRequestReads.Add(new DataRequestRead("SELECT `term`, `partOfSpeech` FROM `dictionary_term`", "dictionaryTerms"));
        dataRequestReads.Add(new DataRequestRead("SELECT `story_id`, `story_name` FROM `story_title`", "storyTitles"));
        dataRequestReads.Add(new DataRequestRead("SELECT `id`, `story`, `group_set`, `subgroup_set`, `response` FROM `story_response`", "storyResponses"));
        dataRequestReads.Add(new DataRequestRead("SELECT `id`, `story_guid`, `category`, `value` FROM `story_categories`", "storyCategories"));
        dataRequestReads.Add(new DataRequestRead("SELECT `id`, `full_name` FROM `users`", "users"));
        dataRequestReads.Add(new DataRequestRead("SELECT `user_id`, `alias` FROM `user_name_aliases`", "userNameAliases"));
        dataRequestReads.Add(new DataRequestRead("SELECT `token`, `term`, `tally` FROM `predictive_term`", "predictiveTerms"));
        dataRequestReads.Add(new DataRequestRead("SELECT `id`, `term_token`, `word`, `usage_count` FROM `predictive_term_word`", "predictiveTermWords"));
        dataRequestReads.Add(new DataRequestRead("SELECT `id`, `name`, `country_name`,`region_name`, `city_name` FROM `airports`", "locations"));

        _dataManager.LoadDataSet(OnReturnData, dataRequestReads.ToArray());
    }

    private static void OnReturnData(DataSet dataSet)
    {
        foreach (DataUnStructured data in dataSet.UnStructuredRecord["dictionaryAlias"].Row)
        {
            _dictionaryAliases.Add(JsonConvert.DeserializeObject<DictionaryAlias>(JsonConvert.SerializeObject(data.Column)));
        }
        
        foreach (DataUnStructured data in dataSet.UnStructuredRecord["dictionaryAuxiliaryVerbs"].Row)
        {
            _dictionaryAuxiliaryVerbs.Add(JsonConvert.DeserializeObject<DictionaryAuxiliaryVerb>(JsonConvert.SerializeObject(data.Column)));
        }
        
        foreach (DataUnStructured data in dataSet.UnStructuredRecord["dictionaryContractions"].Row)
        {
            _dictionaryContractions.Add(JsonConvert.DeserializeObject<DictionaryContraction>(JsonConvert.SerializeObject(data.Column)));
        }
        
        foreach (DataUnStructured data in dataSet.UnStructuredRecord["dictionaryDateTerms"].Row)
        {
            _dictionaryDateTerms.Add(JsonConvert.DeserializeObject<DictionaryDateTerm>(JsonConvert.SerializeObject(data.Column)));
        }
        
        foreach (DataUnStructured data in dataSet.UnStructuredRecord["dictionaryDeterminers"].Row)
        {
            _dictionaryDeterminers.Add(JsonConvert.DeserializeObject<DictionaryDeterminer>(JsonConvert.SerializeObject(data.Column)));
        }
        
        foreach (DataUnStructured data in dataSet.UnStructuredRecord["dictionaryInterrogatives"].Row)
        {
            _dictionaryInterrogatives.Add(JsonConvert.DeserializeObject<DictionaryInterrogative>(JsonConvert.SerializeObject(data.Column)));
        }
        
        foreach (DataUnStructured data in dataSet.UnStructuredRecord["dictionaryTerms"].Row)
        {
            _dictionaryTerms.Add(JsonConvert.DeserializeObject<DictionaryTerm>(JsonConvert.SerializeObject(data.Column)));
        }
        
        foreach (DataUnStructured data in dataSet.UnStructuredRecord["storyTitles"].Row)
        {
            _storyTitles.Add(JsonConvert.DeserializeObject<StoryTitle>(JsonConvert.SerializeObject(data.Column)));
        }
        
        foreach (DataUnStructured data in dataSet.UnStructuredRecord["storyResponses"].Row)
        {
            _storyResponses.Add(JsonConvert.DeserializeObject<StoryResponse>(JsonConvert.SerializeObject(data.Column)));
        }
        
        foreach (DataUnStructured data in dataSet.UnStructuredRecord["storyCategories"].Row)
        {
            _storyCategories.Add(JsonConvert.DeserializeObject<StoryCategory>(JsonConvert.SerializeObject(data.Column)));
        }
        
        foreach (DataUnStructured data in dataSet.UnStructuredRecord["users"].Row)
        {
            _users.Add(JsonConvert.DeserializeObject<User>(JsonConvert.SerializeObject(data.Column)));
        }
        
        foreach (DataUnStructured data in dataSet.UnStructuredRecord["userNameAliases"].Row)
        {
            _userNameAliases.Add(JsonConvert.DeserializeObject<UserNameAlias>(JsonConvert.SerializeObject(data.Column)));
        }
        
        foreach (DataUnStructured data in dataSet.UnStructuredRecord["predictiveTerms"].Row)
        {
            _predictiveTerms.Add(JsonConvert.DeserializeObject<PredictiveTerm>(JsonConvert.SerializeObject(data.Column)));
        }
        
        foreach (DataUnStructured data in dataSet.UnStructuredRecord["predictiveTermWords"].Row)
        {
            _predictiveTermWords.Add(JsonConvert.DeserializeObject<PredictiveTermWord>(JsonConvert.SerializeObject(data.Column)));
        }

        foreach (DataUnStructured data in dataSet.UnStructuredRecord["locations"].Row)
        {
            _locations.Add(JsonConvert.DeserializeObject<Location>(JsonConvert.SerializeObject(data.Column)));
        }

        _onDataLoadComplete?.Invoke();
    }

    public static string GetStoryResponse(string storyType, string groupSet, string subgroupSet)
    {
        string response = "";
        
        foreach (StoryResponse storyResponse in _storyResponses)
        {
            if (storyResponse.HasAttributes(storyType, groupSet, subgroupSet)) response = storyResponse.GetResponse();
        }

        return response;
    }

    public static string GetStoryId(string storyName)
    {
        string result = null;
        
        foreach (StoryTitle storyTitle in _storyTitles)
        {
            if (storyTitle._storyName == storyName)
            {
                result = storyTitle._storyId;
            }
        }

        return result;
    }

    public static List<string> GetValuesForStoryCategory(string storyGuid, string category)
    {
        List<string> values = new List<string>();

        foreach (StoryCategory storyCategory in _storyCategories)
        {
            if (storyCategory._storyGuid == storyGuid && storyCategory._category == category)
            {
                values.Add(storyCategory._value);
            }
        }

        return values;
    }

    public static List<string> GetActionVerbTerms(){
        List<string> actionVerbs = new List<string>();

        foreach (StoryCategory storyCategory in _storyCategories){
            if (storyCategory._category == "action_verb"){
                actionVerbs.Add(storyCategory._value);
            }
        }

        return actionVerbs;
    }

    public static Tuple<string, int> GetTokenTallyForClassifierTerm(string term)
    {
        Tuple<string, int> tokenTally = null;

        foreach (PredictiveTerm predictiveTerm in _predictiveTerms)
        {
            if (predictiveTerm.GetTerm() == term)
            {
                tokenTally = new Tuple<string, int>(predictiveTerm.GetToken(), predictiveTerm.GetTally());
                break;
            }
        }

        return tokenTally;
    }

    public static List<WeightRow> GetWeightRowsForPredictiveTermWord(string termToken)
    {
        List<WeightRow> weightRows = new List<WeightRow>();

        int tally = GetTallyForPredictiveTermToken(termToken);
        
        foreach (PredictiveTermWord predictiveTermWord in _predictiveTermWords)
        {
            if (predictiveTermWord.GetTermToken() == termToken)
            {
                weightRows.Add(new WeightRow(predictiveTermWord.GetWord(), predictiveTermWord.GetUsageCount() / (float)tally));
            }
        }

        return weightRows;
    }

    private static int GetTallyForPredictiveTermToken(string termToken)
    {
        int tally = 100;
        
        foreach (PredictiveTerm predictiveTerm in _predictiveTerms)
        {
            if (predictiveTerm.GetToken() == termToken)
            {
                tally = predictiveTerm.GetTally();

                break;
            }
        }

        return tally;
    }

    public static List<DictionaryContraction> GetDictionaryContractions()
    {
        return _dictionaryContractions;
    }

    public static List<DictionaryDateTerm> GetDictionaryDateTerms()
    {
        return _dictionaryDateTerms;
    }

    public static List<DictionaryInterrogative> GetDictionaryInterrogatives()
    {
        return _dictionaryInterrogatives;
    }

    public static List<DictionaryDeterminer> GetDictionaryDeterminers()
    {
        return _dictionaryDeterminers;
    }
    
    public static List<DictionaryAuxiliaryVerb> GetDictionaryAuxiliaryVerbs()
    {
        return _dictionaryAuxiliaryVerbs;
    }

    public static List<UserNameAlias> GetUserNameAliases()
    {
        return _userNameAliases;
    }

    public static List<User> GetUsers()
    {
        return _users;
    }

    public static List<DictionaryTerm> GetDictionaryTerms()
    {
        return _dictionaryTerms;
    }

    public static List<DictionaryAlias> GetDictionaryAliases()
    {
        return _dictionaryAliases;
    }

    public static List<Location> GetLocations()
    {
        return _locations;
    }

}