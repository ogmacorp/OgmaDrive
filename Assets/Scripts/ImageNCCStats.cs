// -----------------------------------------------------------------------------
// OgmaDrive
// Copyright (c) 2017 Ogma Intelligent Systems Corp. All rights reserved.
// -----------------------------------------------------------------------------

using UnityEngine;
using UnityEngine.UI;
using UnityStandardAssets.Vehicles.Car;
using UnityStandardAssets.CrossPlatformInput;
using System.Collections;

[RequireComponent(typeof(Text))]
public class ImageNCCStats : MonoBehaviour {

    public GameObject carObject = null;

    private OgmaNeoCarController _OgmaNeoCar = null;
    private EOgmaNeoCarController _eOgmaNeoCar = null;

    private float _time = 0.0f;

    // Use this for initialization
    void Start () {
        if (carObject != null)
        {
            _OgmaNeoCar = carObject.GetComponentInChildren<OgmaNeoCarController>();

            if (_OgmaNeoCar == null || !_OgmaNeoCar.isActiveAndEnabled)
                _eOgmaNeoCar = carObject.GetComponentInChildren<EOgmaNeoCarController>();
        }

        _time = 0.0f;
    }

    // Update is called once per frame
    void Update () {
        if (_OgmaNeoCar == null && _eOgmaNeoCar == null)
            return;

        string color = "white";
        _time += Time.deltaTime;

        if (_time > 1.0f)
        {
            if ((_OgmaNeoCar != null && _OgmaNeoCar.Training) ||
                (_eOgmaNeoCar != null && _eOgmaNeoCar.Training))
                color = "cyan";
            else
                color = "red";

            if (_time > 2.0f)
                _time = 0.0f;
        }

        string displayText = "Mode  : <b><color=" + color + ">";
        if ((_OgmaNeoCar != null && _OgmaNeoCar.Training) ||
            (_eOgmaNeoCar != null && _eOgmaNeoCar.Training))
            displayText += "TRAINING";
        else
            displayText += "PREDICTING";
        displayText += "</color></b>";
        displayText += System.Environment.NewLine;

        if (_OgmaNeoCar != null)
            displayText += "Library: OgmaNeo";
        else
            displayText += "Library: EOgmaNeo";
        displayText += System.Environment.NewLine;

        if (_OgmaNeoCar != null)
            displayText += "Laps   : " + _OgmaNeoCar.LapCount;
        else
            displayText += "Laps   : " + _eOgmaNeoCar.LapCount;
        displayText += System.Environment.NewLine;

        if (_OgmaNeoCar != null)
            displayText += "NCC   : " + (_OgmaNeoCar.NCC * 100.0f).ToString("00.0") + "%";
        else
            displayText += "NCC   : " + (_eOgmaNeoCar.NCC * 100.0f).ToString("00.0") + "%";
        displayText += System.Environment.NewLine;

        GetComponent<Text>().text = displayText;
    }
}
