using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ICaptainSequencerInterface 
{
    public enum PromptType : ushort { OnPrompt, OnUnclearPleaseRestate, OnStartNewStory, OnExitConversation, OnConfirmRequest, OnConfirmedStory };

    public void OnPrompt(string prompt);
    public void OnUnclearPleaseRestate();
    public void OnStartNewStory(string prompt = null);
    public void OnExitConversation();

    public void OnConfirmRequest(string prompt);
    public void OnCompletedStory(string storyId, string json_string_data_object);
    //public void OnConfirm(string prompt);
}
