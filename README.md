# BackgroundEmailSender-Test
Unfinished project, still under development and code optimization

## Attention
At the moment it occurs in the build phase the error is found here -> https://github.com/AngeloDotNet/BackgroundEmailSender-Test/blob/master/Startup.cs#L28

Error encountered:
>Exception of type 'System.InvalidOperationException' in Microsoft.Extensions.DependencyInjection.dll not handled in user code: 'Unable to resolve service for type' System.Threading.CancellationToken 'while attempting to activate' BackgroundEmailSenderSample.HostedServices.EmailSenderHostedService '.'
