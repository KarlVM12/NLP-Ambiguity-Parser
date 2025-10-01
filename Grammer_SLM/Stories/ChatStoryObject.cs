using System.Collections.Generic;
using System.Data;
using System.Data.SqlTypes;
using System.Text.RegularExpressions;
using EnhancedScrollerDemos.MainMenu;
using Grammar;
using Unity.VisualScripting.Dependencies.NCalc;
using UnityEngine;
using System.Linq;
using Newtonsoft.Json;
using Unity.VisualScripting;

public class ChatStoryObject : BaseStoryObject
{
    


    private List<SpeechUnit> _recipients;



    private List<SpeechUnit> _messageBody = new List<SpeechUnit>();

    private List<SpeechUnit> _subjectTopic = new List<SpeechUnit>();

    private ResponseManager _responseManager;

    private SpeechUnit _mainVerb;

    private string _messageText;

    private bool _hasMessageBody = true;
    public List<string> verbResponseList = new List<string>();

    
    public ChatStoryObject(SpeechUnit mainVerb,  List<SpeechUnit> recipients, string messageText, int messageBodyStartIndex, int messageBodyEndIndex, List<GrammarPhraseObject> objectPhraseList, GrammarPhraseObject subjectTopic)
    {
        _PromptConfirm = true;

        _messageText = messageText;
        _mainVerb = mainVerb;

        Debug.Log("Chat Object:" + messageText);
        
        _recipients = recipients;
        if (_recipients.Count == 0)
        {
            _missingComponents.Add("recipients");
        }

        if (messageBodyStartIndex == -1)
        {
            _hasMessageBody = false;
            _missingComponents.Add("message body");
        }
        else
        {
            if (messageBodyStartIndex == messageBodyEndIndex)
            {
                foreach (SpeechUnit speechUnit in objectPhraseList[messageBodyEndIndex]._speechUnits)
                {
                    _messageBody.Add(speechUnit);
                }
            }
            else
            {
                for (int i = messageBodyStartIndex; i <= messageBodyEndIndex; i++)
                {
                    foreach (SpeechUnit speechUnit in objectPhraseList[i]._speechUnits)
                    {
                        _messageBody.Add(speechUnit);
                    }
                    
                    if (i == messageBodyEndIndex) break;
                }
            }
        }

        if (subjectTopic != null)
        {
            foreach (SpeechUnit speechUnit in subjectTopic._speechUnits)
            {
                if (speechUnit._definiteType != "preposition")
                {
                    _subjectTopic.Add(speechUnit);
                }
            }
        }
        else
        {
            if (_recipients.Count > 0)
            {
                if (_recipients.Count > 1)
                {
                    _missingComponents.Add("subject topic");
                }
            }
        }

        _captainResponse = GetCaptainResponse();
    }

    private string GetCaptainResponse()
    {
        if (_hasMessageBody)
        {
            if (_recipients.Count == 0)
            {
                return EmptyRecipientsResponseQueue();
            }

            if (_recipients.Count == 1)
            {
                return CompleteMessageResponseQueue();
            }

            if (_subjectTopic.Count == 0)
            {
                return SubjectResponseQueue();
            }

            return CompleteMessageResponseQueue();
        }
        else
        {
            if (_recipients.Count == 0)
            {
                return EmptyRecipientsAndMessageBodyResponseQueue();
            }
            
            return EmptyMessageResponseQueue();
        }
    }

    private string EmptyMessageResponseQueue()
    {
        bool hasMessageText = !string.IsNullOrWhiteSpace(_messageText);

        if (_recipients.Count == 1)
        {
            if (!hasMessageText)
            {
                _responseManager = new ResponseManager("chat", "emptyMessage", "set1");
                _responseManager.Set("recipient", _recipients[0]._display);
            }
            else
            {
                _responseManager = new ResponseManager("chat", "emptyMessage", "set2");
                _responseManager.Set("recipient", _recipients[0]._display);
                _responseManager.Set("messageText", _messageText);
            }
        }

        if (_recipients.Count == 2)
        {
            string recipients = _recipients[0]._display + " and " + _recipients[1]._display;

            _responseManager = new ResponseManager("chat", "emptyMessage", "set3");
            _responseManager.Set("recipients", recipients);
        }

        if (_recipients.Count > 2)
        {
            string recipients = "";
            int secondToLastRecipientIndex = _recipients.Count - 2;
            int lastRecipient = _recipients.Count - 1;

            for (int i = 0; i < _recipients.Count; i++)
            {
                SpeechUnit recipient = _recipients[i];

                if (i == secondToLastRecipientIndex)
                {
                    recipients += recipient._display + ", and ";
                    
                    continue;
                }

                if (i == lastRecipient)
                {
                    recipients += recipient._display;
                    
                    continue;
                }

                recipients += recipient._display + ", ";
            }
            
            _responseManager = new ResponseManager("chat", "emptyMessage", "set4");
            _responseManager.Set("recipients", recipients);
        }
        
        return _responseManager.Output();
    }

    private string EmptyRecipientsResponseQueue()
    {
        bool hasMessageText = !string.IsNullOrWhiteSpace(_messageText);
        string theMessage = "";
        
        if (!hasMessageText)
        {
            foreach (SpeechUnit message in _messageBody)
            {
                theMessage += message._display.ToLower() + " ";
            }
            
            theMessage = theMessage.TrimEnd(' ');

            _responseManager = new ResponseManager("chat", "emptyRecipients", "set1");
            _responseManager.Set("theMessage", theMessage);
        }
        else
        {
            foreach (SpeechUnit message in _messageBody)
            {
                theMessage += message._display.ToLower() + " ";
            }
            
            theMessage = theMessage.TrimEnd(' ');

            _responseManager = new ResponseManager("chat", "emptyRecipients", "set2");
            _responseManager.Set("display", _mainVerb._display);
            _responseManager.Set("messageText", _messageText);
            _responseManager.Set("theMessage", theMessage);
        }

        return _responseManager.Output();
    }

    private string EmptyRecipientsAndMessageBodyResponseQueue()
    {
        bool hasMessageText = !string.IsNullOrWhiteSpace(_messageText);

        if (!hasMessageText)
        {
            _responseManager = new ResponseManager("chat", "emptyRecipientsAndMessageBody", "set1");
        }
        else
        {
            _responseManager = new ResponseManager("chat", "emptyRecipientsAndMessageBody", "set2");
            _responseManager.Set("messageText", _messageText);
        }

        return _responseManager.Output();
    }

    private string CompleteMessageResponseQueue()
    {
        bool hasMessageText = !string.IsNullOrWhiteSpace(_messageText);
        string theMessage = "";

        foreach (SpeechUnit message in _messageBody)
        {
            theMessage += message._display.ToLower() + " ";
        }
        
        theMessage = theMessage.TrimEnd(' ');

        if (_recipients.Count == 1)
        {
            if (!hasMessageText)
            {
                _responseManager = new ResponseManager("chat", "completeMessage", "set1");
                _responseManager.Set("theMessage", theMessage);
                _responseManager.Set("recipient", _recipients[0]._display);
            }
            else
            {
                _responseManager = new ResponseManager("chat", "completeMessage", "set2");
                _responseManager.Set("theMessage", theMessage);
                _responseManager.Set("recipient", _recipients[0]._display);
                _responseManager.Set("messageText", _messageText);
            }
        }

        if (_recipients.Count == 2)
        {
            string subjectString = "", userString = _recipients[0]._display + " and " + _recipients[1]._display;

            foreach (SpeechUnit subject in _subjectTopic)
            {
                subjectString += subject._display + " ";
            }
            
            subjectString = subjectString.TrimEnd(' ');

            _responseManager = new ResponseManager("chat", "completeMessage", "set3");
            _responseManager.Set("userString", userString);
            _responseManager.Set("theMessage", theMessage);
            _responseManager.Set("subjectString", subjectString);
        }

        if (_recipients.Count > 2)
        {
            string subjectString = "", userString = "";

            foreach (SpeechUnit subject in _subjectTopic)
            {
                subjectString += subject._display + " ";
            }
            
            subjectString = subjectString.TrimEnd(' ');
            
            int secondToLastRecipientIndex = _recipients.Count - 2;
            int lastRecipient = _recipients.Count - 1;

            for (int i = 0; i < _recipients.Count; i++)
            {
                SpeechUnit recipient = _recipients[i];

                if (i == secondToLastRecipientIndex)
                {
                    userString += recipient._display + ", and ";
                    
                    continue;
                }

                if (i == lastRecipient)
                {
                    userString += recipient._display;
                    
                    continue;
                }

                userString += recipient._display + ", ";
            }

            _responseManager = new ResponseManager("chat", "completeMessage", "set4");
            _responseManager.Set("theMessage", theMessage);
            _responseManager.Set("userString", userString);
            _responseManager.Set("subjectString", subjectString);
        }
        
        return _responseManager.Output();
    }

    private string SubjectResponseQueue()
    {
        _responseManager = new ResponseManager("chat", "subject", "set1");

        return _responseManager.Output();
    }

    
    public override void UpdatePrompt(string prompt, GrammarManager grammarManager)
    {
        bool hasNoRecipients = false;
        bool hasNoMessageBody = false;
        bool hasNoSubject = false;
        int numberOfRecipients = 0;

        foreach (string missingComponent in _missingComponents)
        {
            switch (missingComponent)
            {
                case "recipients":
                    hasNoRecipients = true;
                    break;
                case "message body":
                    hasNoMessageBody = true;
                    break;
                case "subject topic":
                    hasNoSubject = true;
                    break;
            }
        }
        Debug.Log("COMPONENTS: " + _missingComponents);

        if (!hasNoRecipients){
            numberOfRecipients = _recipients.Count;
        }

        if (hasNoRecipients)
        {
           fillRecipients(grammarManager);
           return;
        }

        if (hasNoMessageBody)
        {
            fillMessageBody(grammarManager);
            return;
        }

        if (hasNoSubject && !hasNoMessageBody && !hasNoRecipients)
        {
            if (numberOfRecipients > 1)
            {
                fillSubject(grammarManager);
                return;
            }

        }

        //step 1 run new prompt via manager....
        //step 2 run locally on re populate actions here ....
        
        //TODO -- Mistake in grammar manager / unknown process somewhere.. 
        // "send ron smith a message about his flight that says he is flying" or something like that..
        
        // has ambiguity app side but not server side

        //UPDATE ---> error is in bigram / phrase fragment / unknown process in app side. works on server, not on app.


        _captainResponse = GetCaptainResponse();
        base.UpdatePrompt(prompt, grammarManager);
    }

    public void fillRecipients(GrammarManager grammar){
        List<SpeechUnit> recipients = new List<SpeechUnit>();

        //Debug.Log(JsonConvert.SerializeObject(grammar._sentenceObject));
        //Debug.Log(JsonConvert.SerializeObject(grammar._masterWordList));

        if (grammar._sentenceObject == null)
        {
            if (grammar._masterWordList.Count == 1)
            {
                string speechUnitKey = grammar._masterWordList.Keys.FirstOrDefault();

                if (speechUnitKey != null)
                {
                    if (grammar._masterWordList[speechUnitKey]._type == "entity")
                    {
                        recipients.Add(grammar._masterWordList[speechUnitKey]);
                    }
                }
            }
        } else {
            foreach (GrammarPhraseObject objectPhrase in grammar._sentenceObject._objectPhraseList)
        {
            if (objectPhrase.dataType == GrammarPhraseObject.DataType.Noun)
            {
                NounObject phraseObject = (NounObject)objectPhrase;
                if (phraseObject._isEntity){
                    recipients.Add(phraseObject._mainNounUnit);
                }

            } else if (objectPhrase.dataType == GrammarPhraseObject.DataType.Unknown)
            {
                UnknownObject phraseObject = (UnknownObject)objectPhrase;
                if (phraseObject._isEntity){
                    foreach (SpeechUnit speechUnit in phraseObject._speechUnits)
                    {
                        if (speechUnit._type == "entity")
                        {
                            recipients.Add(speechUnit);
                        }
                    }
                }
            }

        }
        }
        

        //Debug.Log("Recipient Count: " + recipients.Count);
        if (recipients.Count != 0)
        {
            //Debug.Log("Recipients: " + JsonConvert.SerializeObject(recipients));
             _recipients = recipients;
            _missingComponents.Remove("recipients");
            
            if (_subjectTopic.Count != 0)
            {
                _missingComponents.Add("subject topic");
            }

        }
            _captainResponse = GetCaptainResponse();

    }

    public void fillMessageBody(GrammarManager grammar){

        string storyId = GrammarData.GetStoryId("chat");

        List<string> subjectIndicators = GrammarData.GetValuesForStoryCategory(storyId, "subject_indicator");
        List<string> chatVerbWords = GrammarData.GetValuesForStoryCategory(storyId, "action_verb");

        ChatStory chatStory = new ChatStory(grammar._sentenceObject);
        BaseStoryObject TheStoryObject = chatStory.FillStory(); 

        int returnIndex = checkForIndicatorsInObjectPhraseList(grammar._sentenceObject, subjectIndicators, chatVerbWords);

        if (returnIndex == -1){
            foreach (KeyValuePair<string, SpeechUnit> speechUnit in grammar._masterWordList)
            {
                _messageBody.Add(speechUnit.Value); 
            }
            
        } else {
            int messageBodyStartIndex = returnIndex;
            int messageBodyEndIndex = grammar._sentenceObject._objectPhraseList.Count - 1;

            if (messageBodyStartIndex == messageBodyEndIndex){
                foreach (SpeechUnit speechUnit in grammar._sentenceObject._objectPhraseList[messageBodyEndIndex]._speechUnits)
                {
                    _messageBody.Add(speechUnit);
                }
            } else {
                for (int i = messageBodyStartIndex; i <= messageBodyEndIndex; i++)
                {
                    foreach (SpeechUnit speechUnit in grammar._sentenceObject._objectPhraseList[i]._speechUnits)
                    {
                        _messageBody.Add(speechUnit);
                    }
                    
                    if (i == messageBodyEndIndex) break;
                }
            }
        }

        _missingComponents.Remove("message body");
        _hasMessageBody = true;
        Debug.Log(JsonConvert.SerializeObject(_messageBody));
        _captainResponse = GetCaptainResponse();

    }
    public void fillSubject(GrammarManager grammar){
        _subjectTopic = grammar._masterWordList.Values.ToList();
        _missingComponents.Remove("subject topic");
        _captainResponse = GetCaptainResponse();
    }

    public int checkForIndicatorsInObjectPhraseList(Sentence sentence, List<string> indicators, List<string> chatVerbWords){
        int returnIndex = 0;
        int nonVerbCount = 0;
        int verbCount = 0;

        foreach (GrammarPhraseObject grammarPhrase in sentence._objectPhraseList)
        {
            foreach (SpeechUnit speechUnit in grammarPhrase._speechUnits)
            {
                if (nonVerbCount > 2){
                    continue;
                }
                foreach (string indicator in indicators)
                {
                    if (speechUnit._display == indicator)
                    {
                        return speechUnit._index + 1;
                    }
                }
                nonVerbCount++;
            }

            foreach (SpeechUnit speechUnit in grammarPhrase._speechUnits)
            {
                if (verbCount > 1){
                    continue;
                }

                foreach (string chatVerbWord in chatVerbWords)
                {
                    if (speechUnit._display == chatVerbWord)
                    {
                        returnIndex = 1;
                    }
                }
                verbCount++;
            }
        }

        return returnIndex;
    }
    
}