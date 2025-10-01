using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CaptainVoiceController : MonoBehaviour
{
    AudioSource audioSource;

    //Kendra
    //Brian
    //Matthew

    public delegate void DelegateOnAudioComplete();
    DelegateOnAudioComplete onAudioComplete;


    
    [SerializeField] List<AudioClip> libMatthew;

    [SerializeField] List<AudioClip> libJoanna;

    [SerializeField] List<AudioClip> libBrian;


    public TTSAwsPolly aws_polly_player;


    AudioClip currentClip;
    bool isAudioStarted = false;

    // Start is called before the first frame update
    void Start()
    {
        audioSource = this.transform.GetComponent<AudioSource>();
    }

    // Update is called once per frame
    void Update()
    {
        if(isAudioStarted == true)
        {
            if (!audioSource.isPlaying)
            {
                onAudioComplete?.Invoke();
                isAudioStarted = false;
            }
        }
    }

    public void StopAudioClip()
    {
        if (isAudioStarted == true)
        {
            if (audioSource.isPlaying)
            {
                audioSource.Stop();
            }
        }
    }

    public void PauseAudio()
    {
        if (audioSource.isPlaying)
        {
            audioSource.Pause();
        }
    }

    public void ResumeAudio()
    {
        audioSource.UnPause();
    }

    public void PlayAudioClip(string voice, string text, DelegateOnAudioComplete _onComplete)
    {
        if (AppHelper.LocalDevice.IsCaptainMuted)
        {
            _onComplete?.Invoke();
            return;
        }


        isAudioStarted = false;
        onAudioComplete = _onComplete;
        currentClip = GetLocalClip(voice, text);
        if(currentClip == null)
        {
            //TODO add delegates accordingly for this action to know if audio is started ....

            aws_polly_player.Speak(text, OnRemoteLoadingClipComplete);
            //stream? or make new clip?
            //onAudioComplete?.Invoke();           
        }
        else
        {
            isAudioStarted = true;
            audioSource.PlayOneShot(currentClip);
        }

        

        //do we have clip?
    }

    public void OnRemoteLoadingClipComplete(AudioClip clip_)
    {
        
        isAudioStarted = true;
        audioSource.PlayOneShot(clip_);
        if (AppHelper.LocalDevice.IsCaptainMuted)
        {
            audioSource.Stop();
        }
    }

    public void PlayDemoClip(string voice, AudioSource _audioSource = null)
    {
        isAudioStarted = true;
        onAudioComplete = null;
        currentClip = GetLocalClip(voice, "This is your CAPTAIN speaking.");
        if(_audioSource == null)
        {
            audioSource.PlayOneShot(currentClip);
        }
        else
        {
            _audioSource.PlayOneShot(currentClip);
        }

        
    }

    AudioClip GetLocalClip(string voice, string text)
    {
        int index = -1;

        if(!DoesLocalClipExist(text, out index))
            return null;
                
        if(voice == "Joanna")
        {
            return libJoanna[index];
        }

        if (voice == "Brian")
        {
            return libBrian[index];
        }

        return libMatthew[index];

    }


    public bool DoesLocalClipExist(string text, out int index)
    {
        index = -1;

        switch (text)
        {
            case "Good Afternoon.":
                index = 0;
                break;
            case "Good Morning.":
                index = 1;
                break;
            case "Good Evening.":
                index = 2;
                break;
            case "Hi!":
                index = 3;
                break;
            case "Hi there.":
                index = 4;
                break;
            case "Hey, let's begin.":
                index = 5;
                break;
            case "Hello!":
                index = 6;
                break;
            case "Yes?":
                index = 7;
                break;
            case "How can I help?":
                index = 8;
                break;
            case "Go for CAPTAIN.":
                index = 9;
                break;
            case "I am here.":
                index = 10;
                break;
            case "I'm listening.":
                index = 11;
                break;
            case "I'm ready.":
                index = 12;
                break;
            case "Just a moment.":
                index = 13;
                break;
            case "Stand by.":
                index = 14;
                break;
            case "Give me a second.":
                index = 15;
                break;
            case "Processing.":
                index = 16;
                break;
            case "Hm.  Ok, let me see.":
                index = 17;
                break;
            case "Ok, hold on.":
                index = 18;
                break;
            case "I am checking.":
                index = 19;
                break;
            case "Let me take a look.":
                index = 20;
                break;
            case "I don't understand, can you say it a different way?":
                index = 21;
                break;
            case "Please say it a different way.":
                index = 22;
                break;
            case "I do not follow.":
                index = 23;
                break;
            case "My language is limited, I will let the developers know about this.":
                index = 24;
                break;
            case "I am not sure what you are asking, please, rephrase.":
                index = 25;
                break;
            case "I do not have permission to do that.":
                index = 26;
                break;
            case "Are you having issues?  How can I help?":
                index = 27;
                break;
            case "I cannot hear you, would you like to switch to text prompt instead of voice?":
                index = 28;
                break;
            case "I'm ready for text prompting.":
                index = 29;
                break;
            case "I'm ready for voice prompting.":
                index = 30;
                break;
            case "I'm ready to continue.":
                index = 31;
                break;
            case "Yes.":
                index = 32;
                break;
            case "No.":
                index = 33;
                break;
            case "This is your CAPTAIN speaking.":
                index = 34;
                break;

        }

        //TODO if null then start streaming if we can....


        if (index == -1)
            return false;
        return true;
    }



    public static string GetUnclear(int last, out int _last)
    {
        string Prompt = "";

        System.Random rnd = new System.Random();
        int range = rnd.Next(0, 5);
        while (range == last)
        {
            range = rnd.Next(0, 5);
        }


        switch (range)
        {
            case 0:
                Prompt = "I don't understand, can you say it a different way?";
                break;
            case 1:
                Prompt = "Please say it a different way.";
                break;
            case 2:
                Prompt = "I do not follow.";
                break;
            case 3:
                Prompt = "My language is limited, I will let the developers know about this.";
                break;
            case 4:
                Prompt = "I am not sure what you are asking, please, rephrase.";
                break;
        }

        _last = range;

        return Prompt;
    }


    public static string GetGreetingPrompt()
    {
        if (AppHelper.LocalDevice.IsFirstTimeCaptainMode)
        {
            AppHelper.LocalDevice.IsFirstTimeCaptainMode = false;
            AppHelper.LocalDevice.SaveDeviceSettings();
            return "This is your CAPTAIN speaking.";
        }

        DateTime currentTime = DateTime.Now;

        string Greeting = "";

        string TimeofDayGreeting = "Good Afternoon.";
        if (currentTime.Hour >= 0 && currentTime.Hour <= 11)
        {
            TimeofDayGreeting = "Good Morning.";
        }
        if (currentTime.Hour > 16)
        {
            TimeofDayGreeting = "Good Evening.";
        }

        System.Random rnd = new System.Random();
        int range = rnd.Next(0, 75);

        switch (range)
        {
            case int n when (n >= 0 && n < 15):
                Greeting = "Hi!";
                break;
            case int n when (n >= 15 && n < 25):
                Greeting = "Hello!";
                break;
            case int n when (n >= 25 && n < 35):
                Greeting = "Yes?";
                break;
            case int n when (n >= 35 && n < 40):
                Greeting = "How can I help?";
                break;
            case int n when (n >= 40 && n < 43):
                Greeting = "Go for CAPTAIN.";
                break;
            case int n when (n >= 43 && n < 48):
                Greeting = "I am here.";
                break;
            case int n when (n >= 48 && n < 58):
                Greeting = "I'm listening.";
                break;
            case int n when (n >= 58 && n < 65):
                Greeting = "I'm ready.";
                break;
            case int n when (n >= 65 && n < 75):
                Greeting = TimeofDayGreeting;
                break;
        }


        return Greeting;

        ////static List<string> greetingPrompts = new List<string>() { "Hello", "Hi", "Yes", "How Can I help", "Go For Cap","I'm here","I'm ready","I'm Listening"  };

    }

    public static string GetFillerStart(int last, out int _last)
    {
        string Prompt = "";

        System.Random rnd = new System.Random();
        int range = rnd.Next(0, 7);
        while (range == last)
        {
            range = rnd.Next(0, 7);
        }


        switch (range)
        {
            case 0:
                Prompt = "Just a moment.";
                break;
            case 1:
                Prompt = "Stand by.";
                break;
            case 2:
                Prompt = "Give me a second.";
                break;
            case 3:
                Prompt = "Processing.";
                break;
            case 4:
                Prompt = "Hm.  Ok, let me see.";
                break;
            case 5:
                Prompt = "Ok, hold on.";
                break;
            case 6:
                Prompt = "I am checking.";
                break;
            case 7:
                Prompt = "Let me take a look.";
                break;
        }             

        _last = range;

        return Prompt;

    }
}
