using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

public class CaptainConversation : MonoBehaviour, ICaptainSequencerInterface
{
    [System.Runtime.InteropServices.DllImport("__Internal")]
    public static extern void unity_ios_FileView(string str);
    [System.Runtime.InteropServices.DllImport("__Internal")]
    public static extern void unity_ios_StartVoiceText(int mode);
    [System.Runtime.InteropServices.DllImport("__Internal")]
    public static extern void unity_ios_EndVoiceText();

    [System.Runtime.InteropServices.DllImport("__Internal")]
    public static extern void unity_ios_VibrateNotice();

    private bool IsGrammarDataReady = false;

    public static bool IsCaptainModeActive = false;
    float delayInTalkingTimer = 3;
    int lastUnclear;

    [HideInInspector] public string currentUserTextPrompt;
    [HideInInspector] public string lastUserTextPrompt;
    [HideInInspector] public bool IsUserActiveInput = false;

    UserCaptainConversationsRecord currentConversation;
    List<UserCaptainConversationPromptsRecord> currentPrompts;

    CaptainVoiceController CaptainAudio;
    CaptainDialogManager CaptainDialog;
    CaptainConversationSequencer CaptainSequencer;

    [SerializeField]  CaptainConversationViewController TheViewController;

    bool ExitPromptOnComplete = false;


    public bool IsInDialog
    {
        get
        {
            return CaptainDialog.IsInDialog;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        CaptainAudio = GameObject.Find("CaptainVoice").transform.GetComponent<CaptainVoiceController>();
        CaptainDialog = new CaptainDialogManager(CaptainAudio);
        CaptainSequencer = new CaptainConversationSequencer(this);

        GrammarData.LoadData(OnGrammarDataLoadComplete);
    }

    // Update is called once per frame
    void Update()
    {

        if (!IsCaptainModeActive)
            return;

        
        if (CaptainDialog.IsPaused)
            return;

        if (CaptainDialog.IsInDialog)
        {
            CaptainDialog.Update();
        }

        if (IsUserActiveInput)
        {
            delayInTalkingTimer -= Time.deltaTime;
            if (delayInTalkingTimer < 0)
            {
                //ends prompt .... from user .... if voice mode then on to captain... if keyboard do nothing....
                if (AppHelper.LocalDevice.UserCaptainInterfactionType == "Voice")
                {
                    SetToCaptainPrompt();
                }
            }
            
        }
    }


    public void StartConversation(string initialPrompt = null, bool exitOnComplete = false)
    {
        Screen.sleepTimeout = SleepTimeout.NeverSleep;

        delayInTalkingTimer = 3.0f;
        IsCaptainModeActive = true;
        lastUserTextPrompt = "";
        currentUserTextPrompt = "";
        lastUnclear = 0;

        ExitPromptOnComplete = exitOnComplete;

        NewConversationRecord();
        if(initialPrompt != null)
        {
            SetCaptainPrompt(initialPrompt);
        }
        else
        {
            SetCaptainPrompt(CaptainVoiceController.GetGreetingPrompt());
        }
        

        VibratePhone();

    }



    void SetToUserPrompt()
    {
        if (CaptainDialog.IsInDialog)
            return;

        if(ExitPromptOnComplete){
            OnExitConversation();
        }

        IsUserActiveInput = true;
        delayInTalkingTimer = 3.0f;

        if (AppHelper.LocalDevice.UserCaptainInterfactionType == "Voice")
        {
            StartVoiceToText(0);
        }
        else
        {
            //keyboard prompt
        }               

    }

    void SetToCaptainPrompt()
    {

        bool heardPrompt = false;
        if(currentUserTextPrompt.Length > 0)
        {
            heardPrompt = true;
        }
        //TODO extract and save prompt properly...
        lastUserTextPrompt = currentUserTextPrompt;
        currentUserTextPrompt = "";

        EndVoiceToText();
        IsUserActiveInput = false;


        if (delayInTalkingTimer < 0 && heardPrompt == false && AppHelper.LocalDevice.isVoiceMode)
        {
            VibratePhone();
            SetCaptainPrompt("I cannot hear you, would you like to switch to text prompt instead of voice?");
            currentUserTextPrompt = "";                    
            TheViewController.ShowCannotHear();
            return;
        }

        //lastUserTextPrompt is the current - we clear current after recived to dismiss UI items yet hold hte value
        CaptainSequencer.ProcessUserPrompt(lastUserTextPrompt);
    }



    public void StopButtonPressed()
    {
        EndConversation();        
    }

    public void InterruptCaptainPressed()
    {
        if (CaptainDialog.IsInDialog)
        {
            VibratePhone();

            CaptainAudio.StopAudioClip();
            SetCaptainPrompt("Yes?");
        }        
    }

    public void EndConversation()
    {
        Screen.sleepTimeout = SleepTimeout.SystemSetting;

        CaptainAudio.StopAudioClip();
        SaveConversationRecord();
        IsCaptainModeActive = false;
        EndVoiceToText();
    }

    void SaveConversationRecord()
    {
        if (currentConversation != null)
        {
            //currentConversation.Save(null);

            foreach (UserCaptainConversationPromptsRecord item in currentPrompts)
            {
                //item.Save(null);
            }
            currentConversation = null;
        }
    }
    
    public void receivedAudioText(string text)
    {        
        currentUserTextPrompt = text;
        delayInTalkingTimer = 1.7f;
    }

    public string GetLastCaptainPrompt()
    {
        return CaptainDialog.CurrentDialogDisplayText;
    }

    public string GetCurrentUserPromptString()
    {
        if (IsUserActiveInput)
        {
            return currentUserTextPrompt;
        }
        else
        {
            return lastUserTextPrompt;
        }
    }

    
    public void SwitchToTxtPrompt()
    {        
        CaptainAudio.StopAudioClip();
        SetCaptainPrompt("I'm ready for text prompting. " + CaptainSequencer.CurrentCaptainPrompt);
        
        AppHelper.LocalDevice.SwitchToKeyboardFromVoice();        
        TheViewController.UpdateVisuialItems();
 
    }

    public void TryAgainPrompt()
    {
        CaptainAudio.StopAudioClip();
        SetCaptainPrompt("I'm ready for voice prompting. " + CaptainSequencer.CurrentCaptainPrompt);
        
        AppHelper.LocalDevice.SwitchToVoiceFromKeyboard();
        TheViewController.UpdateVisuialItems();
    }

    void SetCaptainPrompt(string prompt_)
    {
        EndVoiceToText();

        NewPrompt("CAPTAIN", prompt_);
        CaptainDialog.onDialogComplete = SetToUserPrompt;
        CaptainDialog.BeginDialog(prompt_);                       
        
        IsUserActiveInput = false;
        delayInTalkingTimer = 3.0f;        
    }



    void NewConversationRecord()
    {
        CaptainSequencer.ResetConversation();

        currentConversation = new UserCaptainConversationsRecord();
        currentConversation.UserId = AppHelper.User.UserId;
        currentConversation.StartedAt = DateTime.UtcNow;

        currentPrompts = new List<UserCaptainConversationPromptsRecord>();
    }

    void NewPrompt(string _Who, string _Prompt)
    {
        UserCaptainConversationPromptsRecord newPrompt = new UserCaptainConversationPromptsRecord();
        newPrompt.UserId = AppHelper.User.UserId;
        newPrompt.UserCaptainConversationId = currentConversation.Id;
        newPrompt.PromptEntity = _Who;
        newPrompt.Prompt = _Prompt; 
        currentPrompts.Add(newPrompt);        
    }

    public void FinishedTextPromt(string prompt_)
    {
        currentUserTextPrompt = prompt_;
        if (currentUserTextPrompt.Length > 0)
        {
            NewPrompt("USER", prompt_);

            SetToCaptainPrompt();
        }
    }


    public void PauseCaptain()
    {
        CaptainDialog.PauseDialog();
        EndVoiceToText();
    }

    public void UnPauseCaptain()
    {     
        CaptainDialog.UnPauseDialog();
        EndVoiceToText();

        if (!CaptainDialog.IsInDialog)
        {
            SetCaptainPrompt("I'm ready to continue. " + CaptainSequencer.CurrentCaptainPrompt);
        }

        TheViewController.UpdateVisuialItems();


    }

    public void CaptainHasBeenMuted()
    {
        CaptainDialog.CaptainHasBeenMuted();
    }

    public void CaptainHasBeenUnMuted()
    {
        CaptainDialog.CaptainHasBeenUnMuted();
    }

    public void OnGrammarDataLoadComplete()
    {
        IsGrammarDataReady = true;
    }

    

    void VibratePhone()
    {
        
#if !UNITY_EDITOR
       unity_ios_VibrateNotice();        
#endif
        
    }

    public void StartVoiceToText(int mode = 0)
    {

#if !UNITY_EDITOR
        unity_ios_StartVoiceText(mode);        
#endif

    }

    public void EndVoiceToText()
    {
#if !UNITY_EDITOR
        unity_ios_EndVoiceText();
#endif
    }

    public void OnPrompt(string prompt)
    {
        SetCaptainPrompt(prompt);
    }

    public void OnUnclearPleaseRestate()
    {
        SetCaptainPrompt(CaptainVoiceController.GetUnclear(lastUnclear, out lastUnclear));
    }

    public void OnStartNewStory(string prompt = null)
    {
        SaveConversationRecord();
        NewConversationRecord();
        if (prompt == null)
        {
            SetCaptainPrompt("I'm ready to continue.");
        }
        else
        {
            SetCaptainPrompt(prompt);
        }
    }

    public void OnExitConversation()
    {
        //if we need to say good bye...

        //must go to the scene manager to trigger all actions in order...
        this.transform.GetComponent<CaptainScene>().OnCaptianButton();
        //EndConversation();
    }

    public void OnConfirmRequest(string prompt)
    {
        SetCaptainPrompt(prompt);
    }

    public void OnCompletedStory(string storyId, string json_string_data_object)
    {
        //TODO... now we do the action form the story...

        SaveConversationRecord();
        NewConversationRecord();

        Debug.Log(storyId + ":" + json_string_data_object);
        //DO ACTION ... Pass to action delegate ... and perform accordingly...
        //on return form action do
        SetCaptainPrompt("Done.  Anyting else?");
        
    }
}
