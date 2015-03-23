using System;
using System.Collections;
using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.Presentation;
using Microsoft.SPOT.Presentation.Controls;
using Microsoft.SPOT.Presentation.Media;
using Microsoft.SPOT.Presentation.Shapes;
using Microsoft.SPOT.Touch;

using Gadgeteer.Networking;
using GT = Gadgeteer;
using GTM = Gadgeteer.Modules;
using Gadgeteer.Modules.GHIElectronics;
using GHI.Glide;
using GHI.Glide.Display;
using GHI.Glide.UI;

namespace WirelessWebCam
{
    public partial class Program
    {
        /// <summary>Initializes Main window elements.</summary>
        private void initializeMainWindow()
        {
            Button cameraModeButton = (Button)mainWindow.GetChildByName("cameraMode");
            cameraModeButton.TapEvent += cameraModeButton_TapEvent;

            Button webcamModeButton = (Button)mainWindow.GetChildByName("webcamMode");
            webcamModeButton.TapEvent += webcamModeButton_TapEvent;

            Button calibrateButton = (Button)mainWindow.GetChildByName("calibrate");
            calibrateButton.TapEvent += calibrateButton_TapEvent;

            Button wifiButton = (Button)mainWindow.GetChildByName("wifi");
            wifiButton.TapEvent += wifiButton_TapEvent;
        }

        void cameraModeButton_TapEvent(object sender)
        {
            Debug.Print("Camera mode");
            switchState(State.Camera);
        }

        void webcamModeButton_TapEvent(object sender)
        {
            Debug.Print("Webcam mode");
            switchState(State.Webcam);
        }

        void calibrateButton_TapEvent(object sender)
        {
            Debug.Print("Calibration mode");
            switchState(State.Calibrar);
        }

        void wifiButton_TapEvent(object sender)
        {
            Debug.Print("Wifi mode");
            switchState(State.Wifi);
        }
    }
}