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
public class LeftStats : MonoBehaviour {

    public GameObject carObject = null;

    private OgmaNeoCarController _OgmaNeoCar = null;
    private EOgmaNeoCarController _eOgmaNeoCar = null;

    // Use this for initialization
    void Start () {
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
        displayText += "Input (before filtering)" + System.Environment.NewLine;

        //Rigidbody rb = carObject.GetComponent<Rigidbody>();
        //var kph = rb.velocity.magnitude * 3.6f;
        var mph = carObject.GetComponent<CarController>().CurrentSpeed;
        displayText += "Speed: " + mph.ToString("0.00") + " mph"; // + " (" + kph.ToString("0.00") + " kph)";
        displayText += System.Environment.NewLine;

        displayText += System.Environment.NewLine;

        //if (_OgmaNeoCar != null)
        //    displayText += "Steering: " + _OgmaNeoCar.Steer.ToString("0.00") + " ";
        //else
        //    displayText += "Steering: " + _eOgmaNeoCar.Steer.ToString("0.00") + " ";
        displayText += "Training % vs Predicting %";
        displayText += System.Environment.NewLine;

        GetComponent<Text>().text = displayText;
    }
}
