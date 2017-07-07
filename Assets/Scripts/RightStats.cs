// -----------------------------------------------------------------------------
// OgmaDrive
// Copyright (c) 2017 Ogma Intelligent Systems Corp. All rights reserved.
// -----------------------------------------------------------------------------

using UnityEngine;
using UnityEngine.UI;
using UnityStandardAssets.Vehicles.Car;
using System.Collections;

[RequireComponent(typeof(Text))]
public class RightStats : MonoBehaviour {

    public GameObject carObject = null;

    private OgmaNeoCarController _OgmaNeoCar = null;
    private EOgmaNeoCarController _eOgmaNeoCar = null;

    // Use this for initialization
    void Start()
    {
        if (carObject != null)
        {
            _OgmaNeoCar = carObject.GetComponentInChildren<OgmaNeoCarController>();

            if (_OgmaNeoCar == null || !_OgmaNeoCar.isActiveAndEnabled)
                _eOgmaNeoCar = carObject.GetComponentInChildren<EOgmaNeoCarController>();
        }
    }

    // Update is called once per frame
    void Update () {
        if (_OgmaNeoCar == null && _eOgmaNeoCar == null)
            return;

        string displayText = "";
        displayText += "Prediction" + System.Environment.NewLine;

        displayText += System.Environment.NewLine;
        displayText += System.Environment.NewLine;

        if (_OgmaNeoCar != null)
            displayText += "Steering: " + _OgmaNeoCar.PredictedSteer.ToString("0.00") + " ";
        else
            displayText += "Steering: " + _eOgmaNeoCar.PredictedSteer.ToString("0.00") + " ";
        displayText += System.Environment.NewLine;

        GetComponent<Text>().text = displayText;
    }
}
