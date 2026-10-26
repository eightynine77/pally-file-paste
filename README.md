# pally file paste
a handy app for pasting images from clipboard into files and pasting clipboard images in bulk. available for linux, android, and windows.

# download
| windows  | android | linux
|---|---|---| 
|  [download](https://github.com/eightynine77/pally-file-paste/releases/tag/2026.10.6-v1.1.0_windows) |[download](https://github.com/eightynine77/pally-file-paste/releases/tag/v1.2.0) | [download](https://github.com/eightynine77/pally-file-paste/releases/tag/2026.10.5-v1.1.0_linux)

# note
for the linux version, do not expect 32-bit linux version of the app any time soon as the dotnet this app is running on is dotnet 10.0 and that version of dotnet doesn't support 32-bit linux. *feel free to contribute or fork this repository to add 32-bit linux support.*




# what the app looks like
### on android
<img src="https://raw.githubusercontent.com/eightynine77/pally-file-paste/refs/heads/main/images/android.png" width="251" height="509"/>

### on desktop (linux and windows)
<img src="https://raw.githubusercontent.com/eightynine77/pally-file-paste/refs/heads/main/images/desktop.png" width="558" height="312"/>

# compiling
if you want to get the latest feature soon, you'll have to compile this app's source code.  
to compile:
1. download and install the dotnet SDK 10.0
2. run this command (choose depending on your platform):  
for 64-bit android: `dotnet publish pallyFilePaste.Android/pallyFilePaste.Android.csproj -c Release -f net10.0-android -p:AndroidPackageFormat=apk -p:AndroidKeyStore=true -p:AndroidSigningKeyStore="C:\<PATH TO YOUR KEYSTORE FILE>\pallyFilePaste.keystore" -p:AndroidSigningKeyAlias=pallyFilePaste -p:AndroidSigningKeyPass=pallyFilePaste -p:AndroidSigningStorePass=pallyFilePaste -o "C:\<PATH TO YOUR OUTPUT FOLDER>" -p:RuntimeIdentifier=android-arm64`  
for 32-bit android: `dotnet publish pallyFilePaste.Android/pallyFilePaste.Android.csproj -c Release -f net10.0-android -p:AndroidPackageFormat=apk -p:AndroidKeyStore=true -p:AndroidSigningKeyStore="C:\<PATH TO YOUR KEYSTORE FILE>\pallyFilePaste.keystore" -p:AndroidSigningKeyAlias=pallyFilePaste -p:AndroidSigningKeyPass=pallyFilePaste -p:AndroidSigningStorePass=pallyFilePaste -o "C:\<PATH TO YOUR OUTPUT FOLDER>" -p:RuntimeIdentifier=android-arm`

note: for windows and linux, see the [Makefile](https://github.com/eightynine77/pally-file-paste/blob/main/Makefile) file. in that file, it contains commands to compile this app's source code into windows and linux app