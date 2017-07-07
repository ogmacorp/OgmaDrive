// -----------------------------------------------------------------------------
// OgmaDrive
// Copyright (c) 2017 Ogma Intelligent Systems Corp. All rights reserved.
// -----------------------------------------------------------------------------
//
// Source: http://wiki.unity3d.com/index.php?title=FramesPerSecond
//

using UnityEngine;
using System.Collections;

public class FPSDisplay : MonoBehaviour
{
    float deltaTime = 0.0f;
    int frameCount = 0;

    public void Update()
    {
        deltaTime += (Time.deltaTime - deltaTime) * 0.1f;
        frameCount++;

        if (Input.GetKey("escape"))
            Application.Quit();
    }

    public void OnGUI()
    {
        int w = Screen.width, h = Screen.height;

        GUIStyle style = new GUIStyle();

        Rect rect = new Rect(0, 0, w, h * 2 / 100);
        style.alignment = TextAnchor.UpperLeft;
        style.fontSize = h * 2 / 100;
        style.normal.textColor = new Color(1.0f, 1.0f, 1.0f, 1.0f);

        float msec = deltaTime * 1000.0f;
        float fps = 1.0f / deltaTime;
        string text = string.Format("FPS:  {0:0.0} ms ({1:0.} fps, {2} frames)", msec, fps, frameCount);
        text += System.Environment.NewLine;

        //text += System.Environment.NewLine;
        //text += "Keys:" + System.Environment.NewLine;
        //text += "T - toggles training and prediction" + System.Environment.NewLine;
        //text += "F - force prediction mode" + System.Environment.NewLine;
        //text += "R - resets car to track center line" + System.Environment.NewLine;

        GUI.Label(rect, text, style);
    }
}