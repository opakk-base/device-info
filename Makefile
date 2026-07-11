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
	dotnet build installer/GetDevice.wixproj -c Release

clean:
	dotnet clean src/GetDevice/GetDevice.csproj
	dotnet clean tests/GetDevice.Tests/GetDevice.Tests.csproj
	dotnet clean installer/GetDevice.wixproj
	rm -rf src/GetDevice/bin src/GetDevice/obj
	rm -rf tests/GetDevice.Tests/bin tests/GetDevice.Tests/obj

rebuild: clean build
