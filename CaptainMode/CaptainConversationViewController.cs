using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ViewControllers;
using TMPro;
using UnityEngine.UI;

public class CaptainConversationViewController : ViewController
{

    //[SerializeField] TextCadence CaptainRecentPrompt;
    [SerializeField] TMP_Text CaptainRecentPrompt;
    [SerializeField] TMP_Text UserRecentPrompt;

    [SerializeField] VerticalLayoutGroup CaptainVerticalLayout;

    CaptainConversation CapCon;


    [SerializeField] TMP_Text VoiceModeButtonTextTop;
    [SerializeField] TMP_Text VoiceModeButtonTextBottom;
    [SerializeField] GameObject VoiceModeIconOn;
    [SerializeField] GameObject VoiceModeIconOff;

    [SerializeField] GameObject UserActiveDisplay;
    [SerializeField] GameObject CaptainActiveDisplay;
    [SerializeField] GameObject UserDisplayPrompt;

    [SerializeField] GameObject InterruptEnabled;
    [SerializeField] GameObject InterruptDisabled;

    [SerializeField] RectTransform CaptainPromptTextRectransform;
    [SerializeField] RectTransform CaptainDisplayRectransform;

    [SerializeField] GameObject CannotHearDisplay;

    public TMP_InputField TextPrompt;
    [SerializeField] GameObject TextPromptContainer;
    [SerializeField] TextCadence CannotHearPrompt;

    [SerializeField] RectTransform LowerSectionTransform;

    CaptainKeyboardController theKeybaordController;

    //bool voiceModeSetting = true;

    // Start is called before the first frame update
    void Start()
    {
        theKeybaordController = this.transform.GetComponent<CaptainKeyboardController>();
    }

    // Update is called once per frame
    void Update()
    {
        if (CapCon == null)
            return;

        UserActiveDisplay.SetActive(CapCon.IsUserActiveInput);
        CaptainActiveDisplay.SetActive(!CapCon.IsUserActiveInput);

        
        if (UserRecentPrompt.text == "" || CapCon.IsUserActiveInput == false)
        {
            UserDisplayPrompt.SetActive(false);
        }
        else
        {
            if(UserDisplayPrompt.activeSelf == false)
            {
                UserDisplayPrompt.SetActive(true);
            }
        }

        if (AppHelper.LocalDevice.isTextPromptKeyboardMode)
        {
            CapCon.receivedAudioText(TextPrompt.text);

            //290 for keyboard
            //140 for input field

            if(theKeybaordController.keyboardHeightOffset > 0){
                LowerSectionTransform.offsetMin = new Vector2(LowerSectionTransform.offsetMin.x, theKeybaordController.keyboardHeightOffset+5-430);            
            }
            else{
                LowerSectionTransform.offsetMin = new Vector2(LowerSectionTransform.offsetMin.x, theKeybaordController.keyboardHeightOffset);            
            }
            
        }
        else
        {
            LowerSectionTransform.offsetMin = new Vector2(LowerSectionTransform.offsetMin.x, 0);
        }

        if (CapCon.IsUserActiveInput)
        {
            if (AppHelper.LocalDevice.isTextPromptKeyboardMode)
            {
                if (TextPromptContainer.activeSelf == false)
                {
                    TextPromptContainer.SetActive(AppHelper.LocalDevice.isTextPromptKeyboardMode);
                }
            }
            else
            {
                if (TextPromptContainer.activeSelf == true)
                {
                    TextPromptContainer.SetActive(false);
                }
            }
            
        }
        else
        {
            TextPromptContainer.SetActive(false);
        }

        
        UserRecentPrompt.text = CapCon.GetCurrentUserPromptString();


        if (CapCon.IsInDialog)
        {
            InterruptDisabled.SetActive(false);
            InterruptEnabled.SetActive(true);
        }
        else
        {
            InterruptDisabled.SetActive(true);
            InterruptEnabled.SetActive(false);
        }
        

        string lastCaptainDisplay = CapCon.GetLastCaptainPrompt();
        if (CaptainRecentPrompt.text != lastCaptainDisplay)
        {
            CaptainRecentPrompt.text = lastCaptainDisplay;

            Canvas.ForceUpdateCanvases();

            float theValueVert = CaptainPromptTextRectransform.sizeDelta.y;
            if (theValueVert > 200)
            {
                if(theValueVert > 800)
                {
                    CaptainDisplayRectransform.sizeDelta = new Vector2(CaptainDisplayRectransform.sizeDelta.x, 800);
                }
                else
                {
                    CaptainDisplayRectransform.sizeDelta = new Vector2(CaptainDisplayRectransform.sizeDelta.x, theValueVert + 40);
                }
                    
            }
            else
            {
                CaptainDisplayRectransform.sizeDelta = new Vector2(CaptainDisplayRectransform.sizeDelta.x, 210);
            }
            //CaptainPromptTextRectransform
            
            Canvas.ForceUpdateCanvases();
            CaptainVerticalLayout.enabled = false;
            CaptainVerticalLayout.enabled = true;
        }
        //


    }

    public override void ShowView()
    {
        

        TextPrompt.text = "";

        TextPromptContainer.SetActive(AppHelper.LocalDevice.isTextPromptKeyboardMode);

        this.transform.GetComponent<SlideUpCaptainSettingsMenu>().hideFilter();

        CannotHearDisplay.SetActive(false);

        if (CapCon == null)
        {
            CapCon = GameObject.Find("CODE").transform.GetComponent<CaptainConversation>();
        }
        base.ShowView();

        

        CaptainRecentPrompt.text = CapCon.GetLastCaptainPrompt();

        SetVoiceModeForCaptain();

    }


    public void OnInterruptCaptain()
    {
        CapCon.InterruptCaptainPressed();
    }

    public void OnVoiceToggleMode()
    {
        
        AppHelper.LocalDevice.IsCaptainMuted = !AppHelper.LocalDevice.IsCaptainMuted;

        if (AppHelper.LocalDevice.IsCaptainMuted)
        {
            CapCon.CaptainHasBeenMuted();
        }
        else
        {
            CapCon.CaptainHasBeenUnMuted();
        }

        AppHelper.LocalDevice.SaveDeviceSettings();

        SetVoiceModeForCaptain();
    }

    void SetVoiceModeForCaptain()
    {
        VoiceModeIconOn.SetActive(!AppHelper.LocalDevice.IsCaptainMuted);
        VoiceModeIconOff.SetActive(AppHelper.LocalDevice.IsCaptainMuted);

        VoiceModeButtonTextTop.text = "VOICE";
        VoiceModeButtonTextBottom.text = "MODE";
        if(AppHelper.LocalDevice.IsCaptainMuted == true)
        {
            VoiceModeButtonTextTop.text = "TEXT";
            VoiceModeButtonTextBottom.text = "ONLY";
        }
    }

    public void ShowCannotHear()
    {
        
        CannotHearDisplay.SetActive(true);
        CannotHearPrompt.Reset();
        CannotHearPrompt.text = "I cannot hear you, would you like to switch to text prompt instead of voice?";

    }

    public void OnCannotHearYes()
    {
        CannotHearDisplay.SetActive(false);
        CapCon.SwitchToTxtPrompt();
    }


    public void OnCannotHearNo()
    {
        CannotHearDisplay.SetActive(false);
        CapCon.TryAgainPrompt();
    }

    public void ShowSettings()
    {
        //let cap know conv is paused
        this.transform.GetComponent<SlideUpCaptainSettingsMenu>().showFilter();
        CapCon.PauseCaptain();
    }

    public void OnSettingsComplete()
    {
        //let cap know conv is unpaused
        SetVoiceModeForCaptain();
        TextPromptContainer.SetActive(AppHelper.LocalDevice.isTextPromptKeyboardMode);
        TextPrompt.text = "";

        CapCon.UnPauseCaptain();
    }

    public void UpdateVisuialItems()
    {
        TextPromptContainer.SetActive(AppHelper.LocalDevice.isTextPromptKeyboardMode);
        TextPrompt.text = "";
        SetVoiceModeForCaptain();
    }

    public void OnSendTextPrompt()
    {
        CapCon.FinishedTextPromt(TextPrompt.text);
        TextPrompt.text = "";
    }

    
}
