# FaceDetectionTrip
+ FaceDetection Program for use in the exhibition **Trip**
+ UI resources are copyrighted by Tinygem
## Overview
#### The program does three things 
1. Face detection: **detects human face** and takes a photo (screenshot)
   + Related scripts : `FaceDetectorScene.cs`, `FaceProcessor.cs`, `WebCamera.cs`
2. Model generation: **create a model** based on the photo.
   + Related scripts : `SocketClient.cs`, `SocketRequester.cs`, `RunAbleThread.cs`
3. Cloud server communication: upload/download/delete photos and generated models **in server storage**
   + Related scripts : `FirebaseManager.cs`
4. ~Reactor(SD) synthesis (not used) : The development was completed, but the plan changed and it is not used.~
   + Related scripts : `SDSettings.cs`, `StableDiffusionReactor.cs`
#### Test Environment
+ Used Cam(Test) : Logitech C170
+ Used Local PC : Surface Pro7 (vertical)
#### I used this projects
+ OpenCVForUnity : https://github.com/EnoxSoftware/OpenCVForUnity (Use to faceDetection)
+ Mediapipe-facemesh : https://github.com/apple2373/mediapipe-facemesh (Use to model generation)
  > Thank you for allowing to use project! Special Thanks to **apple2373**
+ Unity3D-Python-Communication : https://github.com/off99555/Unity3D-Python-Communication    
  (Use to socket communication between Unity-Python)
+ Firebase (Official Guide) : https://firebase.google.com/docs/storage/unity/start
+ ~(Not used) Reactor in Unity : https://github.com/WooChan-Noh/SDReactorUnity~
## Learn more
+ I made a few minor modifications to automate the process (Facemesh)    
 _(Renaming files, Changing paths, and Adding a few lines of code to communicate with Unity)_    
_**PLEASE Check out the original project** : https://github.com/apple2373/mediapipe-facemesh_
+ The original plan was to use photos and Reactor to create AI photos and upload them to the server, but that was canceled.
+ Use Unity `deltatime` to time face detection and take a photo.
+ Photos are saved to the desktop `Photo` folder (Check Preparation)
+ Model generation results are stored in `facemesh/result`
+ This project used on a **Surface Pro 7** (vertical). So the camera texture is rotated 90 degrees.
+ Render texture proportions are also forced to match the proportions of the Surface Pro 7 
  + If you want to change it, modify it in `WebCamera.cs`
+ In the OpenCV code, I've removed the _red bounding box_ and the _blue face line_ that check it for detect faces.(for exhibition)
+ Resize the bounding box to estimate the distance from the human face
+ 
#### Preparation
+ You should have a folder `Photo`, `Facemesh` on your **Desktop**
  + `Facemesh` folder is ZIP file in this Project
+ Change Firebase URL (in FirebaseManager.cs)
+ Before use Facemesh, install python3.8.10 and check requirements.txt
+ Before you can run your Unity Editor or built app, you need to run `facemeshToObj.py` using the CLI. (in `facemesh` folder)
  > Here's why : Check Issue and TroubleShooting - https://github.com/off99555/Unity3D-Python-Communication
+ Check your webcam connect in `WebCamera.cs`
+ Need Newton pakacge in Unity
### Known Issue
+ If the model fails to generate because the face is not properly photographed, the Python program terminates
+ Memory leak : The webcam texture continues to exist. I didn't write any allocation code after freeing the memory, so it will probably cause memory issues after a long time.
+ ~Reactor Communication : Communication is NOT async. this project has same problem as SDReactorUnity (https://github.com/WooChan-Noh/SDReactorUnity) - Check Known Issue Part!~
***
