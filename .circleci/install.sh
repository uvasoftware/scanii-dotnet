#!/bin/bash

apt-get -qqy install wget libunwind8 gettext apt-transport-https gpg

# https://docs.microsoft.com/en-us/dotnet/core/linux-prerequisites?tabs=netcore2x

wget -qO- https://packages.microsoft.com/keys/microsoft.asc | gpg --dearmor > microsoft.asc.gpg
mv microsoft.asc.gpg /etc/apt/trusted.gpg.d/
wget -q https://packages.microsoft.com/config/debian/10/prod.list
mv prod.list /etc/apt/sources.list.d/microsoft-prod.list

apt-get update && apt-get -qqy install dotnet-sdk-3.1

echo "dotnet runtime information:"
dotnet --info
