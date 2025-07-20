## 6.0.1

* Added Readme to NuGet package

## 6.0.0
* Added support for a new CA1 region
* Now requires .NET 8 or later

## 5.1.0
* Support [v2.2](https://uvasoftware.github.io/openapi/v22/) of the API, thank you @enidvangils-cer

## 5.0.1
* Just a deployment fix 

## 5.0.0
* Improved credentials validation 
* Modified the client interface to take in an Authentication Token instead of an arbitrary string
* Dotnet 6 support

## 4.2.1
* Rolling back Microsoft.Extensions.Logging.Abstractions to 2.1.1

## v4.1.1
* Deploy pipeline fixes

## v4.1.0
* Downgraded Microsoft.Extensions.Logging.Abstraction on netstandard2 and .net 461 for better compatibility with .net core MVC. Fixes https://github.com/uvasoftware/scanii-dotnet/issues/23

## v4.0.2
* Dropped RestSharp in favor of native HttpClient and made HttpClient configurable
* Dropped Serilog in favor of MS.Extensions.Logging.Abstraction (https://github.com/uvasoftware/scanii-dotnet/issues/17)
* Extracted the main class into an interface IScaniiClient
* Added Stream support (https://github.com/uvasoftware/scanii-dotnet/issues/16)
* Extended tests suite including multiple .net runtimes and OSs

