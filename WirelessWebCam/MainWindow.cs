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
        private void initializeMainWindow()
        {
            Button viewStreamingButton = (Button)mainWindow.GetChildByName("viewStreaming");
            viewStreamingButton.TapEvent += viewStreamingButton_TapEvent;

            Button startWebcamButton = (Button)mainWindow.GetChildByName("startWebcam");
            startWebcamButton.TapEvent += startWebcamButton_TapEvent;

            Button stopWebcamButton = (Button)mainWindow.GetChildByName("stopWebcam");
            stopWebcamButton.TapEvent += stopWebcamButton_TapEvent;

            Button calibrateButton = (Button)mainWindow.GetChildByName("calibrate");
            calibrateButton.TapEvent += calibrateButton_TapEvent;

            Button wifiButton = (Button)mainWindow.GetChildByName("wifi");
            wifiButton.TapEvent += wifiButton_TapEvent;
        }

        void viewStreamingButton_TapEvent(object sender)
        {
            Debug.Print("View Streaming");

            isStreaming = true;
            camera.StartStreaming();

            Glide.MainWindow = cameraWindow;
        }

        void startWebcamButton_TapEvent(object sender)
        {
            if (wifi.isConnected() && !isWebCamOn)
            {
                Debug.Print("Habilitando webcam");
                captureTimer.Start();
                isWebCamOn = true;

                TextBlock webcamstatus = (TextBlock)mainWindow.GetChildByName("webcamstatus");
                webcamstatus.Text = "Webcam ON";
                mainWindow.FillRect(webcamstatus.Rect);
                webcamstatus.Invalidate();
            }
        }

        void stopWebcamButton_TapEvent(object sender)
        {
            if (wifi.isConnected() && isWebCamOn)
            {
                Debug.Print("Deshabilitando webcam");
                captureTimer.Stop();
                isWebCamOn = false;

                TextBlock webcamstatus = (TextBlock)mainWindow.GetChildByName("webcamstatus");
                webcamstatus.Text = "Webcam OFF";
                mainWindow.FillRect(webcamstatus.Rect);
                webcamstatus.Invalidate();
            }
        }

        void calibrateButton_TapEvent(object sender)
        {
            Glide.MainWindow = calibrationWindow;
        }

        void wifiButton_TapEvent(object sender)
        {
            Glide.MainWindow = wifiWindow;
            updateWifiWindow();
        }
    }
}