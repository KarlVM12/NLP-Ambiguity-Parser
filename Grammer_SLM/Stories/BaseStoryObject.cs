using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaseStoryObject 
{
    protected bool _PromptConfirm = false;

    public bool PromptConfirm
    {
        get
        {
            return _PromptConfirm;
        }
        set
        {
            _PromptConfirm = value;
        }
    }

    public bool IsStoryComplete
    {
        get
        {
            if (_missingComponents.Count < 1)
            {
                return true;
            }

            return false;

        }

    }

    public string CaptainResponse
    {
        get
        {
            return _captainResponse;
        }
    }


    public string _captainResponse;
    public List<string> _missingComponents = new List<string>();


    public BaseStoryObject()
    {

    }

    public virtual void UpdatePrompt(string prompt, GrammarManager grammarManager)
    {

    }
}
