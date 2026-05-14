using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoginUI : MonoBehaviour
{
    public void OnBackButtonClicked()
    {
        SceneManager.LoadSceneAsync(CONST.MAIN_MENU_SCENE_NAME);
    }
}
