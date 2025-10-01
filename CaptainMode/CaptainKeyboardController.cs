using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CaptainKeyboardController : MonoBehaviour
{
    bool LastCheckKeyboard;
    bool KeyboardInPosition = false;
    bool SimKeyboardOpen = false;

    CaptainConversationViewController CapViewController;

    [SerializeField] GameObject chatBarBoarder;

    float panelRecHeight;
    public float keyboardHeightOffset;

    
    //float keyboardHeightOffset;


    // Start is called before the first frame update
    void Start()
    {
        panelRecHeight =  GetComponent<RectTransform>().rect.height+200;//+200 is top margine

        CapViewController = this.transform.GetComponent<CaptainConversationViewController>();
    }

    // Update is called once per frame
    void Update()
    {
        if (LastCheckKeyboard != TouchScreenKeyboard.visible)
        {
            LastCheckKeyboard = TouchScreenKeyboard.visible;
            SetOffsetValue();
            KeyboardInPosition = false;
        }


        if (TouchScreenKeyboard.visible && KeyboardInPosition == false)
        {            
            SetOffsetValue();
        }

        if (TouchScreenKeyboard.visible)
        {
            if (TouchScreenKeyboard.area.height < 100)
            {
                KeyboardInPosition = false;
            }
            else
            {

            }
        }

    }


    void SetOffsetValue()
    {
        
        keyboardHeightOffset = (TouchScreenKeyboard.area.height / Screen.height)*panelRecHeight;

        chatBarBoarder.SetActive(false);
        //float keyboardOffsetDisplay = TouchScreenKeyboard.area.height - 130;// + 275;
        

        if (TouchScreenKeyboard.visible)
        {
            if (TouchScreenKeyboard.area.height < 100)
            {

                keyboardHeightOffset = 0;

            }
            else
            {
                chatBarBoarder.SetActive(true);
                //full open
                //keyboardHeightOffset = keyboardOffsetDisplay;
                keyboardHeightOffset = (TouchScreenKeyboard.area.height / Screen.height)*panelRecHeight;
                chatBarBoarder.transform.GetComponent<RectTransform>().anchoredPosition = new Vector2(0,keyboardHeightOffset);

                KeyboardInPosition = true;
            }

        }
        else
        {
            keyboardHeightOffset = 0;
        }

#if (UNITY_EDITOR)

        if (SimKeyboardOpen == false)
        {
            keyboardHeightOffset = 0;
        }
        else
        {
            keyboardHeightOffset = 550;

        }
#endif


    }


    public void onEndEditKeyboard()
    {

       
        if (CapViewController.TextPrompt.wasCanceled == true)
        {
            CapViewController.TextPrompt.text = "";
            //onCancelClick();
        }
        else
        {
            if (CapViewController.TextPrompt.text.Length > 0)
            {
                OnSendPrompt();

            }

        }
        
        //inputValue.
    }

    public void OnSendPrompt()
    {

        CapViewController.OnSendTextPrompt();
    }


    public void EditorKeyboardSimOpen()
    {
#if (UNITY_EDITOR)
        SimKeyboardOpen = true;
        SetOffsetValue();

#endif
    }

    public void EditorKeyboardSimClose()
    {
#if (UNITY_EDITOR)
        SimKeyboardOpen = false;
        SetOffsetValue();
#endif

    }



}
