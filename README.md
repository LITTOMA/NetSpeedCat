# Net Speed Cat
Display the right net speed at the right position on Windows 11.

<a href="https://www.microsoft.com/store/apps/9NVX14QXWWM1"><img src="get-it-from-MS.png" width="120"></a><br/>


# What is this?
I really like the way NetSpeedMonitor displays the internet speed on the taskbar via Desk Band. Unfortunately, Microsoft removed the Desk Band feature in Windows 11 (well done, Microsoft!).

I tried to find various alternatives, but none of them met my needs. Until I realised, hey, I'm a software developer too, why not develop one myself? And that's how Net Speed Cat became true.

# How to use
1. Install the app from the store or download it from the releases page.
2. Open the app.
3. Click the checkbox on the left of network interfaces you want to monitor.

### Optional:
You can make the app launch automatically when you start Windows.

* On Windows 11: Right click on the app icon in the tray area and select "Startup" to start the app at Windows startup.

* On Windows 10: Right click on the net speed area in the taskbar and select "Startup" to start the app at Windows startup.

# Build from source
1. Install dotnet sdk 5.0+.
2. Install git.
3. Open a command prompt and run the following commands:
```
git clone https://github.com/LITTOMA/NetSpeedCat.git
cd NetSpeedCat/src
dotnet publish -c Release -o ../bin/Release
```

# Buy me a coffee
Net Speed Cat is a free open source software, if it helps you, please support me by purchasing it on the [Microsoft Store](https://www.microsoft.com/store/apps/9NVX14QXWWM1).

<div align="center">
<img src="src/cat.jpg" width="160"/>
</div>