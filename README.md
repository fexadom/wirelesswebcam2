# wirelesswebcam2
.Net Gadgeteer Wireless Webcam in C#

# Functionality
* Scans and connects to WiFi network
* Camera mode: Takes picture, uploads bitmap to Node.js backend server and displays a QR code image of the URI to download image.
* Webcam mode: Takes picture every few seconds, uploads bitmap to Node.js backen server.

# Node.js backend
* Files are in demo_Image_Processing_Nodejs.zip
* Receives and image via a POST request, compresses the image to PNG, serves the image with a GET request.

