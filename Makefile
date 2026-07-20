.PHONY: all build test run release msi clean rebuild

CONFIG ?= Debug

all: build

build:
	dotnet build src/GetDevice/GetDevice.csproj -c $(CONFIG)

test:
	dotnet test tests/GetDevice.Tests/GetDevice.Tests.csproj

run: build
	dotnet run --project src/GetDevice/GetDevice.csproj -c $(CONFIG)

release:
	dotnet build src/GetDevice/GetDevice.csproj -c Release
	dotnet test tests/GetDevice.Tests/GetDevice.Tests.csproj -c Release

msi:
	powershell -Command "$$v = (Get-Content version.json | ConvertFrom-Json).version; dotnet publish src/GetDevice/GetDevice.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true; dotnet build installer/GetDevice.wixproj -c Release -p:Version=$$v.0"

clean:
	dotnet clean src/GetDevice/GetDevice.csproj
	dotnet clean tests/GetDevice.Tests/GetDevice.Tests.csproj
	dotnet clean installer/GetDevice.wixproj
	rm -rf src/GetDevice/bin src/GetDevice/obj
	rm -rf tests/GetDevice.Tests/bin tests/GetDevice.Tests/obj

rebuild: clean build
