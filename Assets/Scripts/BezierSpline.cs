// ----------------------------------------------------------------------------
//  Source; http://catlikecoding.com/unity/tutorials/
//

using UnityEngine;
using System;

public class BezierSpline : MonoBehaviour {

	[SerializeField]
	private Vector3[] controlPoints;

	[SerializeField]
	private BezierControlPointMode[] controlPointModes;

	[SerializeField]
	private bool loopEnabled;

	public bool Loop {
		get {
			return loopEnabled;
		}
		set {
			loopEnabled = value;
			if (value == true) {
				controlPointModes[controlPointModes.Length - 1] = controlPointModes[0];
				SetControlPoint(0, controlPoints[0]);
			}
		}
	}

	public int ControlPointCount {
		get {
			return controlPoints.Length;
		}
	}

	public Vector3 GetControlPoint (int index) {
		return controlPoints[index];
	}

	public void SetControlPoint (int index, Vector3 point) {
		if (index % 3 == 0) {
			Vector3 delta = point - controlPoints[index];
			if (loopEnabled) {
				if (index == 0) {
					controlPoints[1] += delta;
					controlPoints[controlPoints.Length - 2] += delta;
					controlPoints[controlPoints.Length - 1] = point;
				}
				else if (index == controlPoints.Length - 1) {
					controlPoints[0] = point;
					controlPoints[1] += delta;
					controlPoints[index - 1] += delta;
				}
				else {
					controlPoints[index - 1] += delta;
					controlPoints[index + 1] += delta;
				}
			}
			else {
				if (index > 0) {
					controlPoints[index - 1] += delta;
				}
				if (index + 1 < controlPoints.Length) {
					controlPoints[index + 1] += delta;
				}
			}
		}
		controlPoints[index] = point;
		EnforceMode(index);
	}

	public BezierControlPointMode GetControlPointMode (int index) {
		return controlPointModes[(index + 1) / 3];
	}

	public void SetControlPointMode (int index, BezierControlPointMode mode) {
		int modeIndex = (index + 1) / 3;
		controlPointModes[modeIndex] = mode;
		if (loopEnabled) {
			if (modeIndex == 0) {
				controlPointModes[controlPointModes.Length - 1] = mode;
			}
			else if (modeIndex == controlPointModes.Length - 1) {
				controlPointModes[0] = mode;
			}
		}
		EnforceMode(index);
	}

	private void EnforceMode (int index) {
		int modeIndex = (index + 1) / 3;
		BezierControlPointMode mode = controlPointModes[modeIndex];
		if (mode == BezierControlPointMode.Free || !loopEnabled && (modeIndex == 0 || modeIndex == controlPointModes.Length - 1)) {
			return;
		}

		int middleIndex = modeIndex * 3;
		int fixedIndex, enforcedIndex;
		if (index <= middleIndex) {
			fixedIndex = middleIndex - 1;
			if (fixedIndex < 0) {
				fixedIndex = controlPoints.Length - 2;
			}
			enforcedIndex = middleIndex + 1;
			if (enforcedIndex >= controlPoints.Length) {
				enforcedIndex = 1;
			}
		}
		else {
			fixedIndex = middleIndex + 1;
			if (fixedIndex >= controlPoints.Length) {
				fixedIndex = 1;
			}
			enforcedIndex = middleIndex - 1;
			if (enforcedIndex < 0) {
				enforcedIndex = controlPoints.Length - 2;
			}
		}

		Vector3 middle = controlPoints[middleIndex];
		Vector3 enforcedTangent = middle - controlPoints[fixedIndex];
		if (mode == BezierControlPointMode.Aligned) {
			enforcedTangent = enforcedTangent.normalized * Vector3.Distance(middle, controlPoints[enforcedIndex]);
		}
		controlPoints[enforcedIndex] = middle + enforcedTangent;
	}

	public int CurveCount {
		get {
			return (controlPoints.Length - 1) / 3;
		}
	}

	public Vector3 GetPoint (float t) {
		int i;

        // Catch wrap around
        if (t < 0.0f)
            t = t + 1.0f;
        if (t >= 1f)
            t = t - 1f;
    
        // Catch invalid values
        if (t < 0.0f)
            t = 0.0f;
        if (t > 1.0f)
            t = 1.0f;

        t = Mathf.Clamp01(t) * CurveCount;
		i = (int)t;
		t -= i;
		i *= 3;

		return transform.TransformPoint(Bezier.GetPoint(controlPoints[i], controlPoints[i + 1], controlPoints[i + 2], controlPoints[i + 3], t));
	}
	
	public Vector3 GetVelocity (float t) {
		int i;

        // Catch wrap around
        if (t >= 1f)
            t = t - 1f;

        // Catch invalid values
        if (t < 0.0f)
            t = 0.0f;
        if (t > 1.0f)
            t = 1.0f;

        t = Mathf.Clamp01(t) * CurveCount;
		i = (int)t;
		t -= i;
		i *= 3;

		return transform.TransformPoint(Bezier.GetFirstDerivative(controlPoints[i], controlPoints[i + 1], controlPoints[i + 2], controlPoints[i + 3], t)) - transform.position;
	}
	
	public Vector3 GetDirection (float t) {
		return GetVelocity(t).normalized;
	}

	public void AddCurve () {
		Vector3 point = controlPoints[controlPoints.Length - 1];
		Array.Resize(ref controlPoints, controlPoints.Length + 3);
		point.x += 1f;
		controlPoints[controlPoints.Length - 3] = point;
		point.x += 1f;
		controlPoints[controlPoints.Length - 2] = point;
		point.x += 1f;
		controlPoints[controlPoints.Length - 1] = point;

		Array.Resize(ref controlPointModes, controlPointModes.Length + 1);
		controlPointModes[controlPointModes.Length - 1] = controlPointModes[controlPointModes.Length - 2];
		EnforceMode(controlPoints.Length - 4);

		if (loopEnabled) {
			controlPoints[controlPoints.Length - 1] = controlPoints[0];
			controlPointModes[controlPointModes.Length - 1] = controlPointModes[0];
			EnforceMode(0);
		}
	}
	
	public void Reset () {
		controlPoints = new Vector3[] {
			new Vector3(1f, 0f, 0f),
			new Vector3(2f, 0f, 0f),
			new Vector3(3f, 0f, 0f),
			new Vector3(4f, 0f, 0f)
		};
		controlPointModes = new BezierControlPointMode[] {
			BezierControlPointMode.Free,
			BezierControlPointMode.Free
		};
	}
}