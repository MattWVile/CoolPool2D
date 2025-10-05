using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

public class StartRunCutScene : MonoBehaviour
{
    public VideoPlayer videoPlayer;
    void Start()
    {
        videoPlayer.loopPointReached += OnCutsceneFinished;
    }

    private void OnCutsceneFinished(VideoPlayer vp)
    {
        SceneManager.LoadScene("UIScene");
    }
}
