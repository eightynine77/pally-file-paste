.PHONY: build clean deskrun andrun linpubreg linpubunbun winpub64 winpub86 fresh

build:
	dotnet build

clean:
	dotnet clean

deskrun:
	dotnet run --project pallyFilePaste.Desktop\pallyFilePaste.Desktop.csproj

andrun:
	dotnet run --project pallyFilePaste.Android/pallyFilePaste.Android.csproj

linpubreg:
	dotnet publish pallyFilePaste.Desktop/pallyFilePaste.Desktop.csproj -c Release -r linux-x64 --self-contained true -p:PublishTrimmed=true

linpubunbun:
	dotnet publish pallyFilePaste.Desktop/pallyFilePaste.Desktop.csproj -c Release -r linux-x64 --self-contained false

winpub64:
	dotnet publish pallyFilePaste.Desktop\pallyFilePaste.Desktop.csproj -c Release -r win-x64 --self-contained false

winpub86:
	dotnet publish pallyFilePaste.Desktop\pallyFilePaste.Desktop.csproj -c Release -r win-x86 --self-contained false 

fresh: clean build