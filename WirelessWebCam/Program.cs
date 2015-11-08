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
using GHI.Glide;
using GHI.Glide.Display;
using GHI.Glide.UI;
using Gadgeteer.Modules.GHIElectronics;

namespace WirelessWebCam
{
    // A delegate type for hooking up network up notifications.
    public delegate void NetworkUpEventHandler(WifiNetwork network);

    // A delegate type for hooking up network down notifications.
    public delegate void NetworkDownEventHandler(String msg);

    // A delegate type for hooking up network attempt notifications.
    public delegate void NetworkAttemptEventHandler();

    //System state enumaration
    enum State { Main, Camera, Webcam, Wifi, Calibrar };

    public partial class Program
    {
        /// <summary>The URL of the remote server.</summary>
        const String SERVER_URL = "200.126.23.246";
        /// <summary>The port where the remote server listens to REST requests.</summary>
        const String SERVER_PORT = "8080";

        //All main  GLIDE GUI windows used in this application
        private static GHI.Glide.Display.Window wifiWindow;
        private static GHI.Glide.Display.Window mainWindow;
        private static GHI.Glide.Display.Window cameraWindow;
        private static GHI.Glide.Display.Window cameraBackgroundWindow;
        private static GHI.Glide.Display.Window pictureWindow;
        private static GHI.Glide.Display.Window webcamWindow;
        private static GHI.Glide.Display.Window qrBackgroundWindow;
        private static GHI.Glide.Display.Window sleepWindow;

        //The Glide calibration application
        private static CalibrationWindow calibrationWindow;

        /// <summary>The wifi encapsulation object.</summary>
        private WiFiConfiguration wifi;

        /// <summary>Indicates whehter the camera is streaming video to the display.</summary>
        private bool isStreaming;

        /// <summary>Indicates whehter the webcam is ON.</summary>
        private bool isWebCamOn;

        /// <summary>The last bitmap returned by the camera.</summary>
        Bitmap currentBitmap;

        /// <summary>The current system state.</summary>
        State systemState;

        // This method is run when the mainboard is powered up or reset.   
        void ProgramStarted()
        {
            /*******************************************************************************************
            Modules added in the Program.gadgeteer designer view are used by typing 
            their name followed by a period, e.g.  button.  or  camera.
            
            Many modules generate useful events. Type +=<tab><tab> to add a handler to an event, e.g.:
                button.ButtonPressed +=<tab><tab>
            
            If you want to do something periodically, use a GT.Timer and handle its Tick event, e.g.:
                GT.Timer timer = new GT.Timer(1000); // every second (1000ms)
                timer.Tick +=<tab><tab>
                timer.Start();
            *******************************************************************************************/


            // Use Debug.Print to show messages in Visual Studio's "Output" window during debugging.
            Debug.Print("Program Started");

            //Initialize global bitmap buffer with the camera resolution
            currentBitmap = new Bitmap(camera.CurrentPictureResolution.Width, camera.CurrentPictureResolution.Height);

            isStreaming = false;
            isWebCamOn = false;

            //Main window stuff
            mainWindow = GlideLoader.LoadWindow(Resources.GetString(Resources.StringResources.mainWindow));
            initializeMainWindow();

            //Webcam stuff
            webcamWindow = GlideLoader.LoadWindow(Resources.GetString(Resources.StringResources.webcamWindow));
            initializeWebcamWindow();

            // Wifi Stuff
            wifiWindow = GlideLoader.LoadWindow(Resources.GetString(Resources.StringResources.wifiWindow));
            wifi = new WiFiConfiguration(wifiRS21);
            wifi.NetworkUp += wifi_NetworkUp;
            wifi.NetworkDown += wifi_NetworkDown;
            wifi.NetworkAttempt += wifi_NetworkAttempt;
            initializeWifiWindow();

            //QR stuf
            qrBackgroundWindow = GlideLoader.LoadWindow(Resources.GetString(Resources.StringResources.qrBackgroundWindow));
            qrBackgroundWindow.TapEvent += qrWindow_TapEvent;

            //Calibration stuff
            calibrationWindow = new CalibrationWindow(false, false);
            calibrationWindow.CloseEvent += OnCloseCalibrar;

            //Camera stuff
            cameraWindow = GlideLoader.LoadWindow(Resources.GetString(Resources.StringResources.cameraWindow));
            initializeCameraWindow();

            //Set bitmap streamed callback, used by camera mode and webcam mode
            camera.BitmapStreamed += camera_BitmapStreamed;
            
            //Battery stuff
            ucBattery4xAA.DebugPrintEnabled = true;

            GlideTouch.Initialize();

            switchState(State.Main);
        }

        /// <summary>State switch logic.</summary>
        private void switchState(State s)
        {
            switch (s)
            {
                case State.Main:
                    systemState = s;
                    Glide.MainWindow = mainWindow;
                    break;
                case State.Camera:
                    systemState = s;
                    Glide.MainWindow = cameraWindow;
                    break;
                case State.Webcam:
                    systemState = s;
                    Glide.MainWindow = webcamWindow;
                    break;
                case State.Wifi:
                    systemState = s;
                    Glide.MainWindow = wifiWindow;
                    updateWifiWindow();
                    break;
                case State.Calibrar:
                    systemState = s;
                    Glide.MainWindow = calibrationWindow;
                    break;
                default:
                    systemState = State.Main;
                    Glide.MainWindow = mainWindow;
                    break;
            }
        }

        /// <summary>Indicates what to do when the QR window is tapped.</summary>
        void qrWindow_TapEvent(object sender)
        {
            switch (systemState)
            {
                case State.Camera:
                    switchState(systemState);
                    break;
                case State.Webcam:
                    switchState(systemState);
                    break;
                default:
                    switchState(State.Main);
                    break;
            }
        }

        /// <summary>Bitmap streamed callback, depending on the system state the device sends the bitmap to the display.</summary>
        void camera_BitmapStreamed(Camera sender, Bitmap e)
        {
            switch (systemState)
            {
                case State.Camera:
                    if(isStreaming)
                        displayT35.SimpleGraphics.DisplayImage(e, 0, 0);
                    break;

                case State.Webcam:
                    camera.StopStreaming();
                    break;

                default:
                    break;
            }
        }

        private void OnCloseCalibrar(object sender)
        {
            switchState(State.Main);
        }

        /// <summary>Updates GUI in case of network down event.</summary>
        void wifi_NetworkDown(String msg)
        {
            TextBlock connectedToText = (TextBlock)wifiWindow.GetChildByName("connectedto");
            TextBlock wifiStatusMainText = (TextBlock)mainWindow.GetChildByName("wifistatus");
            TextBlock wifiStatusCameraText = (TextBlock)cameraWindow.GetChildByName("wifistatus");
            TextBlock wifiStatusWebcamText = (TextBlock)webcamWindow.GetChildByName("wifistatus");

            connectedToText.Text = msg;
            connectedToText.FontColor = GHI.Glide.Colors.Red;

            wifiStatusMainText.Text = "WIFI: OFF";
            wifiStatusMainText.FontColor = GHI.Glide.Colors.Red;

            wifiStatusCameraText.Text = "WIFI: OFF";
            wifiStatusCameraText.FontColor = GHI.Glide.Colors.Red;

            wifiStatusWebcamText.Text = "WIFI: OFF";
            wifiStatusWebcamText.FontColor = GHI.Glide.Colors.Red;

            switch (systemState)
            {
                case State.Main:
                    cameraWindow.FillRect(wifiStatusMainText.Rect);
                    wifiStatusMainText.Invalidate();
                    break;
                case State.Camera:
                    cameraWindow.FillRect(wifiStatusCameraText.Rect);
                    wifiStatusCameraText.Invalidate();
                    break;
                case State.Webcam:
                    webcamWindow.FillRect(wifiStatusWebcamText.Rect);
                    wifiStatusWebcamText.Invalidate();
                    break;
                case State.Wifi:
                    wifiWindow.FillRect(connectedToText.Rect);
                    connectedToText.Invalidate();
                    break;
            }

        }

        /// <summary>Updates GUI in case of network up event.</summary>
        void wifi_NetworkUp(WifiNetwork network)
        {
            TextBlock connectedToText = (TextBlock)wifiWindow.GetChildByName("connectedto");
            TextBlock wifiStatusMainText = (TextBlock)mainWindow.GetChildByName("wifistatus");
            TextBlock wifiStatusCameraText = (TextBlock)cameraWindow.GetChildByName("wifistatus");
            TextBlock wifiStatusWebcamText = (TextBlock)webcamWindow.GetChildByName("wifistatus");

            connectedToText.Text = "Conectado a: " + network.getLabel();
            connectedToText.FontColor = GHI.Glide.Colors.Green;

            wifiStatusMainText.Text = "WIFI: ON";
            wifiStatusMainText.FontColor = GHI.Glide.Colors.White;

            wifiStatusCameraText.Text = "WIFI: ON";
            wifiStatusCameraText.FontColor = GHI.Glide.Colors.White;

            wifiStatusWebcamText.Text = "WIFI: ON";
            wifiStatusWebcamText.FontColor = GHI.Glide.Colors.White;

            switch (systemState)
            {
                case State.Main:
                    cameraWindow.FillRect(wifiStatusMainText.Rect);
                    wifiStatusMainText.Invalidate();
                    break;
                case State.Camera:
                    cameraWindow.FillRect(wifiStatusCameraText.Rect);
                    wifiStatusCameraText.Invalidate();
                    break;
                case State.Webcam:
                    webcamWindow.FillRect(wifiStatusWebcamText.Rect);
                    wifiStatusWebcamText.Invalidate();
                    break;
                case State.Wifi:
                    wifiWindow.FillRect(connectedToText.Rect);
                    connectedToText.Invalidate();
                    break;
            }
        }

        /// <summary>Updates GUI in case of network attempt event.</summary>
        void wifi_NetworkAttempt()
        {
            TextBlock connectedTo = (TextBlock)wifiWindow.GetChildByName("connectedto");
            connectedTo.Text = "Conectando...";
            connectedTo.FontColor = GHI.Glide.Colors.White;

            if (systemState == State.Wifi)
            {
                wifiWindow.FillRect(connectedTo.Rect);
                connectedTo.Invalidate();
            }
        }

        /// <summary>Sends bitmap to remote server using a POST request.</summary>
        private void sendBitmapToCloud()
        {
            try
            {
                POSTContent content = POSTContent.CreateBinaryBasedContent(currentBitmap.GetBitmap());
                HttpRequest request = HttpHelper.CreateHttpPostRequest("http://"+SERVER_URL+":"+SERVER_PORT+"/uploadImage", content, "multipart/form-data");

                request.SendRequest();
                request.ResponseReceived += request_ResponseReceived;

                Debug.Print("Imagen enviada");
            }
            catch (System.ObjectDisposedException oe)
            {
                Debug.Print("Error in sendBitmapToCloud(): " + oe.Message);
            }
            
        }

        /// <summary>Callback for bitmap POST request.</summary>
        void request_ResponseReceived(HttpRequest sender, HttpResponse response)
        {
            Debug.Print("Response: "+response.StatusCode);
        }

        /// <summary>Sends a QR code image file request to remote server.</summary>
        public void showQR()
        {
            Glide.MainWindow = qrBackgroundWindow;

            var qrrequest = WebClient.GetFromWeb("http://" + SERVER_URL + ":" + SERVER_PORT + "/showqr");
            qrrequest.ResponseReceived += qrrequest_ResponseReceived;
            qrrequest.SendRequest();
        }


        /// <summary>Fetches QR code image file from remote server and shows it in the display</summary>
        void qrrequest_ResponseReceived(HttpRequest sender, HttpResponse response)
        {
            if (response.StatusCode == "200")
            {
                Debug.Print("Response OK");
                //This only works if the bitmap is the
                //same size as the screen it's flushing to
                displayT35.SimpleGraphics.DisplayImage(response.Picture.MakeBitmap(),0,0);
                displayT35.SimpleGraphics.DisplayText("Leer QR para descargar", Resources.GetFont(Resources.FontResources.NinaB), GT.Color.Black, 80, 220);
            }
            else
            {
                Debug.Print("QR Request failed with status code " + response.StatusCode);
            }
        }
    }
}
