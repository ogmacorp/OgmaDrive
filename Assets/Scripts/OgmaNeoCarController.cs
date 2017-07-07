// -----------------------------------------------------------------------------
// OgmaDrive
// Copyright (c) 2017 Ogma Intelligent Systems Corp. All rights reserved.
// -----------------------------------------------------------------------------

using UnityEngine;
using UnityStandardAssets.Vehicles.Car;
using UnityStandardAssets.CrossPlatformInput;
using System.Collections;

using ogmaneo;

public class OgmaNeoCarController : MonoBehaviour {

    [Header("World objects")]
    [Tooltip("Link to car controller script")]
    public CarController carController;
    [Tooltip("Camera texture that gets sent into an OgmaNeo hierarchy")]
    public RenderTexture cameraTexture;
    [Tooltip("Texture to hold the OgmaNeo hierarchy output")]
    public Texture2D predictionTexture;

    [Header("Training splines")]
    [Tooltip("Spline used to generate the spline")]
    public BezierSpline trackSpline;
    [Tooltip("List of splines to use during training")]
    public BezierSpline[] splineList;
    [Tooltip("Number of laps before switching to the next spline (for training)")]
    public int lapsPerSpline = 1;

    [Tooltip("Distance to look ahead when spline following (for training)")]
    public float SteerAhead = 4.0f;

    [Header("Serialization")]
    [Tooltip("Reload a saved hierarchy")]
    public bool reloadHierarchy = false;
    [Tooltip("Filename to use when saving and loading a hierarchy")]
    public string hierarchyFileName = "StockCarHierarchy.ohr";

    private float _time;

    private int _trainingCount;
    private int _predictingCount;
    public float TrainingPercent { get; private set; }
    public float PredictionPercent { get; private set; }

    private float prevBestT = 0.0f;
    public int SplineIndex { get; private set; }
    public int LapCount { get; private set; }

    public bool ForcePredictionMode { get; private set; }
    public bool Training { get; private set; }
    public float Steer { get; private set; }
    public float Accel { get; private set; }
    public float Brake { get; private set; }

    public float PredictedSteer { get; private set; }
    public float PredictedAccel { get; private set; }
    public float PredictedBrake { get; private set; }
    public float HandBrake { get; private set; }

    private float carSteer;
    private float carAccel;
    private float carBrake;

    private ogmaneo.Resources _res = null;
    private ogmaneo.Hierarchy _hierarchy = null;

    private int _inputWidth, _inputHeight;
    private ogmaneo.ValueField2D _inputField = null;
    private ogmaneo.ValueField2D _inputValues = null;

    private float[,] sourceImage;
    private float[,] previousImage;
    private float[,] predictedImage;
    public float NCC { get; private set; }

    private bool applicationExiting = false;
    void OnApplicationQuit()
    {
        applicationExiting = true;
    }

    private bool userControl = false;

    private bool enableDebugLines = false;
    private LineRenderer debugLine;
    private Vector3[] debugLinePositions;

    // Use this for initialization
    void Start()
    {
        _time = 0.0f;

        LapCount = 0;

        if (carController == null)
            return;

        print("Initializing OgmaNeo");
        _res = new ogmaneo.Resources();
        _res.create(ComputeSystem.DeviceType._gpu);

        _inputWidth = cameraTexture.width;
        _inputHeight = cameraTexture.height;

        print("Capture image size: " + _inputWidth + "x" + _inputHeight);

        // Image input fields
        _inputField = new ogmaneo.ValueField2D(new Vec2i(_inputWidth, _inputHeight), 0.0f);

        // Scalar values input field
        _inputValues = new ogmaneo.ValueField2D(new Vec2i(1, 1), 0.0f);

        print("Constructing hierarchy");
        Architect arch = new Architect();
        arch.initialize(1234, _res);

        // Add Y' input layer (typically 64x64)
        arch.addInputLayer(new Vec2i(_inputWidth, _inputHeight));

        // Add scalar values input layer (steering angle)
        arch.addInputLayer(new Vec2i(1, 1));

        arch.addHigherLayer(new Vec2i(96, 96), SparseFeaturesType._distance);
        arch.addHigherLayer(new Vec2i(96, 96), SparseFeaturesType._chunk);
        arch.addHigherLayer(new Vec2i(96, 96), SparseFeaturesType._chunk);
        arch.addHigherLayer(new Vec2i(96, 96), SparseFeaturesType._chunk);
        arch.addHigherLayer(new Vec2i(60, 60), SparseFeaturesType._chunk);
        arch.addHigherLayer(new Vec2i(60, 60), SparseFeaturesType._chunk);

        print("Generating hierarchy");
        _hierarchy = arch.generateHierarchy();

        if (reloadHierarchy && hierarchyFileName.Length > 0)
        {
            _hierarchy.load(_res.getComputeSystem(), hierarchyFileName);
            print("Reloaded OgmaNeo hierarchy from " + hierarchyFileName);
        }

        // For calculating the normalized cross-corrolation
        sourceImage = new float[_inputWidth,  _inputHeight];
        previousImage = new float[_inputWidth, _inputHeight];
        predictedImage = new float[_inputWidth, _inputHeight];

        _trainingCount = 0;
        _predictingCount = 0;
        TrainingPercent = 1.0f;
        PredictionPercent = 0.0f;

        NCC = 0.0f;
        Training = false;
        ForcePredictionMode = false;

        if (splineList != null && splineList[SplineIndex] != null)
        {
            Vector3 position = splineList[SplineIndex].GetPoint(0.0f);
            Vector3 splineDirection = splineList[SplineIndex].GetDirection(0.0f);

            carController.gameObject.transform.localPosition = position;
            carController.gameObject.transform.LookAt(position - splineDirection);

            Training = true;
        }

        if (enableDebugLines)
        {
            debugLine = gameObject.AddComponent<LineRenderer>();
            debugLine.startWidth = 0.1f;
            debugLine.endWidth = 0.1f;
            debugLinePositions = new Vector3[4];
            debugLine.numPositions = debugLinePositions.Length;
        }

        PredictedSteer = 0.0f;
        PredictedAccel = 0.0f;
        PredictedBrake = -1.0f;
        HandBrake = 1.0f;

        carSteer = PredictedSteer;
        carAccel = PredictedAccel;
        carBrake = PredictedBrake;
    }

    private void FixedUpdate()
    {
        if (applicationExiting)
            return;

        if (carController != null)
        {
            // Steering [-1, 1], Acccelerator [0, 1], Footbrake [-1, 0], Handbrake [0, 1]
            carController.Move(carSteer, carAccel, carBrake, HandBrake);
        }
    }

    void Update () {
        if (applicationExiting)
            return;

        if (cameraTexture == null || predictionTexture == null || carController == null)
            return;

        ogmaneo.Vec2i pixelPos = new Vec2i();

        // Remember currently active render texture
        RenderTexture currentActiveRT = RenderTexture.active;

        // Transfer the camera capture into the prediction texture (temporarily)
        RenderTexture.active = cameraTexture;
        predictionTexture.ReadPixels(new Rect(0, 0, _inputWidth, _inputHeight), 0, 0);
        predictionTexture.Apply();

        // Restore active render texture
        RenderTexture.active = currentActiveRT;

        // Transfer the RGB camera texture into ValueField2D fields
        Color actualPixel = new Color();
        Color yuvPixel = new Color(0.0f, 0.0f, 0.0f);
        for (int x = 0; x < _inputWidth; x++)
        {
            for (int y = 0; y < _inputHeight; y++)
            {
                actualPixel = predictionTexture.GetPixel(x, y);

                // SDTV (BT.601) Y'UV conversion
                yuvPixel.r = actualPixel.r * 0.299f + actualPixel.g * 0.587f + actualPixel.b * 0.114f;   // Y' luma component

                // Chrominance
                // U = r * -0.14713 + g * -0.28886 + b * 0.436
                //yuvPixel.g = 0.0f;
                // V = r * 0.615 + g * -0.51499 + b * -0.10001
                //yuvPixel.b = 0.0f;

                predictionTexture.SetPixel(x, y, yuvPixel);
            }
        }

        // Edge Detection Convolution methods:
        //   Laplacian of the Gaussian (LoG) - https://en.wikipedia.org/wiki/Blob_detection#The_Laplacian_of_Gaussian
        // - Sobel-Feldman and Sharr operators - https://en.wikipedia.org/wiki/Sobel_operator
        // - Prewitt operator - https://en.wikipedia.org/wiki/Prewitt_operator
        //   Kirch operator - https://en.wikipedia.org/wiki/Kirsch_operator
        Texture2D horzTexture = ConvolutionFilter.Apply(predictionTexture, ConvolutionFilter.Sobel3x3Horizontal);// ConvolutionFilter.Prewitt3x3Horizontal);
        Texture2D vertTexture = ConvolutionFilter.Apply(predictionTexture, ConvolutionFilter.Sobel3x3Vertical);// ConvolutionFilter.Prewitt3x3Vertical);

        Texture2D convolvedTexture = new Texture2D(_inputWidth, _inputHeight, predictionTexture.format, false);
        Color tempPixel = new Color(0.0f, 0.0f, 0.0f);

        for (int x = 0; x < _inputWidth; x++)
        {
            for (int y = 0; y < _inputHeight; y++)
            {
                Color horzPixel = horzTexture.GetPixel(x, y);
                Color vertPixel = vertTexture.GetPixel(x, y);

                tempPixel.r = Mathf.Sqrt((horzPixel.r * horzPixel.r) + (vertPixel.r * vertPixel.r));
                tempPixel.g = tempPixel.r;// Mathf.Sqrt((horzPixel.g * horzPixel.g) + (vertPixel.g * vertPixel.g));
                tempPixel.b = tempPixel.r;// Mathf.Sqrt((horzPixel.b * horzPixel.b) + (vertPixel.b * vertPixel.b));

                convolvedTexture.SetPixel(x, y, tempPixel);
            }
        }

        predictionTexture.SetPixels(convolvedTexture.GetPixels());
        predictionTexture.Apply();

        // Transfer the RGB camera texture into ValueField2D fields
        for (int x = 0; x < _inputWidth; x++)
        {
            for (int y = 0; y < _inputHeight; y++)
            {
                actualPixel = predictionTexture.GetPixel(x, y);

                pixelPos.x = x;
                pixelPos.y = y;

                _inputField.setValue(pixelPos, actualPixel.r);

                previousImage[x, y] = sourceImage[x, y];
                sourceImage[x, y] = actualPixel.r;// * 0.299f + actualPixel.g * 0.587f + actualPixel.b * 0.114f;
            }
        }

        // Encode scalar values from the car controller
        Steer = carController.CurrentSteerAngle / carController.m_MaximumSteerAngle;
        Accel = carController.AccelInput;
        Brake = carController.BrakeInput;

        pixelPos.x = 0;
        pixelPos.y = 0;
        _inputValues.setValue(pixelPos, Steer);

        // Setup the hierarchy input vector
        vectorvf inputVector = new vectorvf();
        inputVector.Add(_inputField);
        inputVector.Add(_inputValues);

        // Step the hierarchy
        _hierarchy.activate(inputVector);

        if (Training)
            _hierarchy.learn(inputVector);

        // Grab the predictions vector
        vectorvf prediction = _hierarchy.getPredictions();

        // Transfer the ValueField2D fields into the RGB prediction texture
        Color predictedPixel = new Color();
        for (int x = 0; x < _inputWidth; x++)
        {
            for (int y = 0; y < _inputHeight; y++)
            {
                pixelPos.x = x;
                pixelPos.y = y;

                predictedPixel.r = prediction[0].getValue(pixelPos);
                predictedPixel.g = predictedPixel.r;// prediction[1].getValue(pixelPos);
                predictedPixel.b = predictedPixel.r;// prediction[2].getValue(pixelPos);

                predictionTexture.SetPixel(x, y, predictedPixel);

                predictedImage[x, y] = predictedPixel.r;// * 0.299f + predictedPixel.g * 0.587f + predictedPixel.b * 0.114f;
            }
        }
        predictionTexture.Apply();

        // Wait for physics to settle
        if (_time < 1.0f)
        {
            _time += Time.deltaTime;

            // Apply hand brake
            carSteer = 0.0f;
            carAccel = 0.0f;
            carBrake = -1.0f;
            HandBrake = 1.0f;
        }
        else
        {
            // Release hand brake
            HandBrake = 0.0f;

            Accel = -1.0f;
            Brake = Accel;

            pixelPos.x = 0;
            pixelPos.y = 0;

            // Update the car controller
            PredictedSteer = prediction[1].getValue(pixelPos);
            PredictedAccel = Accel;
            PredictedBrake = Brake;

            carSteer = PredictedSteer;// * carController.m_MaximumSteerAngle;
            carAccel = PredictedAccel;
            carBrake = PredictedBrake;

            // Search along the spline for the closest point to the current car position
            float bestT = 0.0f, minDistance = 100000.0f;
            Vector3 carPosition = carController.gameObject.transform.localPosition;

            // When not training use the track spline
            BezierSpline spline = trackSpline;

            if (Training)
                spline = splineList[SplineIndex];

            float totalDistance = 0.0f;

            for (float t = 0.0f; t <= 1.0f; t += 0.001f)
            {
                Vector3 position = spline.GetPoint(t);
                Vector3 positionPrev = spline.GetPoint(t - 0.001f);

                float distance = Vector3.Distance(position, carPosition);

                totalDistance += Vector3.Distance(position, positionPrev);

                if (distance <= minDistance)
                {
                    minDistance = distance;
                    bestT = t;
                }
            }

            // Reset car position and direction?
            if (Input.GetKeyUp(KeyCode.R) || carController.Collided)
            {
                if (ForcePredictionMode == false)
                    Training = true;

                carController.ResetCollided();

                // Spline 0 is usually set as the spline used to create the track
                SplineIndex = 0;

                Vector3 position = spline.GetPoint(bestT);
                carController.gameObject.transform.localPosition = position;

                Vector3 splineDirection = spline.GetDirection(bestT).normalized;
                carController.gameObject.transform.forward = -splineDirection;
            }

            // Determine the difference between the input image (t) and predicted image (t+1)
            CalculateNormalizedCrossCorrelation();

            // Toggle training on iff too divergent?
            if (Training == false && ForcePredictionMode == false && NCC < 0.25f)
                Training = true;

            // Toggle training off iff quite confident?
            if (Training == true && NCC > 0.85f && LapCount >= 1)
                Training = false;

            if (carController.CurrentSpeed < 2.0f)
                Training = true;

            if (Training)
                _trainingCount++;
            else
                _predictingCount++;

            if (Training && spline != null)
            {
                Vector3 carDirection = -carController.gameObject.transform.forward.normalized;

                Vector3 targetPosition = spline.GetPoint(bestT + SteerAhead / totalDistance);

                //Vector3 splineDirection = spline.GetDirection(bestT).normalized;

                Vector3 targetDirection = (targetPosition - carPosition).normalized;

                float angle = (1.0f - Vector3.Dot(carDirection, targetDirection));// * Mathf.Rad2Deg;

                Vector3 right = Vector3.Cross(carDirection, Vector3.up);
                float angle2 = Vector3.Dot(right, targetDirection);

                float newCarSteer = Mathf.Exp(256.0f * angle) - 1.0f;

                if (Mathf.Abs(minDistance) > 0.01f)//newCarSteer > Mathf.PI / 64.0f)
                {
                    newCarSteer += angle2 * Mathf.Abs(minDistance);
                }

                if (angle2 > 0.0f)
                    newCarSteer = -newCarSteer;

                if (newCarSteer > 1.0f)
                    newCarSteer = 1.0f;
                else
                if (newCarSteer < -1.0f)
                    newCarSteer = -1.0f;

                float steerBlend = 0.75f;
                carSteer = (steerBlend * newCarSteer) + ((1.0f - steerBlend) * carSteer);

                if (enableDebugLines)
                {
                    debugLinePositions[0] = carController.gameObject.transform.localPosition;
                    debugLinePositions[1] = debugLinePositions[0] + carDirection * 10.0f;
                    debugLinePositions[2] = carController.gameObject.transform.localPosition;
                    debugLinePositions[3] = debugLinePositions[2] + targetDirection * 10.0f;
                    debugLine.SetPositions(debugLinePositions);
                }
            }

            float totalCount = _trainingCount + _predictingCount;

            if (totalCount == 0.0f)
            {
                TrainingPercent = 1.0f;
                PredictionPercent = 0.0f;
            }
            else
            {
                TrainingPercent = (float)_trainingCount / totalCount;
                PredictionPercent = (float)_predictingCount / totalCount;
            }

            if (bestT < prevBestT)
            {
                LapCount++;

                _trainingCount = 0;
                _predictingCount = 0;

                if ((LapCount % lapsPerSpline) == 0)
                {
                    SplineIndex++;

                    if (SplineIndex >= splineList.Length)
                        SplineIndex = 0;
                }
            }

            prevBestT = bestT;
        }

        if (userControl)
        {
            // Control overides
            // pass the input to the car!
            float h = CrossPlatformInputManager.GetAxis("Horizontal");
            float v = CrossPlatformInputManager.GetAxis("Vertical");
#if !MOBILE_INPUT
            float handbrake = CrossPlatformInputManager.GetAxis("Jump");
#endif
            carSteer = h;
            carAccel = v;
            carBrake = v;
            HandBrake = handbrake;
        }

        // Toggle training?
        if (Input.GetKeyUp(KeyCode.T))
        {
            Training = !Training;
            ForcePredictionMode = false;
        }
        else
        // Force prediction mode?
        if (Input.GetKeyUp(KeyCode.F))
        {
            Training = false;
            ForcePredictionMode = true;
        }

        // Save out the current state of the hierarchy?
        if (Input.GetKeyUp(KeyCode.O) && hierarchyFileName.Length > 0)
        {
            _hierarchy.save(_res.getComputeSystem(), hierarchyFileName);
            print("Saved OgmaNeo hierarchy to " + hierarchyFileName);
        }
    }

    private void CalculateNormalizedCrossCorrelation()
    {
        // Calculate the normalized cross-correlation between source and predicted images
        // https://en.wikipedia.org/wiki/Cross-correlation#Normalized_cross-correlation

        // Determine image means
        float sourceMean = 0.0f, predictedMean = 0.0f;
        for (int x = 0; x < _inputWidth; x++)
        {
            for (int y = 0; y < _inputHeight; y++)
            {
                sourceMean += previousImage[x, y];
                predictedMean += predictedImage[x, y];
            }
        }
        sourceMean /= _inputWidth * _inputHeight;
        predictedMean /= _inputWidth * _inputHeight;

        // Determine image standard deviations
        float sourceSD = 0.0f, predictedSD = 0.0f;
        for (int x = 0; x < _inputWidth; x++)
        {
            for (int y = 0; y < _inputHeight; y++)
            {
                sourceSD += (previousImage[x, y] - sourceMean) * (previousImage[x, y] - sourceMean);
                predictedSD += (predictedImage[x, y] - predictedMean) * (predictedImage[x, y] - predictedMean);
            }
        }
        sourceSD = Mathf.Sqrt(sourceSD / (_inputWidth * _inputHeight));
        predictedSD = Mathf.Sqrt(predictedSD / (_inputWidth * _inputHeight));

        // Determine final normalized cross-correlation (NCC)
        NCC = 0.0f;
        for (int x = 0; x < _inputWidth; x++)
        {
            for (int y = 0; y < _inputHeight; y++)
            {
                NCC += ((previousImage[x, y] - sourceMean) * (predictedImage[x, y] - predictedMean)) / (sourceSD * predictedSD);
            }
        }
        NCC /= _inputWidth * _inputHeight;
    }
}
