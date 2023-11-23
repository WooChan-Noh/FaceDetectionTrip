namespace OpenCvSharp.Demo
{
    using System;
    using UnityEngine;
    using UnityEngine.UI;
    using OpenCvSharp;
    using System.Collections;
    using System.IO;
    using TMPro;

    /// <summary>
    /// FaceDetection 동작(인식 및 촬영)에 연관된 스크립트(현재 스크립트 제외하고) : { FaceProcessor.cs, WebCamera.cs }
    /// 통신에 사용되는 스크립트 :  { SocketClient.cs, SocketRequester.cs, RunAbleThread.cs }
    /// </summary>

    public class FaceDetectorScene : WebCamera
    {
        public TextAsset faces;
        public TextAsset eyes;
        public TextAsset shapes;

        private FaceProcessorLive<WebCamTexture> processor;

        //*********추가한 변수********//
        //UI
        public Image topUI;
        public Image bottomUI;
        public Image maskUI;
        public Image preocessDoneCheckUI;//사진 촬영 완료 화면에 나오는 체크 모양 UI
        public Image farewellUI;//마지막 화면
        public Image loadingUI;//사진 촬영 중 나오는 로딩 애니메이션
        public Sprite originalBlackSprite;
        public Sprite processDoneBlueSprite;//사진 촬영 완료 화면에 UI들을 파란색으로 바꿔준다.
        public TextMeshProUGUI boxSize;//얼굴 인식 범위 확인용 (사용x)
        public TextMeshProUGUI infoMassage;//bottomUI의 문구
        public Color processColor = new Color(74f / 255f, 160f / 255f, 249f / 255f);
        //StableDiffusionReactor stableDiffusionReactor;//통신하기 위해
#if UNITY_ANDROID
        string albumName="OpenCV_FaceDetect";
#endif
        //string
        string infoText = "얼굴이 화면에 맞도록 가까이 와주세요";//디폴트 문구
        string processText = "얼굴 확인 진행 중...";
        string errorText = "얼굴이 너무 가깝거나 멀어요";
        string imageStillAliveText = "이전 탑승자의 수속이 완료되지 않았습니다.\n잠시만 기다려주세요";//파이어베이스 체크용
        static public string photoName = "Facemesh";//실제 얼굴 사진 이름 -> facemesh나 firebase통신과에 전부 사용함
        string folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Photo");//실제 얼굴 사진이 저장될 폴더. Awake에서 검사함

        //Flow Control
        public bool socketCommunicationFlag = false;//SocketClient.cs의 Update에서 사용 - 통신 제어
        float waitingTime = 2.0f;//UI 변경에 쓰임
        bool textChangeFlag = false;
        string fileFormat = ".png";
        //[HideInInspector] bool check = false;//Test()에서 사용

        //*********추가한 변수*********//

        /// <summary>
        /// Default initializer for MonoBehavior sub-classes
        /// </summary>

        //************추가한 함수************//

        protected override void Awake()//저장 폴더 있는지 검사하는 코드 추가
        {
            base.Awake();
            base.forceFrontalCamera = true; // we work with frontal cams here, let's force it for macOS s MacBook doesn't state frontal cam correctly

            byte[] shapeDat = shapes.bytes;
            if (shapeDat.Length == 0)
            {
                string errorMessage =
                    "In order to have Face Landmarks working you must download special pre-trained shape predictor " +
                    "available for free via DLib library website and replace a placeholder file located at " +
                    "\"OpenCV+Unity/Assets/Resources/shape_predictor_68_face_landmarks.bytes\"\n\n" +
                    "Without shape predictor demo will only detect face rects.";

#if UNITY_EDITOR
                // query user to download the proper shape predictor
                if (UnityEditor.EditorUtility.DisplayDialog("Shape predictor data missing", errorMessage, "Download", "OK, process with face rects only"))
                    Application.OpenURL("http://dlib.net/files/shape_predictor_68_face_landmarks.dat.bz2");
#else
             UnityEngine.Debug.Log(errorMessage);
#endif
            }

            processor = new FaceProcessorLive<WebCamTexture>();
            processor.Initialize(faces.text, eyes.text, shapes.bytes);

            // data stabilizer - affects face rects, face landmarks etc.
            processor.DataStabilizer.Enabled = true;        // enable stabilizer
            processor.DataStabilizer.Threshold = 2.0;       // threshold value in pixels
            processor.DataStabilizer.SamplesCount = 2;      // how many samples do we need to compute stable data

            // performance data - some tricks to make it work faster
            processor.Performance.Downscale = 256;          // processed image is pre-scaled down to N px by long side
            processor.Performance.SkipRate = 0;             // we actually process only each Nth frame (and every frame for skipRate = 0)

            //저장 폴더 검사
            if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

        }
        private void Start()
        {
           // stableDiffusionReactor = GetComponent<StableDiffusionReactor>();
        }
        void InitVariables()//사진 찍혔을 때 또는 사람이 없을 때 측정값 초기화
        {
            processor.measureFaceTime = 0.0f;
            processor.measureEmptyTime = 0.0f;
            processor.squareSideLength = 0;
        }

        void ActiveUI()
        {
            topUI.gameObject.SetActive(true);
            bottomUI.gameObject.SetActive(true);
            maskUI.gameObject.SetActive(true);
        }

        void DeactivateUI(WebCamTexture input)
        {
            topUI.gameObject.SetActive(false); // 이미지 UI 비활성화
            bottomUI.gameObject.SetActive(false);
            maskUI.gameObject.SetActive(false);
            loadingUI.gameObject.SetActive(false);
            processor.ProcessTexture(input, TextureParameters, false);// 얼굴 마커 비활성화 							
            infoMassage.gameObject.SetActive(false);   // 텍스트 비활성화
        }

        void SwitchToProcessDoneUI()//사진 촬영 완료 시 UI 색 변경
        {
            topUI.sprite = processDoneBlueSprite;
            bottomUI.sprite = processDoneBlueSprite;
        }

        void SwitchToOriginalUI()
        {
            topUI.sprite = originalBlackSprite;
            bottomUI.sprite = originalBlackSprite;
        }

        void UpdateInfo(bool loadingUIFlag, string text, Color color)//UI 정보 업데이트
        {
            loadingUI.gameObject.SetActive(loadingUIFlag);
            infoMassage.text = text;
            infoMassage.color = color;
        }

        private bool IsScreenshotCaptured(string filePath)//파일이 생성되었는지 확인
        {
            Debug.Log("파일을 확인하러 무한루프에 들어왔음");
            // 스크린샷 파일이 생성되었는지 여부를 확인하는 코드
            while (true)
            {
                if (File.Exists(filePath))
                {
                    Debug.Log("파일이 존재합니다.");
                    Debug.Log("무한루프 종료");
                    break;
                }
                else
                {
                    Debug.Log("파일이 존재하지 않습니다.");
                    Debug.Log("무한루프로 돌아갑니다.");
                }
            }
            return true;
        }

        IEnumerator TakePhoto()//사진 찍기 / UI 변경 / 파이어베이스, facemesh와 통신
        {
            //UI를 비활성화 시키고  1프레임 기다린 후 스크린샷 캡쳐 시작
            //기다리지 않으면 UI가 같이 찍힌다
            yield return new WaitForEndOfFrame();

            string filePath = Path.Combine(folderPath, photoName + fileFormat);
            ScreenCapture.CaptureScreenshot(filePath);//촬영 

#if UNITY_ANDROID
			Texture2D screenTexture = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
			screenTexture.ReadPixels(new UnityEngine.Rect(0, 0, Screen.width, Screen.height), 0, 0);
			screenTexture.Apply();

			NativeGallery.SaveImageToGallery(screenTexture, albumName, fileName);
			Destroy(screenTexture);
#endif


            InitVariables();//찍었으니 파라미터 초기화
            SwitchToProcessDoneUI();//사진 촬영 완료 화면으로 UI 변경해 놓고
            ActiveUI();//UI 활성화
            preocessDoneCheckUI.gameObject.SetActive(true);//완료 화면에만 필요한 추가 UI(체크 모양)

            Time.timeScale = 0; // 프로그램을 일시 중지(측정을 중지함)
            ///11.13 15:00경부터 업로드 코드가 실행되기전에 사진저장이 완료되지 않아 로드가 안되는 문제가 발생함.
            ///기존의 2초 대기 코드를 위로 올렸음 + 확실하게 나왔는지 한번 더 확인
            ///그 전에는 왜 됐는지, 저 시점 이후로 왜 안되는지 이유는 여전히 알 수 없음. 
            yield return new WaitForSecondsRealtime(waitingTime);// 사진이 저장될 시간을 기다림
            yield return new WaitUntil(() => IsScreenshotCaptured(filePath));//사진이 저장되었는지 확인

            FireBaseManager.Instance.UploadFiles(FireBaseManager.Instance.uploadFileFormat[0]);//사진 파일 파이어 베이스에 업로드
            //stableDiffusionReactor.Generate();//리액터로 합성 시작
            socketCommunicationFlag = true;//facemesh와 통신 시작

            yield return new WaitForEndOfFrame();//SocketClient.cs의 Update에서 통신을 시작하므로 프레임 대기
            yield return new WaitForEndOfFrame();//11.14기준, 그 전에는 한 프레임 휴식으로 실행됐었음

            socketCommunicationFlag = false;
            ///yield return new WaitForSecondsRealtime(waitingTime); // 실제 시간 기준으로 2초 동안 대기. 여기까진 사진 촬영이 완료되었다는 UI를 보여줌
            ///그 전에 위치함

            farewellUI.gameObject.SetActive(true);//좋은 여행 되세요 화면 전환
            yield return new WaitForSecondsRealtime(waitingTime);//2초동안 대기

            SwitchToOriginalUI();  //모든 과정이 끝났으므로 기존 UI로 변경
            infoMassage.gameObject.SetActive(true);//안내 문구 활성화

            //기존 UI에 포함되지 않는 UI들 다시 비활성화
            preocessDoneCheckUI.gameObject.SetActive(false);
            farewellUI.gameObject.SetActive(false);

            Time.timeScale = 1; // 프로그램을 다시 시작
        }

        //************추가한 함수************//

        /// <summary>
        /// Per-frame video capture processor
        /// </summary>

        //************프로세스는 여기서 시작한다 - WebCamera.cs의 Update의 IF 조건에서 호출됨(오버라이드)************//
        //************WebCamera.cs에서 반드시 환경에 맞추어 해상도 조절 - 해상도가 맞지 않으면 인식률 저하************//
        protected override bool ProcessTexture(WebCamTexture input, ref Texture2D output)
        {
            //**********UI 텍스트 관련 코드**********//

            //얼굴 인식 거리가 허용범위가 아닌 경우 UI변경을 위한 if문
            if (processor.faceDistanceCheck == false && Time.timeScale != 0)
            {
                UpdateInfo(false, errorText, Color.white);
                processor.measureFaceTime -= Time.deltaTime * processor.timeControl * 2f;//인식 거리가 허용범위로 들어갔을 때 바로 찍히는 것을 방지하기 위한 누적 시간 조정
                textChangeFlag = true;//텍스트 변경을 위한 flag변수
            }

            //얼굴 인식 거리가 허용범위가 아니였다가 허용범위로 들어온 경우 UI변경을 위한 if문
            if (textChangeFlag == true && processor.faceDistanceCheck == true && Time.timeScale != 0)
            {
                UpdateInfo(true, processText, processColor);
                textChangeFlag = false;//텍스트 변경을 위한 flag변수
            }

            ///측정된 empty의 누적 시간이 초기화 시키는 기준empty시간보다 많으면 초기화	
            /// => 사람이 카메라 앞에 없다는 뜻이니까
            /// => 혹은 1초 이상 사람이 카메라 앞에서 얼굴을 돌렸다는 뜻이니까
            /// => 정면이 아닐 확률이 높으므로 다시 측정해야함
            if (processor.measureEmptyTime >= processor.resetTime)
            {
                processor.faceDistanceCheck = true;//false로 설정하면 안내 문구에 설정에 문제가 생김
                InitVariables();//empty 상태이니 측정하던 값들 초기화
                UpdateInfo(false, infoText, Color.white);//모든 측정 수치가 초기화 되었으므로 기존 안내 문구 출력        
            }

            //**********UI 텍스트 관련 코드**********//


            //*********촬영 로직**********//

            //거리가 허용 범위인 상태에서 측정한 face의 누적 시간이 촬영 기준 시간보다 많으면 촬영 시작 
            if (processor.faceDistanceCheck == true && processor.measureFaceTime >= processor.takePhotoTime)
            {
                DeactivateUI(input);//스크린샷을 찍으므로 모든 요소 비활성화
                StartCoroutine(TakePhoto()); //촬영 시작                                                                                                            
            }
            else//측정하고 있는 face의 누적 시간이 촬영 기준 시간 아래면 측정을 계속한다.
            {

                FireBaseManager.Instance.CheckLastImages();//파이어베이스에 파일이 살아있으면 = 아직 VR이 받아가지 않았으면

                // detect everything we're interested in
                processor.ProcessTexture(input, TextureParameters);

                //mark detected objects
                processor.MarkDetected();

                if (FireBaseManager.Instance.imageExist)
                {
                    Debug.Log("파일이 아직 존재함. 삭제하세요");
                    //Test();//테스트용-파이어베이스 다운로드/삭제 확인
                    InitVariables();//측정 전부 초기화
                }
                //else check = false; //테스트용

                if (processor.faceDistanceCheck == true && processor.measureFaceTime != 0.0f)//측정하고 있으면 UI 바꿔주기       
                    UpdateInfo(true, processText, processColor);//촬영중~
                else if (processor.faceDistanceCheck == true && FireBaseManager.Instance.imageExist)//아직 파이어베이스의 파일이 살아있으면
                    UpdateInfo(false, imageStillAliveText, Color.white);//앞선 사용자가 VR을 쓰고 파일을 받아가서 삭제할때까지 대기
                else if (processor.faceDistanceCheck == false && processor.measureFaceTime != 0.0f)//얼굴이 허용거리 벗어났으면 알려주기
                    UpdateInfo(false, errorText, Color.white);//예외상황          
                else//파이어베이스에 파일이 없고 측정 시간이 초기화되었으면 기존 안내 UI(처음화면)로 바꾸기
                    UpdateInfo(false, infoText, Color.white);//디폴트
            }

            //*********촬영 로직**********//

            // processor.Image now holds data we'd like to visualize
            output = Unity.MatToTexture(processor.Image, output);   // if output is valid texture it's buffer will be re-used, otherwise it will be re-created

            ///Red Box 수치 확인 및 해상도 체크 (FaceSize 오브젝트 켜야함)
            ///boxSize.text = "Width&Height : " + processor.squareSideLength.ToString() + "\n" + myDeviceWidthRatio + " " + myDeviceHeightRatio;

            return true;
        }
    /*    void Test()
        {
            if (check == false)
            {
                FireBaseManager.Instance.DeleteFiles();

                //StableDiffusionReactor.targetImagesLength = 12;
                //FireBaseManager.Instance.DownloadFiles("Photo");
                //FireBaseManager.Instance.DownloadFiles("Facemesh");
                //FireBaseManager.Instance.DownloadFiles("Reactor");
                check = true;
            }
        }
*/
    }

}
