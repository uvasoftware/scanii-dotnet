#!/usr/bin/env bash -x

dotnet build
dotnet test

echo $?

