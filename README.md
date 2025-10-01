# Ambiguity Parsing
This contains a project surrounding a custom Grammar based Small Language Model with the goal or reducing ambiguity and increasing context given increased proper syntactic connections. 
Part of AI known as CAPTAIN (Comprehensive Aviation Platform Technical Artificial Intelligence Network), a Unity App for Pilots & Scheduling Ops for private jet companies (Charter 135 & 91) 

The method used here were strict grammar rules with a custom POS Bigram Model for examining & determining POS frequency, custom Depedency Parser, custom Intent Classifier (based on intents specified toward CAPTAIN's capabilities), custom Semantic Mapping to map full syntactically and intent classified prompts to extract exact and critical data from (e.g. recipients, locations, times, etc), 

Example:
```
        // Customer GrammarManager -> Dependency Parsing to find subordinate class within the message
        // Input: "send a message to ron smith about his flight that says he is flying";
        // Expected tree:
        //      Root Main Verb: "send"
        //      Subject: implied you ("captain ai")
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
        // Output:
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
```
From here, we were able to use our Intent Classifier and Semantic Mapping to pull out the key context of this sentence like the main verb intent to 'send' a 'message' and entity/recipient 'Ron Smith'<br>
Used this intent to filter the 'story' of the prompt -> we have a message intent and recipient with a subordinate clause containing a message -> classify 'CHAT' story 
-> CAPTAIN AI knows that means to send a message to Ron Smith that says "he is flying in flight <FLIGHT_NUMBER>". The CAPTAIN also knows when it has an entities and a query about a flight, it grabs that info -> <FLIGHT_NUMBER> will be filled in by CAPTAIN after querying database.<br>

**All this is done with just a Grammar based language model, no transformers, super fast, lightweight, can run on device** <br>
This helps greatly in increase true solid context, not based on probability, reducing ambiguity and achieving exactly as the user intended while being able to break down any prompt.

