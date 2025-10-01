using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DT7.Data;
using Newtonsoft.Json;
using Unity.VisualScripting;
using UnityEngine.UIElements;
using System.Linq;


public class StoryTester: MonoBehaviour
{
 private DataManager _dataManager;
 BaseStoryObject TheStoryObject;

    private bool _performAction = true;

    private void Update()
    {
        if (_performAction)
        {
            AppHelper.LoadAuthorizedUser();
            _dataManager = DataManager.instance;

            GrammarData.LoadData(OnDataLoadComplete);
            
            _performAction = false;
        }
        
        _dataManager.QueUpdate();
    }

    private void OnDataLoadComplete()
    {
        string promptOne = "send a message to ron smith about his flight which says he is flying"; // "flight" misidentifies as adj -> noun, "which" misidentifies as noun -> rel pronoun -> fixed in GrammarFinalPass()
        promptOne = "send a message to ron smith about his flight that says he is flying"; // "that" misidentifies as adv -> relative pronoun -> fixed in GrammarFinalPass()
        //promptOne = "tell ron smith";
        //promptOne = "Send about his flight"; // actually kinda works?
        //promptOne = "Send a message saying he is flying to Ron Smith"; // is the message "he is flying to Ron Smith" or "he is flying" recipient Ron Smith? ambiguous enough to let slide
        //promptOne = "Send about his flight a message that says he is flying to Ron Smith"; // works, but interprets the message as "he is flying to Ron Smith", also story input object issue
        //promptOne = "send a chat to ron smith about the flight saying it is in the sky";
        //promptOne = "tell ron smith he is flying tomorrow";
        //promptOne = "tell ron smith he is flying";
        //promptOne = "send him a message";
        //promptOne = "send a message to ron smith about his flight that says hi";
        //promptOne = "is he on tomorrow?"; 
        //promptOne = "Put Ron on the schedule for tomorrow on trip 2024-76206 at 12:00"; // 99 is a datetime for some reason, but anything >= 100 isn't
        //promptOne = "Put Ron on the schedule for tomorrow from 12:00pm to 8:00pm for trip 2024-76206"; // this works very well - very exciting !!
        //promptOne = "he is flying to the airport on saturday"; // "he" marked as N/A - has no unknown phrase in grammarPhraseList so bigram never checks it - fixed
        //promptOne = "he is flying today"; // "he" and "tomorrow" still N/A - "tomorrow" disappears, in masterWordList, not in grammarList - fixed
        //promptOne = "tell ron smith about his flight tomorrow"; // flight misidentified as adjective, so don't get flight context - fixed
        //promptOne = "send a message to ron smith about his flight that just says hi"; // "flight", "that", "just" all N/A - kinda fixed, no phrase for "about his flight"
        //promptOne = "put me on the schedule for 11am on the flight tomorrow"; // "flight" misidentified as adj causing it not to be obj of sentence

        // N/A issues
        //promptOne = "This is the flight for tomorrow"; // sorta works, issues with "this"
        //promptOne = "send tony malfa a message about tomorrow"; // "about" still N/A even after grammarManager but contained in a noun phrase??
        //promptOne = "that is good"; // "that" still N/A, "good" disappears - no phrase in grammarList - now becomes a relativeClause so no root verb detected

        // new sentences
        promptOne = "She said she is going to the United States, then to North Dakota, and then to Linfen"; // Isn't that good?   
        //promptOne = "This is a test for Capital Letters in Ron Smith's sentence. He'd tell us if It works";

        GrammarManager grammar = new GrammarManager(promptOne);

        Debug.Log("HERE: GRAMMAR_MANAGER GRAMMAR");
        Debug.Log(JsonConvert.SerializeObject(grammar._masterWordList));
        Debug.Log(JsonConvert.SerializeObject(grammar._grammarPhraseList));

        // Dependency Parsing to find subordinate class within the message
        // i.e.) "send a message to ron smith about his flight that says he is flying";
        //      Root Main Verb: "send"
        //      Subject: implied you ("captain")
        //      Direct Object: "message"
        //          determiner: "a"
        //      Prep phrase: "to Ron Smith"
        //      Prep phrase: "about his flight"
        //      Relative clause: "that says he is flying"
        //          relative trigger: "that"
        //              verb: "says"
        //          subordinate clause:
        //              subject: "he"
        //              aux: "is"
        //              verb: "flying"

        //    └─ send(ROOT, verb)
        //      ├─ message(dobj, noun)
        //      │   ├─ a(det, determiner)
        //      │   └─ about(prep, preposition)
        //      │       └─ flight(pobj, noun)
        //      │           ├─ his(det, determiner)
        //      │           └─ that(relcl, pronoun)
        //      │               └─ says(acl: relcl, verb)
        //      │                   └─ flying(ccomp, verb)
        //      │                       ├─ is (aux, verb)
        //      │                       └─ he(nsubj, noun)
        //      └─ to(prep, preposition)
        //          └─ Ron Smith(pobj, noun)

        // PredictiveProcess --> Chat --> message <-- subordinate clause if not null, else...
        
        DependencyParser parser = new DependencyParser(new GrammarManager(promptOne));

        // i.e.) "send a message to ron smith about his flight that says he is flying";
        //      "that" is being incorrectly indentified as an adverb when it is actually a relative pronoun marking a relative clause
        //      Therefore, have to fix that with a relativeClauseGrammarPass before we can actually make the dependency tree
        parser.RelativeClauseGrammarPass();
        DependencyNode rootDependencyTree = parser.Parse();
        Debug.Log("HERE: PARSER GRAMMAR");
        Debug.Log(JsonConvert.SerializeObject(parser._grammar._masterWordList));
        Debug.Log(JsonConvert.SerializeObject(parser._grammar._grammarPhraseList));

        if (rootDependencyTree != null)
        {
            Debug.Log("DEPENDENCY TREE");
            PrintDependencyTree(rootDependencyTree);

            var labels = SemanticMapper.Map(rootDependencyTree);
            var labelsStr = ""; 
            foreach (var role in labels)
            {
                labelsStr += "[" + role.Key + ": "; 
                foreach (var node in role.Value)
                {
                    labelsStr += (node.Word) + ", ";
                }

                labelsStr += "], ";
            }
            Debug.Log("PROMPT LABELS: " + labelsStr);
            Debug.Log("CHAT MESSAGE: " + parser.SubordinateClause);

            // go to IntentClassification -> using synsets (a group of synonyms belonging to the same concept)
            //  [] this will allow us to determine some classification before Predictive ML
            //  [] Princeton WordNet -> maybe make model in python and export json?
            //      [] https://wordnet.princeton.edu/
            //  [] synsets - synonyms linked by conceptual relations
            //  [] troponymy - tree linked verbs that get more specific further down the tree
            //      [] {communicate}->{talk}->{whisper}

            // Lemmatizing Verbs?
            //  [] reducing verbs to their base
            //      [] {sending}->{send}, {sent}->{send}, {sends}->{send}
            //      [] {booking}->{book}, {booked}->{book}, {books}->{book} !! Special case b/c booking is more known as a noun
            //  [] hard to do in practice logical without ML

            // We can combine lemmatization and synsets using a python model
            //  [] intent_classifier.py
            //  [] lemmatizes the word, grabs all relevant hyponyms to the current synsets and keywords

            var classifiedIntent = IntentClassification.DetermineIntent(labels);
            Debug.Log("CLASSIFIED INTENT :\n" + classifiedIntent);

            if (classifiedIntent == "chat")
            {
                

                Grammar.SpeechUnit main_verbSpeechUnit = parser._grammar._masterWordList[(labels["main_verb"][0].Hash)];
                List<Grammar.SpeechUnit> recipientsList = labels["recipients"].Select(node => parser._grammar._masterWordList[node.Hash]).ToList();

                // should probably be a little more nuanced
                string messageText = (labels["objects"].Count > 0 ? labels["objects"][0].Word : null);

                // selecting each node of the subordinate clause to grab its hash from the masterWordList to form the List<SpeechUnit> for the GrammarPhraseObject
                List<GrammarPhraseObject> objectPhraseList = new List<GrammarPhraseObject>();
                GrammarPhraseObject message = new GrammarPhraseObject();
                var subordinateClause = parser.GetLastNestedComplementClauseNodes() ?? new List<DependencyNode>();
                message._speechUnits = subordinateClause.Count > 0 ? subordinateClause.Select(node => parser._grammar._masterWordList[node.Hash]).ToList() : new List<Grammar.SpeechUnit>();

                // if no message exists, -1, else message will be placed and end at first index 0
                int messageBodyStartIndex = subordinateClause.Count > 0 ? 0 : -1;
                int messageBodyEndIndex = subordinateClause.Count > 0 ? 0 : -1;
                objectPhraseList.Add(message);

                // not really sure what this means
                GrammarPhraseObject subjectTopic = new GrammarPhraseObject();

                TheStoryObject = new ChatStoryObject(main_verbSpeechUnit, recipientsList, messageText, messageBodyStartIndex, messageBodyEndIndex, objectPhraseList, subjectTopic);
                Debug.Log("====================");
                UnityEngine.Debug.Log(JsonConvert.SerializeObject(TheStoryObject.CaptainResponse));
                UnityEngine.Debug.Log(JsonConvert.SerializeObject(TheStoryObject._missingComponents));
                Debug.Log("====================");
            }
        }
        else
        {
            Debug.Log("CAPTAIN, we messed up...");
        }


        PredictiveProcess predictiveProcess = new PredictiveProcess(promptOne, new GrammarManager(promptOne));
        predictiveProcess.process();
        float exitWeight = predictiveProcess.checkExitStatus(); //how we check exit weight
        string classifier = predictiveProcess._guessedClassifierTerm;
        float weight = predictiveProcess._guessedClassifierWeight;


        Debug.Log("classifier " + classifier + " : " + exitWeight + " : " + weight);



        // current prompt is about .33 in weight here, but .45 in php.. why? 
        //UnityEngine.Debug.Log(exitWeight);
        if (exitWeight > .4){
            UnityEngine.Debug.Log("EXIT");
            return;
        }

        Dictionary<string, List<string>> quickCommands = new Dictionary<string, List<string>>();

        string quickID = GrammarData.GetStoryId("quick_command");

        List<string> quickCommandDataSet = GrammarData.GetValuesForStoryCategory(quickID, "action_verb");

        foreach (var command in quickCommandDataSet)
        {
            List<string> aliasValues = GrammarData.GetValuesForStoryCategory(quickID, $"{command}_alias");

            if (!quickCommands.ContainsKey(command))
            {
                quickCommands[command] = new List<string>();
            }

            quickCommands[command].AddRange(aliasValues);
        }

        UnityEngine.Debug.Log(JsonConvert.SerializeObject(grammar._masterWordList));
        UnityEngine.Debug.Log(JsonConvert.SerializeObject(grammar._sentenceObject));
        bool isQuickCommand = false;
        if (grammar._sentenceObject != null)
        {
            string mainVerb = grammar._sentenceObject._mainVerbObject._mainVerb._display;
            isQuickCommand = quickCommands.ContainsKey(mainVerb) || quickCommands.Values.Any(list => list.Contains(mainVerb));
        }
        
        if (isQuickCommand)
        {
            classifier = "quick_command";
        }

        if (classifier == "quick_command")
        {
            UnityEngine.Debug.Log("QUICK COMMAND");
        }
        if (classifier == "navigate")
        {
            NavigateStory navigateStory = new NavigateStory(grammar._sentenceObject);
            NavigateStoryObject navigateStoryObject = navigateStory.FillStory();

            UnityEngine.Debug.Log(JsonConvert.SerializeObject(navigateStoryObject));

        }

        if (classifier == "chat")
        {
            
            ChatStory chatStory = new ChatStory(grammar._sentenceObject);
            TheStoryObject = chatStory.FillStory();

            //UnityEngine.Debug.Log(JsonConvert.SerializeObject(chatStory._chatVerbWords));
            UnityEngine.Debug.Log(JsonConvert.SerializeObject(TheStoryObject.CaptainResponse));
            UnityEngine.Debug.Log(JsonConvert.SerializeObject(TheStoryObject._missingComponents));
            //UnityEngine.Debug.Log(JsonConvert.SerializeObject(TheStoryObject._messageText));
             UnityEngine.Debug.Log(JsonConvert.SerializeObject(grammar._masterWordList));

        }
        if (classifier == "schedule")
        {
            UnityEngine.Debug.Log("HERE SCHEDULE");
        }
        
    }

    public void PrintDependencyTree(DependencyNode node, string indent = "", bool last = true)
    {
        // Print the current node
        Debug.Log(indent + (last ? "└─ " : "├─ ") + node.Word + " (" + node.DependencyType + ", " + node.PartOfSpeech + ")");

        // Increase indentation for child nodes
        indent += last ? "    " : "│   ";

        for (int i = 0; i < node.Children.Count; i++)
        {
            // Determine if the child is the last in the list
            bool isLast = i == node.Children.Count - 1;
            PrintDependencyTree(node.Children[i], indent, isLast);
        }
    }

}
