# FaceDetectionTrip
+ FaceDetection Program for use in the exhibition **Trip**
+ UI resources are copyrighted by Tinygem
+ You can **ignore** the parts related to Reactor and Stable Diffusion
## Overview
#### The program does three things 
1. Face detection: **detects human face** and takes a photo (screenshot)
   + Related scripts : `FaceDetectorScene.cs`, `FaceProcessor.cs`, `WebCamera.cs`
2. Model generation: **create a model** based on the photo.
   + Related scripts : `SocketClient.cs`, `SocketRequester.cs`, `RunAbleThread.cs` and **Facemesh ZIP file** (another project)
3. Cloud server communication: upload/download/delete photos and generated models **in server storage**
   + Related scripts : `FirebaseManager.cs`
4. ~Reactor(SD) synthesis (not used) : The development was completed, but the plan changed and it is not used.~
   + Related scripts : `SDSettings.cs`, `StableDiffusionReactor.cs`
#### Test Environment
+ Used Cam(Test) : Logitech C170
+ Used Local PC : Surface Pro7 (vertical)
#### I used this projects
+ OpenCVForUnity : [Original Github](https://github.com/EnoxSoftware/OpenCVForUnity) (Use to faceDetection)
+ Mediapipe-facemesh : [Original Github](https://github.com/apple2373/mediapipe-facemesh) (Use to model generation)
  > Thank you for allowing to use project! Special Thanks to **apple2373**
+ Unity3D-Python-Communication : [Original Github](https://github.com/off99555/Unity3D-Python-Communication)    
  (Use to socket communication between Unity-Python)
+ Firebase : [Official Guide](https://firebase.google.com/docs/storage/unity/start)
+ ~(Not used) Reactor in Unity : [Original Github](https://github.com/WooChan-Noh/SDReactorUnity)~
##### Preparation
+ You should have a folder `Photo`, `Facemesh` on your **Desktop**
  + `Facemesh` folder is ZIP file in this Project
+ Change Firebase URL (in `FirebaseManager.cs`)
+ Before use Facemesh, install python3.8.10 and check requirements.txt
+ Before you can run your Unity Editor or built app, you need to run `facemeshToObj.py` using the CLI. (in `facemesh` folder)
  > Here's why : Check Issue and TroubleShooting - [Original Github](https://github.com/off99555/Unity3D-Python-Communication)
+ Check your webcam connect in `WebCamera.cs`
+ Need Newton pakacge in Unity
## Learn more
+ The original plan was included to use photos and Reactor to create AI photos and upload them to the server, but that was canceled.
+ Facemesh generate three outputs : (texture(`.jpg`),`.mtl`,`.obj`)
   + Those files must be in the **same** space, and if they are not named **Facemesh** _(Be careful with case)_, they will not be connected.
   + If you want to fix this problem, check out `facemeshToObj.py` 
###### WebCamera.cs   
+ This project used on a **Surface Pro 7** (vertical). So the camera texture is rotated 90 degrees.
+ Render texture proportions are also forced to match the proportions of the Surface Pro 7 
  + If you want to change it, modify it in `WebCamera.cs`    
###### FaceProcessor.cs
+ Use Unity `deltaTime` to time face detection and take a photo.
+ Resize the bounding box to estimate the distance from the human face
+ I've removed the _red bounding box_ and the _blue face line_ that check it for detect faces. (for exhibition)
###### FaceDetectorScene.cs  
+ Photos are saved to the desktop `Photo` folder (Check Preparation)
+ Change the UI based on take a photo conditions
+ Enable communication when take a photo, and call upload method (only photo)
###### SocketClient.cs
+ I made a few minor modifications to automate the process (means `facemeshToObj.py` in facemesh - not `SockeClient.cs`)    
 _(Renaming files, Changing paths, and Adding a few lines of code to communicate with Unity)_    
_**PLEASE check out the original faecemesh project** : [Original Github](https://github.com/apple2373/mediapipe-facemesh)_
+ Communication code is almost identical to the referenced project : [Original Github](https://github.com/off99555/Unity3D-Python-Communication) 
+ Model generation results are stored in `facemesh/result`
+ Pass the name of the photo as a string to the Python program
+ Python program send a "model generation complete" message to Unity
+ When you receive a message from Python, call upload method (Facemesh)
###### FirebaseManager.cs
+ Make it a singleton object and use it
+ All methods take a file format as a string (do not use "Reactor" format)
+ File formats are very simple. "Photo" or "Facemesh" (model). Check out the code
+ Delete method deletes **all** files in Firebase
+ The upload/download methods will overwrite the file because the file name is fixed. 
   + If you want to keep the files separate, modify them by adding time to the name (This part is in `FaceDetectorScene.cs`).
+ In Download method, call _`save to the desktop method`_ for test. You can use the **`byte fileData`** inside the code as you wish.(check the comments)
+ If you already have a photo in Firebase, this program does not recognize faces. It was added for the exhibit, so delete it if you don't need it.
###### StableDiffusionReactor.cs
+ Not used
### Known Issue
+ If the model fails to generate because the face is not properly photographed, the Python program terminates
+ Memory leak : The webcam texture continues to exist. I didn't write any allocation code after freeing the memory, so it will probably cause memory issues after a long time.
+ ~Reactor Communication : Communication is NOT async. this project has same problem as SDReactorUnity (https://github.com/WooChan-Noh/SDReactorUnity) - Check Known Issue Part!~
***
#### Photo and Facemesh Result
<img src="https://github.com/WooChan-Noh/FaceDetectionTrip/assets/103042258/448de5ee-af0a-4597-95ee-66a98bcd1167" width="256" height="512"/>
<img src="https://github.com/WooChan-Noh/FaceDetectionTrip/assets/103042258/7bdcbf41-7a15-4197-a3fe-0680ecb9f362" width="640" height="512"/></br>

![1](https://github.com/WooChan-Noh/FaceDetectionTrip/assets/103042258/df138866-4ae7-4dee-a612-510b33559f3e)
![2](https://github.com/WooChan-Noh/FaceDetectionTrip/assets/103042258/7494b360-8fe1-45f1-8a78-40fb1275ca1e)
![3](https://github.com/WooChan-Noh/FaceDetectionTrip/assets/103042258/76e6d47c-751b-44ff-8aa2-69d117a00a9d)
![4](https://github.com/WooChan-Noh/FaceDetectionTrip/assets/103042258/541fc97b-8171-420a-9e5f-b6764475fccb)


