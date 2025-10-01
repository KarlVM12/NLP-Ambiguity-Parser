using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;

public class SlideUpCaptainSettingsMenu : MonoBehaviour
{
    [SerializeField] AudioSource audioSource;

    [SerializeField] GameObject displayPanel;
    [SerializeField] Image SlidUpDiplay;

    [SerializeField] List<GameObject> voice_options;
    [SerializeField] List<GameObject> settings_Options;
    [SerializeField] List<GameObject> settings_AppStartPrompt;
    [SerializeField] List<GameObject> settings_AppStartScreen;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (displayPanel.activeSelf == true)
        {
            if (SlidUpDiplay.transform.position.y < 0)
            {
                SlidUpDiplay.transform.position = new Vector3(SlidUpDiplay.transform.position.x, SlidUpDiplay.transform.position.y + Time.deltaTime * 6000, 0);
            }
            else
            {
                SlidUpDiplay.transform.position = new Vector3(SlidUpDiplay.transform.position.x, 0, 0);
            }

            if (SlidUpDiplay.transform.position.y > 0)
            {
                SlidUpDiplay.transform.position = new Vector3(SlidUpDiplay.transform.position.x, 0, 0);
            }
        }
    }


    public void showFilter()
    {
        displayPanel.SetActive(true);

        SetupInitialDisplay();
    }

    public void hideFilter()
    {
        audioSource.Stop();

        displayPanel.SetActive(false);

        SlidUpDiplay.transform.position = new Vector3(SlidUpDiplay.transform.position.x, -1218, 0);


    }

    public void OnDonePressed()
    {
        hideFilter();
        AppHelper.LocalDevice.SaveDeviceSettings();

        this.transform.GetComponent<CaptainConversationViewController>().OnSettingsComplete();

        //Decide how to processed with AI Prompt        
    }

    
    void SetupInitialDisplay()
    {
        foreach (GameObject item in voice_options)
        {
            item.SetActive(false);
        }
        voice_options[AppHelper.LocalDevice.VoiceId].SetActive(true);

        settings_Options[0].SetActive(AppHelper.LocalDevice.IsCaptainMuted);
        settings_Options[1].SetActive(!AppHelper.LocalDevice.isVoiceMode);
        settings_Options[2].SetActive(AppHelper.LocalDevice.IsDisplayCaptainTextAtOnce);


        foreach (GameObject item in settings_AppStartPrompt)
        {
            item.SetActive(false);
        }

        settings_AppStartPrompt[AppHelper.LocalDevice.StartPrompt].SetActive(true);

        foreach (GameObject item in settings_AppStartScreen)
        {
            item.SetActive(false);
        }

        settings_AppStartScreen[AppHelper.LocalDevice.StartScreen].SetActive(true);
    }



    public void AdjustSettings(int _index)
    {
        if (_index == 0)
        {
            AppHelper.LocalDevice.IsCaptainMuted = !AppHelper.LocalDevice.IsCaptainMuted;
        }
        if (_index == 1)
        {
            if (AppHelper.LocalDevice.isVoiceMode)
            {
                AppHelper.LocalDevice.SwitchToKeyboardFromVoice();
            }
            else
            {
                AppHelper.LocalDevice.SwitchToVoiceFromKeyboard();
            }
        }

        if (_index == 2)
        {
            AppHelper.LocalDevice.IsDisplayCaptainTextAtOnce = !AppHelper.LocalDevice.IsDisplayCaptainTextAtOnce;            
        }

        //

        SetupInitialDisplay();
    }

    public void AdjustVoice(int _index)
    {
        AppHelper.LocalDevice.VoiceId = _index;

        //PlayDemoClip
        GameObject.Find("CaptainVoice").transform.GetComponent<CaptainVoiceController>().PlayDemoClip(AppHelper.LocalDevice.VoiceName, audioSource);
        SetupInitialDisplay();
    }

    public void AdjustStartAppPrompt(int _index)
    {
        AppHelper.LocalDevice.StartPrompt = _index;
        foreach (GameObject item in settings_AppStartPrompt)
        {
            item.SetActive(false);
        }

        settings_AppStartPrompt[AppHelper.LocalDevice.StartPrompt].SetActive(true);
    }

    public void AdjustStartAppView(int _index)
    {
        AppHelper.LocalDevice.StartScreen = _index;
        foreach (GameObject item in settings_AppStartScreen)
        {
            item.SetActive(false);
        }

        settings_AppStartScreen[AppHelper.LocalDevice.StartScreen].SetActive(true);
    }
}
