using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class BackToMap : MonoBehaviour
{
    private void Awake() {
        // Get the Button component and add listener
        var button = GetComponent<Button>();
        button.onClick.AddListener(OnButtonClick);
    }

    private void OnButtonClick() {
        SceneManager.LoadScene("MapScene");
    }
}
