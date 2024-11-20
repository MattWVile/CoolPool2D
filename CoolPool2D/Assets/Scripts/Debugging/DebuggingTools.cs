using System.Linq;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class DebuggingTools : MonoBehaviour
{
    private bool f1Pressed = false;
    private bool f2Pressed = false;
    private bool f3Pressed = false;

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            f1Pressed = true;
        }

        if (Input.GetKeyUp(KeyCode.F1))
        {
            f1Pressed = false;
        }

        if (Input.GetKeyDown(KeyCode.F2))
        {
            f2Pressed = true;
        }

        if (Input.GetKeyUp(KeyCode.F2))
        {
            f2Pressed = false;
        }
        if (Input.GetKeyDown(KeyCode.F3))
        {
            f3Pressed = true;
        }

        if (Input.GetKeyUp(KeyCode.F3))
        {
            f3Pressed = false;
        }
        //if (Input.GetKeyUp(KeyCode.X))
        //{
        //    var rails = Rails.rails.OrderBy(x => Random.value).FirstOrDefault();
        //    if (rails != null)
        //    {
        //        rails.state = Random.value > 0.5f ? RailState.Bonus : RailState.Penalty;
        //    }
        //}

        if (f1Pressed)
        {
            Time.timeScale *= 0.998f;
            Debug.Log($"Decreased Time Scale [{Time.timeScale}]");
        }

        if (f2Pressed)
        {
            Time.timeScale /= 0.998f;
            Debug.Log($"Increased Time Scale [{Time.timeScale}]");
        }
        if (f3Pressed)
        {
            Time.timeScale = 1f;
            Debug.Log($"Increased Time Scale [{Time.timeScale}]");
        }
    }
}