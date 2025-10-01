using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Unity.VisualScripting;
using UnityEngine;

public static class Lemmatizer
{
    public static string LemmatizeVerb(string verb)
    {
        // basic format, there are definitely some faults - maybe just keep this to python model

        // need a better way to deal with irregular verbs
        Dictionary<string, string> irregularVerbs = new Dictionary<string, string>
        {
            { "sent", "send" },
            { "found", "find" },
            { "searched", "search" },
        };

        if (irregularVerbs.ContainsKey(verb))
        {
            return irregularVerbs[verb];
        }

        // -ing verbs
        if (verb.EndsWith("ing"))
        {
            if (verb.Length > 4 && verb[verb.Length - 4] == verb[verb.Length - 5])
            {
                // sometimes conjucated with double consonants on -ing
                //  [] i.e. "running" -> "run"
                return verb.Substring(0, verb.Length - 4);
            }
            return verb.Substring(0, verb.Length - 3);
        }

        // -ed verbs
        else if (verb.EndsWith("ed"))
        {
            if (verb.Length > 3 && verb[verb.Length - 3] == verb[verb.Length - 4])
            {
                // sometimes conjucated with double consonants on -ing
                //  [] i.e. "booked" -> "book"
                return verb.Substring(0, verb.Length - 3);
            }
            return verb.Substring(0, verb.Length - 2);
        }

        // -s plural verbs - ! "is"? !
        else if (verb.EndsWith("s") && verb != "is")
        {
            return verb.Substring(0, verb.Length - 1);
        }

        return verb;
    }
}

