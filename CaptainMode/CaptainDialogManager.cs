using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CaptainDialogManager 
{
    public delegate void DelegateOnDialogComplete();
    public DelegateOnDialogComplete onDialogComplete;

    const float CadenceCycle = 0.25f; //might need to base this off the voice cadience so voice type
    float CadenceTimer = CadenceCycle;
    int CadenceIndex = 0;
    int CadenceSentenseIndex = -1;
    string[] CadenceWords;
    string CurrentCadenceString = "";

    List<string> Sentences;
    int SentenceIndex = 0;

    string ThePrompt;

    CaptainVoiceController theVoice;

    int lastFiller = -1;


    bool IsCurrentAudioComplete;
    bool IsCurrentCadenceComplete;
    bool IsDialogComplete;


    bool IsCaptainPaused;
    bool IsCaptainSpeaking;
    bool IsCaptainDisplaying;
   

    public bool IsPaused
    {
        get
        {
            return IsCaptainPaused;
        }
    }

    public bool IsInDialog
    {
        get
        {
            return IsCaptainSpeaking | IsCaptainDisplaying;
        }
    }


    public CaptainDialogManager(CaptainVoiceController _theVoice)
    {
        theVoice = _theVoice;
    }
    

    public void BeginDialog(string _prompt)
    {
        if (_prompt == "" || _prompt == null)
        {
            Debug.LogError("PROMPT CANNOT BE EMPTY...");
            return;
        }
        IsCaptainSpeaking = true;
        IsCaptainDisplaying = true;
        IsCaptainPaused = false;
        IsDialogComplete = false;

        ThePrompt = _prompt;

        //split prompt up into sentenses so we can process for speed or shoter cycles ... consider quing up?
        string splitString = ThePrompt.Replace(". ", ".|").Replace("! ", "!|").Replace(";", ":|");
        string[] BreakOut = splitString.Split('|', StringSplitOptions.RemoveEmptyEntries);

        Sentences = new List<string>(BreakOut);

        //skip for now and expand on later to allow for more QUE type actions intead of 1 off responces...
        /*
        int indexTemp;
        if (!theVoice.DoesLocalClipExist(Sentences[0], out indexTemp) && Sentences[0].Length > 10)
        {
            Sentences.Insert(0, CaptainVoiceController.GetFillerStart(lastFiller, out lastFiller));
        }
        */
        SentenceIndex = 0;

        SetupCadenceWordList();
        SetupAudioProcess();
    }

    public void PauseDialog()
    {
        IsCaptainPaused = true;
        if (IsInDialog)
        {
            theVoice.PauseAudio();
        }
    }

    public void UnPauseDialog()
    {
        IsCaptainPaused = false;
        if (IsInDialog)
        {
            theVoice.ResumeAudio();
        }               
    }


    void SetupAudioProcess()
    {
        IsCurrentAudioComplete = false;
        theVoice.PlayAudioClip(AppHelper.LocalDevice.VoiceName, Sentences[SentenceIndex], SentenceVoiceAudioComplete);
    }

    void SentenceVoiceAudioComplete()
    {
        IsCurrentAudioComplete = true;               
    }

    public void Update()
    {
        if (IsDialogComplete)
            return;

        if (IsCaptainPaused)
            return;

        UpdateDialogCadence();

        if ((IsCurrentCadenceComplete | AppHelper.LocalDevice.IsDisplayCaptainTextAtOnce) & ( IsCurrentAudioComplete | AppHelper.LocalDevice.IsCaptainMuted))
        {
            IncrementSentense();
            if (!IsDialogComplete)
            {
                SetupCadenceWordList();
                SetupAudioProcess();
            }
            
        }      
    }


    void IncrementSentense()
    {
        if (SentenceIndex < Sentences.Count)
        {
            SentenceIndex++;
        }

        if (SentenceIndex >= Sentences.Count)
        {
            IsCaptainSpeaking = false;
            IsCaptainDisplaying = false;

            if (IsDialogComplete == false)
            {
                onDialogComplete?.Invoke();
            }
            IsDialogComplete = true;
            
        }
            
    }

    public string CurrentDialogDisplayText
    {
        get
        {
            string Display = "";

            if (AppHelper.LocalDevice.IsDisplayCaptainTextAtOnce)
            {
                foreach(string item in Sentences)
                {
                    Display += item + "  ";
                }
                return Display.Trim();               
            }

            
            if (SentenceIndex > 0)
            {
                for (int i = 0; i < SentenceIndex; i++)
                {
                    Display += Sentences[i] + "  ";
                }
                if (CadenceSentenseIndex == SentenceIndex)
                {
                    Display += CurrentCadenceString.Trim();
                }
            }
            else
            {
                Display = CurrentCadenceString.Trim();
            }
            return Display;
        }
    }


    void SetupCadenceWordList()
    {
        IsCurrentCadenceComplete = false;
        CurrentCadenceString = "";

        CadenceTimer = 0;
        CadenceIndex = 0;
        CadenceSentenseIndex = SentenceIndex;
        if (SentenceIndex < Sentences.Count)
        {
            CadenceWords = Sentences[SentenceIndex].Split(' ');
        }
        else
        {
            CadenceWords = null;
        }
    }

    public void UpdateDialogCadence()
    {

        if(CadenceSentenseIndex != SentenceIndex)
        {
            SetupCadenceWordList();
        }
        if (CadenceWords == null)
            return;

        if (CadenceIndex < CadenceWords.Length)
        {
            if (CadenceTimer > 0)
            {
                CadenceTimer -= Time.deltaTime;
            }
            else
            {

                CurrentCadenceString += CadenceWords[CadenceIndex] + " ";
                //theTextLabel.text = CurrentCadenceString.Trim();
                CadenceTimer = CadenceCycle;
                CadenceIndex++;
                if (CadenceIndex >= CadenceWords.Length)
                {
                    IsCurrentCadenceComplete = true;
                }
                
            }

        }
        else
        {
            CurrentCadenceString = Sentences[SentenceIndex];
        }
    }


    public void CaptainHasBeenMuted()
    {
        theVoice.StopAudioClip();
    }

    public void CaptainHasBeenUnMuted()
    {
        if (!IsDialogComplete)
        {
            SetupCadenceWordList();
            SetupAudioProcess();
        }
    }

}
