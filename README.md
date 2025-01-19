# BassThatHz_ASIO_DSP
A Windows ASIO based DSP written in c#, based off of a pruned down NAudio branch.

Minimum OS requirements are:
Win 10 / 2012 R2, (newest updates) with .Net 9.0 installed
or
Win 11 / Server 2025, (newest updates) with .Net 9.0 installed

Direct Link to .Net 9.0 Desktop Runtime installer Win x64:
https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/runtime-desktop-9.0.0-windows-x64-installer

.Net 9.0 Desktop Runtime for Win x64 Downloads page:
https://dotnet.microsoft.com/en-us/download/dotnet/9.0

Minimum Hardware requirements are:
OS Minimum

Recommended Specs are:
As many CPU cores as you can get / need
(It is CPU bottlenecked when pushed to the extremes, one Core\HT\LPU per DSP channel is ideal, it uses the .Net threadpool system for threading.)
