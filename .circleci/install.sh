#!/bin/bash

# https://docs.microsoft.com/en-us/dotnet/core/linux-prerequisites?tabs=netcore2x

apt-get -qqy install curl libunwind8 gettext apt-transport-https gpg
curl https://packages.microsoft.com/keys/microsoft.asc | gpg --dearmor > microsoft.gpg
mv microsoft.gpg /etc/apt/trusted.gpg.d/microsoft.gpg
sh -c 'echo "deb [arch=amd64] https://packages.microsoft.com/repos/microsoft-debian-stretch-prod stretch main" > /etc/apt/sources.list.d/dotnetdev.list'

apt-get update && apt-get -qqy install dotnet-sdk-2.0.0

echo "dotnet runtime information:"
dotnet --info
