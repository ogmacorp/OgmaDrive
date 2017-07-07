// -----------------------------------------------------------------------------
// OgmaDrive
// Copyright (c) 2017 Ogma Intelligent Systems Corp. All rights reserved.
// -----------------------------------------------------------------------------

using UnityEngine;
using UnityEngine.UI.Extensions;
using System.Collections.Generic;

public class LeftGraphB : MonoBehaviour {

    public GameObject carObject = null;

    private OgmaNeoCarController _OgmaNeoCar = null;
    private EOgmaNeoCarController _eOgmaNeoCar = null;

    private float _graphWidth;
    private float _graphHeight;
    private UILineRenderer _lineRenderer;
    private List<float> _graphValues;
    private Vector2[] _graphPoints;
    private int _maxNumValues = 50;

    // Use this for initialization
    void Start () {
        if (carObject != null)
        {
            _OgmaNeoCar = carObject.GetComponentInChildren<OgmaNeoCarController>();

            if (_OgmaNeoCar == null || !_OgmaNeoCar.isActiveAndEnabled)
                _eOgmaNeoCar = carObject.GetComponentInChildren<EOgmaNeoCarController>();
        }

        _lineRenderer = GetComponentInChildren<UILineRenderer>();

        RectTransform rectTransform = _lineRenderer.GetComponent<RectTransform>();
        Rect rect = rectTransform.rect;
        _graphWidth = rect.width;
        _graphHeight = rect.height;

        _graphValues = new List<float>();

        _graphPoints = new Vector2[_maxNumValues];
        for (int i = 0; i < _maxNumValues; i++)
        {
            float x = i * (_graphWidth / _maxNumValues);
            float y = 0.0f;

            _graphPoints[i].Set(x, y);
        }

        _lineRenderer.Points = _graphPoints;
    }

    // Update is called once per frame
    void Update () {
        if (_OgmaNeoCar != null)
        {
            _graphValues.Add(_OgmaNeoCar.PredictionPercent);
        }
        else
        {
            _graphValues.Add(_eOgmaNeoCar.PredictionPercent);
        }

        if (_graphValues.Count > _maxNumValues)
            _graphValues.RemoveAt(0);

        for (int i = 0; i < _maxNumValues && i < _graphValues.Count; i++)
        {
            int _index = _graphValues.Count - i - 1;

            float x = i * (_graphWidth / _maxNumValues);
            float y = _graphValues[_index] * _graphHeight;

            _graphPoints[_index].Set(x, y);
        }

        _lineRenderer.Points = null;
        _lineRenderer.Points = _graphPoints;
    }
}