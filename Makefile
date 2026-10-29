.PHONY: build buildandroid builddesk clean deskrun andrun linpubreg linpubunbun winpub64 winpub86 fresh

build:
	dotnet build

# build for android
buildandroid:
	dotnet build pallyFilePaste.Android/pallyFilePaste.Android.csproj

# build for desktop (linux and windows)
builddesk:
	dotnet build pallyFilePaste.Desktop/pallyFilePaste.Desktop.csproj

clean:
	dotnet clean

# run the project on desktop (linux and windows)
deskrun:
	dotnet run --project pallyFilePaste.Desktop\pallyFilePaste.Desktop.csproj

# run the project on android emulator (with android studio's emulator or whatever android emulator you prefer)
andrun:
	dotnet run --project pallyFilePaste.Android/pallyFilePaste.Android.csproj

# compile the project for linux as a framework-dependent app. 
# meaning the app won't bundle the .NET runtime and the user has to install .NET runtime on their system
linpubbun:
	dotnet publish pallyFilePaste.Desktop/pallyFilePaste.Desktop.csproj -c Release -r linux-x64 --self-contained true -p:PublishTrimmed=true

# compile the project for linux as a self-contained app. 
# meaning the app will bundle .NET runtime and the user can start the app without installing the .NET runtime on their system
linpubunbun:
	dotnet publish pallyFilePaste.Desktop/pallyFilePaste.Desktop.csproj -c Release -r linux-x64 --self-contained false

# compile the project for windows as a 64-bit app. it compiles as a framework-dependent app by default
winpub64:
	dotnet publish pallyFilePaste.Desktop\pallyFilePaste.Desktop.csproj -c Release -r win-x64 --self-contained false

# compile the project for windows as a 32-bit app. it compiles as a framework-dependent app by default
winpub86:
	dotnet publish pallyFilePaste.Desktop\pallyFilePaste.Desktop.csproj -c Release -r win-x86 --self-contained false 

fresh: clean build
