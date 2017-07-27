// -----------------------------------------------------------------------------
// OgmaDrive
// Copyright (c) 2017 Ogma Intelligent Systems Corp. All rights reserved.
// -----------------------------------------------------------------------------

using UnityEngine;
using UnityStandardAssets.Vehicles.Car;
using UnityStandardAssets.CrossPlatformInput;
using System.Collections.Generic;

using eogmaneo;

public class EOgmaNeoCarController : MonoBehaviour {

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
    public string hierarchyFileName = "StockCarHierarchy.eohr";

    [Header("Visualization")]
    [Tooltip("Connect to NeoVis")]
    public bool connectToNeoVis = false;
    [Tooltip("Port to use for NeoVis comm")]
    public int neoVisPort = 54000;
    private eogmaneo.VisAdapter _neoVis = null;

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

    private eogmaneo.ComputeSystem _system = null;
    private eogmaneo.Hierarchy _hierarchy = null;

    private int _inputWidth, _inputHeight;
    private int _hiddenWidth, _hiddenHeight;
    private eogmaneo.StdVecf _inputField = null;
    private eogmaneo.StdVeci _rotationSDR = null;
    private eogmaneo.StdVeci _inputValues = null;

    private eogmaneo.OpenCVInterop _openCV = null;

    // Size of SDR history buffer
    private int _maxNumStates = 15;
    private List<int> _ccStates;
    private List<int> _onStates;
    private eogmaneo.StdVeci _predictedSDR = null;

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

        print("Initializing EOgmaNeo");

        _system = new eogmaneo.ComputeSystem(4, 1111);

        _inputWidth = cameraTexture.width;
        _inputHeight = cameraTexture.height;

        print("Capture image size: " + _inputWidth + "x" + _inputHeight);

        _hiddenWidth = _inputWidth;
        _hiddenHeight = _inputHeight / 2;

        // Y' input field
        _inputField = new eogmaneo.StdVecf(_hiddenWidth * _hiddenHeight);

        for (int i = 0; i < _hiddenWidth * _hiddenHeight; i++)
            _inputField.Add(0.0f);

        // Scalar values input field
        _inputValues = new eogmaneo.StdVeci(1);
        _inputValues.Add(0);

        print("Constructing hierarchy");

        // Hierarchy layer descriptions
        const int layerSize = 36;
        const int numLayers = 4;

        StdVecLayerDesc lds = new StdVecLayerDesc(numLayers);

        for (int l = 0; l < numLayers; l++)
        {
            lds.Add(new LayerDesc());
            lds[l]._width = layerSize;
            lds[l]._height = layerSize;
            lds[l]._chunkSize = 6;
            lds[l]._forwardRadius = 9;
            lds[l]._backwardRadius = 9;
            lds[l]._ticksPerUpdate = 2;
            lds[l]._temporalHorizon = 2;
            lds[l]._alpha = 0.04f;
            lds[l]._beta = 0.16f;

            // Disable reinforcement learning
            lds[l]._delta = 0.0f;
            //lds[i]._gamma = 0.0f;
            //lds[l]._epsilon = 0.0f;
            //lds[l]._maxReplaySamples = 0;
            //lds[l]._replayIter = 0.0f;
        }

        // Encoder output sizes, as input sizes to hierarchy
        StdVecPairi inputSizes = new eogmaneo.StdVecPairi();
        inputSizes.Add(new StdPairi(_hiddenWidth, _hiddenHeight));
        inputSizes.Add(new StdPairi(6, 6));

        StdVeci inputChunkSizes = new StdVeci();
        inputChunkSizes.Add(6);
        inputChunkSizes.Add(6);

        // Whether to predict an input
        StdVecb predictInputs = new StdVecb();
        predictInputs.Add(true);
        predictInputs.Add(true);

        print("Generating hierarchy");

        // Generate the hierarchy
        _hierarchy = new eogmaneo.Hierarchy();
        _hierarchy.create(inputSizes, inputChunkSizes, predictInputs, lds, 41);

        if (reloadHierarchy && hierarchyFileName.Length > 0)
        {
            _hierarchy.load(hierarchyFileName);
            print("Reloaded OgmaNeo hierarchy from " + hierarchyFileName);
        }

        if (connectToNeoVis)
        {
            _neoVis = new eogmaneo.VisAdapter();
            _neoVis.create(_hierarchy, neoVisPort);
        }

        _openCV = new eogmaneo.OpenCVInterop();

        int numSDRbits = (_hiddenWidth / inputChunkSizes[0]) * (_hiddenHeight / inputChunkSizes[1]);
        _rotationSDR = new StdVeci(numSDRbits);
        _predictedSDR = new StdVeci(numSDRbits);
        for (int i = 0; i < numSDRbits; i++)
        {
            _rotationSDR.Add(0);
            _predictedSDR.Add(0);
        }

        // For calculating the normalized cross-corrolation
        _ccStates = new List<int>();
        _onStates = new List<int>();
        NCC = 0.0f;

        _trainingCount = 0;
        _predictingCount = 0;
        TrainingPercent = 1.0f;
        PredictionPercent = 0.0f;

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

        // Remember currently active render texture
        RenderTexture currentActiveRT = RenderTexture.active;

        // Transfer the camera capture into the prediction texture (temporarily)
        RenderTexture.active = cameraTexture;
        predictionTexture.ReadPixels(new Rect(0, 0, _inputWidth, _inputHeight), 0, 0);
        predictionTexture.Apply();

        // Restore active render texture
        RenderTexture.active = currentActiveRT;

        // Edge Detection Convolution methods:
        // - Canny - https://en.wikipedia.org/wiki/Canny_edge_detector
        //   Laplacian of the Gaussian (LoG) - https://en.wikipedia.org/wiki/Blob_detection#The_Laplacian_of_Gaussian
        // - Sobel-Feldman and Sharr operators - https://en.wikipedia.org/wiki/Sobel_operator
        // - Prewitt operator - https://en.wikipedia.org/wiki/Prewitt_operator
        //   Kirch operator - https://en.wikipedia.org/wiki/Kirsch_operator
        bool useSobel = false;
        bool useCanny = false && !useSobel;
        bool useBlur = false && !useCanny;   // Canny already includes Gaussian blurring
        bool useThreholding = false;
        bool useGaborFilter = false;

        // Blur entire camera image?
        if (useBlur)
        {
            Texture2D blurredTexture = ConvolutionFilter.Apply(predictionTexture, ConvolutionFilter.GaussianBlur);
            predictionTexture.SetPixels(blurredTexture.GetPixels());
        }

        // Convert from RGB space to Y'UV (ignoring chrominance)
        Color actualPixel = new Color();
        Color yuvPixel = new Color();
        for (int x = 0; x < _inputWidth; x++)
        {
            for (int y = 0; y < _inputHeight; y++)
            {
                actualPixel = predictionTexture.GetPixel(x, y);

                // SDTV (BT.601) Y'UV conversion
                yuvPixel.r = actualPixel.r * 0.299f + actualPixel.g * 0.587f + actualPixel.b * 0.114f;   // Y' luma component

                // Chrominance
                // U = r * -0.14713 + g * -0.28886 + b * 0.436
                yuvPixel.g = 0.0f;
                // V = r * 0.615 + g * -0.51499 + b * -0.10001
                yuvPixel.b = 0.0f;

                predictionTexture.SetPixel(x, y, yuvPixel);
            }
        }

        int pixelPos;

        // Extract a portion of the camera image (half height)
        int yOffset = 16;   // Set to 0 for bottom half, _hiddenHeight for top half
        int yHeight = _hiddenHeight;

        for (int y = yOffset; y < yOffset + yHeight; y++)
        {
            for (int x = 0; x < _hiddenWidth; x++)
            {
                pixelPos = ((y - yOffset) * _hiddenWidth) + x;
                _inputField[pixelPos] = predictionTexture.GetPixel(x, y).r;
            }
        }

        if (useGaborFilter)
        {
            _openCV.GaborFilter(_inputField, 5, 4.0f, 0.0f, 10.0f, 0.5f, 0.0f);

            Color tempPixel = new Color(0.0f, 0.0f, 0.0f);
            for (int y = 0; y < yHeight; y++)
            {
                for (int x = 0; x < _hiddenWidth; x++)
                {
                    pixelPos = (y * _hiddenWidth) + x;
                    tempPixel.r = _inputField[pixelPos];
                    tempPixel.g = tempPixel.r;
                    tempPixel.b = tempPixel.r;
                    predictionTexture.SetPixel(x, y + yHeight, tempPixel);
                }
            }
            predictionTexture.Apply();
        }

        if (useThreholding)
        {
            //_openCV.Threshold(_inputField, 0.0f, 255.0f, 
            //    eogmaneo.OpenCVInterop.CV_THRESH_TOZERO | eogmaneo.OpenCVInterop.CV_THRESH_OTSU);

            _openCV.AdaptiveThreshold(_inputField, 255.0f,
                eogmaneo.OpenCVInterop.CV_ADAPTIVE_THRESH_GAUSSIAN_C,
                eogmaneo.OpenCVInterop.CV_THRESH_BINARY,
                5, 2);

            Color tempPixel = new Color(0.0f, 0.0f, 0.0f);
            for (int y = 0; y < yHeight; y++)
            {
                for (int x = 0; x < _hiddenWidth; x++)
                {
                    pixelPos = (y * _hiddenWidth) + x;
                    tempPixel.r = _inputField[pixelPos];
                    tempPixel.g = tempPixel.r;
                    tempPixel.b = tempPixel.r;
                    predictionTexture.SetPixel(x, y + yHeight, tempPixel);
                }
            }
            predictionTexture.Apply();
        }

        if (useCanny)
        {
            _openCV.CannyEdgeDetection(_inputField, 50.0f, 50.0f * 3.0f);

            Color tempPixel = new Color(0.0f, 0.0f, 0.0f);
            for (int y = 0; y < yHeight; y++)
            {
                for (int x = 0; x < _hiddenWidth; x++)
                {
                    pixelPos = (y * _hiddenWidth) + x;
                    tempPixel.r = _inputField[pixelPos];
                    tempPixel.g = tempPixel.r;
                    tempPixel.b = tempPixel.r;
                    predictionTexture.SetPixel(x, y + yHeight, tempPixel);
                }
            }
            predictionTexture.Apply();
        }

        if (useSobel)
        {
            // Make sure that Sobel input and output uses a signed pixel data type,
            // e.g. convert after to 8-bit unsigned
            // sobelx64f = cv2.Sobel(img, cv2.CV_64F, 1, 0, ksize = 5)
            // abs_sobel64f = np.absolute(sobelx64f)
            // sobel_8u = np.uint8(abs_sobel64f)

            Texture2D horzTexture = ConvolutionFilter.Apply(predictionTexture, ConvolutionFilter.Sobel3x3Horizontal);
            Texture2D vertTexture = ConvolutionFilter.Apply(predictionTexture, ConvolutionFilter.Sobel3x3Vertical);

            Texture2D convolvedTexture = new Texture2D(_inputWidth, _inputHeight, predictionTexture.format, false);
            Color tempPixel = new Color(0.0f, 0.0f, 0.0f);

            for (int y = yOffset; y < yOffset + yHeight; y++)
            {
                for (int x = 0; x < _hiddenWidth; x++)
                {
                    Color horzPixel = horzTexture.GetPixel(x, y);
                    Color vertPixel = vertTexture.GetPixel(x, y);

                    tempPixel.r = Mathf.Sqrt((horzPixel.r * horzPixel.r) + (vertPixel.r * vertPixel.r));
                    tempPixel.g = tempPixel.r;// Mathf.Sqrt((horzPixel.g * horzPixel.g) + (vertPixel.g * vertPixel.g));
                    tempPixel.b = tempPixel.r;// Mathf.Sqrt((horzPixel.b * horzPixel.b) + (vertPixel.b * vertPixel.b));

                    convolvedTexture.SetPixel(x, (y - yOffset) + _hiddenHeight, tempPixel);

                    pixelPos = ((y - yOffset) * _hiddenWidth) + x;
                    _inputField[pixelPos] = (int)(tempPixel.r * 255.0f);
                }
            }

            predictionTexture.SetPixels(convolvedTexture.GetPixels());
            predictionTexture.Apply();
        }

        // Pass filtered image into the Line Segment Detector (optionally drawing found lines),
        // and construct the rotation SDR for passing into the hierarchy
        bool drawLines = true;
        _openCV.LineSegmentDetector(_inputField, _hiddenWidth, _hiddenHeight, 6, _rotationSDR, drawLines);

        if (drawLines)
        {
            // With drawLines enabled, the _inputField gets overriden with black background
            // pixels and detected white lines drawn ontop.

            // Transfer back into the predictionTexture for display (top half, bottom will show SDRs)
            Color tempPixel = new Color(0.0f, 0.0f, 0.0f);
            for (int y = yOffset; y < yOffset + yHeight; y++)
            {
                for (int x = 0; x < _hiddenWidth; x++)
                {
                    pixelPos = ((y - yOffset) * _hiddenWidth) + x;
                    tempPixel.r = _inputField[pixelPos];
                    tempPixel.g = tempPixel.r;
                    tempPixel.b = tempPixel.r;
                    predictionTexture.SetPixel(x, (y - yOffset) + _hiddenHeight, tempPixel);
                }
            }
            predictionTexture.Apply();
        }

        Color predictedPixel = new Color();

        // Plot pre-encoder SDR output just underneath the input filtered image
        int onState = 0;
        for (int y = 16; y < 32; y++)
        {
            for (int x = 0; x < _inputWidth; x++)
            {
                if (x < _rotationSDR.Count)
                {
                    predictedPixel.r = _rotationSDR[x];

                    if (y == 16)
                        onState += (int)predictedPixel.r;
                }
                else
                    predictedPixel.r = 0.0f;

                predictedPixel.g = predictedPixel.r;
                predictedPixel.b = predictedPixel.r;

                predictionTexture.SetPixel(x, y, predictedPixel);
            }
        }

        // Plot predicted SDR output at the bottom
        int ccState = 0;
        for (int y = 0; y < 16; y++)
        {
            for (int x = 0; x < _inputWidth; x++)
            {
                if (x < _rotationSDR.Count)
                {
                    predictedPixel.r = _predictedSDR[x];

                    if (y == 0)
                        ccState += _rotationSDR[x] & _predictedSDR[x];
                }
                else
                    predictedPixel.r = 0.0f;

                predictedPixel.g = predictedPixel.r;
                predictedPixel.b = predictedPixel.r;

                predictionTexture.SetPixel(x, y, predictedPixel);
            }
        }
        predictionTexture.Apply();

        _onStates.Add(onState);
        _ccStates.Add(ccState);

        // Trim lists?
        if (_onStates.Count > _maxNumStates)
        {
            _onStates.RemoveAt(0);
            _ccStates.RemoveAt(0);
        }

        NCC = 0.0f;
        for (int i = 0; i < _onStates.Count; i++)
        {
            if (_ccStates[i] == 0 && _onStates[i] == 0)
                NCC += 1.0f;
            else if (_onStates[i] == 0)
                NCC += 1.0f;
            else
                NCC += (float)_ccStates[i] / (float)_onStates[i];
        }
        NCC /= (float)_onStates.Count;

        // Encode scalar values from the car controller
        Steer = carController.CurrentSteerAngle / carController.m_MaximumSteerAngle;
        Accel = carController.AccelInput;
        Brake = carController.BrakeInput;

        //for (int i = 0; i < 6 * 6; i++)
        //    _inputValues[i] = 0;
        //int index = (int)((Steer * 0.5f + 0.5f) * (6 * 6 - 1) + 0.5f);
        //_inputValues[index] = 1;

        _inputValues[0] = (int)((Steer * 0.5f + 0.5f) * (6.0f * 6.0f - 1.0f) + 0.5f);

        // Setup the hierarchy input vector
        Std2DVeci input = new Std2DVeci();
        input.Add(_rotationSDR);
        input.Add(_inputValues);

        // Step the hierarchy
        _hierarchy.step(input, _system, Training);

        StdVeci predictions = _hierarchy.getPrediction(0);
        for (int i = 0; i < _predictedSDR.Count; i++)
            _predictedSDR[i] = predictions[i];

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

            // Update the car controller

            StdVeci steeringPredictions = _hierarchy.getPrediction(1);

            //int maxIndex = 0;
            //for (int i = 1; i < 6 * 6; i++)
            //    if (steeringPredictions[i] > steeringPredictions[maxIndex])
            //        maxIndex = i;
            //PredictedSteer = (float)(maxIndex) / (float)(6 * 6 - 1) * 2.0f - 1.0f;
            PredictedSteer = (steeringPredictions[0] / (6.0f * 6.0f - 1.0f)) * 2.0f - 1.0f;

            PredictedAccel = Accel;
            PredictedBrake = Brake;

            carSteer = PredictedSteer;
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

            // Assume +-2 units is maximum distance the car is allowed to be from the center spline
            NCC = Mathf.Max(0.0f, NCC - (1.0f - ((2.0f - Vector3.Distance(carPosition, spline.GetPoint(bestT))) / 2.0f)));
            //NCC = ((2.0f - Vector3.Distance(carPosition, spline.GetPoint(bestT))) / 2.0f);

            // Reset car position and direction?
            if (Input.GetKeyUp(KeyCode.R) || carController.Collided)
            {
                if (ForcePredictionMode == false)
                    Training = true;

                carController.ResetCollided();

                // Spline 0 is usually set as the spline used to create the track
                SplineIndex = 0;

                Vector3 position = spline.GetPoint(bestT);
                position.y = carController.gameObject.transform.localPosition.y;
                carController.gameObject.transform.localPosition = position;

                Vector3 splineDirection = spline.GetDirection(bestT).normalized;
                carController.gameObject.transform.forward = -splineDirection;
            }

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

                Vector3 targetPosition = spline.GetPoint(bestT + (SteerAhead / totalDistance));

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

                float steerBlend = 0.5f;
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

        if (connectToNeoVis && _neoVis != null)
        {
            _neoVis.update(0.01f);
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

        // Toggle training mode?
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
            _hierarchy.save(hierarchyFileName);
            print("Saved OgmaNeo hierarchy to " + hierarchyFileName);
        }
    }
}
