

## Vmware VCloud Director .Net C# API examples

These are additional C# Samples for the .Net C# API

There are a large number of samples that ship with the vCloud SDK for .NET (download link below)
Samples are shown against a VMWare vCloud Air Target Environment

#### Dependencies:

A VMWare vCloud Air Virtual Data Center  (or VCloud Directory environment) and user credentials.

##### Visual Studio 2015 Community Edition
https://www.visualstudio.com/en-us/products/visual-studio-community-vs.aspx


##### vCloud SDK for .NET for vCloud Suite 5.5
http://developercenter.vmware.com/web/sdk/5.5.0/vcloud-dotnet
(May require myvmware registration)


###### Setup Notes

The following DLLs must be taken from the vCloud SDK and copied to to the same folder as the .snl file


-      VcloudRestSchema_V5_5.dll
-      VcloudSDK_V5_5.dll

######## Using Visual Studio
- Open the MyVCDSDKSamples.sln file
- Build the Solution

######## Build Issues
- References may need to be fixed if compile fails, View->Solution Explorer
  Under each Project, expand the reference node, 
  Expand the VcloudRestSchema_V5_5.dll node, fix the value of the Path property to point to the correct location of the dll.
  Repeat for the VcloudSDK_V5_5.dll node

######## Run each Project sample
- Open a cmd/console window to Run individual samples
- cd to the bin/debug folder of the project and run the .exe file

##### More info

#####Public Forums
Questions .Net SDK API Forum : http://developercenter.vmware.com/forums?id=3895

Questions  vCloud API Forum  : http://developercenter.vmware.com/forums?id=3221