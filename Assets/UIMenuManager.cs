using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class UIMenuManager : MonoBehaviour {
    public void Start()
    {
		Startup init = new Startup ();

        Component[] buttons = GameObject.Find("Canvas").GetComponentsInChildren<Button>();
        foreach(Button button in buttons)
        {
            EnableDescriptionText(button.gameObject, false);
        }
    }

    public void Update()
    {
        if (Input.GetKey("escape"))
            Application.Quit();
    }

    public void LoadOgmaNeoVersion()
    {
        SceneManager.LoadScene("OgmaNeo");
    }
    public void OnPointerEnter_OgmaNeoButton()
    {
        GameObject button = GameObject.Find("OgmaNeo");
        EnableDescriptionText(button, true);
    }
    public void OnPointerExit_OgmaNeoButton()
    {
        GameObject button = GameObject.Find("OgmaNeo");
        EnableDescriptionText(button, false);
    }

    public void LoadEOgmaNeoVersion()
    {
        SceneManager.LoadScene("EOgmaNeo");
    }
    public void OnPointerEnter_EOgmaNeoButton()
    {
        GameObject button = GameObject.Find("EOgmaNeo");
        EnableDescriptionText(button, true);
    }
    public void OnPointerExit_EOgmaNeoButton()
    {
        GameObject button = GameObject.Find("EOgmaNeo");
        EnableDescriptionText(button, false);
    }

    public void LoadRegressorTraining()
    {
        SceneManager.LoadScene("RegressorTraining");
    }

    private void EnableDescriptionText(GameObject button, bool state)
    {
        if (button != null)
        {
            Component[] childText = button.GetComponentsInChildren<Text>(true);
            foreach (Text t in childText)
                if (t.name == "DescriptionText")
                    t.gameObject.SetActive(state);
        }
    }
}
