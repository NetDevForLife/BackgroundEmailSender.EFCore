# BackgroundEmailSender-EFCore
This application is an updated clone with .NET 5.x and Entity Framework Core of the homonymous application of the one developed by BrightSoul

## Notice
The bug indicated below (https://github.com/AngeloDotNet/BackgroundEmailSender-EFCore#attention) has been fixed, for a working version of this application, download the version 2 branch code.

## Thanks
I thank Moreno G. for providing me with the appropriate information so that I can solve the bug that occurred during the start of the project compilation.

## Attention
At the moment it occurs in the build phase the error is found here -> https://github.com/AngeloDotNet/BackgroundEmailSender-Test/blob/master/Startup.cs#L28

Error encountered while debugging with Visual Studio Code:
>Si è verificata un'eccezione: CLR/System.InvalidOperationException
Eccezione non gestita di tipo 'System.InvalidOperationException' in System.Private.CoreLib.dll: 'Cannot consume scoped service 'System.Collections.Generic.IEnumerable`1[Microsoft.Extensions.Hosting.IHostedService]' from singleton 'Microsoft.AspNetCore.Hosting.HostedServiceExecutor'.'

Also, starting the application with the dotnet run command sends email only when it starts and no further process of managing the email queue is performed in the background.
