# simplewmqttbroker
A MQTT Broker module for Crestron 3-Series Control Systems written in SIMPL#

# How to use
:warning: **FOR TLS VERSION - SSL MUST BE ENABLED ON THE CONTROL SYSTEM**

![alt text](SSL.png " The default settings work fine as well.")

Fill the information required for the connection as in the image
**THE WSS True/False choice is useless** <br /> 
**Broker doesn't support reconnection. Clients must connect with clean session true** <br /> 
**QoS 2 and 3 are not supported**
![alt text](Example.png "Example")



## Donation
If this module saves you time feel free to show your appreciation using the following button :D  

[![paypal](https://www.paypalobjects.com/en_US/IT/i/btn/btn_donateCC_LG.gif)](https://www.paypal.com/donate?hosted_button_id=W8J2B4E92NEQ2)
