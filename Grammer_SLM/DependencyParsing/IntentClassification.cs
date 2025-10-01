using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Unity.VisualScripting;
using UnityEngine;


public class IntentClassification
{
    // holds intent verbs json from python model
    public static Dictionary<string, List<string>> IntentVerbs;

    public static string DetermineIntent(Dictionary<string, List<DependencyNode>> roles)
    {
        Dictionary<string, double> roleWeights = new Dictionary<string, double>
        {
            { "main_verb", 3.0 },
            { "subject", 1.5 },
            { "objects", 2.0 },
            { "entities", 1.0 },
            { "recipients", 1.5 },
            { "datetimes", 1.0 },
            { "locations", 1.0 }
            // roles come from SemanticMapper - make sure same extra keys !!
        };

        var intents = GetIntentSynsetsFromJson(); //IntentSynonyms.IntentVerbs;
        Dictionary<string, double> intentScores = new Dictionary<string, double>();

        // lemmatize words in roles - again maybe keep this to python
        var rolesKeys = new List<string>(roles.Keys);
        var intentsKeys = new List<string>(intents.Keys);
        foreach (var role in rolesKeys)
        {
            foreach (var node in roles[role])
            {
                node.Word = Lemmatizer.LemmatizeVerb(node.Word.ToLower());
            }
        }

        // iterate over each intent class and get a score
        foreach (var intent in intentsKeys)
        {
            double totalScore = 0.0;
            var intentWords = new HashSet<string>(intents[intent]);

            foreach (var role in rolesKeys)
            {
                // get weight, amount of matches, and add to total score
                double weight = roleWeights.ContainsKey(role) ? roleWeights[role] : 1.0;
                int matchCount = roles[role].Count(word => intentWords.Contains(word.Word));
                totalScore += matchCount * weight;
            }

            intentScores[intent] = totalScore;
        }

        // get highest intent class score
        string bestIntent = "unknown";
        double highestScore = 0.0;
        foreach (var intentScore in intentScores)
        {
            if (intentScore.Value > highestScore)
            {
                highestScore = intentScore.Value;
                bestIntent = intentScore.Key;
            }
        }

        // return all values
        //return intentScores;

        // score threshold for false positives - not sure what value is appropriate here
        //double scoreThreshold = 2.6; // reasoning - 3.0 gets you a verb, so we want a little more than that, although this might be high
        double scoreThreshold = 1.0;
        if (highestScore >= scoreThreshold)
        {
            return bestIntent;
        }

        return "unknown";
    }

    public static Dictionary<string, List<string>> GetIntentSynsetsFromJson()
    {
        TextAsset jsonText = Resources.Load<TextAsset>("intent_verbs");
        string jsonContent = jsonText.text;

        IntentVerbs = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(jsonContent);

        // incase any become uppercase
        var keys = new List<string>(IntentVerbs.Keys);
        foreach (var intent in keys)
        {
            IntentVerbs[intent] = IntentVerbs[intent].ConvertAll(v => v.ToLower());
        }

        return IntentVerbs;
    }
}
