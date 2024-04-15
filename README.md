[Read Me - English](https://github.com/WooChan-Noh/FaceDetectionTrip/blob/main/ReadMeEng.md)
[Read Me - Japanese](https://github.com/WooChan-Noh/FaceDetectionTrip/blob/main/ReadMeJp.md)
# FaceDetectionTrip
+ 전시회 "Trip"을 위한 얼굴 감지 프로그램입니다.
   + 예술x기술 융합 프로젝트 (참가 회사 및 스튜디오 : 타이니젬x길몽)   
   + 2023년 11월 19일부터 25일까지 진행되었습니다.
   + 강원도 춘천시 삭주로 206번길 11 1층 737.Point(후평동737)에서 진행되었습니다.    
+ UI resources are copyrighted by Tinygem
+ 본 프로젝트에서 Reactor와 StableDiffusion에 연관된 부분은 사용하지 않았습니다.(무시하세요)
## Overview
#### 이 프로그램은 세 가지 기능이 있습니다.
1. 얼굴 감지(Face detection): **사람의 얼굴을 감지**하고 사진을 찍습니다. (스크린샷)
   + 이 기능에 연관된 스크립트 : `FaceDetectorScene.cs`, `FaceProcessor.cs`, `WebCamera.cs`
2. 3d모델 생성(Model generation): 사진을 바탕으로 **3d모델을 생성**합니다.
   + 이 기능에 연관된 스크립트 : `SocketClient.cs`, `SocketRequester.cs`, `RunAbleThread.cs`
   + **Facemesh ZIP** 파일이 필요합니다. (유니티와는 전혀 다른 프로젝트로, 이 Git에 포함되어 있습니다.)
3. 서버 통신 : 사진을 서버에 업로드/다운로드/삭제하고 생성된 3d 모델을 서버 스토리지에 저장합니다.
   + 이 기능에 연관된 스크립트 : `FirebaseManager.cs`
4. ~Reactor를 활용한 사진 합성(not used)~ : 해당 기능은 개발이 완료되었지만 기획이 바뀌어 사용하지 않습니다.
   + 이 기능에 연관된 스크립트 : `SDSettings.cs`, `StableDiffusionReactor.cs`
#### Test Environment
+ 테스트에 사용한 웹캠 : Logitech C170
+ 테스트에 사용한 PC : Surface Pro7 (세로 모드)
#### I used this projects
+ OpenCVForUnity : [Original Github](https://github.com/EnoxSoftware/OpenCVForUnity) (얼굴 감지에 사용)
+ Mediapipe-facemesh : [Original Github](https://github.com/apple2373/mediapipe-facemesh) (모델 생성에 사용)
  > **apple2373**님께서 사용을 허락해주셨습니다. 감사합니다!
+ Unity3D-Python-Communication : [Original Github](https://github.com/off99555/Unity3D-Python-Communication)    
  (유니티-파이썬 프로그램간의 소켓 통신)
+ Firebase : [Official Guide](https://firebase.google.com/docs/storage/unity/start)
+ ~(Not used) Reactor in Unity : [Original Github](https://github.com/WooChan-Noh/SDReactorUnity)~
##### Preparation
+ **바탕화면**에 `Photo`폴더와 `Facemesh`폴더가 있어야 합니다.
  + `Facemesh` 폴더는 이 프로젝트에 포함된 zip파일을 압축 해제한 폴더입니다.
  + `Photo` 폴더는 빈 폴더입니다.
+ `FirebaseManager.cs`에서 본인의 Firebase URL로 바꿔주세요.
+ `Facemesh`를 사용하기 전에, python3.8.10을 설치하고 requirements.txt를 확인해 필요한 것을 설치해 주세요
+ 유니티 에디터나 빌드한 앱을 실행하기 전에, `facemesh` 폴더에서 `facemeshToObj.py` 파이썬 프로그램을 실행시켜놔야 합니다. 
  > 이유 : 소켓 통신 프로젝트에서 알려진 이슈가 있습니다. - [Original Github](https://github.com/off99555/Unity3D-Python-Communication)
+ `WebCamera.cs`에서 본인이 사용하는 웹캠이 연결되어 있는지 확인해주세요
+ 유니티에 Newton 패키지가 설치되어 있어야 합니다.
## Learn more
+ 처음 기획안에서는, 사진을 Reactor를 활용하여 합성한 뒤 서버에 업로드하는 기능이 있었지만 취소되었습니다.
+ `Facemesh` 는 세 개의 파일을 생성합니다. : 1. texture(`.jpg`) 2.`.mtl` 3.`.obj`
   + 이 세 개의 파일들은 모두 **같은** 폴더에 위치해야합니다. 그리고 파일 이름이 **Facemesh** _(스펠링 주의)_ 로 같아야 합니다.
   + 이 문제를 해결하고 싶으면 `facemeshToObj.py`을 수정하세요 
###### WebCamera.cs   
+ 카메라 각도를 90도 돌립니다.
   > 이 프로젝트는 **Surface Pro 7** (세로 모드)를 사용합니다.
+ 렌더링 텍스처 비율도 Surface Pro 7의 비율과 일치하도록 강제 설정됩니다.
  + 텍스처 비율을 변경하려면 `WebCamera.cs`에서 수정하세요.    
###### FaceProcessor.cs
+ 프레임 단위로 얼굴을 감지하고 사진을 찍습니다.
+ 얼굴을 감지했을 때 나오는 bounding box의 크기를 계산하여 카메라와 사람의 거리를 추정합니다.
+ OpenCV의 원래 기능에서, 얼굴이 감지되었음을 알리는 bounding box 표시와 얼굴 구조를 나타내는 시각적 구조를 제거했습니다.
###### FaceDetectorScene.cs  
+ 바탕화면에 생성한 `Photo` 폴더에 사진을 저장합니다.
+ 프로그램의 상테에 따라 알맞은 UI로 변경합니다. (사진 촬영 시작, 진행 중, 완료, 조건 불일치 등)
+ 사진을 찍으면, 소켓 통신을 활성화 합니다. (`upload method`를 호출합니다 : 사진용)
###### SocketClient.cs
> `facemeshToObj.py`에서 프로세스 자동화를 위해 일부를 수정했습니다.    
 _(파일 이름 바꾸기, 경로 변경, Unity와 통신하기 위한 코드 몇 줄 추가)_    
_**Facemesh 원본 프로젝트를 참고하세요**  [Original Github](https://github.com/apple2373/mediapipe-facemesh)_
+ 소켓 통신에 사용하는 코드는 원본 프로젝트와 거의 동일합니다. : [Original Github](https://github.com/off99555/Unity3D-Python-Communication) 
+ 이 프로그램에서 촬영하여 생성된 사진의 이름을 파이썬 프로그램에 문자열로 전달합니다.
+ 파이썬 프로그램이 3d모델을 생성하면 "model generation complete" 메세지를 이 프로그램으로 보냅니다
   + 3d모델은 `facemesh/result` 에 저장됩니다. 
+ 파이썬 프로그램에서 메세지를 받으면 `upload method`를 호출합니다 : 3d모델용
###### FirebaseManager.cs
+ 싱글톤 객체로 만들어서 사용하세요.
+ 이 스크립트에서 모든 메서드는 파라미터를 문자열로 받습니다.("Reactor" 형식은 사용하지 않음)
+ 파라미터는 2종류 있습니다. "Photo" 또는 "Facemesh"입니다. 주석을 확인하세요
+ Delete메서드는 Firebase의 모든 파일을 삭제합니다.
+ upload/download 메서드는 사용하는 파일 이름이 고정되어 있으므로 firebase의 파일을 덮어쓰는 방식으로 동작합니다.
   + 파일 이름을 별도로 설정하여 개별 파일로 관리하고 싶으면 `FaceDetectorScene.cs`에서 수정하세요.
+ 이 프로젝트에 작성되어 있는 Download 메서드에는 다운로드한 파일을 바탕화면에 저장하는 테스트 기능이 포함되어 있습니다.
   + byte 형태로 다운로드 되므로, 위의 기능을 삭제하고 원하는 방식으로 사용하세요. 자세한건 주석을 확인하세요.
+ Firebase에 이미 사진이 있는 경우 이 프로그램은 얼굴을 인식하지 못합니다. 전시회를 위해 추가한 것이므로 삭제하세요.
###### StableDiffusionReactor.cs
+ 사용하지 않습니다.
### Known Issue
+ 얼굴이 제대로 촬영되지 않아 모델 생성에 실패하면 파이썬 프로그램이 종료됩니다. 이 경우 모든 프로그램을 종료하고 처음부터 다시 실행시켜 주세요.
+ 메모리 누수 : 웹캠 텍스처가 계속 존재해서 발생하는 것으로 생각됩니다. 메모리를 직접 관리하지 않기 때문에 시간이 오래 지나면 메모리 문제가 발생할 수 있습니다.
+ ~Reactor 통신 : 통신이 비동기로 진행되지 않습니다.. 이 프로젝트는 [SDReactorUnity](https://github.com/WooChan-Noh/SDReactorUnity)와 동일한 문제가 있습니다 - Known Issue 부분을 확인하세요~
***
#### Photo and Facemesh Result
<img src="https://github.com/WooChan-Noh/FaceDetectionTrip/assets/103042258/448de5ee-af0a-4597-95ee-66a98bcd1167" width="256" height="512"/>
<img src="https://github.com/WooChan-Noh/FaceDetectionTrip/assets/103042258/7bdcbf41-7a15-4197-a3fe-0680ecb9f362" width="640" height="512"/></br>

![1](https://github.com/WooChan-Noh/FaceDetectionTrip/assets/103042258/df138866-4ae7-4dee-a612-510b33559f3e)
![2](https://github.com/WooChan-Noh/FaceDetectionTrip/assets/103042258/7494b360-8fe1-45f1-8a78-40fb1275ca1e)
![3](https://github.com/WooChan-Noh/FaceDetectionTrip/assets/103042258/76e6d47c-751b-44ff-8aa2-69d117a00a9d)
![4](https://github.com/WooChan-Noh/FaceDetectionTrip/assets/103042258/541fc97b-8171-420a-9e5f-b6764475fccb)


