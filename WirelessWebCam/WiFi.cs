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
    /*******************************************************************************************
    This class encapsulates all WiFi functionality
     * Created by: Federico Domínguez
    *******************************************************************************************/
    public class WiFiConfiguration
    {
        private bool connected;
        private WifiNetwork network;

        /// <summary>The WiFi RS21 module using socket 9 of the mainboard.</summary>
        private Gadgeteer.Modules.GHIElectronics.WiFiRS21 wifiRS21;

        // An event that clients can use to be notified whenever the
        // network is up.
        public event NetworkUpEventHandler NetworkUp;

        // An event that clients can use to be notified whenever the
        // network is down.
        public event NetworkDownEventHandler NetworkDown;

        // An event that clients can use to be notified whenever the
        // the wifi is attempting to connect.
        public event NetworkAttemptEventHandler NetworkAttempt;

        public WiFiConfiguration(Gadgeteer.Modules.GHIElectronics.WiFiRS21 wifiRS21)
        {
            connected = false;
            this.wifiRS21 = wifiRS21;

            wifiRS21.NetworkUp += wifiRS21_NetworkUp;
            wifiRS21.NetworkDown += wifiRS21_NetworkDown;

            //Open the interface
            if (wifiRS21.NetworkInterface.Opened)
                wifiRS21.NetworkInterface.Close();
            if (!wifiRS21.NetworkInterface.Opened)
                wifiRS21.NetworkInterface.Open();
        }

        public bool isConnected()
        {
            return connected;
        }

        /// <summary>Returns an array of all scanned networks, encapsulated using the WifiNetwork class.</summary>
        public WifiNetwork[] getAllWiFiNetworks()
        {
            WifiNetwork[] networks;

            GHI.Networking.WiFiRS9110.NetworkParameters[] parameters = wifiRS21.NetworkInterface.Scan();

            if (parameters != null)
            {
                networks = new WifiNetwork[parameters.Length];
                for (int i = 0; i < parameters.Length; i++)
                    networks[i] = new WifiNetwork(parameters[i]);
            }
            else
            {
                networks = new WifiNetwork[1];
                networks[0] = new WifiNetwork();
            }


            return networks;
        }

        public void connect(WifiNetwork network)
        {
            //Notify that the module will attempt to connect to a wifi network
            NetworkAttempt();
            
            if (wifiRS21.NetworkInterface.Opened)
                wifiRS21.NetworkInterface.Close();
            if (!wifiRS21.NetworkInterface.Opened)
                wifiRS21.NetworkInterface.Open();
            if (!wifiRS21.NetworkInterface.IsDhcpEnabled)
                wifiRS21.NetworkInterface.EnableDhcp();

            try
            {
                if (network.isKnown)
                {
                    Debug.Print("Network: " + network.parameters.Ssid + " with password: " + network.parameters.Key);
                    try {
                        wifiRS21.NetworkInterface.Join(network.parameters.Ssid, network.parameters.Key);
                    }
                    catch (System.InvalidOperationException e)
                    {
                        //Account for strange error when you have several networks with the same SSID
                        Debug.Print("Exception: " + e.Message + " rescanning...");

                        //Try again to rescan that network
                        GHI.Networking.WiFiRS9110.NetworkParameters[] parameters = wifiRS21.NetworkInterface.Scan(network.parameters.Ssid);

                        if (parameters != null)
                        {
                            //Just select network with largest Rssi
                            int largest = 0;
                            int bigRssid = parameters[0].Rssi;
                            for (int i = 0; i < parameters.Length; i++)
                                if (bigRssid < parameters[i].Rssi)
                                    largest = i;

                            WifiNetwork n = new WifiNetwork(parameters[largest]);
                            wifiRS21.NetworkInterface.Join(n.parameters);
                        }else
                        {
                            Debug.Print("No encontro la red...");
                            connected = false;
                            NetworkDown("Error de coneccion");
                        }
                    }
                }
                else
                    wifiRS21.NetworkInterface.Join(network.parameters.Ssid);
            }
            catch (GHI.Networking.WiFiRS9110.JoinException e)
            {
                Debug.Print("Error de coneccion: " + e.Message);
                connected = false;
                NetworkDown("Error de coneccion");
            }
            catch (GHI.Networking.WiFiRS9110.HardwareFailureException e)
            {
                Debug.Print("Error de coneccion: " + e.Message);
                connected = false;
                NetworkDown("Error de coneccion");
            }
            catch (System.InvalidOperationException e)
            {
                Debug.Print("Error de coneccion: " + e.Message);
                connected = false;
                NetworkDown("Error de coneccion");
            }

            this.network = network;
        }

        public void disconnect()
        {
            if (connected)
            {
                wifiRS21.NetworkInterface.Disconnect();
                connected = false;
                NetworkDown("Desconectado");
            }
            
        }

        void wifiRS21_NetworkDown(GTM.Module.NetworkModule sender, GTM.Module.NetworkModule.NetworkState state)
        {
            Debug.Print("Network down");
            connected = false;
            NetworkDown("Desconectado");
        }

        void wifiRS21_NetworkUp(GTM.Module.NetworkModule sender, GTM.Module.NetworkModule.NetworkState state)
        {
            Debug.Print("Network up");
            connected = true;
            NetworkUp(network);
        }

        public String connectedTo()
        {
            if (connected)
                return network.getLabel();
            else
                return "No hay red disponible";
        }
    }

    /*******************************************************************************************
    This class encapsulates information about an scanned wifi network
     * Created by: Federico Domínguez
    *******************************************************************************************/
    public class WifiNetwork
    {
        public GHI.Networking.WiFiRS9110.NetworkParameters parameters { get; private set; }

        public bool isEmptyNetwork { get; private set; }
        public bool isKnown { get; private set; }

        /// <summary>A hashtable with known Wifi network passwords.</summary>
        static private Hashtable passwords = new Hashtable();

        // Static constructor to initialize the static passwords hashtable.
        // It is invoked before the first instance constructor is run.
        static WifiNetwork()
        {
            //Add passwords for known networks
            passwords.Add("CTI", "ct1esp0l15");
            passwords.Add("CTI_DOMO", "ct1esp0l15");
        }

        public WifiNetwork(GHI.Networking.WiFiRS9110.NetworkParameters parameters)
        {
            this.parameters = parameters;
            this.isEmptyNetwork = false;

            if (passwords.Contains(parameters.Ssid))
            {
                isKnown = true;
                parameters.Key = (String)passwords[parameters.Ssid];
            }else
                isKnown = false;
        }

        /// <summary>Creates an empty network, used to represent zero scanned networks.</summary>
        public WifiNetwork()
        {
            this.isEmptyNetwork = true;
        }

        public String getLabel()
        {
            if (isEmptyNetwork)
                return "No hay ninguna red";
            else
                return parameters.Ssid + " -" + parameters.Rssi + "dB";
        }

        public override String ToString()
        {
            return parameters.Ssid;
        }

    }
}