# IP-UnityAR

### System Requirements
Platform: Windows

Unity: 2019.3.4f1


### Installing Unity
- Download UnityHub and set up a personal license
- Install Unity 2019.3.4f1 with **Android Build support** and both submenu options, **Android SDK & NDK tools** and **OpenJDK**

### Running
- Open *SampleScene* from **UnityAR/Assets/Scenes**
- To run in the editor, just press the play button and it will use your default webcam
- To run on an Android phone, first set up USB Debugging on your device. After that, **File->Build Settings**, then choose *Android* as the platform. Select *Switch Platform*, and after loading, *Build And Run* with your phone connected

### USB Debugging on Android
Instructions may vary between phones, but the general idea should be the same, here is what works one a OnePlus 5T, Android 9:
- Plug your phone into your pc via USB, then execute any necessary driver setup
- Open settings on your phone, then **About Phone**, and tap **Build Number** lots of times (around 8-10). You are now a developer
- Go back to settings and int **System->Developer Options**. Scroll down and enable **USB Debugging**

### Troubleshooting
- If build succeeds but APK failed to install to device, make sure screen is on, USB Debugging is enabled and try uninstalling the old app build

### Important Notes
- Application.PersistentDataPath is "[Internal Storage]/Android/data/com.com.ProiectIP.AR.UnityAR/files"
- Currently to run on phone, you need to have a "sample.jpg" in phone storage at persistent data path mentioned above

### Useful Resources

Vuforia SDK: https://developer.vuforia.com/downloads/sdk

Trello: https://trello.com/b/3ms1vJAD/unity-ar

Adding Vuforia target at runtime (adding a detected page as a target): https://library.vuforia.com/articles/Solution/How-To-Access-and-Modify-Targets-at-Run-Time.html
