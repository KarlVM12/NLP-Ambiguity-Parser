using DT7.Data;
using UnityEngine;

public class Tester : MonoBehaviour
{
    private DataManager _dataManager;

    private bool _performAction = true;

    private void Update()
    {
        if (_performAction)
        {
            AppHelper.LoadAuthorizedUser();
            _dataManager = DataManager.instance;

            GrammarData.LoadData(OnDataLoadComplete);
            
            _performAction = false;
        }
        
        _dataManager.QueUpdate();
    }

    private void OnDataLoadComplete()
    {
        new GrammarManager("This is a test for Capital Letters in Ron Smith's sentence. He'd tell us if It works");
        //new GrammarManager("This is a test for flight crew in Ron Smith's sentence. He'd tell us if It works or if it isn't working");
        // new GrammarManager("She said she's going to the United States, then to North Dakota, and then to Linfen. Isn't that Cool?");
    
        //initial -- 0, 6, 7, 10, 11, 15, 16, 18
        // final -- 0, 7, 10, 14, 15, 18
    }
}