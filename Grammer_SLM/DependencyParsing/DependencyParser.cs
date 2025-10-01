using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Unity.VisualScripting;
using UnityEngine;
using Grammar;

public class DependencyParser
{
    public GrammarManager _grammar;
    private List<string> phrasalVerbs;
    private Dictionary<string, DependencyNode> wordNodes;

    public string SubordinateClause { get; private set; }

    public DependencyParser(GrammarManager grammar)
    {
        _grammar = grammar;
        wordNodes = new Dictionary<string, DependencyNode>();
        phrasalVerbs = GetPhrasalVerbsFromJson();

        // do a final bigram pass to fix:
        //  [] N/A words that for some reason are still in the list at this point
        //  [] N/A words that also don't have a related unknown phrase in the grammarPhraseList - better to at least have that phrase for this process even if unknown can still interpret it
        _grammar.FinalBigramPassMissingUnknownPhrase(); // for missing unknown phrases
        _grammar.FinalBigramPassMissingUnknownPhrase(false); // for unknown phrases that already exists, false lets it know this and to identify accordingly
    }

    public DependencyNode Parse()
    {
        foreach (var entry in _grammar._masterWordList)
        {
            var wordUnit = entry.Value;

            string partOfSpeech = wordUnit._definiteType;

            DependencyNode node = new DependencyNode
            {
                Hash = wordUnit._hash,
                Word = wordUnit._display,
                PartOfSpeech = partOfSpeech,
                Index = wordUnit._index,
                IsPossessive = wordUnit._isPossessive,
                IsAuxiliary = wordUnit._isAuxiliary,
                IsDateTimeUnit = wordUnit._isDateTimeUnit,
                IsEntity = (wordUnit._type == "entity" ? true : false),
                IsNumeral = (wordUnit._type == "numeral" ? true : false),
            };
            wordNodes.Add(wordUnit._hash, node);
        }

        // everything stems from root verb
        DependencyNode root = FindRootVerb();

        if (root != null)
        {
            root.DependencyType = "ROOT";
            BuildDependencies(root);
        }
        else
        {
            Debug.LogWarning("No main verb found in the sentence.");
            return null;
        }

        SubordinateClause = GetLastNestedComplementClause();

        return root;
    }

    private DependencyNode FindRootVerb()
    {
        var mainVerbPhrase = _grammar._grammarPhraseList.FirstOrDefault(p => p._phraseType == "verb phrase");

        if (mainVerbPhrase != null)
        {
            var verbHashes = mainVerbPhrase._speechHashUnits.Where(key => _grammar._masterWordList[key]._definiteType == "verb").ToList();

            if (verbHashes.Count == 0)
            {
                return null;
            }

            // find the main verb, not aux
            var mainVerbHash = verbHashes.FirstOrDefault(key => !_grammar._masterWordList[key]._isAuxiliary);
            if (mainVerbHash != null)
            {
                return wordNodes[mainVerbHash];
            }
            else
            {
                return wordNodes[verbHashes.Last()]; // if all are aux, returns the last verb of the phrase
            }
        }

        return null;
    }

    private void BuildDependencies(DependencyNode root)
    {
        DependencyNode currentVerbRoot = root;
        foreach (var phrase in _grammar._grammarPhraseList)
        {
            if (phrase._phraseType == "noun phrase")
            {
                // find the head noun in the phrase
                var nounNode = phrase._speechHashUnits
                    .Select(hash => wordNodes[hash])
                    .FirstOrDefault(n => n.PartOfSpeech.Contains("noun"));

                if (nounNode != null)
                {
                    // determine if the noun is the subject or object
                    if (nounNode.Index < currentVerbRoot.Index)
                    {
                        // if we are befoer the verb --> subject
                        nounNode.Head = currentVerbRoot;
                        nounNode.DependencyType = "nsubj";
                        currentVerbRoot.Children.Add(nounNode);
                    }
                    else
                    {
                        // could also add when we have a demonstrative as the nsubj, there exist an attribute complement which it is describing
                        //  i.e. "This is the flight for tomorrow": "this" (nsubj, demonstrative) --> "flight" (attr, noun) instead of just "flight" (dobj, noun)
                        //  implementation: if (root.IsAuxilary) { nounNode.DependencyType = "attr"; } else { nounNode.DependencyType = "dobj"; }
                        //  dobj serves it fine though, just a thought


                        
                        // after the verb -> direct object
                        nounNode.Head = currentVerbRoot;
                        nounNode.DependencyType = "dobj";

                        // before we add, check if there exists a dobj that was detected before, if so make that previous dobj an iobj
                        IndirectObjectCheck(currentVerbRoot);

                        // after checking, good to assign
                        currentVerbRoot.Children.Add(nounNode);
                    }

                    // attach modifiers to the noun
                    foreach (var wordHash in phrase._speechHashUnits)
                    {
                        var node = wordNodes[wordHash];
                        if (node != nounNode)
                        {
                            if (node.PartOfSpeech == "determiner")
                            {
                                node.Head = nounNode;
                                node.DependencyType = node.IsPossessive ? "poss" : "det";
                                nounNode.Children.Add(node);
                            }
                            else if (node.PartOfSpeech == "adjective")
                            {
                                node.Head = nounNode;
                                node.DependencyType = "amod";
                                nounNode.Children.Add(node);
                            }
                            else if (node.IsNumeral || node.PartOfSpeech == "numeral")
                            {
                                node.Head = nounNode;
                                node.DependencyType = "nummod";
                                nounNode.Children.Add(node);
                            }
                            else if (node.IsDateTimeUnit)
                            {
                                node.Head = nounNode;
                                node.DependencyType = "dtmod";
                                nounNode.Children.Add(node);
                            }
                        }
                    }
                }
            }
            else if (phrase._phraseType == "prepositional phrase")
            {
                var prepositionHash = phrase._speechHashUnits.First();
                var prepositionNode = wordNodes[prepositionHash];

                DependencyNode headNode = FindPrepositionHead(prepositionNode, currentVerbRoot);

                prepositionNode.Head = headNode;
                prepositionNode.DependencyType = "prep";
                headNode.Children.Add(prepositionNode);

                // attach the object of the preposition and its modifiers
                AttachPrepositionalObject(prepositionNode, phrase._speechHashUnits.Skip(1).ToList());
            }
            else if (phrase._phraseType == "verb phrase" && !phrase._speechHashUnits.Contains(currentVerbRoot.Hash))
            {
                var verbHashes = phrase._speechHashUnits;
                DependencyNode mainVerbNode = null;
                List<DependencyNode> auxVerbNodes = new List<DependencyNode>();

                // have to differentiate between aux and main verb of the phrase
                foreach (var verbHash in verbHashes)
                {
                    var verbNode = wordNodes[verbHash];
                    if (verbHashes.Count == 1) // if only one word in verb phrase, even if aux it is the main verb of that clause
                    {
                        mainVerbNode = verbNode;
                    }
                    else if (verbNode.PartOfSpeech == "verb" && verbNode.IsAuxiliary)
                    {
                        auxVerbNodes.Add(verbNode);
                    }
                    else
                    {
                        // necessary to check because we can get a phrase like "is flying tomorrow" where "tomorrow" shouldn't be there, but will get incorrectly identifyied as an adverb so will select "tomorrow" as ccomp:verb incorrectly because its the last word in the verb phrase
                        if (verbNode.PartOfSpeech == "verb")
                        {
                            mainVerbNode = verbNode;
                        }
                        else
                        {
                            // any other word attached to verb phrase for some reason, like "tomorrow" in the example
                            //  [] should not have to do this, but in misidentified phrases it helps
                            verbNode.Head = mainVerbNode;
                            verbNode.DependencyType = DetermineDependencyTypeSimplified(verbNode, mainVerbNode);
                            mainVerbNode.Children.Add(verbNode);
                        }
                    }
                }

                if (mainVerbNode != null)
                {
                    // check if a ccomp does not already exist, if it does attach to latest ccomp
                    // once a ccomp does exist, anything next in the sentence has to the latest ccomp
                    //      [] i.e. ROOT -> ccomp -> ccomp: [send, ROOT] ... [saying, ccomp] ... [is, ccomp] ...

                    // we want the latest ccomp:verb so LastOrDefault
                    var existingCcompNode = wordNodes.Values.LastOrDefault(node => node.DependencyType == "ccomp");
                    if (existingCcompNode != null)
                    {
                        currentVerbRoot = existingCcompNode;
                    }

                    // attach main verb to root or appropriate secondary node --> complementary clause, ccomp
                    mainVerbNode.Head = currentVerbRoot;
                    mainVerbNode.DependencyType = "ccomp";

                    // before we add new ccomp:verb, make any children of the previous ccomp the child of the current ccomp
                    //  [] if we do this after we add - infinite recursion loop
                    if (existingCcompNode != null)
                    {
                        // add all of the previous ccomp children to the current ccomp
                        mainVerbNode.Children.AddRange(existingCcompNode.Children);

                        // remove those children from previous ccomp
                        existingCcompNode.Children.Clear();

                        // reclassify children because words in between ccomp:verbs most likely wrongly identified
                        ReclassifyCcompChildren(mainVerbNode);

                    }
                    // first ccomp detected in sentence
                    else
                    {
                        // need to do look back for nsubj that belongs inside ccomp because of no ccomp delimiter, only if there doesn't exist a noun past the ccomp
                        //  i.e. "tell ron smith he is flying" - "he" at this point is a dobj of "tell", we should look back at the last dobj and make it nsubj of ccomp
                        //  i.e. "send a message saying he is flying" - we DONT want "message" to be attached to "saying" though, so we also have to do a lookahead for if there are no noun phrases first
                        var lookaheadNounPhrase = wordNodes.Values.FirstOrDefault(node => (node.PartOfSpeech == "noun" || node.PartOfSpeech == "pronoun") && node.Index > mainVerbNode.Index); // after ccomp noun or pronoun
                        if (lookaheadNounPhrase == null)
                        {
                            var lastDirectObject = currentVerbRoot.Children.LastOrDefault(node => node.DependencyType == "dobj");
                            if (lastDirectObject != null)
                            {
                                // make the directObject the child of ccomp and change it to the nsubj
                                currentVerbRoot.Children.Remove(lastDirectObject);
                                lastDirectObject.Head = mainVerbNode;
                                mainVerbNode.Children.Add(lastDirectObject);
                                lastDirectObject.DependencyType = "nsubj";
                            }

                        }

                    }

                    // now we can add new verb to current root
                    currentVerbRoot.Children.Add(mainVerbNode);

                    // make new ccomp next "main" verb root
                    currentVerbRoot = mainVerbNode;

                    // aux attach to main verb of phrase
                    foreach (var auxVerbNode in auxVerbNodes)
                    {
                        auxVerbNode.Head = mainVerbNode;
                        auxVerbNode.DependencyType = "aux";
                        mainVerbNode.Children.Add(auxVerbNode);
                    }

                }
            }
            else if (phrase._phraseType == "relative clause")
            {
                BuildRelativeClauseDependencies(currentVerbRoot, phrase);

                // if we found a relcl, acl:relcl, or ccomp, the deepest in the tree of the three should become the next root so any remaining nodes are attached to it
                var clauseDependencies = new List<string> { "ccomp", "acl:relcl", "relcl" };
                var deepestClause = wordNodes.Values.Where(node => clauseDependencies.Contains(node.DependencyType)).OrderByDescending(node => node.Index).FirstOrDefault();
                if (deepestClause != null)
                {
                    currentVerbRoot = deepestClause;
                }

            }
            else if (phrase._phraseType == "adverb" || phrase._phraseType == "adjective" || phrase._phraseType == "possessive" || phrase._phraseType == "determiner")
            {
                List<DependencyNode> nodes = phrase._speechHashUnits.Select(hash => wordNodes[hash]).ToList();
                foreach (var node in nodes)
                {
                    node.Head = currentVerbRoot;
                    node.DependencyType = "dep";
                    currentVerbRoot.Children.Add(node);
                }
            }

        }

        //  any remaining unattached nodes
        foreach (var node in wordNodes.Values)
        {
            if (node.Head == null && node != currentVerbRoot)
            {

                // skip anything we already attached to dependency tree
                if (IsNodeInRelativeOrComplementClause(node))
                {
                    continue;
                }


                // supplementary nodes that have yet to be attached
                if (node.PartOfSpeech == "determiner")
                {
                    var nounNode = wordNodes.Values
                        .Where(n => n.PartOfSpeech.Contains("noun") && n.Index > node.Index)
                        .OrderBy(n => n.Index)
                        .FirstOrDefault();

                    if (nounNode != null)
                    {
                        node.Head = nounNode;
                        node.DependencyType = node.IsPossessive ? "poss" : "det";
                        nounNode.Children.Add(node);
                    }
                }
                else if (node.PartOfSpeech == "verb" && node.IsAuxiliary)
                {
                    var mainVerbNode = wordNodes.Values
                        .Where(n => n.PartOfSpeech.Contains("verb") && n.PartOfSpeech != "adverb" && n.Index > node.Index && !n.IsAuxiliary)
                        .OrderBy(n => n.Index)
                        .FirstOrDefault();

                    if (mainVerbNode != null)
                    {
                        node.Head = mainVerbNode;
                        node.DependencyType = "aux";
                        mainVerbNode.Children.Add(node);
                    }
                }
                else if (node.PartOfSpeech == "adverb")
                {
                    node.Head = currentVerbRoot;
                    node.DependencyType = "advmod";
                    currentVerbRoot.Children.Add(node);
                }
                else if (node.PartOfSpeech == "pronoun")
                {
                    // relate pronoun to appropriate verb 
                    var verbNode = wordNodes.Values
                        .Where(n => n.PartOfSpeech.Contains("verb") && n.PartOfSpeech != "adverb" && n.Index > node.Index)
                        .OrderBy(n => n.Index)
                        .FirstOrDefault();

                    if (verbNode != null)
                    {
                        node.Head = verbNode;
                        node.DependencyType = "nsubj";
                        verbNode.Children.Add(node);
                    }
                }
                else if (node.PartOfSpeech == "noun" || node.PartOfSpeech == "demonstrative")
                {
                    node.Head = currentVerbRoot;
                    node.DependencyType = "nsubj";
                    currentVerbRoot.Children.Add(node);
                }
                else if (node.PartOfSpeech == "N/A")
                {
                    node.Head = currentVerbRoot;
                    node.DependencyType = "N/A";
                    currentVerbRoot.Children.Add(node);
                 
                }
            }
        }
    }

    private void IndirectObjectCheck(DependencyNode root)
    {
        var directObject = root.Children.LastOrDefault(c => c.DependencyType == "dobj");
        if (directObject == null)
        {
            return;
        }

        // just make them all iobj? i think it should be a little more nuanced, but not really possible
        //      [] i.e. "send ron smith a message", IsEntity works
        //      [] i.e. "send him a message", him is just a noun, so kind of just have to accept this

        directObject.DependencyType = "iobj";
    }

    // if there is a verb/verb phrase after a word, changes how we classify it
    private DependencyNode FindNearestVerbAfter(int index)
    {
        // get the first verb node after the current index if it exists
        return wordNodes.Values.Where(n => n.PartOfSpeech.Contains("verb") && n.PartOfSpeech != "adverb" && n.Index > index).OrderBy(n => n.Index).FirstOrDefault();
    }

    // reclassify children of a ccomp phrase when assigning them to a new ccomp:verb root
    private void ReclassifyCcompChildren(DependencyNode ccompRoot)
    {
        List<DependencyNode> reclassifyList = ccompRoot.Children.ToList();
        ccompRoot.Children.Clear();

        // we only want to reclassify the words before the ccompRoot since the ones after have yet to be classified
        foreach (var child in reclassifyList)
        {
            if (child.Index < ccompRoot.Index)
            {
                child.Head = ccompRoot;
                child.DependencyType = DetermineDependencyTypeSimplified(child, ccompRoot);
                ccompRoot.Children.Add(child);
            }
        }

    }

    // simplified version of BuildDependency as a helper for ReclassifyCcompChildren
    private string DetermineDependencyTypeSimplified(DependencyNode child, DependencyNode parent)
    {
        if (child.PartOfSpeech.Contains("noun") || child.PartOfSpeech == "pronoun")
        {
            if (child.Index < parent.Index)
            {
                return "nsubj";
            }
            else
            {
                return "dobj"; // iobj check here as well? but again ccomp is really just for chat messages so doesn't really matter
            }
        }
        else if (child.PartOfSpeech == "determiner")
        {
            return child.IsPossessive ? "poss" : "det";
        }
        else if (child.PartOfSpeech == "preposition")
        {
            return "prep";
        }
        else if (child.PartOfSpeech == "adjective")
        {
            return "amod";
        }
        else if (child.PartOfSpeech == "adverb")
        {
            return "advmod";
        }
        else
        {
            return "N/A";
        }
    }

    public void RelativeClauseGrammarPass()
    {
        // if grammarPhraseList contains any relative pronouns, mark relative clause
        // if they are being marked as an adverb like "that" --> immediate relative clause
        //      "that" is a relative clause which is describing the the preceding noun/prep phrase with the following verb and subordinate clause
        //      The GrammarManager currently marks "that" as an adverb modifying its following verb phrase, when it is actually the trigger for the relative clause, making it a relative pronoun
        //      This is why if we see a relative pronoun being marked as an adverb, we know it is actually the start of the relative clause
        //
        // if relative pronouns are not marked as adverb, dig into it to see if proper
        //      "relative pronoun" as conjunction, next word is noun --> correct
        //      "relative pronoun" as determiner, next word is noun and doesn't introduce clause --> correct
        //      "relative pronoun" as pronoun, try swapping out with "it" to see if structure changes, if yes --> relative pronoun
        //      "relative pronoun" as noun, have to attempt detaching from phrase and reidentifying phrase, if successful and clause follows --> relative pronoun

        if (!CheckForRelativePronouns())
        {
            return;
        }

        List<string> relativePronouns = new List<string> { "that", "which", "who", "whom", "whose", "where", "when", "why", "what" };

        // grammarManager only marks relativePronouns as pronouns, here we go through the grammarPhraseList and change it to an identified relative clause
        for (int i = 0; i < _grammar._grammarPhraseList.Count - 1; i++)
        {
            var currentPhrase = _grammar._grammarPhraseList[i];

            var currentPhraseIndex = 0;
            var currentPhraseHash = currentPhrase._speechHashUnits[0];
            var masterListIndex = 0;
            var wordDefiniteType = "N/A";
            var containsRelativePronoun = false;
            foreach (var hash in currentPhrase._speechHashUnits)
            {
                if (relativePronouns.Contains(_grammar._masterWordList[hash]._display))
                {
                    masterListIndex = _grammar._masterWordList[hash]._index;
                    wordDefiniteType = _grammar._masterWordList[hash]._definiteType;
                    currentPhraseHash = hash;
                    containsRelativePronoun = true;
                    break;
                }
                currentPhraseIndex += 1;
            }

            if (!containsRelativePronoun || wordDefiniteType == "conjunction")
            {
                continue;
            }

            // works if we know its a misidentified adverb, but a lot of the times its contained in another noun, prep, or verb phrase already
            if (currentPhrase._phraseType == "adverb" && _grammar._masterWordList[currentPhraseHash]._isRelative)
            {
                var wordHash = currentPhraseHash;
                var wordUnit = _grammar._masterWordList[wordHash];

                if (relativePronouns.Contains(wordUnit._display.ToLower()))
                {

                    var relativeClause = new GrammarUnit("relative clause", new List<string>());
                    relativeClause._speechHashUnits.AddRange(currentPhrase._speechHashUnits);

                    int j = i + 1;
                    while (j < _grammar._grammarPhraseList.Count)
                    {
                        var nextPhrase = _grammar._grammarPhraseList[j];

                        // adds all following phrases into relative clause
                        relativeClause._speechHashUnits.AddRange(nextPhrase._speechHashUnits);
                        _grammar._grammarPhraseList.RemoveAt(j);

                    }

                    // replace the phrase
                    _grammar._grammarPhraseList[i] = relativeClause;

                    break;
                }
            }

            // if relative pronoun is capture inside a noun/prep phrase
            if (currentPhrase._phraseType == "noun phrase" || currentPhrase._phraseType == "prepositional phrase")
            {
                // next phrase lookahead
                int j = i + 1;

                GrammarUnit potentialRelClause = null;
                int potentialRelClauseStartIndex = -1;

                while (j < _grammar._grammarPhraseList.Count)
                {
                    var nextPhrase = _grammar._grammarPhraseList[j];
                    var firstWordHash = nextPhrase._speechHashUnits.First();
                    var firstWordUnit = _grammar._masterWordList[firstWordHash];

                    if (CanIntroduceClause(firstWordUnit))
                    {
                        potentialRelClauseStartIndex = j;
                        potentialRelClause = new GrammarUnit("relative clause", new List<string>());
                        potentialRelClause._speechHashUnits.AddRange(nextPhrase._speechHashUnits);

                        // collect the rest of the phrases with that phrase to see if relatve clause
                        int k = j + 1;
                        while (k < _grammar._grammarPhraseList.Count)
                        {
                            var clausePhrase = _grammar._grammarPhraseList[k];
                            potentialRelClause._speechHashUnits.AddRange(clausePhrase._speechHashUnits);
                            k++;
                        }

                        break;
                    }
                    // relative pronouns contained in a noun/prep phrase are usually followed by a verb phrase, the noun phrase happens by failsafe since CheckForRelativePronouns isn't fully implemented yet
                    else if (nextPhrase._phraseType == "verb phrase" || nextPhrase._phraseType == "noun phrase")
                    {
                        if (PhraseContainsVerb(nextPhrase))
                        {
                            potentialRelClauseStartIndex = j;
                            potentialRelClause = new GrammarUnit("relative clause", new List<string>());
                            potentialRelClause._speechHashUnits.AddRange(nextPhrase._speechHashUnits);

                            // collect the rest of the phrases with that phrase to see if relatve clause
                            int k = j + 1;
                            while (k < _grammar._grammarPhraseList.Count)
                            {
                                var clausePhrase = _grammar._grammarPhraseList[k];
                                potentialRelClause._speechHashUnits.AddRange(clausePhrase._speechHashUnits);
                                k++;
                            }

                            if (!potentialRelClause._speechHashUnits.Contains(currentPhraseHash))
                            {
                                // does not contain itself, take out from current phrase list, add onto itself at FIRST position
                                potentialRelClause._speechHashUnits.Insert(0, currentPhraseHash);

                                _grammar._masterWordList[currentPhraseHash]._isRelative = true;
                                _grammar._masterWordList[currentPhraseHash]._definiteType = "pronoun";

                                // current phrase needs reidentification - this needs to be better, but majority of time its from a noun/prep phrase
                                if (currentPhrase._speechHashUnits.Count > 1)
                                {
                                    _grammar._masterWordList[currentPhrase._speechHashUnits[currentPhraseIndex - 1]]._definiteType = "noun";
                                }

                                currentPhrase._speechHashUnits.Remove(currentPhraseHash);

                                // this will sometimes leave an empty phrase in the list - can't delete it here because it gets rid of the reference - delete at bottom of function
                            }

                            break;
                        }
                        else
                        {
                            // not correct phrase -> next
                            j++;
                        }
                    }
                    else
                    {
                        // not correct phrase -> next
                        j++;
                    }
                }

                if (potentialRelClause != null && potentialRelClauseStartIndex != -1)
                {
                    // clean up rest of phrases attached to rel clause
                    _grammar._grammarPhraseList.RemoveRange(potentialRelClauseStartIndex, _grammar._grammarPhraseList.Count - potentialRelClauseStartIndex);

                    // insert the relative clause after the noun phrase - not proper
                    //_grammar._grammarPhraseList.Insert(i + 1, potentialRelClause);

                    // append relative clause to end of grammar list
                    _grammar._grammarPhraseList.Add(potentialRelClause);
    
                    break;
                }
            }
        }

        // if "relative clause" exists, find any indexed words from a phrase that should actually be in there - clear them, then clean up 
        GrammarUnit relativeClausePhrase = _grammar._grammarPhraseList.FirstOrDefault(phrase => phrase._phraseType == "relative clause");
        if (relativeClausePhrase != null)
        {
            int relativeClausePhraseFirstWordIndex = _grammar._masterWordList[relativeClausePhrase._speechHashUnits[0]]._index;
            foreach (var phrase in _grammar._grammarPhraseList)
            {
                if (phrase._phraseType != "relative clause" && phrase._speechHashUnits.Count > 0 && _grammar._masterWordList[phrase._speechHashUnits[0]]._index > relativeClausePhraseFirstWordIndex)
                {
                    relativeClausePhrase._speechHashUnits.InsertRange(1, phrase._speechHashUnits);
                    phrase._speechHashUnits.Clear();
                    continue;
                }
            }
        }

        // clean up any empty phrases
        List<GrammarUnit> emptyPhrases = _grammar._grammarPhraseList.Where(phrase => phrase._speechHashUnits.Count == 0).ToList();
        foreach (var empty in emptyPhrases)
        {
            _grammar._grammarPhraseList.Remove(empty);
        }

        
    }

    private bool CanIntroduceClause(SpeechUnit wordUnit)
    {
        string pos = wordUnit._definiteType;
        return pos == "pronoun" || pos == "conjunction" || pos == "determiner" || pos == "adverb";
    }

    private bool PhraseContainsVerb(GrammarUnit phrase)
    {
        foreach (var wordHash in phrase._speechHashUnits)
        {
            var wordUnit = _grammar._masterWordList[wordHash];
            if (wordUnit._definiteType == "verb")
            {
                return true;
            }
        }
        return false;
    }

    private void BuildRelativeClauseDependencies(DependencyNode root, GrammarUnit phrase)
    {
        var wordHashes = phrase._speechHashUnits;
        var relativePronounHash = wordHashes.First();
        var relativePronounNode = wordNodes[relativePronounHash];

        // find noun that the relative clause modifies
        DependencyNode modifiedNounNode = wordNodes.Values
            .Where(n => n.PartOfSpeech.Contains("noun") && n.Index < relativePronounNode.Index && n.DependencyType != "pobj")
            .OrderByDescending(n => n.Index)
            .FirstOrDefault();

        if (modifiedNounNode != null)
        {
            // attach the relative pronoun to the noun it modifies
            relativePronounNode.Head = modifiedNounNode;
            relativePronounNode.DependencyType = "relcl"; // Relative clause
            relativePronounNode.PartOfSpeech = "pronoun"; // Relative clause --> mark as proper pronoun POS
            modifiedNounNode.Children.Add(relativePronounNode);

            // process the rest of the words in the relative clause
            var remainingHashes = wordHashes.Skip(1).ToList();
            ParseRelativeClause(relativePronounNode, remainingHashes);
        }
        else
        {
            // can't make a relative clause
            Debug.LogWarning("No noun found to attach the relative clause.");
        }
    }

    private void ParseRelativeClause(DependencyNode relativePronounNode, List<string> wordHashes)
    {
        var nodes = wordHashes.Select(hash => wordNodes[hash]).ToList();
        DependencyNode mainVerbNode = nodes.FirstOrDefault(n => n.PartOfSpeech == "verb" && !n.IsAuxiliary);

        if (mainVerbNode != null)
        {
            // connecting the main verb to the relative pronoun helps in identifying subordinate clause, especially using for chat prompts
            mainVerbNode.Head = relativePronounNode;
            mainVerbNode.DependencyType = "acl:relcl";
            relativePronounNode.Children.Add(mainVerbNode);

            // extra aux verbs if necessary
            var auxVerbNodes = nodes.Where(n => n.IsAuxiliary && n.Index < mainVerbNode.Index).ToList();
            foreach (var auxVerbNode in auxVerbNodes)
            {
                auxVerbNode.Head = mainVerbNode;
                auxVerbNode.DependencyType = "aux";
                mainVerbNode.Children.Add(auxVerbNode);
            }

            // process any complement clauses ccomp under the main verb i.e. "he is flying"
            var remainingNodes = nodes.Where(n => n.Head == null && n != mainVerbNode && !auxVerbNodes.Contains(n)).ToList();
            if (remainingNodes.Any())
            {
                ParseComplementClause(mainVerbNode, remainingNodes);
            }
        }
        else
        {
            // can't make rel clause without an attaching verb
            Debug.LogWarning("No main verb found in the relative clause.");
        }
    }

    private void ParseComplementClause(DependencyNode parentVerbNode, List<DependencyNode> nodes)
    {
        // main verb in ccomp
        DependencyNode mainVerbNode = nodes.FirstOrDefault(n => n.PartOfSpeech == "verb" && !n.IsAuxiliary);

        if (mainVerbNode != null)
        {
            // connect ccomp verb to acl:relcl verb
            mainVerbNode.Head = parentVerbNode;
            mainVerbNode.DependencyType = "ccomp";
            parentVerbNode.Children.Add(mainVerbNode);

            // any extra aux verbs !!
            var auxVerbNodes = nodes.Where(n => n.IsAuxiliary).ToList();
            foreach (var auxVerbNode in auxVerbNodes)
            {
                auxVerbNode.Head = mainVerbNode;
                auxVerbNode.DependencyType = "aux";
                mainVerbNode.Children.Add(auxVerbNode);
            }

            // any following subjects in the ccomp
            var subjectNode = nodes.FirstOrDefault(n => n.PartOfSpeech.Contains("noun"));
            if (subjectNode != null && subjectNode.Head == null)
            {
                subjectNode.Head = mainVerbNode;
                subjectNode.DependencyType = "nsubj";
                mainVerbNode.Children.Add(subjectNode);
            }

            // anything else -> just an obj at this point because i don't want to get more granular
            var remainingNodes = nodes.Where(n => n.Head == null && n != mainVerbNode && !auxVerbNodes.Contains(n) && n != subjectNode).ToList();
            foreach (var node in remainingNodes)
            {
                // we don't really care about the structure of the ccomp since its just used as the chat message, but should really make more structure here

                node.Head = mainVerbNode;
                node.DependencyType = "obj";
                mainVerbNode.Children.Add(node);
            }

        }
        else
        {
            // again can't really have a clause without a verb
            Debug.LogWarning("No main verb found in the complement clause.");
        }
    }

    // didn't end up using this one - just de facto to ccomp and getting subordinate after all processed
    private void ParseSubordinateClause(DependencyNode parentVerbNode, List<DependencyNode> nodes)
    { 
        DependencyNode mainVerbNode = nodes.FirstOrDefault(n => n.PartOfSpeech == "verb" && !n.IsAuxiliary);

        if (mainVerbNode != null)
        {
            mainVerbNode.Head = parentVerbNode;
            mainVerbNode.DependencyType = "ccomp";
            parentVerbNode.Children.Add(mainVerbNode);

            var auxVerbNodes = nodes.Where(n => n.IsAuxiliary).ToList();
            foreach (var auxVerbNode in auxVerbNodes)
            {
                auxVerbNode.Head = mainVerbNode;
                auxVerbNode.DependencyType = "aux";
                mainVerbNode.Children.Add(auxVerbNode);
            }

            var subjectNode = nodes.FirstOrDefault(n => n.PartOfSpeech.Contains("noun") && n.Index < mainVerbNode.Index);
            if (subjectNode != null)
            {
                subjectNode.Head = mainVerbNode;
                subjectNode.DependencyType = "nsubj";
                mainVerbNode.Children.Add(subjectNode);
            }

            AttachNounsToVerb(mainVerbNode, nodes.Where(n => n.Head == null && n != mainVerbNode && !auxVerbNodes.Contains(n) && n != subjectNode).ToList());
        }
        else
        {
            Debug.LogWarning("No main verb found in the subordinate clause.");
        }
    }

    // also didn't end up using this one - for subordinate clauses --> ccomp for now
    private void AttachNounsToVerb(DependencyNode verbNode, List<DependencyNode> nounNodes)
    {
        foreach (var node in nounNodes)
        {
            if (node.PartOfSpeech.Contains("noun"))
            {
                if (node.Index < verbNode.Index)
                {
                    node.Head = verbNode;
                    node.DependencyType = "nsubj";
                    verbNode.Children.Add(node);
                }
                else
                {
                    node.Head = verbNode;
                    node.DependencyType = "dobj";
                    verbNode.Children.Add(node);
                }
            }
            else if (node.PartOfSpeech == "determiner")
            {
                var nounNode = wordNodes.Values
                    .FirstOrDefault(n => n.PartOfSpeech.Contains("noun") && n.Index == node.Index + 1);

                if (nounNode != null)
                {
                    node.Head = nounNode;
                    node.DependencyType = node.IsPossessive ? "poss" : "det";
                    nounNode.Children.Add(node);
                }
            }
        }
    }

    // !! depreceated !! - GetLastNestedComplementClause() covers all cases
    public string GetSubordinateClause()
    {
        // just do it from ROOT because we aren't guaranteed to have a relcl/acl:relcl, so just deal with getting a ccomp if exists
        var subordinateVerbNode = wordNodes.Values.FirstOrDefault(n => n.DependencyType == "ccomp");
        if (subordinateVerbNode == null)
        {
            Debug.Log("No ccomp clause detected - if a chat, still needs a message");
            return null;
        }

        // order words and form subordinate sentence in order
        List<DependencyNode> nodes = new List<DependencyNode>();
        CollectNodes(subordinateVerbNode, nodes);

        var orderedNodes = nodes.OrderBy(n => n.Index).ToList();
        List<string> words = orderedNodes.Select(n => n.Word).ToList();

        string subordinateClause = string.Join(" ", words);
        return subordinateClause;
    }

    // !! depreceated !! - GetLastNestedComplementClauseNodes() covers all cases
    public List<DependencyNode> GetSubordinateClauseNodes()
    {
        
        // just do it from ROOT because we aren't guaranteed to have a relcl/acl:relcl, so just deal with getting a ccomp if exists
        var subordinateVerbNode = wordNodes.Values.FirstOrDefault(n => n.DependencyType == "ccomp");
        if (subordinateVerbNode == null)
        {
            Debug.Log("No ccomp clause detected - if a chat, still needs a message");
            return null;
        }

        // order words and form subordinate sentence in order
        List<DependencyNode> nodes = new List<DependencyNode>();
        CollectNodes(subordinateVerbNode, nodes);

        List<DependencyNode> orderedNodes = nodes.OrderBy(n => n.Index).ToList();

        return orderedNodes;
    }

    public string GetLastNestedComplementClause()
    {
        // just do it from ROOT because we aren't guaranteed to have a relcl/acl:relcl, so just deal with getting a ccomp if exists
        var clauseDependencies = new List<string> { "ccomp", "acl:relcl", "relcl" };
        var deepestClause = wordNodes.Values.Where(node => clauseDependencies.Contains(node.DependencyType)).OrderByDescending(node => node.Index).FirstOrDefault();
        if (deepestClause == null)
        {
            Debug.Log("No ccomp clause detected - if a chat, still needs a message");
            return null;
        }

        // order words and form subordinate sentence in order
        List<DependencyNode> nodes = new List<DependencyNode>();
        CollectNodes(deepestClause, nodes);

        var orderedNodes = nodes.OrderBy(n => n.Index).ToList();
        List<string> words = new List<string>();
        if (deepestClause.DependencyType == "ccomp")
        {
            words = orderedNodes.Select(n => n.Word).ToList();
        }
        else
        {
            words = orderedNodes.Where(n => n.Index > deepestClause.Index).Select(n => n.Word).ToList();
        }

        // if deepest is relcl or acl:relcl - don't include as they aren't actually part of the message
        if (deepestClause.DependencyType.Contains("relcl"))
        {
            if (words.Count > 0 && words[0] == deepestClause.Word)
            {
                words.RemoveAt(0);
            }    
        }

        string subordinateClause = string.Join(" ", words);
        return subordinateClause;
    }

    public List<DependencyNode> GetLastNestedComplementClauseNodes()
    {
        // just do it from ROOT because we aren't guaranteed to have a relcl/acl:relcl, so just deal with getting a ccomp if exists
        var clauseDependencies = new List<string> { "ccomp", "acl:relcl", "relcl" };
        var deepestClause = wordNodes.Values.Where(node => clauseDependencies.Contains(node.DependencyType)).OrderByDescending(node => node.Index).FirstOrDefault();
        if (deepestClause == null)
        {
            Debug.Log("No ccomp clause detected - if a chat, still needs a message");
            return null;
        }

        // order words and form subordinate sentence in order
        List<DependencyNode> nodes = new List<DependencyNode>();
        CollectNodes(deepestClause, nodes);

        List<DependencyNode> orderedNodes = nodes.OrderBy(n => n.Index).ToList();

        // if deepest is relcl or acl:relcl - don't include as they aren't actually part of the message
        if (deepestClause.DependencyType.Contains("relcl"))
        {
            orderedNodes.Remove(deepestClause);
        }

        if (deepestClause.DependencyType == "ccomp")
        {
            return orderedNodes;
        }
        else
        {
            List<DependencyNode> justClauseNode = orderedNodes.Where(n => n.Index > deepestClause.Index).ToList();
            return justClauseNode;
        }
    }


    private DependencyNode FindSubordinateClauseNode(DependencyNode relativeClauseNode)
    {
        // recursive travel to find subordinate clause -> ccomp
        foreach (var child in relativeClauseNode.Children)
        {
            if (child.DependencyType == "ccomp")
            {
                return child;
            }
            else
            {
                var result = FindSubordinateClauseNode(child);
                if (result != null)
                    return result;
            }
        }
        return null;
    }

    // helper to gather dependency nodes together connected to a certain word
    private void CollectNodes(DependencyNode node, List<DependencyNode> nodes, HashSet<DependencyNode> visited = null)
    {
        if (node == null)
            return;

        // ensuring a unique visited set to avoid infinite loops
        if (visited == null)
            visited = new HashSet<DependencyNode>();

        if (visited.Contains(node))
            return;

        visited.Add(node);
        nodes.Add(node);

        foreach (var child in node.Children)
        {
            CollectNodes(child, nodes, visited);
        }
    }

    private bool IsNodeInRelativeOrComplementClause(DependencyNode node)
    {
        return node.DependencyType == "relcl" || node.DependencyType == "acl:relcl" || node.DependencyType == "ccomp" || IsAncestorInClause(node);
    }

    private bool IsNodeInRelativeClause(DependencyNode node)
    {
        // check if hash falls into rel cl
        return _grammar._grammarPhraseList.Any(phrase => phrase._phraseType == "relative clause" && phrase._speechHashUnits.Contains(node.Hash));
    }

    private bool IsAncestorInClause(DependencyNode node)
    {
        // recursive travel to parent rel clause if exists

        if (node.Head == null)
        {
            return false;
        }
        if (node.Head.DependencyType == "relcl" || node.Head.DependencyType == "acl:relcl" || node.Head.DependencyType == "ccomp")
        {
            return true;

        }
        return IsAncestorInClause(node.Head);
    }

    // for BuildDependencies() if currentPhrase._phraseType == "preposition phrase"
    private void AttachPrepositionalObject(DependencyNode prepositionNode, List<string> wordHashes)
    {
        var nodes = wordHashes.Select(hash => wordNodes[hash]).ToList();
        var objectNode = nodes.FirstOrDefault(n => n.PartOfSpeech.Contains("noun"));

        if (objectNode != null)
        {
            // prep obj --> pobj
            objectNode.Head = prepositionNode;
            objectNode.DependencyType = "pobj";
            prepositionNode.Children.Add(objectNode);

            // extra words in prep phrase
            foreach (var node in nodes)
            {
                if (node != objectNode)
                {
                    if (node.PartOfSpeech == "determiner")
                    {
                        node.Head = objectNode;
                        node.DependencyType = node.IsPossessive ? "poss" : "det";
                        objectNode.Children.Add(node);
                    }
                    else if (node.PartOfSpeech == "adjective")
                    {
                        node.Head = objectNode;
                        node.DependencyType = "amod";
                        objectNode.Children.Add(node);
                    }
                    else if (node.IsNumeral || node.PartOfSpeech == "numeral")
                    {
                        node.Head = objectNode;
                        node.DependencyType = "nummod";
                        objectNode.Children.Add(node);
                    }
                    else if (node.IsDateTimeUnit)
                    {
                        node.Head = objectNode;
                        node.DependencyType = "dtmod";
                        objectNode.Children.Add(node);
                    }
                    else
                    {
                        node.Head = objectNode;
                        node.DependencyType = "dep";
                        objectNode.Children.Add(node);
                    }
                }
            }
        }
        else
        {
            // if no noun, connect words to prep directly
            foreach (var node in nodes)
            {
                node.Head = prepositionNode;
                node.DependencyType = "pobj";
                prepositionNode.Children.Add(node);
            }
        }
    }

    // also for BuildDependencies() if currentPhrase._phraseType == "preposition phrase"
    private DependencyNode FindPrepositionHead(DependencyNode prepositionNode, DependencyNode root)
    {
        // sometimes even if there is a directObj after the verb, the prep still needs to be attached to the verb
        if (IsPrepositionModifyingVerbAfterDirectObject(prepositionNode, root))
        {
            return root;
        }

        // if preposition immediately follows verb or in a ccomp, attach to verb
        if (root.Index + 1 == prepositionNode.Index)
        {
            return root;
        }


        // look for the closest noun before the prep that is not part of a different prep object
        var potentialHeads = wordNodes.Values
            .Where(n => n.PartOfSpeech.Contains("noun") && n.Index < prepositionNode.Index && n.Index > root.Index)
            .OrderByDescending(n => n.Index);

        foreach (var nounNode in potentialHeads)
        {
            if (nounNode.DependencyType != "pobj")
            {
                return nounNode;
            }
        }

        // again if no noun found, connect to the direct object of the root verb
        var directObjectFallback = root.Children.FirstOrDefault(c => c.DependencyType == "dobj");
        if (directObjectFallback != null)
        {
            return directObjectFallback;
        }

        // all else just attach to root verb
        return root;
    }

    private bool IsPrepositionModifyingVerbAfterDirectObject(DependencyNode prepositionNode, DependencyNode root)
    {
        var directObject = root.Children.FirstOrDefault(c => c.DependencyType == "dobj");
        if (directObject == null)
        {
            // check if directObject exists, if not, --> false for now, it still could attach to verb but rest of the FindPrepositionHead() checks those conditions
            return false;
        }

        var phrasalVerb = root.Word + " " + prepositionNode.Word;
        if (prepositionNode.Word == "to" || phrasalVerbs.Contains(phrasalVerb.ToLower()))
        {
            if (directObject != null && prepositionNode.Index > directObject.Index)
            {
                return true;
            }
        }

        return false;
    }

    private bool CheckForRelativePronouns()
    {
        // 1) if it isn't a relative pronoun, if we replace it with "it", the structure shouldn't change
        //      [] so it is a relative pronoun if the grammar changes -> return true
        // 2) for relative pronouns stuck in noun/prep phrases
        //      [] see if it is the last word of a noun/prep phrase
        //      [] see if the previous word has a definiteType or possible POS of a noun
        //      [] see if following word/phrase is a verb --> acl:relcl
        //          [] else see if rest of sentence has a noun and verb phrase -> subordinate clause
        //      [] all true -> return true

        List<string> relativePronouns = new List<string> { "that", "which", "who", "whom", "whose", "where", "when", "why", "what" };

        string replaced_sentence = "";
        bool foundRelativePronoun = false;

        foreach (var speechUnit in _grammar._masterWordList)
        {
            if (relativePronouns.Contains(speechUnit.Value._display) && speechUnit.Value._definiteType != "conjunction")
            {
                foundRelativePronoun = true;
                replaced_sentence += "it ";
            }
            else
            {
                replaced_sentence += speechUnit.Value._display + " ";
            }
        }

        if (!foundRelativePronoun)
        {
            return false;
        }

        replaced_sentence = replaced_sentence.Trim();
        GrammarManager it_grammar = new GrammarManager(replaced_sentence);

        // if grammarList sizes different -> relative pronoun
        if (_grammar._grammarPhraseList.Count != it_grammar._grammarPhraseList.Count)
        {
            return true;
        }

        // testing same phrase structure, if different -> relative pronoun
        for (int i = 0; i < _grammar._grammarPhraseList.Count; i++)
        {
            if (_grammar._grammarPhraseList[i]._phraseType != it_grammar._grammarPhraseList[i]._phraseType)
            {
                return true;
            }
        }

        // testing same definite type structure, if different -> relative pronoun
        for (int i = 0; i < _grammar._masterWordList.Count; i++)
        {
            if (_grammar._masterWordList.ElementAt(i).Value._definiteType != it_grammar._masterWordList.ElementAt(i).Value._definiteType)
            {
                return true;
            }
        }

        // above works when relative pronoun isn't misidentified

        // 2) dealing with misidentified noun/prep phrases
        for (int i = 0; i < _grammar._grammarPhraseList.Count - 1; i++)
        {
            var currentPhrase = _grammar._grammarPhraseList[i];

            if (currentPhrase._phraseType == "noun phrase" || currentPhrase._phraseType == "prepositional phrase")
            {
                // check to see if phrase contains a relative pronoun and get its key
                //var matchingRelativePronounKey = currentPhrase._speechHashUnits.FirstOrDefault(key => relativePronouns.Contains(_grammar._masterWordList[key]._display.ToLower()));
                var matchingRelativePronounKey = currentPhrase._speechHashUnits
                    .Select((key, index) => new { Key = key, Index = index })
                    .FirstOrDefault(pair => relativePronouns.Contains(_grammar._masterWordList[pair.Key]._display.ToLower()));

                // if key is null or if the found word is not the last word of the phrase, it has to be the last word for this method to work
                if (matchingRelativePronounKey == null || matchingRelativePronounKey.Index != currentPhrase._speechHashUnits.Count - 1)
                {
                    continue;
                }

                // now we know we have a possible relative pronoun marked as a noun at the end of a phrase

                // if last phrase of the grammar list -> impossible to start a relative clause, has to actually be third to last technically because relcl needs a verb and a noun
                if (i >= _grammar._grammarPhraseList.Count - 2)
                {
                    break;
                }

                // then previous word needs to be either a definiteType noun or POS noun
                // if next word is a verb -> acl:relcl -> return true
                // if next word is a noun, investigate further
                //      [] continue to check if there is a clause (subject, verb) present, if yes -> relcl -> return true, else false

                // word before possible rel pronoun && its possible POS include noun
                if (currentPhrase._speechHashUnits.Count > 1 && _grammar._masterWordList[currentPhrase._speechHashUnits[matchingRelativePronounKey.Index-1]]._partsOfSpeech.Contains("noun"))
                {
                    var relativePronounMasterWordListIndex = _grammar._masterWordList[matchingRelativePronounKey.Key]._index;
                    if (_grammar._masterWordList.ElementAt(relativePronounMasterWordListIndex + 1).Value._definiteType == "verb" ||
                        (_grammar._masterWordList.ElementAt(relativePronounMasterWordListIndex + 1).Value._definiteType == "adverb" && _grammar._masterWordList.ElementAt(relativePronounMasterWordListIndex + 2).Value._definiteType == "verb"))
                    {
                        return true; // i.e. "that says... (sub cl)", "which says ... (sub cl)"
                    }
                    else
                    {
                        // check if a possible clause is present in the rest of the sentence
                        bool containsNounPhrase = false;
                        bool containsVerbPhrase = false;
                        for (int j = i+1; j < _grammar._grammarPhraseList.Count; j++)
                        {
                            if (_grammar._grammarPhraseList[j]._phraseType == "noun phrase")
                            {
                                containsNounPhrase = true;
                            }
                            else if (_grammar._grammarPhraseList[j]._phraseType == "verb phrase")
                            {
                                containsVerbPhrase = true;
                            }
                        }

                        if (containsNounPhrase && containsVerbPhrase)
                        {
                            return true;
                        }
                    }
                }


            }

        }


        return false;
    }

    public List<string> GetPhrasalVerbsFromJson()
    {
        TextAsset jsonText = Resources.Load<TextAsset>("phrasal_verbs_preposition");
        string jsonContent = jsonText.text;

        var fileInput = JsonConvert.DeserializeObject<Dictionary<string, double>>(jsonContent);

        List<string> keys = new List<string>();
        foreach (var item in fileInput)
        {
            keys.Add(item.Key);
        }    

        return keys;
    }

}
