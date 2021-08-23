# BackgroundEmailSender-Test
Unfinished project, still under development and code optimization

## Attention
At the moment it occurs in the build phase the error is found here -> https://github.com/AngeloDotNet/BackgroundEmailSender-Test/blob/master/Startup.cs#L28

Error encountered while debugging with Visual Studio Code:
>Si è verificata un'eccezione: CLR/System.InvalidOperationException
Eccezione non gestita di tipo 'System.InvalidOperationException' in System.Private.CoreLib.dll: 'Cannot consume scoped service 'System.Collections.Generic.IEnumerable`1[Microsoft.Extensions.Hosting.IHostedService]' from singleton 'Microsoft.AspNetCore.Hosting.HostedServiceExecutor'.'

>It also sends emails only when the application is started, subsequently no email queue management process is performed in the background.