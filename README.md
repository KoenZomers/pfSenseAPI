# pfSenseAPI
This project is still a basic setup and is meant to be able to pull data out of pfSense remotely. The makers of pfSense have announced that the next major release of pfSense, version 3.0, will contain native REST endpoints to communicate with which will make this way easier. For now we're stuck with HTTP Replays and parsing HTML to get the data we need. You can easily fork from my code and add further implementations for data you require from pfSense.

If you'd like me to add functionality to get specific data from pfSense, just send me an e-mail and I'll look into adding it.

## Version History

[1.1.0.0](https://www.nuget.org/packages/KoenZomers.pfSense.Api/1.1.0) - September 29, 2021

- Compiled against .NET 5.0 now
- Fixed GetLastMonthsDataUse and GetThisMonthsDataUse not working anymore
- Added SaveBackupAs and GetBackupContents to retrieve a backup from pfSense
- Removed dependency on Newtonsoft JSON

1.0.1.0 - August 16, 2017

- Transformed the framework to become asynchronous for all requests
- You'll need to manually call Authenticate() once to authenticate your pfSense session. This breaks backward compatibility but makes things more common in the future. The async stuff will break backwards compatibility anyway.
- Compiled against .NET 4.6.2 now

## System Requirements

This API is built using the Microsoft .NET 5.0 framework and is fully asynchronous

## Usage Instructions

To communicate with the pfSense API, add the NuGet package to your solution and add a using reference in your code:

```C#
using KoenZomers.pfSense.Api;
```

Then create a new session instance using:

```C#
var pfSense = new pfSense("https://192.168.0.1", "admin, "password");
```

Note that this line does not perform any communications with the pfSense API yet. You need to manually trigger authenticate before you can start using the session:

```C#
await pfsense.Authenticate();
```

Once this succeeds, you can call one of the methods on the session instance to retrieve data, i.e.:

```C#
// Gets this months data use
var dataUsage = await pfSense.GetThisMonthsDataUse();
```

Check out the UnitTest project in this solution for full insight in the possibilities and working code samples.

## Available via NuGet

You can also pull this API in as a NuGet package by adding "[KoenZomers.pfSense.Api](https://www.nuget.org/packages/KoenZomers.pfsense.Api)" or running:

Install-Package KoenZomers.pfSense.Api

Package statistics: https://www.nuget.org/packages/KoenZomers.pfsense.Api

## Current functionality

With this API at its current state you can:

- Authenticate to pfSense
- Get the RAW data from one of the pages on pfSense so you can parse it yourself
- Get this months data use
- Get lasts months data use
- Download backup of pfSense

## Opening in Visual Studio

1. Git clone the project
2. Copy the App.config.sample in the UnitTest project to App.config and fill it with the URL, username and password of your pfSense box
3. Run the Unit Tests to see if it works well

## Feedback

Any kind of feedback is welcome! Feel free to drop me an e-mail at koen@zomers.eu or [create a new issue](https://github.com/KoenZomers/pfSenseAPI/issues/new)
