using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;

public class CaptainConversationSequencer 
{
    private ICaptainSequencerInterface _delegate;

    public ICaptainSequencerInterface Delegate { get { return _delegate; } set { _delegate = value; } }

    List<string> UserPrompts;
    List<string> CaptainPrompts;
    string LatestUserPrompt;
    string LatestCaptainPrompt;
    int TextIndex = 0;

    bool ConfirmedPrompt = false;


    bool waitingOnConfirmActionPrompt;

    string StoryType;
    string StoryObjectResult;
    BaseStoryObject TheStoryObject;

    public string CurrentCaptainPrompt
    {
        get
        {
            return LatestCaptainPrompt;
        }

    }




    public CaptainConversationSequencer(ICaptainSequencerInterface linker)
    {
        _delegate = linker;
        ResetConversation();
    }

    public void ResetConversation()
    {
        UserPrompts = new List<string>();
        CaptainPrompts = new List<string>();
        LatestUserPrompt = "";
        LatestCaptainPrompt = "";
        StoryType = "";
        StoryObjectResult = "";
        waitingOnConfirmActionPrompt = false;
        TextIndex = 0;
        TheStoryObject = null;
        ConfirmedPrompt = false;
    }

    public static async Task<string> SendOpenAIRequest(string apiKey, string systemPrompt, object[] messages, object response_format)
    {
        using (var client = new HttpClient())
        {
            var requestBody = new
            {
                model = "gpt-4o-mini",
                instructions = systemPrompt,
                input = messages,
                text = response_format,
                tool_choice = "none",
                temperature = 1,
                max_output_tokens = 2048,
                top_p = 1,
                stream = false,
                store = false
            };

            string json = JsonConvert.SerializeObject(requestBody);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            client.DefaultRequestHeaders.Add("OpenAI-Beta", "assistants=v2");

            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await client.PostAsync("https://api.openai.com/v1/responses", content);

            string result = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                UnityEngine.Debug.LogError("OpenAI Error: " + result);
            }

            response.EnsureSuccessStatusCode();
            return result;
        }
    }




    public async void ProcessUserPrompt(string newPrompt)
    {
        LatestUserPrompt = newPrompt;
        UserPrompts.Add(newPrompt);


        //here is all the prompts we have in order for this conversation....
        Debug.Log("USER PROMPTS");
        Debug.Log(JsonConvert.SerializeObject(UserPrompts));


        // Send prompt off to OpenAI
        string apiKey = "";
        string systemPrompt = @"You are the prompt picker. Your job is to determine which classifier a prompt belong to. The purpose of classification is to streamline software for airlines and pilots, so take meanings surrounding airlines into account. The four categories and their meaning are:
                                    Navigate: prompt asking to go somewhere within the software or application. For example: 'Bring me to the home page'
                                    Query: A prompt that would need to return some form of data from the database and existing knowledge to the user upon request. For example: 'What time do I work this week?', 'Who is on the schedule tomorrow?', 'What aircrafts are available right now?' 
                                    Schedule: A type of prompt that deals with scheduling for pilots and maintenance. This is for a different functionality in the app where we can make new schedules for pilots, aircrafts, trips, or bookings. It is different from a Query involving scheduling as it doesn't involve past data, it would be making some completely new. For example: 'Schedule <EMPLOYEE> for the booking <BOOKING_NUMBER> tomorrow' or 'Create a booking from <START_AIRPORT> to <END_AIRPORT> for <DATE>'
                                    Message: A prompt requesting to send others a message. For example: 'Send Taylor a message'";

        // need to expand UserPrompts list onto messages
        var messages = new object[]
        {
            new { role = "system", content = systemPrompt},
            new { role = "user", content = @" Using the below categories, determine which category the following prompt belongs in:
                                    Navigate: prompt asking to go somewhere within the software or application. For example: 'Bring me to the home page'
                                    Query: A prompt that would need to return some form of data from the database and existing knowledge to the user upon request. For example: 'What time do I work this week?', 'Who is on the schedule tomorrow?', 'What aircrafts are available right now?' 
                                    Schedule: A type of prompt that deals with scheduling for pilots and maintenance. This is for a different functionality in the app where we can make new schedules for pilots, aircrafts, trips, or bookings. It is different from a Query involving scheduling as it doesn't involve past data, it would be making some completely new. For example: 'Schedule <EMPLOYEE> for the booking <BOOKING_NUMBER> tomorrow' or 'Create a booking from <START_AIRPORT> to <END_AIRPORT> for <DATE>'
                                    Message: A prompt requesting to send others a message. For example: 'Send Taylor a message'

                                    Prompt: " + newPrompt}
        };

        var response_format = new
        {
            format = new
            {
                type = "json_schema",
                name = "prompt_classifier",
                strict = true,
                schema = new
                {
                    type = "object",
                    properties = new
                    {
                        prompt_picker = new
                        {
                            type = "string",
                            description = @"Prompts can be one of 4 main categories: Navigation, Query, Schedule, or Message 

Navigate: prompt asking to go somewhere within the software or application. For example: 'Bring me to the home page'
Query: A prompt that would need to return some form of data from the database and existing knowledge to the user upon request. For example: 'What time do I work this week?', 'Who is on the schedule tomorrow?', 'What aircrafts are available right now?' 
Schedule: A type of prompt that deals with scheduling for pilots and maintenance. This is for a different functionality in the app where we can make new schedules for pilots, aircrafts, trips, or bookings. It is different from a Query involving scheduling as it doesn't involve past data, it would be making some completely new. For example: 'Schedule <EMPLOYEE> for the booking <BOOKING_NUMBER> tomorrow' or 'Create a booking from <START_AIRPORT> to <END_AIRPORT> for <DATE>'
Message: A prompt requesting to send others a message. For example: 'Send Taylor a message'
",
                            @enum = new[] { "Schedule", "Navigate", "Query", "Message" }
                        }
                    },
                    required = new[] { "prompt_picker" },
                    additionalProperties = false
                }
            }
        };

        string openAIResponse = await SendOpenAIRequest(apiKey, systemPrompt, messages, response_format);

        var storyChoiceJson = JObject.Parse(openAIResponse);
        Debug.Log("OPENAI: " + openAIResponse);
        string storyChoice = JObject.Parse(storyChoiceJson["output"]?[0]?["content"]?[0]?["text"].ToString())["prompt_picker"]?.ToString();

        // Now that we have the story, we can now run it through another model that can ensure that story is filled out.
        // if that story is filled out, can technically go back to old story process, or can continue with another agent down that path

        var captainResponse = "";
        switch (storyChoice)
        {
            case "Message":
                // need receiver(s) and message
                systemPrompt = @"You are the Message clarifier. Your job is to determine if enough information has been gathered to send a message. All you need to send a message is a receiver and a message. For example: 'Send <RECEIVER> a message that says <MESSAGE>";

                var response_format_message = new
                {
                    format = new
                    {
                        type = "json_schema",
                        name = "message_clarifier",
                        strict = true,
                        schema = new
                        {
                            type = "object",
                            properties = new
                            {
                                message_receiver = new
                                {
                                    type = "string",
                                    description = @"The receiver(s) of the message. Multiple receiver should be comma delimited. For example: John, Joe, Bob",
                                },
                                message_to_send = new
                                {
                                    type = "string",
                                    description = @"The message to be sent"
                                }
                            },
                            required = new[] { "message_receiver", "message_to_send" },
                            additionalProperties = false
                        }
                    }
                };

                messages = new object[]
                {
                    new { role = "system", content = systemPrompt},
                    new { role = "user", content = newPrompt}
                };

                openAIResponse = await SendOpenAIRequest(apiKey, systemPrompt, messages, response_format_message);
                var messageJson = JObject.Parse(openAIResponse);
                string receivers = JObject.Parse(messageJson["output"]?[0]?["content"]?[0]?["text"].ToString())["message_receiver"]?.ToString();
                string message = JObject.Parse(messageJson["output"]?[0]?["content"]?[0]?["text"].ToString())["message_to_send"]?.ToString();

                captainResponse = $"Sending a message to {receivers} with the message {message}";
                break;
            case "Query":
                // need one-shot of database schema
                systemPrompt = $"You are the Query Former. Your job is to form only MySQL queries and nothing else. The following are the database schema to reference from: {DatabaseString}";

                var response_format_query = new
                {
                    format = new
                    {
                        type = "json_schema",
                        name = "query_former",
                        strict = true,
                        schema = new
                        {
                            type = "object",
                            properties = new
                            {
                                query = new
                                {
                                    type = "string",
                                    description = @"Create a MySQL query from the prompt.",
                                },
                            },
                            required = new[] { "query" },
                            additionalProperties = false
                        }
                    }
                };

                messages = new object[]
                {
                    //new { role = "system", content = systemPrompt},
                    new { role = "user", content = newPrompt}
                };

                openAIResponse = await SendOpenAIRequest(apiKey, systemPrompt, messages, response_format_query);
                var queryJson = JObject.Parse(openAIResponse);
                Debug.Log("OPENAI <QUERY USAGE>: " + JsonConvert.SerializeObject(JObject.Parse(queryJson["usage"].ToString())));
                string query = JObject.Parse(queryJson["output"]?[0]?["content"]?[0]?["text"].ToString())["query"]?.ToString();
                Debug.Log("OPENAI <QUERY>: " + query);

                captainResponse = $"Here is the query for the data you requested:\n {query}";
                break;
            case "Schedule":
                // bring them to schedule screen?
                // or try to form the schedule
                systemPrompt = $"You are the Scheduler. Your job is to form a schedule around a flight booking.";

                var response_format_schedule = new
                {
                    format = new
                    {
                        type = "json_schema",
                        name = "scheduler",
                        strict = true,
                        schema = new
                        {
                            type = "object",
                            properties = new
                            {
                                flight_booking_number = new
                                {
                                    type = "string",
                                    description = @"Booking or Flight number referenced in prompt.",
                                },
                                departure_time = new
                                {
                                    type = "string",
                                    description = @"Time of Depature.",
                                },
                                departure_location = new
                                {
                                    type = "string",
                                    description = @"Departure Airport.",
                                },
                                arrival_time = new
                                {
                                    type = "string",
                                    description = @"Time of Arrival.",
                                },
                                arrival_location = new
                                {
                                    type = "string",
                                    description = @"Arrival Airport.",
                                },
                                pilots = new
                                {
                                    type = "string",
                                    description = @"The pilot(s) specified for the scheduled flight. Multiple pilots should be comma delimited. For example: John, Joe, Bob"
                                }
                            },
                            required = new[] { "flight_booking_number", "departure_time", "departure_location", "arrival_time", "arrival_location", "pilots" },
                            additionalProperties = false
                        }
                    }
                };

                messages = new object[]
                {
                    //new { role = "system", content = systemPrompt},
                    new { role = "user", content = newPrompt}
                };

                openAIResponse = await SendOpenAIRequest(apiKey, systemPrompt, messages, response_format_schedule);
                var scheduleJson = JObject.Parse(openAIResponse);
                var scheduleResponse = JObject.Parse(scheduleJson["output"]?[0]?["content"]?[0]?["text"].ToString());
                string scheduleNumber = scheduleResponse["flight_booking_number"]?.ToString();
                string scheduleDepartureTime = scheduleResponse["departure_time"]?.ToString();
                string scheduleDepartureLocation = scheduleResponse["departure_location"]?.ToString();
                string scheduleArrivalTime = scheduleResponse["arrival_time"]?.ToString();
                string scheduleArrivalLocation = scheduleResponse["arrival_location"]?.ToString();
                string schedulePilots = scheduleResponse["pilots"]?.ToString();
                string scheduleString = scheduleNumber + "\n" + scheduleDepartureTime + "\n" + scheduleDepartureLocation + "\n" + scheduleArrivalTime + "\n" + scheduleArrivalLocation + "\n" + schedulePilots;
                Debug.Log("OPENAI <SCHEDULE>: " + scheduleString);



                captainResponse = $"Schedule:\n{scheduleString}";
                break;
            case "Navigate":
                // Have a list of keywords that can be referenced as part of one-shot
                //  just need to capture that screen name and go to it
                //  currentScreen = navigateScreenName, prevScreen = CaptainConversationView

                systemPrompt = @"You are the App Navigator. Your job is to determine which location or screen the user wants to go to. The following are all the navigatable location in the app:
Home: home page
Chat: page with all user's chats
Chat <CHAT_NAME>: a specific chat thread to enter
Overwatch: information containing current flights and other info for today
Schedule: screen where past, present, and future schedules are contained
Booking <BOOKING_NUMBER>: a specific booking flight number screen to enter";

                var response_format_navigate = new
                {
                    format = new
                    {
                        type = "json_schema",
                        name = "app_navigator",
                        strict = true,
                        schema = new
                        {
                            type = "object",
                            properties = new
                            {
                                destination_screen = new
                                {
                                    type = "string",
                                    description = @"A screen name to navigate to",
                                    //@enum = new[] {"Home", "Chat", "Overwatch", "Schedule"},
                                },
                            },
                            required = new[] { "destination_screen" },
                            additionalProperties = false
                        }
                    }
                };

                messages = new object[]
                {
                    //new { role = "system", content = systemPrompt},
                    new { role = "user", content = newPrompt}
                };

                openAIResponse = await SendOpenAIRequest(apiKey, systemPrompt, messages, response_format_navigate);
                var navigateJson = JObject.Parse(openAIResponse);
                string navigate = JObject.Parse(navigateJson["output"]?[0]?["content"]?[0]?["text"].ToString())["destination_screen"]?.ToString();
                Debug.Log("OPENAI <NAVIGATE>: " + navigate);

                captainResponse = $"Navigating to Screen: {navigate}";
                break;
            default:
                // could have other field which could field request that don't fit functionality
                captainResponse = "Other";
                break;
        }


        ProcessCaptainPrompt(captainResponse, ICaptainSequencerInterface.PromptType.OnPrompt);
        return;

        ////1: Do we have a story?

        //if (TheStoryObject == null)
        //{
        //    DetermineStory();
        //    //StoryType = AppHelper.newGuid;
        //}
        //else
        //{
        //    //Keep reprompting until complete.....
        //    ContinueStoryPrompt();
            
        //}



        /*



        //if we are waiting on confirm action ... do the following
        if (waitingOnConfirmActionPrompt)
        {
            //process prompt ...
            //did user change something or accept?  will have ot expand on that later

            //look for command accept words or no go words let YES, NO,
            //if processed as yes....
            //   _delegate.OnConfirmedStory("storyid", "storydata");
            // if no....
            // advance logic here .... but for now just ....            
            ProcessCaptainPrompt("", ICaptainSequencerInterface.PromptType.OnConfirmedStory);
            //_delegate.OnStartNewStory("Ok I aborted that request.  What else can I help you with?");
            //ResetConversation();
            return;
        }




        //1st do we have a story?
        if(StoryId.Length != 32)
        {
            waitingOnConfirmActionPrompt = true;
            StoryId = AppHelper.newGuid;

            ProcessCaptainPrompt("You want me to delete all my records?  Please Confirm with a yes?", ICaptainSequencerInterface.PromptType.OnConfirmRequest);
            
            return;
            //figure out if we can make a story form the prompt or combo of prompts ....

            //if we determin story then ....
            // Do Action...

        }
        else
        {
            //We have story already ... so lets continue to figure out what we need to do....

            //prompt back if we have questions to keep asking....
            //if hte latest prompt does not make sense ... then we can do a few things we could remove it form the prompt list we have if it will confuse hte grammer but we should prmpt back as dont understand

            //since we have story ...
        }
        */


            /*
            if(TextIndex == 0)
            {
                _delegate.OnPrompt("Hi.  I'm repeating back what you said. "+newPrompt);
                TextIndex++;
                return;
            }
            if (TextIndex == 1)
            {
                _delegate.OnUnclearPleaseRestate();
                TextIndex++;
                return;
            }
            if (TextIndex == 2)
            {
                //StartNewStory();
                _delegate.OnStartNewStory();
                TextIndex++;
                return;
            }
            if (TextIndex == 3)
            {
                _delegate.OnExitConversation();
                TextIndex++;
                return;
            }
            */
    }

    void DetermineStory()
    {

        GrammarManager grammar = new GrammarManager(LatestUserPrompt);


        ///Identify a single or command word ....
        /// 

        if(grammar.CouldNotProcess == true)
        {
            ProcessCaptainPrompt("Please restate.", ICaptainSequencerInterface.PromptType.OnUnclearPleaseRestate);
            return;
        }
        
        PredictiveProcess predictiveProcess = new PredictiveProcess(LatestUserPrompt, grammar);
        predictiveProcess.process();

        float exitWeight = predictiveProcess.checkExitStatus(); //how we check exit weight
        string classifier = predictiveProcess._guessedClassifierTerm;
        float weight = predictiveProcess._guessedClassifierWeight;


        if (classifier == "chat")
        {
            StoryType = "chat";
            //Debug.Log("CHAT: "+ JsonConvert.SerializeObject(grammar._sentenceObject));

            ChatStory chatStory = new ChatStory(grammar._sentenceObject);

            TheStoryObject = chatStory.FillStory();            

            if (TheStoryObject.IsStoryComplete)
            {
                StoryObjectResult = JsonConvert.SerializeObject(TheStoryObject);
                ProcessCaptainPrompt("", ICaptainSequencerInterface.PromptType.OnConfirmedStory);
                
                //OnConfirmedStory(string storyId, string json_string_data_object);
            }
            else
            {
                ProcessCaptainPrompt(TheStoryObject.CaptainResponse, ICaptainSequencerInterface.PromptType.OnPrompt);
                //chatStoryObject
            }

            return;
        } else if (classifier == "navigate"){
            StoryType = "navigate";
            //Debug.Log("NAVIGATE: "+ JsonConvert.SerializeObject(grammar._sentenceObject));

            NavigateStory navigateStory = new NavigateStory(grammar._sentenceObject);

            TheStoryObject = navigateStory.FillStory();            

            if (TheStoryObject.IsStoryComplete)
            {
                StoryObjectResult = JsonConvert.SerializeObject(TheStoryObject);
                ProcessCaptainPrompt("", ICaptainSequencerInterface.PromptType.OnConfirmedStory);
                
                //OnConfirmedStory(string storyId, string json_string_data_object);
            }
            else
            {
                ProcessCaptainPrompt(TheStoryObject.CaptainResponse, ICaptainSequencerInterface.PromptType.OnPrompt);
                //chatStoryObject
            }

            return;
        } else if (classifier == "schedule"){
            StoryType = "schedule";
            UserScheduleStory scheduleStory = new UserScheduleStory(grammar._sentenceObject);

            TheStoryObject = scheduleStory.FillStory();            

            if (TheStoryObject.IsStoryComplete)
            {
                if(ConfirmedPrompt == false && TheStoryObject.PromptConfirm == true)
                {
                    ConfirmedPrompt = true;
                    StoryObjectResult = JsonConvert.SerializeObject(TheStoryObject);
                    //Debug.Log(StoryObjectResult);
                    ProcessCaptainPrompt(TheStoryObject.CaptainResponse + " Is this correct?", ICaptainSequencerInterface.PromptType.OnConfirmRequest);

                }
                else
                {
                
                    if(TheStoryObject.PromptConfirm == true)
                    {
                        //TODO ....
                        // did the user say yes or no.... or some positive action .... was it required
                        //we need a positive confirm
                    }
                    else
                    {
                        //TODO: are we adjusting the story or starting over .....
                        //TODO expand on ....
                        // for now its ok....
                    }

                    StoryObjectResult = JsonConvert.SerializeObject(TheStoryObject);
                    Debug.Log(StoryObjectResult);
                    ProcessCaptainPrompt("", ICaptainSequencerInterface.PromptType.OnConfirmedStory);

                }
            }
            else
            {
                ProcessCaptainPrompt(TheStoryObject.CaptainResponse, ICaptainSequencerInterface.PromptType.OnPrompt);
            }

            return;
        } else if (classifier == "quick_command"){
            
            string quickTitle = grammar._sentenceObject._mainVerbObject._mainVerb._display;

            StoryType = "quick command";
            QuickCommand quickCommandStory = new QuickCommand(grammar._sentenceObject, quickTitle);

            TheStoryObject = quickCommandStory.FillStory();            

            if (TheStoryObject.IsStoryComplete)
            {
                StoryObjectResult = JsonConvert.SerializeObject(TheStoryObject);
                ProcessCaptainPrompt("", ICaptainSequencerInterface.PromptType.OnConfirmedStory);
                
                //OnConfirmedStory(string storyId, string json_string_data_object);
            }
            else
            {
                ProcessCaptainPrompt(TheStoryObject.CaptainResponse, ICaptainSequencerInterface.PromptType.OnPrompt);
                //chatStoryObject
            }

            return;
        }


        if (classifier == "exit")
        {
            _delegate.OnExitConversation();            
            return;
        }

        ProcessCaptainPrompt("I don't know or understand what you are requesting.  Please try rephrasing the request.  Note that my dictionary terms have been limited to 30 words preventing me from understanding the context.", ICaptainSequencerInterface.PromptType.OnPrompt);

    }

    void ContinueStoryPrompt()
    {
        GrammarManager grammar = new GrammarManager(LatestUserPrompt);
        TheStoryObject.UpdatePrompt(LatestUserPrompt, grammar);

        //Check for exit on prompt....
        PredictiveProcess predictiveProcess = new PredictiveProcess(LatestUserPrompt, grammar);
        predictiveProcess.process();

        float exitWeight = predictiveProcess.checkExitStatus();
        //UnityEngine.Debug.Log("EXIT WEIGHT: " + exitWeight);
        if (exitWeight > 0.3)
        {
            //UnityEngine.Debug.Log("EXIT FOUND");
            _delegate.OnExitConversation();
            return;
        }


        //TODO take last prompt and do a percent against exit ... if so then ... exit....
        // Check against the exit requirement
        //    _delegate.OnExitConversation();

        
        //chatStoryObject
        if (TheStoryObject.IsStoryComplete)
        {

            if(ConfirmedPrompt == false && TheStoryObject.PromptConfirm == true)
            {
                ConfirmedPrompt = true;
                StoryObjectResult = JsonConvert.SerializeObject(TheStoryObject);
                Debug.Log(StoryObjectResult);
                ProcessCaptainPrompt(TheStoryObject.CaptainResponse + " Is this correct?", ICaptainSequencerInterface.PromptType.OnConfirmRequest);

            }
            else
            {
                
                if(TheStoryObject.PromptConfirm == true)
                {
                    //TODO ....
                    // did the user say yes or no.... or some positive action .... was it required
                    //we need a posibive confirm
                }
                else
                {
                    //TODO: are we adjsuting hte story or starting over .....
                    //TODO expand on ....
                    // for now its ok....
                }

                StoryObjectResult = JsonConvert.SerializeObject(TheStoryObject);
                Debug.Log(StoryObjectResult);
                ProcessCaptainPrompt("", ICaptainSequencerInterface.PromptType.OnConfirmedStory);

            }

            //OnConfirmedStory(string storyId, string json_string_data_object);
        }
        else
        {
            ProcessCaptainPrompt(TheStoryObject.CaptainResponse, ICaptainSequencerInterface.PromptType.OnPrompt);
            //chatStoryObject
        }
       

    }

    void ProcessCaptainPrompt(string prompt, ICaptainSequencerInterface.PromptType promptType)
    {
        CaptainPrompts.Add(prompt);
        LatestCaptainPrompt = prompt;

        switch (promptType)
        {
            case ICaptainSequencerInterface.PromptType.OnConfirmRequest:
                _delegate.OnConfirmRequest(prompt);
                break;
            case ICaptainSequencerInterface.PromptType.OnUnclearPleaseRestate:
                _delegate.OnUnclearPleaseRestate();
                break;

            case ICaptainSequencerInterface.PromptType.OnConfirmedStory:
                //get values form class that have been determined
                _delegate.OnCompletedStory(StoryType,StoryObjectResult);
                break;
            case ICaptainSequencerInterface.PromptType.OnPrompt:
                _delegate.OnPrompt(prompt);
                break;
        }


    }

    
    
    void StartNewStory()
    {
        ResetConversation();
        _delegate.OnStartNewStory();
    }


    private string DatabaseString = @"CREATE TABLE `aircraft` (
  `id` char(32) NOT NULL,
  `tail_number` varchar(32) NOT NULL,
  `serial_number` varchar(32) DEFAULT NULL,
  `chat_thread_id` char(32) DEFAULT NULL,
  `icao_code` varchar(32) DEFAULT NULL,
  `faa_designator` varchar(32) DEFAULT NULL,
  `status` varchar(32) DEFAULT NULL,
  `description` varchar(255) DEFAULT NULL,
  `icon` varchar(1024) DEFAULT NULL,
  `icon_color` varchar(45) DEFAULT NULL,
  `image` varchar(1024) DEFAULT NULL,
  `cruise_knot` double DEFAULT NULL,
  `fuel_type` varchar(32) DEFAULT NULL,
  `fuel_metric` varchar(15) DEFAULT NULL,
  `max_togw` double DEFAULT NULL,
  `max_togw_aft` double DEFAULT NULL,
  `max_togw_fwd` double DEFAULT NULL,
  `max_zfw` double DEFAULT NULL,
  `max_zfw_aft` double DEFAULT NULL,
  `max_zfw_fwd` double DEFAULT NULL,
  `max_ramp` double DEFAULT NULL,
  `max_ramp_aft` double DEFAULT NULL,
  `max_ramp_fwd` double DEFAULT NULL,
  `max_fuel` double DEFAULT NULL,
  `empty_weight` double DEFAULT NULL,
  `max_landing_weight` double DEFAULT NULL,
  `max_landing_weight_aft` double DEFAULT NULL,
  `max_landing_weight_fwd` double DEFAULT NULL,
  `empty_arm` double DEFAULT NULL,
  `fuel_arm1` double DEFAULT NULL,
  `fuel_arm2` double DEFAULT NULL,
  `fuel_arm3` double DEFAULT NULL,
  `required_pilots` int DEFAULT '1',
  `is_active` tinyint(1) NOT NULL,
  `deleted_at` datetime DEFAULT NULL,
  `updated_at` datetime NOT NULL,
  `created_at` datetime NOT NULL,
  `version_data_sync` bigint unsigned NOT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `version_data_sync_UNIQUE` (`version_data_sync`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE `aircraft_component_attachments` (
  `id` char(32) NOT NULL,
  `aircraft_component_id` char(32) NOT NULL,
  `aircraft_id` char(32) NOT NULL,
  `attached_at` datetime DEFAULT NULL,
  `detached_at` datetime DEFAULT NULL,
  `attached_location` varchar(64) DEFAULT NULL,
  `is_active` tinyint(1) NOT NULL,
  `deleted_at` datetime DEFAULT NULL,
  `updated_at` datetime NOT NULL,
  `created_at` datetime NOT NULL,
  `version_data_sync` bigint unsigned NOT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `version_data_sync_UNIQUE` (`version_data_sync`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE `aircraft_component_readings` (
  `id` char(32) NOT NULL,
  `aircraft_component_id` char(32) NOT NULL,
  `performed_by` char(32) DEFAULT NULL,
  `performed_at` datetime DEFAULT NULL,
  `time` double DEFAULT NULL,
  `cycle` double DEFAULT NULL,
  `is_active` tinyint(1) NOT NULL,
  `deleted_at` datetime DEFAULT NULL,
  `updated_at` datetime NOT NULL,
  `created_at` datetime NOT NULL,
  `version_data_sync` bigint unsigned NOT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `version_data_sync_UNIQUE` (`version_data_sync`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE `aircraft_components` (
  `id` char(32) NOT NULL,
  `type` varchar(32) DEFAULT NULL,
  `description` varchar(64) DEFAULT NULL,
  `serial_number` varchar(32) DEFAULT NULL,
  `is_active` tinyint(1) NOT NULL,
  `deleted_at` datetime DEFAULT NULL,
  `updated_at` datetime NOT NULL,
  `created_at` datetime NOT NULL,
  `version_data_sync` bigint unsigned NOT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `version_data_sync_UNIQUE` (`version_data_sync`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE `aircraft_hobbs` (
  `id` char(32) NOT NULL,
  `aircraft_id` char(32) DEFAULT NULL,
  `performed_by` char(32) DEFAULT NULL,
  `performed_at` datetime DEFAULT NULL,
  `reading` double DEFAULT NULL,
  `is_active` tinyint(1) NOT NULL,
  `deleted_at` datetime DEFAULT NULL,
  `updated_at` datetime NOT NULL,
  `created_at` datetime NOT NULL,
  `version_data_sync` bigint unsigned NOT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `version_data_sync_UNIQUE` (`version_data_sync`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE `aircraft_maintenance_schedule` (
  `id` char(32) NOT NULL,
  `aircraft_id` char(32) NOT NULL,
  `scheduled_down_at` datetime DEFAULT NULL,
  `scheduled_up_at` datetime DEFAULT NULL,
  `down_at` datetime DEFAULT NULL,
  `up_at` datetime DEFAULT NULL,
  `type` varchar(32) DEFAULT NULL,
  `approved_down_by` varchar(32) DEFAULT NULL,
  `approved_up_by` varchar(32) DEFAULT NULL,
  `created_by` varchar(32) DEFAULT NULL,
  `is_active` tinyint(1) NOT NULL,
  `deleted_at` datetime DEFAULT NULL,
  `updated_at` datetime NOT NULL,
  `created_at` datetime NOT NULL,
  `version_data_sync` bigint unsigned NOT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `version_data_sync_UNIQUE` (`version_data_sync`),
  KEY `aircraft_index` (`aircraft_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE `aircraft_tasks` (
  `id` char(32) NOT NULL,
  `aircraft_id` char(32) NOT NULL,
  `chat_thread_id` char(32) DEFAULT NULL,
  `schedule_maintenance_id` char(32) DEFAULT NULL,
  `created_by` char(32) NOT NULL,
  `created_from_flight_id` char(32) DEFAULT NULL,
  `resolved_by` char(32) DEFAULT NULL,
  `resolved_at` datetime DEFAULT NULL,
  `task` text,
  `type` char(32) DEFAULT NULL,
  `resolution` text,
  `urgency` char(32) DEFAULT NULL,
  `reminder_at` datetime DEFAULT NULL,
  `deferred_until` datetime DEFAULT NULL,
  `deferred_by` char(32) DEFAULT NULL,
  `due_at` datetime DEFAULT NULL,
  `is_active` tinyint(1) NOT NULL,
  `deleted_at` datetime DEFAULT NULL,
  `updated_at` datetime NOT NULL,
  `created_at` datetime NOT NULL,
  `version_data_sync` bigint unsigned NOT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `version_data_sync_UNIQUE` (`version_data_sync`),
  KEY `due_index` (`due_at`),
  KEY `remind_index` (`reminder_at`),
  KEY `maintenance_index` (`schedule_maintenance_id`),
  KEY `aircraft_index` (`aircraft_id`),
  KEY `defered_index` (`deferred_until`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE `aircraft_vors` (
  `id` char(32) NOT NULL,
  `aircraft_id` char(32) NOT NULL,
  `performed_at` datetime NOT NULL,
  `user_id` char(32) DEFAULT NULL,
  `place` varchar(52) DEFAULT NULL,
  `distance` double DEFAULT NULL,
  `vor1_direction` int DEFAULT NULL,
  `vor1_degree` int DEFAULT NULL,
  `vor2_direction` int DEFAULT NULL,
  `vor2_degree` int DEFAULT NULL,
  `is_active` tinyint(1) NOT NULL,
  `deleted_at` datetime DEFAULT NULL,
  `updated_at` datetime NOT NULL,
  `created_at` datetime NOT NULL,
  `version_data_sync` bigint unsigned NOT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `version_data_sync_UNIQUE` (`version_data_sync`),
  KEY `perforemd_index` (`performed_at` DESC)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE `aircraft_weight_balances` (
  `id` char(32) NOT NULL,
  `aircraft_id` char(32) NOT NULL,
  `json_object` text,
  `is_active` tinyint(1) NOT NULL,
  `deleted_at` datetime DEFAULT NULL,
  `updated_at` datetime NOT NULL,
  `created_at` datetime NOT NULL,
  `version_data_sync` bigint unsigned NOT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `version_data_sync_UNIQUE` (`version_data_sync`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE `booking_actions` (
  `id` char(32) NOT NULL,
  `title` varchar(128) DEFAULT NULL,
  `type` varchar(32) DEFAULT NULL,
  `cost` float DEFAULT NULL,
  `customer_billing` float DEFAULT NULL,
  `json_notes` text,
  `is_crew` tinyint(1) NOT NULL,
  `is_active` tinyint(1) NOT NULL,
  `deleted_at` datetime DEFAULT NULL,
  `updated_at` datetime NOT NULL,
  `created_at` datetime NOT NULL,
  `version_data_sync` bigint unsigned NOT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `version_data_sync_UNIQUE` (`version_data_sync`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE `booking_attachments` (
  `id` char(32) NOT NULL,
  `booking_id` char(32) NOT NULL,
  `file_id` char(32) NOT NULL,
  `subject` varchar(45) DEFAULT NULL,
  `created_by` char(32) DEFAULT NULL,
  `is_active` tinyint(1) NOT NULL,
  `deleted_at` datetime DEFAULT NULL,
  `updated_at` datetime NOT NULL,
  `created_at` datetime NOT NULL,
  `version_data_sync` bigint unsigned NOT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `version_data_sync_UNIQUE` (`version_data_sync`),
  KEY `booking_id` (`booking_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE `booking_flight_actions` (
  `booking_action_id` char(32) NOT NULL,
  `booking_flight_id` char(32) NOT NULL,
  `is_active` tinyint(1) NOT NULL,
  `deleted_at` datetime DEFAULT NULL,
  `updated_at` datetime NOT NULL,
  `created_at` datetime NOT NULL,
  `version_data_sync` bigint unsigned NOT NULL,
  UNIQUE KEY `booking_flight_actions` (`booking_action_id`,`booking_flight_id`),
  UNIQUE KEY `version_data_sync_UNIQUE` (`version_data_sync`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE `booking_flight_aircrew` (
  `booking_flight_id` char(32) NOT NULL,
  `users_aircrew_id` char(32) NOT NULL,
  `users_schedule_id` char(32) DEFAULT NULL,
  `position` varchar(32) DEFAULT NULL,
  `is_active` tinyint(1) NOT NULL,
  `deleted_at` datetime DEFAULT NULL,
  `updated_at` datetime NOT NULL,
  `created_at` datetime NOT NULL,
  `version_data_sync` bigint unsigned NOT NULL,
  UNIQUE KEY `booking_flight_aircrew` (`booking_flight_id`,`users_aircrew_id`),
  UNIQUE KEY `version_data_sync_UNIQUE` (`version_data_sync`),
  KEY `booking_id` (`booking_flight_id`),
  KEY `users_aircrew_id` (`users_aircrew_id`),
  KEY `users_schedule_id` (`users_schedule_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE `booking_flight_attachments` (
  `booking_attachment_id` char(32) NOT NULL,
  `booking_flight_id` char(32) NOT NULL,
  `is_active` tinyint(1) NOT NULL,
  `deleted_at` datetime DEFAULT NULL,
  `updated_at` datetime NOT NULL,
  `created_at` datetime NOT NULL,
  `version_data_sync` bigint unsigned NOT NULL,
  UNIQUE KEY `booking_flight_attachments` (`booking_attachment_id`,`booking_flight_id`),
  UNIQUE KEY `version_data_sync_UNIQUE` (`version_data_sync`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE `booking_flight_manifest` (
  `booking_flight_id` char(32) NOT NULL,
  `entity_id` char(32) NOT NULL,
  `is_active` tinyint(1) NOT NULL,
  `deleted_at` datetime DEFAULT NULL,
  `updated_at` datetime NOT NULL,
  `created_at` datetime NOT NULL,
  `version_data_sync` bigint unsigned NOT NULL,
  UNIQUE KEY `booking_flight_manifest` (`booking_flight_id`,`entity_id`),
  UNIQUE KEY `version_data_sync_UNIQUE` (`version_data_sync`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE `booking_flights` (
  `id` char(32) NOT NULL,
  `booking_id` char(32) DEFAULT NULL,
  `type` varchar(20) DEFAULT NULL,
  `status` varchar(20) DEFAULT NULL,
  `aircraft_id` varchar(32) DEFAULT NULL,
  `departure_scheduled_at` datetime DEFAULT NULL,
  `departure_fbo_id` char(32) DEFAULT NULL,
  `departure_airport_id` char(32) NOT NULL,
  `departure_airport_ident` varchar(12) NOT NULL,
  `arrival_scheduled_at` datetime DEFAULT NULL,
  `arrival_fbo_id` char(32) DEFAULT NULL,
  `arrival_airport_id` char(32) NOT NULL,
  `arrival_airport_ident` varchar(12) NOT NULL,
  `required_pilots` int NOT NULL DEFAULT '1',
  `is_active` tinyint(1) NOT NULL,
  `deleted_at` datetime DEFAULT NULL,
  `updated_at` datetime NOT NULL,
  `created_at` datetime NOT NULL,
  `version_data_sync` bigint unsigned NOT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `version_data_sync_UNIQUE` (`version_data_sync`),
  KEY `aircraft_id` (`aircraft_id`),
  KEY `booking_id` (`booking_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb3;

CREATE TABLE `booking_manifest` (
  `booking_id` char(32) NOT NULL,
  `entity_id` char(32) NOT NULL,
  `entity_type` enum('passenger','cargo','pet','luggage') DEFAULT NULL,
  `weight_lbs` double DEFAULT NULL,
  `is_active` tinyint(1) NOT NULL,
  `deleted_at` datetime DEFAULT NULL,
  `updated_at` datetime NOT NULL,
  `created_at` datetime NOT NULL,
  `version_data_sync` bigint unsigned NOT NULL,
  UNIQUE KEY `booking_manifest` (`booking_id`,`entity_id`),
  UNIQUE KEY `version_data_sync_UNIQUE` (`version_data_sync`),
  KEY `booking_id` (`booking_id`),
  KEY `entity_id` (`entity_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE `bookings` (
  `id` char(32) NOT NULL,
  `number` varchar(32) NOT NULL,
  `agent_id` char(32) NOT NULL,
  `customer_id` char(32) DEFAULT NULL,
  `chat_thread_id` char(32) DEFAULT NULL,
  `type` varchar(32) DEFAULT NULL,
  `status` varchar(32) DEFAULT NULL,
  `quoted_date` date DEFAULT NULL,
  `quoted_aircraft_type` varchar(145) DEFAULT NULL,
  `quoted_aircraft_tail` char(32) DEFAULT NULL,
  `quoted_total` float DEFAULT NULL,
  `paid_in_full` datetime DEFAULT NULL,
  `is_active` tinyint(1) NOT NULL,
  `deleted_at` datetime DEFAULT NULL,
  `updated_at` datetime NOT NULL,
  `created_at` datetime NOT NULL,
  `version_data_sync` bigint unsigned NOT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `version_data_sync_UNIQUE` (`version_data_sync`),
  KEY `customer_id` (`customer_id`),
  KEY `agent_id` (`agent_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE `cargo_photos` (
  `id` char(32) NOT NULL,
  `cargo_id` char(32) NOT NULL,
  `bucket` varchar(145) DEFAULT NULL,
  `bucket_location` varchar(1024) DEFAULT NULL,
  `created_by` char(32) DEFAULT NULL,
  `is_active` tinyint(1) NOT NULL,
  `deleted_at` datetime DEFAULT NULL,
  `updated_at` datetime NOT NULL,
  `created_at` datetime NOT NULL,
  `version_data_sync` bigint unsigned NOT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `version_data_sync_UNIQUE` (`version_data_sync`),
  KEY `cargo_id` (`cargo_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE `cargos` (
  `id` char(32) NOT NULL,
  `description` varchar(128) DEFAULT NULL,
  `type` varchar(32) DEFAULT NULL,
  `tracking_number` varchar(128) DEFAULT NULL,
  `weight_lbs` double DEFAULT NULL,
  `customer_id` char(32) DEFAULT NULL,
  `passenger_id` char(32) DEFAULT NULL,
  `json_contact_info` text,
  `json_notes` text,
  `status` varchar(32) DEFAULT NULL,
  `is_active` tinyint(1) NOT NULL,
  `deleted_at` datetime DEFAULT NULL,
  `updated_at` datetime NOT NULL,
  `created_at` datetime NOT NULL,
  `version_data_sync` bigint unsigned NOT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `version_data_sync_UNIQUE` (`version_data_sync`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE `chat_thread_attachments` (
  `id` char(32) NOT NULL,
  `chat_thread_id` char(32) NOT NULL,
  `user_id` char(32) NOT NULL,
  `type` varchar(20) NOT NULL,
  `file_id` char(32) NOT NULL,
  `category` varchar(20) NOT NULL,
  `message` text,
  `sent_at` datetime DEFAULT NULL,
  `is_active` tinyint(1) NOT NULL,
  `deleted_at` datetime DEFAULT NULL,
  `updated_at` datetime NOT NULL,
  `created_at` datetime NOT NULL,
  `version_data_sync` bigint unsigned NOT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `version_data_sync_UNIQUE` (`version_data_sync`),
  KEY `chat_thread_id` (`chat_thread_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE `chat_thread_messages` (
  `id` char(32) NOT NULL,
  `chat_thread_id` char(32) DEFAULT NULL,
  `user_id` char(32) DEFAULT NULL,
  `message` text,
  `sent_at` datetime DEFAULT NULL,
  `is_active` tinyint(1) NOT NULL,
  `deleted_at` datetime DEFAULT NULL,
  `updated_at` datetime NOT NULL,
  `created_at` datetime NOT NULL,
  `version_data_sync` bigint unsigned NOT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `version_data_sync_UNIQUE` (`version_data_sync`),
  KEY `chat_thread_id` (`chat_thread_id`),
  KEY `index4` (`chat_thread_id`,`created_at`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE `chat_threads` (
  `id` char(32) NOT NULL,
  `type` varchar(20) NOT NULL,
  `created_by` varchar(32) DEFAULT NULL,
  `subject` varchar(45) DEFAULT NULL,
  `recent_message_at` datetime DEFAULT NULL,
  `is_active` tinyint(1) NOT NULL,
  `deleted_at` datetime DEFAULT NULL,
  `updated_at` datetime NOT NULL,
  `created_at` datetime NOT NULL,
  `version_data_sync` bigint unsigned NOT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `version_data_sync_UNIQUE` (`version_data_sync`),
  KEY `type` (`type`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE `chat_thread_users` (
  `chat_thread_id` char(32) NOT NULL,
  `user_id` char(32) NOT NULL,
  `last_read_at` datetime DEFAULT NULL,
  `is_active` tinyint(1) NOT NULL,
  `deleted_at` datetime DEFAULT NULL,
  `updated_at` datetime NOT NULL,
  `created_at` datetime NOT NULL,
  `version_data_sync` bigint unsigned NOT NULL,
  UNIQUE KEY `version_data_sync_UNIQUE` (`version_data_sync`),
  UNIQUE KEY `chat_thread_id_user_id` (`chat_thread_id`,`user_id`),
  KEY `index_chat_thread_id` (`chat_thread_id`),
  KEY `index_user_id` (`user_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE `compliance_check` (
  `id` char(32) NOT NULL,
  `user_id` char(32) NOT NULL,
  `number` varchar(12) DEFAULT NULL,
  `description` varchar(245) DEFAULT NULL,
  `param_object` text,
  `suggestion_object` text,
  `result_object` longtext,
  `processed` datetime DEFAULT NULL,
  `resolved` datetime DEFAULT NULL,
  `is_unresolvable` tinyint(1) DEFAULT NULL,
  `is_active` tinyint(1) NOT NULL,
  `deleted_at` datetime DEFAULT NULL,
  `updated_at` datetime NOT NULL,
  `created_at` datetime NOT NULL,
  `version_data_sync` bigint unsigned NOT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `version_data_sync_UNIQUE` (`version_data_sync`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE `compliance_check_impact` (
  `compliance_check_id` char(32) NOT NULL,
  `table_name` varchar(32) NOT NULL,
  `table_id` char(32) NOT NULL,
  `is_active` tinyint(1) NOT NULL,
  `deleted_at` datetime DEFAULT NULL,
  `updated_at` datetime NOT NULL,
  `created_at` datetime NOT NULL,
  `version_data_sync` bigint unsigned NOT NULL,
  UNIQUE KEY `version_data_sync_UNIQUE` (`version_data_sync`),
  UNIQUE KEY `compliance_check_impact` (`compliance_check_id`,`table_name`,`table_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE `customers` (
  `id` char(32) NOT NULL,
  `name` varchar(128) DEFAULT NULL,
  `company` varchar(64) DEFAULT NULL,
  `phone` varchar(32) DEFAULT NULL,
  `email` varchar(145) DEFAULT NULL,
  `json_contacts` text,
  `json_addresses` text,
  `json_notes` text,
  `status` varchar(32) DEFAULT NULL,
  `is_broker` tinyint(1) NOT NULL,
  `is_active` tinyint(1) NOT NULL,
  `deleted_at` datetime DEFAULT NULL,
  `updated_at` datetime NOT NULL,
  `created_at` datetime NOT NULL,
  `version_data_sync` bigint unsigned NOT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `version_data_sync_UNIQUE` (`version_data_sync`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE `developer_test_table` (
  `id` char(32) NOT NULL,
  `value_number` int DEFAULT NULL,
  `value_string` varchar(145) DEFAULT NULL,
  `value_text` text,
  `value_json` text,
  `is_active` tinyint(1) NOT NULL,
  `deleted_at` datetime DEFAULT NULL,
  `updated_at` datetime NOT NULL,
  `created_at` datetime NOT NULL,
  `version_data_sync` bigint unsigned NOT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `version_data_sync_UNIQUE` (`version_data_sync`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE `device_push_token_users` (
  `id` varchar(64) NOT NULL,
  `user_id` char(32) NOT NULL,
  `push_token` varchar(64) DEFAULT NULL,
  `device_type` varchar(64) DEFAULT NULL,
  `deleted_at` datetime DEFAULT NULL,
  `updated_at` datetime NOT NULL,
  `created_at` datetime NOT NULL,
  PRIMARY KEY (`id`),
  KEY `index2` (`user_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE `entity_record` (
  `record_id` char(32) NOT NULL,
  `entity_id` char(32) NOT NULL,
  `entity_type` varchar(32) NOT NULL,
  `is_active` tinyint(1) NOT NULL,
  `deleted_at` datetime DEFAULT NULL,
  `updated_at` datetime NOT NULL,
  `created_at` datetime NOT NULL,
  `version_data_sync` bigint unsigned NOT NULL,
  UNIQUE KEY `records_entity` (`record_id`,`entity_id`),
  UNIQUE KEY `version_data_sync_UNIQUE` (`version_data_sync`),
  KEY `entity` (`entity_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE `file_assimilate` (
  `id` char(32) NOT NULL,
  `file_id` char(32) DEFAULT NULL,
  `json_object` text,
  `dateTime` datetime DEFAULT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE `file_digests` (
  `id` char(32) NOT NULL,
  `identified_type_id` char(32) DEFAULT NULL,
  `verified` tinyint(1) DEFAULT '0',
  `json_digest` mediumtext,
  `json_key_value` text,
  `json_lines` text,
  `json_words` text,
  `deleted_at` datetime DEFAULT NULL,
  `updated_at` datetime NOT NULL,
  `created_at` datetime NOT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE `file_record` (
  `record_id` char(32) NOT NULL,
  `file_id` char(32) NOT NULL,
  `is_active` tinyint(1) NOT NULL,
  `deleted_at` datetime DEFAULT NULL,
  `updated_at` datetime NOT NULL,
  `created_at` datetime NOT NULL,
  `version_data_sync` bigint unsigned NOT NULL,
  UNIQUE KEY `version_data_sync_UNIQUE` (`version_data_sync`),
  UNIQUE KEY `records_files` (`record_id`,`file_id`),
  KEY `record_id` (`record_id`),
  KEY `file_id` (`file_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE `files` (
  `id` char(32) NOT NULL,
  `name` varchar(128) NOT NULL,
  `extension` varchar(10) DEFAULT NULL,
  `original_name` varchar(512) NOT NULL,
  `bucket` varchar(145) DEFAULT NULL,
  `bucket_location` varchar(1024) DEFAULT NULL,
  `icon_location` varchar(1024) DEFAULT NULL,
  `app_alias` varchar(1024) DEFAULT NULL,
  `app_prefix` varchar(1024) DEFAULT NULL,
  `status` varchar(32) DEFAULT NULL,
  `created_by` char(32) DEFAULT NULL,
  `size_height` int unsigned DEFAULT NULL,
  `size_width` int unsigned DEFAULT NULL,
  `is_active` tinyint(1) NOT NULL,
  `deleted_at` datetime DEFAULT NULL,
  `updated_at` datetime NOT NULL,
  `created_at` datetime NOT NULL,
  `version_data_sync` bigint unsigned NOT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `version_data_sync_UNIQUE` (`version_data_sync`),
  KEY `name` (`name`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE `flight_aircrew` (
  `flight_id` char(32) NOT NULL,
  `users_aircrew_id` char(32) NOT NULL,
  `users_schedule_id` char(32) DEFAULT NULL,
  `position` varchar(32) DEFAULT NULL,
  `weight_lbs` double DEFAULT NULL,
  `data_aircraft_weight_balance_id` char(32) DEFAULT NULL,
  `data_aircraft_weight_balance_location` varchar(32) DEFAULT NULL,
  `data_aircraft_weight_balance_short` varchar(4) DEFAULT NULL,
  `identification_verified_by` char(32) DEFAULT NULL,
  `is_active` tinyint(1) NOT NULL,
  `deleted_at` datetime DEFAULT NULL,
  `updated_at` datetime NOT NULL,
  `created_at` datetime NOT NULL,
  `version_data_sync` bigint unsigned NOT NULL,
  UNIQUE KEY `flight_aircrew` (`flight_id`,`users_aircrew_id`),
  UNIQUE KEY `version_data_sync_UNIQUE` (`version_data_sync`),
  KEY `flight_id` (`flight_id`),
  KEY `users_aircrew_id` (`users_aircrew_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE `flight_manifest` (
  `flight_id` char(32) NOT NULL,
  `entity_id` char(32) NOT NULL,
  `entity_type` enum('passenger','cargo','pet','luggage') DEFAULT NULL,
  `weight_lbs` double DEFAULT NULL,
  `data_aircraft_weight_balance_id` char(32) DEFAULT NULL,
  `data_aircraft_weight_balance_location` varchar(32) DEFAULT NULL,
  `data_aircraft_weight_balance_short` varchar(4) DEFAULT NULL,
  `identification_verified_by` char(32) DEFAULT NULL,
  `is_active` tinyint(1) NOT NULL,
  `deleted_at` datetime DEFAULT NULL,
  `updated_at` datetime NOT NULL,
  `created_at` datetime NOT NULL,
  `version_data_sync` bigint unsigned NOT NULL,
  UNIQUE KEY `flight_manifest` (`flight_id`,`entity_id`),
  UNIQUE KEY `version_data_sync_UNIQUE` (`version_data_sync`),
  KEY `flight_id` (`flight_id`),
  KEY `entity_id` (`entity_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE `flights` (
  `id` char(32) NOT NULL,
  `booking_id` char(32) DEFAULT NULL,
  `booking_flight_id` char(32) DEFAULT NULL,
  `type` varchar(20) DEFAULT NULL,
  `status` varchar(20) DEFAULT NULL,
  `aircraft_id` varchar(32) DEFAULT NULL,
  `departure_at` datetime DEFAULT NULL,
  `departure_airport_id` char(32) NOT NULL,
  `departure_airport_ident` varchar(12) NOT NULL,
  `arrival_at` datetime DEFAULT NULL,
  `arrival_airport_id` char(32) NOT NULL,
  `arrival_airport_ident` varchar(12) NOT NULL,
  `block_time` double DEFAULT NULL,
  `flight_time` double DEFAULT NULL,
  `flight_time_estimated` double DEFAULT NULL,
  `ifr_time` double DEFAULT NULL,
  `night_time` double DEFAULT NULL,
  `is_night_takeoff` tinyint(1) DEFAULT '0',
  `is_night_landing` tinyint(1) DEFAULT '0',
  `approach_user_id` char(32) DEFAULT NULL,
  `approach_type` varchar(32) DEFAULT NULL,
  `wheels_up_at` datetime DEFAULT NULL,
  `wheels_down_at` datetime DEFAULT NULL,
  `engine_on_at` double DEFAULT NULL,
  `engine_off_at` double DEFAULT NULL,
  `landing_cycle` tinyint(1) DEFAULT NULL,
  `fuel_departure` double DEFAULT NULL,
  `fuel_arrival` double DEFAULT NULL,
  `fuel_burn` double DEFAULT NULL,
  `fuel_adjust_pre_flight` double DEFAULT NULL,
  `fuel_adjust_post_flight` double DEFAULT NULL,
  `fuel_level_pre_flight` double DEFAULT NULL,
  `fuel_level_post_flight` double DEFAULT NULL,
  `divert_to_airport_id` char(32) DEFAULT NULL,
  `divert_to_airport_ident` varchar(12) DEFAULT NULL,
  `divert_type` varchar(32) DEFAULT NULL,
  `divert_reason` varchar(64) DEFAULT NULL,
  `divert_notes` text,
  `is_adverse_condition` tinyint(1) DEFAULT NULL,
  `data_entry_user_id` char(32) DEFAULT NULL,
  `data_entry_at` datetime DEFAULT NULL,
  `sign_off_user_id` char(32) DEFAULT NULL,
  `sign_off_at` datetime DEFAULT NULL,
  `adjusted_user_id` char(32) DEFAULT NULL,
  `adjusted_at` datetime DEFAULT NULL,
  `is_post_fuel_checked` tinyint(1) DEFAULT NULL,
  `is_post_numbers_checked` tinyint(1) DEFAULT NULL,
  `is_weight_balance_auto` tinyint(1) DEFAULT NULL,
  `weight_balance_signoff_at` datetime DEFAULT NULL,
  `weight_balance_id` char(32) DEFAULT NULL,
  `is_flight_empty` tinyint(1) DEFAULT NULL,
  `is_flight_complete` tinyint(1) DEFAULT NULL,
  `is_active` tinyint(1) NOT NULL,
  `deleted_at` datetime DEFAULT NULL,
  `updated_at` datetime NOT NULL,
  `created_at` datetime NOT NULL,
  `version_data_sync` bigint unsigned NOT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `version_data_sync_UNIQUE` (`version_data_sync`),
  KEY `aircraft_id` (`aircraft_id`),
  KEY `transport_id` (`booking_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb3;

CREATE TABLE `notification_topic_messages` (
  `id` char(32) NOT NULL,
  `notification_topic_id` char(32) DEFAULT NULL,
  `user_id` char(32) DEFAULT NULL,
  `message` text,
  `sent_at` datetime DEFAULT NULL,
  `is_active` tinyint(1) NOT NULL,
  `deleted_at` datetime DEFAULT NULL,
  `updated_at` datetime NOT NULL,
  `created_at` datetime NOT NULL,
  `version_data_sync` bigint unsigned NOT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `version_data_sync_UNIQUE` (`version_data_sync`),
  KEY `notification_topic_id` (`notification_topic_id`),
  KEY `sent_index` (`sent_at` DESC)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE `notification_topics` (
  `id` char(32) NOT NULL,
  `type` varchar(45) NOT NULL,
  `description` varchar(145) DEFAULT NULL,
  `is_active` tinyint(1) NOT NULL,
  `deleted_at` datetime DEFAULT NULL,
  `updated_at` datetime NOT NULL,
  `created_at` datetime NOT NULL,
  `version_data_sync` bigint unsigned NOT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `version_data_sync_UNIQUE` (`version_data_sync`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE `notification_topic_user_read` (
  `notification_topic_id` char(32) NOT NULL,
  `user_id` char(32) NOT NULL,
  `last_read_at` datetime DEFAULT NULL,
  `is_active` tinyint(1) NOT NULL,
  `deleted_at` datetime DEFAULT NULL,
  `updated_at` datetime NOT NULL,
  `created_at` datetime NOT NULL,
  `version_data_sync` bigint unsigned NOT NULL,
  UNIQUE KEY `index1` (`notification_topic_id`,`user_id`),
  UNIQUE KEY `version_data_sync_UNIQUE` (`version_data_sync`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE `passenger_luggage` (
  `id` char(32) NOT NULL,
  `passenger_id` char(32) NOT NULL,
  `description` varchar(128) DEFAULT NULL,
  `type` varchar(32) DEFAULT NULL,
  `tracking_number` varchar(128) DEFAULT NULL,
  `weight_lbs` double DEFAULT NULL,
  `status` varchar(32) DEFAULT NULL,
  `is_active` tinyint(1) NOT NULL,
  `deleted_at` datetime DEFAULT NULL,
  `updated_at` datetime NOT NULL,
  `created_at` datetime NOT NULL,
  `version_data_sync` bigint unsigned NOT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `version_data_sync_UNIQUE` (`version_data_sync`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE `passenger_pets` (
  `id` char(32) NOT NULL,
  `passenger_id` char(32) NOT NULL,
  `name` varchar(128) DEFAULT NULL,
  `description` varchar(128) DEFAULT NULL,
  `weight_lbs` double DEFAULT NULL,
  `json_notes` text,
  `is_active` tinyint(1) NOT NULL,
  `deleted_at` datetime DEFAULT NULL,
  `updated_at` datetime NOT NULL,
  `created_at` datetime NOT NULL,
  `version_data_sync` bigint unsigned NOT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `version_data_sync_UNIQUE` (`version_data_sync`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE `passenger_photos` (
  `id` char(32) NOT NULL,
  `passenger_id` char(32) NOT NULL,
  `bucket` varchar(145) DEFAULT NULL,
  `bucket_location` varchar(1024) DEFAULT NULL,
  `created_by` char(32) DEFAULT NULL,
  `is_active` tinyint(1) NOT NULL,
  `deleted_at` datetime DEFAULT NULL,
  `updated_at` datetime NOT NULL,
  `created_at` datetime NOT NULL,
  `version_data_sync` bigint unsigned NOT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `version_data_sync_UNIQUE` (`version_data_sync`),
  KEY `passenger_id` (`passenger_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE `passengers` (
  `id` char(32) NOT NULL,
  `name` varchar(128) DEFAULT NULL,
  `phone` varchar(32) DEFAULT NULL,
  `email` varchar(145) DEFAULT NULL,
  `date_of_birth` date DEFAULT NULL,
  `weight_lbs` double DEFAULT NULL,
  `identification_type` varchar(32) DEFAULT NULL,
  `identification_number` varchar(64) DEFAULT NULL,
  `passport_country` varchar(64) DEFAULT NULL,
  `passport_number` varchar(64) DEFAULT NULL,
  `json_contacts` text,
  `json_addresses` text,
  `json_notes` text,
  `status` varchar(32) DEFAULT NULL,
  `is_owner` tinyint(1) NOT NULL DEFAULT '0',
  `owner_aircraft_id` char(32) DEFAULT NULL,
  `is_active` tinyint(1) NOT NULL,
  `deleted_at` datetime DEFAULT NULL,
  `updated_at` datetime NOT NULL,
  `created_at` datetime NOT NULL,
  `version_data_sync` bigint unsigned NOT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `version_data_sync_UNIQUE` (`version_data_sync`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE `push_notice_tokens` (
  `user_id` char(32) NOT NULL,
  `token` varchar(128) NOT NULL,
  `device_id` varchar(128) NOT NULL,
  `endpoint_arn` varchar(128) DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL,
  UNIQUE KEY `push_notice_tokens_device_id_token_unique` (`device_id`,`token`,`user_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE `records` (
  `id` char(32) NOT NULL,
  `record_template_id` char(32) NOT NULL,
  `title` varchar(145) NOT NULL,
  `category` varchar(145) DEFAULT NULL,
  `app_alias` varchar(1024) DEFAULT NULL,
  `status` varchar(32) NOT NULL,
  `json_object` text,
  `created_by` char(32) DEFAULT NULL,
  `is_active` tinyint(1) NOT NULL,
  `deleted_at` datetime DEFAULT NULL,
  `updated_at` datetime NOT NULL,
  `created_at` datetime NOT NULL,
  `version_data_sync` bigint unsigned NOT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `version_data_sync_UNIQUE` (`version_data_sync`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE `system_settings` (
  `id` varchar(32) NOT NULL,
  `description` varchar(145) DEFAULT NULL,
  `is_active` tinyint(1) DEFAULT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE `user_captain_conversation_prompts` (
  `id` char(32) NOT NULL,
  `user_captain_conversation_id` char(32) NOT NULL,
  `user_id` char(32) NOT NULL,
  `prompt_entity` varchar(45) DEFAULT NULL,
  `prompt` varchar(1024) DEFAULT NULL,
  `started_at` datetime DEFAULT NULL,
  `deleted_at` datetime DEFAULT NULL,
  `updated_at` datetime NOT NULL,
  `created_at` datetime NOT NULL,
  `version_data_sync` bigint unsigned NOT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `version_data_sync_UNIQUE` (`version_data_sync`),
  KEY `index3` (`user_id`),
  KEY `index4` (`started_at`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE `user_captain_conversations` (
  `id` char(32) NOT NULL,
  `user_id` char(32) NOT NULL,
  `started_at` datetime DEFAULT NULL,
  `deleted_at` datetime DEFAULT NULL,
  `updated_at` datetime NOT NULL,
  `created_at` datetime NOT NULL,
  `version_data_sync` bigint unsigned NOT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `version_data_sync_UNIQUE` (`version_data_sync`),
  KEY `index3` (`user_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE `user_devices` (
  `device_id` varchar(42) NOT NULL,
  `user_id` char(32) NOT NULL,
  `token` varchar(64) NOT NULL,
  `device_type` varchar(128) DEFAULT NULL,
  `is_active` tinyint(1) NOT NULL,
  `updated_at` datetime NOT NULL,
  `created_at` datetime NOT NULL,
  PRIMARY KEY (`device_id`),
  KEY `token` (`token`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE `user_name_aliases` (
  `alias` varchar(64) NOT NULL,
  `user_id` char(32) NOT NULL,
  `is_active` tinyint(1) NOT NULL,
  `deleted_at` datetime DEFAULT NULL,
  `updated_at` datetime NOT NULL,
  `created_at` datetime NOT NULL,
  `version_data_sync` bigint unsigned NOT NULL,
  PRIMARY KEY (`alias`),
  UNIQUE KEY `version_data_sync_UNIQUE` (`version_data_sync`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE `users` (
  `id` char(32) NOT NULL,
  `email` varchar(145) DEFAULT NULL,
  `password` varchar(255) DEFAULT NULL,
  `full_name` varchar(145) DEFAULT NULL,
  `title` varchar(32) DEFAULT NULL,
  `phone` varchar(32) DEFAULT NULL,
  `date_of_birth` date DEFAULT NULL,
  `hire_date` date DEFAULT NULL,
  `employee_number` varchar(45) DEFAULT NULL,
  `weight_lbs` double DEFAULT NULL,
  `icon_color` varchar(45) DEFAULT NULL,
  `profile_image` varchar(1024) DEFAULT NULL,
  `json_addresses` text,
  `json_contacts` text,
  `schedule_position_type` varchar(45) NOT NULL DEFAULT 'OTHER',
  `welcome_email_sent_at` datetime DEFAULT NULL,
  `first_signin_at` datetime DEFAULT NULL,
  `force_password_reset` tinyint(1) DEFAULT NULL,
  `pin_access` varchar(6) DEFAULT NULL,
  `app_access` tinyint(1) DEFAULT NULL,
  `platform_access` tinyint(1) DEFAULT '0',
  `is_admin` tinyint(1) DEFAULT NULL,
  `is_owner` tinyint(1) DEFAULT NULL,
  `is_captain` tinyint(1) DEFAULT NULL,
  `allow_process_flights` tinyint(1) DEFAULT NULL,
  `allow_process_trips` tinyint(1) DEFAULT NULL,
  `allow_process_aircraft` tinyint(1) DEFAULT NULL,
  `allow_process_compliance` tinyint(1) DEFAULT NULL,
  `manage_schedules` tinyint(1) DEFAULT NULL,
  `manage_members` tinyint(1) DEFAULT NULL,
  `manage_company` tinyint(1) DEFAULT NULL,
  `manage_aircraft` tinyint(1) DEFAULT NULL,
  `manage_financial` tinyint(1) DEFAULT NULL,
  `manage_chat` tinyint(1) DEFAULT NULL,
  `flight_notices` tinyint(1) DEFAULT NULL,
  `flightaware_notices` tinyint(1) DEFAULT NULL,
  `compliance_notices` tinyint(1) DEFAULT NULL,
  `booking_notices` tinyint(1) DEFAULT NULL,
  `aircraft_notices` tinyint(1) DEFAULT NULL,
  `mark_all_read_at` datetime DEFAULT NULL,
  `is_active` tinyint(1) NOT NULL,
  `deleted_at` datetime DEFAULT NULL,
  `updated_at` datetime NOT NULL,
  `created_at` datetime NOT NULL,
  `version_data_sync` bigint unsigned NOT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `version_data_sync_UNIQUE` (`version_data_sync`),
  UNIQUE KEY `email_UNIQUE` (`email`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE `users_aircrew` (
  `user_id` char(32) NOT NULL,
  `certificate` varchar(64) DEFAULT NULL,
  `medical_due_at` date DEFAULT NULL,
  `active_license` tinyint(1) NOT NULL,
  `permitted` tinyint(1) NOT NULL,
  `certificate_code` varchar(4) NOT NULL,
  `pic_rating` tinyint(1) NOT NULL,
  `json_no_fly_with` text,
  `is_active` tinyint(1) NOT NULL,
  `deleted_at` datetime DEFAULT NULL,
  `updated_at` datetime NOT NULL,
  `created_at` datetime NOT NULL,
  `version_data_sync` bigint unsigned NOT NULL,
  PRIMARY KEY (`user_id`),
  UNIQUE KEY `version_data_sync_UNIQUE` (`version_data_sync`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE `user_settings` (
  `user_id` char(32) NOT NULL,
  `flight_notices` tinyint(1) DEFAULT '0',
  `flightaware_notices` tinyint(1) DEFAULT '0',
  `compliance_notices` tinyint(1) DEFAULT '0',
  `booking_notices` tinyint(1) DEFAULT '0',
  `aircraft_notices` tinyint(1) DEFAULT '0',
  `mark_all_read_at` datetime DEFAULT NULL,
  `message_filter_unread` tinyint(1) DEFAULT '0',
  `message_filter_date` varchar(45) DEFAULT 'ALL',
  `message_suppress_rest` tinyint(1) DEFAULT '0',
  `mute_captain` tinyint(1) DEFAULT '0',
  `captain_voice` varchar(45) DEFAULT 'Matthew',
  `captain_text_mode` tinyint(1) DEFAULT '0',
  `updated_at` datetime NOT NULL,
  `version_data_sync` bigint unsigned NOT NULL,
  PRIMARY KEY (`user_id`),
  UNIQUE KEY `version_data_sync_UNIQUE` (`version_data_sync`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE `users_maintenance` (
  `user_id` char(32) NOT NULL,
  `certificate` varchar(64) DEFAULT NULL,
  `medical_due_at` date DEFAULT NULL,
  `active_license` tinyint(1) DEFAULT NULL,
  `permitted` tinyint(1) DEFAULT NULL,
  `is_active` tinyint(1) NOT NULL,
  `deleted_at` datetime DEFAULT NULL,
  `updated_at` datetime NOT NULL,
  `created_at` datetime NOT NULL,
  `version_data_sync` bigint unsigned NOT NULL,
  PRIMARY KEY (`user_id`),
  UNIQUE KEY `version_data_sync_UNIQUE` (`version_data_sync`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE `users_schedule` (
  `id` char(32) NOT NULL,
  `user_id` char(32) NOT NULL,
  `scheduled_on_at` datetime DEFAULT NULL,
  `scheduled_off_at` datetime DEFAULT NULL,
  `duty_on_at` datetime DEFAULT NULL,
  `duty_off_at` datetime DEFAULT NULL,
  `type` varchar(32) DEFAULT NULL,
  `cover_requested` datetime DEFAULT NULL,
  `cover_json_object` text,
  `is_cover` tinyint(1) NOT NULL,
  `approved_by` varchar(32) DEFAULT NULL,
  `created_by` varchar(32) DEFAULT NULL,
  `is_compliant` tinyint(1) NOT NULL,
  `is_active` tinyint(1) NOT NULL,
  `deleted_at` datetime DEFAULT NULL,
  `updated_at` datetime NOT NULL,
  `created_at` datetime NOT NULL,
  `version_data_sync` bigint unsigned NOT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `version_data_sync_UNIQUE` (`version_data_sync`),
  KEY `user_index` (`user_id`),
  KEY `index4` (`type`,`scheduled_on_at`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE `users_trainer` (
  `user_id` char(32) NOT NULL,
  `certificate` varchar(64) DEFAULT NULL,
  `medical_due_at` date DEFAULT NULL,
  `active_license` tinyint(1) DEFAULT NULL,
  `permitted` tinyint(1) DEFAULT NULL,
  `is_active` tinyint(1) NOT NULL,
  `deleted_at` datetime DEFAULT NULL,
  `updated_at` datetime NOT NULL,
  `created_at` datetime NOT NULL,
  `version_data_sync` bigint unsigned NOT NULL,
  PRIMARY KEY (`user_id`),
  UNIQUE KEY `version_data_sync_UNIQUE` (`version_data_sync`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
";
}
