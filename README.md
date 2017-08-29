<!---
 OgmaDrive
 Copyright (c) 2017 Ogma Intelligent Systems Corp. All rights reserved.
--->

# OgmaDrive

[![Join the chat at https://gitter.im/ogmaneo/Lobby](https://img.shields.io/gitter/room/nwjs/nw.js.svg)](https://gitter.im/ogmaneo/Lobby)

## Introduction

OgmaDrive performs [online machine learning](https://en.wikipedia.org/wiki/Online_machine_learning) using a `predictive hierarchy` applied to `vehicle driving assistance` ([ADAS](https://en.wikipedia.org/wiki/Advanced_driver-assistance_systems)). 

The predictive hierarchy requires only a front-facing camera and steering angle as input. Which it then learns from, and predicts the next desired steering angle.

Further information on `Online Predictive Hierarchies` can be seen in our arXiv.org paper: [Feynman Machine: The Universal Dynamical Systems Computer](http://arxiv.org/abs/1609.03971). And blog posts on our website: https://ogma.ai/category/ogmaneo/

## Overview

OgmaDrive uses the [Unity](https://unity3d.com/) engine, with custom C# scripts that create and use predictive hierarchies from two similar implementations. An initial menu allows the user to choose between the two predictive hierarchy implementations:

- [EOgmaNeo](https://github.com/ogmacorp/EOgmaNeo/) (CPU only), or
- [OgmaNeo](https://github.com/ogmacorp/OgmaNeo/) (CPU/GPU via OpenCL)

Two Unity scenes contain the implementations: `Assets/EOgmaNeo.unity` and `Assets/OgmaNeo.unity`. Both implementations provide a hands-free C# car controller script that follows a central spline around a procedurally generated track, that is used to initially teach the predictive hierarchy. After the _first_ lap, hierarchy prediction confidence metrics are used to determine when to alternate between further training or taking control and autonomously drive the vehicle. Use only the front-facing camera as input to the hierarchy that then predicts the steering angle based on it's acquired knowledge.

The [EOgmaNeo](https://github.com/ogmacorp/EOgmaNeo/) version has been optimized the most for this self-driving task, and is a SoC/Embedded system ready version. We currently use and test this implementation on a Raspberry Pi3 controlling an R/C car. Refer to the [EOgmaDrive](https://github.com/ogmacorp/EOgmaDrive/) repository for further details. To see the R/C car in action using EOgmaNeo refer to the following YouTube video:

<a href="http://www.youtube.com/watch?feature=player_embedded&v=0ibVhtuQkZA
" target="_blank"><img src="http://img.youtube.com/vi/0ibVhtuQkZA/0.jpg" 
alt="Self Driving Car Learns Online and On-board on Raspberry Pi 3" width="480" height="360" border="1"/></a><div>https://www.youtube.com/watch?v=0ibVhtuQkZA

A third Unity scene, `Assets/OgmaDrive.unity`, is used as a main menu to allow a choice of implementation when packaged and used with a Unity player as a standalone application. The standalone pre-built version of OgmaDrive for Windows and Mac OSX can be downloaded from this [Github repo](https://github.com/ogmacorp/OgmaDriveApp/releases).

## Implementation details

C# car controller scripts can be found in the `Assets/Scripts/` directory, called `EOgmaNeoCarController.cs` and `OgmaNeoCarController.cs`. These car controller scripts, and associated game objects, can be found in the EOgmaNeo and OgmaNeo Unity scenes as children of the main `StockCar` game object (found via the Unity Hierarchy panel).

Unity C# scripts are used to edit and define a closed-loop spline that procedurally generates a track and barriers.

- When in training mode this central track spline is used to determine appropriate steering values to allow the car to follow the spline.
- When in prediction mode the resulting predicted steering value from the hierarchy is used to steer the car autonomously.

Built-in Unity API calls are used to grab per frame images from a front-facing camera attached to the car. Pre-encoding then takes place to process and prepare the current steering angle and this camera image before deliver to the predictive hierarchy.

### Training versus Prediction mode

Taking inspiration from Dean A. Pomerleau's work on IRRE ("_Input Reconstruction Reliability Estimation_") for determining the response reliability of a restricted class of multi-layer perceptrons, a confidence metric is determined from the hierarchy predictions. This metric is shown as a NCC percentage value and plotted to a central graph.

The NCC value is used, *after the first lap*, to determine whether to only use steering predictions (**NCC >85%**), or whether to continue/revert to training (**NCC <15%**). During the first few laps training and prediction is expected to fluctuate. This actually helps the predictive hierarchy to discover more information about the driving around the track, and produces more accurate, confident, and consistent predictions for steering values.

If the car's speed drops below 2.0 units the training mode is also re-enabled. This typically occurs when high frequency steering fluctuations occur.

During the first handful of laps it is expected that during prediction mode the car can drift towards a barrier. Therefore training is re-enabled if the car comes too close to the barrier.

### Pre-encoding

Similar pre-processing (pre-encoding) takes place before information is sent into the predictive hierarchy. Differences between the two car controller pre-encoding implementations are described below.

#### EOgmaNeo car controller

A central part of the camera image is extracted and converted from RGB to Y'uv space. This is then passed into a C++ and OpenCV based pre-encoder within the EOgmaNeo library.

An OpenCV [Line Segment Detector](http://www.ipol.im/pub/art/2012/gjmr-lsd/) detects certain length lines, that are then 'chunked' into a sparse representation (SDR). This representation is drawn below the right hand half-height image (with lines detected superimposed), and below that is drawn the predicted representation from the hierarchy.

Different combinations and forms of filtering, thresholding, and feature detection have been explored before settling on the LSD detector and sparse chunked representation.

A history buffer of the input and predicted representations are collected over a number of frames and used in calculating the NCC confidence value.

#### OgmaNeo car controller

The camera image is converted from RGB space into Y'uv space, before the luminance (Y channel) is passed through a Sobel edge detection filter. This filtered version is then passed into an OgmaNeo predictive hierarchy (formed from distance and chunk encoders/decoders), along with the current steering value.

Unlike the EOgmaNeo predictive hierarchy, the OgmaNeo hierarchy is capable of predicting not just the next steering value but also the next camera image. This predicted camera image output is used to form the NCC value (normalized cross correlation percentage), and hence determine whether the hierarchy is confident in it's predictions and should be in predicted driving mode. Or less confident and should be in training mode.

### Screen overlays

Both implementations provide overlays containing pertinent information:

- Left:
  - Pre-filtered front-facing camera image
  - Graph of Training % vs. Prediction % (per lap)
- Middle:
  - General information, including current mode
  - Confidence graph (**>85%** toggles prediction only driving, **<15%** reverts back to training)
- Right:
  - For **OgmaNeo:** Predicted hierarchy output image
  - For **EOgmaNeo:** Detected line segments, and Pre-encoder SDR vs. predicted SDR
  - Graph of predicted steering value

### Hierarchy serialization

Both implementations allow for hierarchy state to be saved out and reloaded back in. When the Unity simulator is running the `O` key can be pressed to save out the current state of a hierarchy. **Note:** Saving a hierarchy pauses the simulator for quite a few seconds.

To reload a saved hierarchy, a check box in the EOgmaNeoCarController or OgmaNeoCarController game object inspector panel can be enabled to perform a reload upon commencement of a new simulator run.

A text box contains the filename used for saving/reloading a hierarchy.

### NeoVis hierarchy visualisation

The [EOgmaNeo](https://github.com/ogmacorp/EOgmaNeo/) library contains an SFML (and ImGui) visualization tool called [NeoVis](https://github.com/ogmacorp/NeoVis/). In the EOgmaNeoCarController script and game object a checkbox (boolean) can be enabled that will start the NeoVis client code. This introduces a slight delay when the Unity EOgmaNeo scene is running, until the NeoVis application has connected to the EOgmaNeo car controller script. The NeoVis application is a great way of discovering what is happening within an EOgmaNeo hierarchy when the simulation is running.

For NeoVis to connect to the EOgmaNeo client-side code, the EOgmaNeo Unity scene needs to be started first (hitting the play button in Unity). Then the NeoVis `Connection Wizard` can be used to `Connect!` to the EOgmaNeo car controller script.

## Tutorial

For a more detailed description of how OgmaDrive works within Unity and interacts with OgmaNeo and EOgmaNeo libraries, see the [TUTORIAL.md](TUTORIAL.md) files. The following video show the EOgmaNeo version running inside Unity:

**Note:** Closed captions / subtitles provide an explanation during the following video. 

<a href="https://www.youtube.com/embed/MpzHAjeRFhU?cc_load_policy=1" target="_blank"><img src="http://img.youtube.com/vi/MpzHAjeRFhU/0.jpg" alt="OgmaDrive (EOgmaNeo version)" width="480" height="360" border="1"/></a><div>https://www.youtube.com/watch?v=MpzHAjeRFhU?cc_load_policy=1

## Contributions

Refer to the [CONTRIBUTING.md](https://github.com/ogmacorp/OgmaDrive/blob/master/CONTRIBUTING.md) file for information on making contributions to OgmaDrive.

## License and Copyright

<a rel="license" href="http://creativecommons.org/licenses/by-nc-sa/4.0/"><img alt="Creative Commons License" style="border-width:0" src="https://i.creativecommons.org/l/by-nc-sa/4.0/88x31.png" /></a><br />The work in this repository is licensed under the <a rel="license" href="http://creativecommons.org/licenses/by-nc-sa/4.0/">Creative Commons Attribution-NonCommercial-ShareAlike 4.0 International License</a>.See the  [OGMADRIVE_LICENSE.md](https://github.com/ogmacorp/OgmaDrive/blob/master/OGMADRIVE_LICENSE.md) and [LICENSE.md](https://github.com/ogmacorp/OgmaDrive/blob/master/LICENSE.md) file for further information.

Contact Ogma via licenses@ogmacorp.com to discuss commercial use and licensing options.

The OgmaNeo library uses the Google [FlatBuffers](http://google.github.io/flatbuffers/) package that is licensed with an Apache License (Version 2.0). Refer to this [LICENSE.txt](https://github.com/google/flatbuffers/blob/master/LICENSE.txt) file for the full licensing text associated with the FlatBuffers package.

Jasper Flick's [Catlike Coding](http://catlikecoding.com/unity/tutorials/) Unity C# scripts are used for handling spline creation and manipulation.

OgmaDrive Copyright (c) 2016 [Ogma Intelligent Systems Corp](https://ogmacorp.com). All rights reserved.
