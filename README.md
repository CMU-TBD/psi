# Platform for Situated Intelligence + TBD Components

This is a fork of [Platform for Situated Intelligence](https://github.com/microsoft/psi) with additional components + study tools for TBD Lab, CMU. We opted to fork instead of using Nuget package in order to directly edit Psi's internal component. A list of changes from main repo (Except for evertyhing in the TBD folder) is listed below.

## Changes from Psi Main Repo
* Microsoft.Psi.AzureKinect.x64:
	* Enable editting of AzureBody ID after creation. This lets us convert bodies from Kinect 2 to Azure Kinect. 