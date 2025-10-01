using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Grammar;

public class NavigateStoryObject : BaseStoryObject
{

    private SpeechUnit _mainVerb;
    public bool _noScreenFound = false;
    public bool _hasSubScreen = true;
    public string _mainScreen;
    public string _subScreen; 
    private ResponseManager _responseManager;


    public NavigateStoryObject(SpeechUnit mainVerb, string mainScreen, string subScreen)
    {
        _mainVerb = mainVerb;
        _mainScreen = mainScreen;
        _subScreen = subScreen;

        if (_subScreen == "" && _mainScreen == "")
        {
            _noScreenFound = true;
        }

        if (_subScreen == "")
        {
            _hasSubScreen = false;
        }

        _captainResponse = GetCaptainResponse();

    }
    
     private string GetCaptainResponse()
    {
        if (_noScreenFound)
        {
            return NoScreenFoundResponseQueue();
        }
        else
        {
            return DisplayScreenResponseQueue();
        }
    }

    public string NoScreenFoundResponseQueue(){

        _responseManager = new ResponseManager("navigate", "displayNoScreenFound", "set1");
        return _responseManager.Output();
    }

    public string DisplayScreenResponseQueue(){
        if (_hasSubScreen){
            _responseManager = new ResponseManager("navigate", "displayFound", "set1");
            _responseManager.Set("mainScreen", _mainScreen);
            _responseManager.Set("subScreen", _subScreen);
            return _responseManager.Output();
        } else {
            _responseManager = new ResponseManager("navigate", "displayFound", "set2");
            _responseManager.Set("mainScreen", _mainScreen);
            return _responseManager.Output();
        }
    }


}
