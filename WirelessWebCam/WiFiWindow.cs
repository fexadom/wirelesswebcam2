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
        /// <summary>List of all wifi networks in range.</summary>
        private List wifiNetworksList;

        /// <summary>Initializes all wifi functionality elements.</summary>
        private void initializeWifiWindow()
        {
            Dropdown dd = (Dropdown)wifiWindow.GetChildByName("drop");
            dd.TapEvent += dd_TapEvent;
            dd.ValueChangedEvent += dd_ValueChangedEvent;

            Button connectWiFi = (Button)wifiWindow.GetChildByName("connect");
            connectWiFi.TapEvent += connectWiFi_TapEvent;

            Button disconnectWiFi = (Button)wifiWindow.GetChildByName("disconnect");
            disconnectWiFi.TapEvent += disconnectWiFi_TapEvent;

            Button backWiFi = (Button)wifiWindow.GetChildByName("back");
            backWiFi.TapEvent += backWiFi_TapEvent;
        }

        /// <summary>Rescans wifi networks and updates the GUI.</summary>
        private void updateWifiWindow()
        {
            if (!wifi.isConnected())
            {
                Debug.Print("Updating WiFi networks");
                WifiNetwork[] networks = wifi.getAllWiFiNetworks();

                Dropdown dd = (Dropdown)wifiWindow.GetChildByName("drop");

                dd.Options.Clear();
                foreach (WifiNetwork network in networks)
                {
                    dd.Options.Add(new object[] { network.getLabel(), network });
                }

                wifiNetworksList = new List(dd.Options, 300);
                wifiNetworksList.CloseEvent += wifiNetworksList_CloseEvent;
                wifiNetworksList.TapOptionEvent += wifiNetworksList_TapOptionEvent;

                Debug.Print("Updating WiFi networks done");
            }
            
        }

        void backWiFi_TapEvent(object sender)
        {
            switchState(State.Main);
        }

        /// <summary>Display a list with all nearby wifi networks.</summary>
        void connectWiFi_TapEvent(object sender)
        {
            if (!wifi.isConnected())
            {
                Dropdown dd = (Dropdown)wifiWindow.GetChildByName("drop");

                WifiNetwork network = (WifiNetwork)dd.Value;

                if ((network != null) && !network.isEmptyNetwork)
                {
                    Debug.Print("Connecting to: " + network.ToString());
                    wifi.connect(network);
                }
                else
                    Debug.Print("No network");
            }
        }

        void disconnectWiFi_TapEvent(object sender)
        {
            if (wifi.isConnected()) 
            {
                wifi.disconnect();
            }               
        }

        void wifiNetworksList_TapOptionEvent(object sender, TapOptionEventArgs args)
        {
            Debug.Print("wifiNetworksList_TapOptionEvent: " + args.Value.ToString());
            Glide.CloseList();
        }

        void wifiNetworksList_CloseEvent(object sender)
        {
            Glide.CloseList();
        }

        void dd_ValueChangedEvent(object sender)
        {
            Dropdown dropdown = (Dropdown)sender;
            Debug.Print("dd_ValueChangedEvent: " + dropdown.Text + " : " + dropdown.Value.ToString());
            wifiWindow.FillRect(dropdown.Rect);
            dropdown.Invalidate();
        }

        void dd_TapEvent(object sender)
        {
            Glide.OpenList(sender, wifiNetworksList);
        }
    }
}