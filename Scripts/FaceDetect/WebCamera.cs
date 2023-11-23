namespace OpenCvSharp.Demo
{
	using System;
	using UnityEngine;
	using UnityEngine.UI;
	using OpenCvSharp;

	// Many ideas are taken from http://answers.unity3d.com/questions/773464/webcamtexture-correct-resolution-and-ratio.html#answer-1155328

	/// <summary>
	/// Base WebCamera class that takes care about video capturing.
	/// Is intended to be sub-classed and partially overridden to get
	/// desired behavior in the user Unity script
	/// </summary>
	public abstract class WebCamera: MonoBehaviour
	{
		//내가 추가한 변수
		int rotationAngle = 90;
		string webCamName = "Webcam C170";//사용하는 웹캠 이름으로 - 맥북에서 웹캠을 연결해서 테스트할때만 사용함(맥북 전면 캠 사용 불가능)
		//app에 나오는 해상도를 결정하는 변수
		[HideInInspector]
		public int myDeviceWidthRatio;
		[HideInInspector]
		public int myDeviceHeightRatio;
		//<서피스 프로7 기준> app 해상도 계산 변수
		int myDeviceScreenWidth;
		int myDeviceScreenHeight; 

		/// <summary>
		/// Target surface to render WebCam stream
		/// </summary>
		public GameObject Surface;

		private Nullable<WebCamDevice> webCamDevice = null;
		private WebCamTexture webCamTexture = null;
		private Texture2D renderedTexture = null;

		/// <summary>
		/// A kind of workaround for macOS issue: MacBook doesn't state it's webcam as frontal
		/// </summary>
		protected bool forceFrontalCamera = false;

		/// <summary>
		/// WebCam texture parameters to compensate rotations, flips etc.
		/// </summary>
		protected Unity.TextureConversionParams TextureParameters { get; private set; }

		/// <summary>
		/// Camera device name, full list can be taken from WebCamTextures.devices enumerator
		/// </summary>
		public string DeviceName
		{
			get
			{
				return (webCamDevice != null) ? webCamDevice.Value.name : null;
			}
			set
			{
				// quick test
				if (value == DeviceName)
					return;

				if (null != webCamTexture && webCamTexture.isPlaying)
					webCamTexture.Stop();

				// get device index
				int cameraIndex = -1;
				for (int i = 0; i < WebCamTexture.devices.Length && -1 == cameraIndex; i++)
				{
					if (WebCamTexture.devices[i].name == value)
						cameraIndex = i;
				}

				// set device up
				if (-1 != cameraIndex)
				{
					webCamDevice = WebCamTexture.devices[cameraIndex];
					webCamTexture = new WebCamTexture(webCamDevice.Value.name);

					// read device params and make conversion map
					ReadTextureConversionParameters();

					webCamTexture.Play();
				}
				else
				{
					throw new ArgumentException(String.Format("{0}: provided DeviceName is not correct device identifier", this.GetType().Name));
				}
			}
		}

		/// <summary>
		/// This method scans source device params (flip, rotation, front-camera status etc.) and
		/// prepares TextureConversionParameters that will compensate all that stuff for OpenCV
		/// </summary>
		private void ReadTextureConversionParameters()//화면 회전시키는 코드 추가함
        {
			Unity.TextureConversionParams parameters = new Unity.TextureConversionParams();

			// frontal camera - we must flip around Y axis to make it mirror-like
			parameters.FlipHorizontally = forceFrontalCamera || webCamDevice.Value.isFrontFacing;
			
			// TODO:
			// actually, code below should work, however, on our devices tests every device except iPad
			// returned "false", iPad said "true" but the texture wasn't actually flipped

			// compensate vertical flip
			//parameters.FlipVertically = webCamTexture.videoVerticallyMirrored;
			
			// deal with rotation
			if (0 != webCamTexture.videoRotationAngle)
				parameters.RotationAngle = webCamTexture.videoRotationAngle; // cw -> ccw

			
			//회전을 위해 추가
			//surface를 세로로 사용했음
			parameters.RotationAngle = rotationAngle;
            
			
			// apply
            TextureParameters = parameters;

            //UnityEngine.Debug.Log (string.Format("front = {0}, vertMirrored = {1}, angle = {2}", webCamDevice.isFrontFacing, webCamTexture.videoVerticallyMirrored, webCamTexture.videoRotationAngle));
        }

        /// <summary>
        /// Default initializer for MonoBehavior sub-classes
        /// </summary>
        protected virtual void Awake()//웹캠 연결하는 코드 추가
        {
#if UNITY_ANDROID
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            if (WebCamTexture.devices.Length > 0)
                DeviceName = WebCamTexture.devices[WebCamTexture.devices.Length - 1].name;//전면카메라
                //DeviceName = "DroidCam Source 3";//에디터+리모트
#endif
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            if (WebCamTexture.devices[WebCamTexture.devices.Length - 1].name == webCamName)
                DeviceName = WebCamTexture.devices[WebCamTexture.devices.Length - 1].name;//맥북 테스트용- 맥북의 전면 카메라는 동작하지 않는다. 따로 웹캠 필요
            else
				DeviceName = WebCamTexture.devices[0].name;
			
        }
  
        void OnDestroy() 
		{
			if (webCamTexture != null)
			{
				if (webCamTexture.isPlaying)
				{
					webCamTexture.Stop();
				}
				webCamTexture = null;
			}

			if (webCamDevice != null) 
			{
				webCamDevice = null;
			}
		}
		/// <summary>
		/// Updates web camera texture
		/// </summary>
		private void Update ()
		{
            if (Input.GetKey(KeyCode.Escape))
                Application.Quit();//esc누르면 종료
            
            if (webCamTexture != null && webCamTexture.didUpdateThisFrame)
			{
				// this must be called continuously
				ReadTextureConversionParameters();

				// process texture with whatever method sub-class might have in mind

				if (ProcessTexture(webCamTexture, ref renderedTexture))//모든 프로세스의 시작 -> FaceDetectorScene.cs에서 오버라이드함
                {
					RenderFrame();//여기서 화면 비율 수정
				}
			}
        }

		/// <summary>
		/// Processes current texture
		/// This function is intended to be overridden by sub-classes
		/// </summary>
		/// <param name="input">Input WebCamTexture object</param>
		/// <param name="output">Output Texture2D object</param>
		/// <returns>True if anything has been processed, false if output didn't change</returns>
		protected abstract bool ProcessTexture(WebCamTexture input, ref Texture2D output);
		/// <summary>
		/// Renders frame onto the surface
		/// </summary>
		private void RenderFrame()//화면 비율 수정하는 코드 추가함
        {
			if (renderedTexture != null)
			{
				// apply
				Surface.GetComponent<RawImage>().texture = renderedTexture;

                // Adjust image ration according to the texture sizes 
                //Surface.GetComponent<RectTransform>().sizeDelta = new Vector2(renderedTexture.width,renderedTexture.height); 



                //웹캠이 받아오는 해상도 <-> 스크린에 랜더링 해주는 해상도 간의 비율을 맞추며 화면을 채워야한다.(스케일업)
                //-> 당연히 위아래, 혹은 좌우로 canvas를 넘어가는 부분 있음
                //짤리는 부분을 없애기 위해 device screen을 사용하면 영상 자체가 늘어남 => openCV 인식률 이슈 발생함

                //가로를 기준으로 세로의 비율 조정 - 서피스 프로7 기준
                //화면이 회전하면 width와 height가 자동으로 바뀌어서 인식되므로 바꿔줄 필요 없음 -> 자동회전기능이 없으면 서로 바꿔줘야함
                myDeviceScreenWidth = Screen.width;
                myDeviceScreenHeight = Screen.height;
                myDeviceWidthRatio = myDeviceScreenWidth;
                myDeviceHeightRatio = myDeviceScreenWidth * renderedTexture.height / renderedTexture.width;
                //surface pro7비율에 맞게 강제 고정함 -> 9:16 비율을 유지
                Surface.GetComponent<RectTransform>().sizeDelta = new Vector2(myDeviceWidthRatio,myDeviceHeightRatio);
                //강제로 해상도를 고정하는 경우 scale도 같이 고정해야함(아니면 바뀐다 이유 모름)
                Surface.GetComponent<RectTransform>().localScale= new Vector3(1, 1, 1);
				
			

            }
        }

    }
}