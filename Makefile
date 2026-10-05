.PHONY: build clean fresh

build:
	dotnet build

clean:
	dotnet clean

deskrun:
	dotnet run --project pallyFilePaste.Desktop\pallyFilePaste.Desktop.csproj

andrun:
	dotnet run --project pallyFilePaste.Android/pallyFilePaste.Android.csproj

fresh: clean build