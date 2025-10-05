using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using static UnityEngine.Rendering.DebugUI;
using Button = UnityEngine.UIElements.Button;

public class MainMenuScript : MonoBehaviour
{
    [SerializeField]
    private UIDocument m_UIDocument;

    private Button m_Button_StartRun;
    private Button m_Button_Unlocks;
    private Button m_Button_Challenges;
    private Button m_Button_Options;


    void Start()
    {
        var rootElement = m_UIDocument.rootVisualElement;

        m_Button_StartRun = rootElement.Q<Button>("StartRunButton");
        m_Button_StartRun.clickable.clicked += OnButtonClicked_StartRun;


        m_Button_Unlocks = rootElement.Q<Button>("UnlocksButton");
        m_Button_Unlocks.clickable.clicked += OnButtonClicked_Unlocks;

        m_Button_Challenges = rootElement.Q<Button>("ChallengesButton");
        m_Button_Challenges.clickable.clicked += OnButtonClicked_Challenges;

        m_Button_Options = rootElement.Q<Button>("OptionsButton");
        m_Button_Options.clickable.clicked += OnButtonClicked_Options;

    }

    void OnButtonClicked_StartRun() {
        Debug.Log("You have clicked the m_Button_StartRun button!");
        SceneManager.LoadScene("StartRunCutScene");
    }
    void OnButtonClicked_Unlocks() {
        Debug.Log("You have clicked the m_Button_Unlocks button!");
    }
    void OnButtonClicked_Challenges() {
        Debug.Log("You have clicked the m_Button_Challenges button!");
    }
    void OnButtonClicked_Options() {
        Debug.Log("You have clicked the m_Button_Options button!");
    }
}
