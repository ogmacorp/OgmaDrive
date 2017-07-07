// -----------------------------------------------------------------------------
// OgmaDrive
// Copyright (c) 2017 Ogma Intelligent Systems Corp. All rights reserved.
// -----------------------------------------------------------------------------

using UnityEngine;
using UnityEditor;
using System.Collections;

public class ConeDecorator : ScriptableWizard
{
    public BezierSpline spline;

    public int frequency;
    public float gap;

    public Transform cone;


    [MenuItem("GameObject/Create Other/Cone Decorator")]
    static void CreateWizard()
    {
        ScriptableWizard.DisplayWizard("Cone Decorator", typeof(ConeDecorator));
    }

    void OnWizardCreate()
    {
        float stepSize = frequency;
        if (spline.Loop || stepSize == 1)
        {
            stepSize = 1f / stepSize;
        }
        else
        {
            stepSize = 1f / (stepSize - 1);
        }
        for (int p = 0, f = 0; f < frequency; f++)
        {
            for (int i = 0; i < 1; i++, p++)
            {
                if (gap == 0.0f)
                {
                    Transform item = Instantiate(cone) as Transform;
                    Vector3 position = spline.GetPoint(p * stepSize);
                    item.transform.localPosition = position;
                }
                else
                {
                    Transform item1 = Instantiate(cone) as Transform;
                    Transform item2 = Instantiate(cone) as Transform;
                    Vector3 position = spline.GetPoint(p * stepSize);
                    Vector3 direction = spline.GetDirection(p * stepSize);
                    Vector3 right = Vector3.Cross(direction, Vector3.up);
                    item1.transform.localPosition = position + (right * (gap / 2.0f));
                    item2.transform.localPosition = position - (right * (gap / 2.0f));
                }
            }
        }
    }
}