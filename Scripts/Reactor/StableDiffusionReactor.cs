using Newtonsoft.Json;
using System;
using System.Collections;
using System.IO;
using System.Net;
using UnityEngine;
using System.Threading.Tasks;
using OpenCvSharp.Demo;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Component to help generate a UI Image or RawImage using Stable Diffusion.
/// </summary>
[ExecuteAlways]
public class StableDiffusionReactor : StableDiffusionGenerator
{
    [ReadOnly]
    public string guid = "";
    [SerializeField]
    public string[] samplersList
    {
        get
        {
            if (sdc == null)
                sdc = GameObject.FindObjectOfType<StableDiffusionConfiguration>();
            return sdc.samplers;
        }
    }
    [SerializeField]
    public string[] modelsList
    {
        get
        {
            if (sdc == null)
                sdc = GameObject.FindObjectOfType<StableDiffusionConfiguration>();
            return sdc.modelNames;
        }
    }


    [HideInInspector]
    public int selectedSampler = 16;//DPM++ SDE Karras임
    [HideInInspector]
    public int selectedModel = 0;//Xl모델

    public int width = 512;
    public int height = 512;
    public int steps = 50;
    public float cfgScale = 7;
    public long seed = -1;

    public string sourceImageFolderPath = "";//사용자의 얼굴 사진이 저장된 "폴더" 경로
    private string sourceImageName = "";//사용자의 얼굴 사진 이름(용도 : 예외처리, 바이트 배열에 이미지 저장) - Generate()에서 설정됨

    public string targetImageFolderPath = "";//배경사진이 저장된 "폴더" 경로
    private string targetImagePath = "";//배경사진 이름(용도 : 바이트 배열에 이미지 저장)
    string[] targetImageFolderList;//배경사진 목록 가져옴
    public static int targetImagesLength=0;//타겟 이미지 총 개수 => GenerateAsync()에서 targetImageName 설정에 사용된다.
    float waitHTTPTime = 5f;//웹 응답 대기 시간 (리액터 생성 시간동안 멈추지 않게)

    public string resultImageFolderPath = "";//리액터로 합성된 사진이 저장될 "폴더" 경로
    public static string resultImageName = "";//리액터로 합성된 사진의 이름(용도 : 파이어베이스 연동에 사용) - SetupFolders()에서 설정됨
    private string reactorFilePath = "";//리액터로 합성된 "사진"의 최종 경로 - 위의 두 string의 합(용도 : 리액터에서 받은 byte 데이터를 이미지로 저장)

    string base64Format = "data:image/png;base64,";
    string fileFormat = ".png";
    public long generatedSeed = -1;

    private bool generating = false;
  
    void Awake()
    {
#if UNITY_EDITOR
        if (width < 0 || height < 0)
        {
            StableDiffusionConfiguration sdc = GameObject.FindObjectOfType<StableDiffusionConfiguration>();
            if (sdc != null)
            {
                SDSettings settings = sdc.settings;
                if (settings != null)
                {

                    width = settings.width;
                    height = 768;
                    steps = settings.steps;
                    cfgScale = settings.cfgScale;
                    seed = settings.seed;
                    return;
                }
            }

            width = 512;
            height = 512;
            steps = 50;
            cfgScale = 7;
            seed = -1;
        }
#endif
        width=512;
        height = 768;
    }
    private void Start()
    {
        selectedSampler = 16;
        //파일 경로들 연결해주기
        //바탕화면에 반드시 ReactorTargetImages, ReactorResultImages 폴더가 있어야함
        sourceImageFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\Photo\\";//facemesh의 Photo와 동일한 폴더임(얼굴사진)
        targetImageFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\ReactorTargetImages\\";//이 폴더 사진들이랑 합성해서
        resultImageFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\ReactorResultImages\\";//여기에 저장한다.

        targetImageFolderList = Directory.GetFiles(targetImageFolderPath);//타겟 이미지를 받아온다. 몇장이든 상관없음
        targetImagesLength=targetImageFolderList.Length;
    }
    void SetupReactorImageName(int num)
    {
     
        try
        {
            resultImageName = "Reactor" + num + fileFormat;//리액터로 합성된 사진의 이름(파이어베이스 연동에 사용)
            // Determine output path
            reactorFilePath = Path.Combine(resultImageFolderPath, resultImageName);//리액터로 합성된 사진 경로
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message + "\n\n" + e.StackTrace);
        }
    }
    string[] loadTargetImages()//타겟 이미지를 로드함
    {
        string[] targetImages = new string[targetImagesLength];
        for (int i = 0; i < targetImagesLength; i++)
        {
            targetImages[i] = Path.GetFileName(targetImageFolderList[i]);
        }
        Debug.Log("타겟 이미지 로딩 완료");
        return targetImages;

    }
    public void Generate()
    {
        sourceImageName = sourceImageFolderPath+FaceDetectorScene.photoName + fileFormat;//얼굴 사진 이름 설정(용도 : 예외처리, 바이트 배열에 이미지 저장)

        // Start generation asynchronously
        if (!generating && !string.IsNullOrEmpty(sourceImageName))//얼굴 사진이 있다면 시작(예외처리)
        {
            StartCoroutine(GenerateAsync());
        }
        else
        {
            Debug.LogError("No Source Image or Target Image! Check Path", this); return;
        }
    }
 
    IEnumerator GenerateAsync()
    {
        generating = true;
        if (sdc == null)
            sdc = GameObject.FindObjectOfType<StableDiffusionConfiguration>();
        // Set the model parameters
        yield return sdc.SetModelAsync(modelsList[selectedModel]);

        string[] targetImageName =loadTargetImages();//타겟 이미지 이름들 배열에 저장

        for (int i = 0; i < targetImagesLength; i++)//타겟 이미지 개수만큼 하나씩 통신 시작
        {
            SetupReactorImageName(i);//이름 시간을 업데이트해줘야함. 안하면 기존 이미지를 덮어씌운다.

            targetImagePath = targetImageFolderPath + targetImageName[i];//타겟 이미지의 최종 "경로"

            // Generate the image
            HttpWebRequest httpWebRequest = null;
            try
            {
                // Make a HTTP POST request to the Stable Diffusion server
                httpWebRequest = (HttpWebRequest)WebRequest.Create(sdc.settings.StableDiffusionServerURL + sdc.settings.ReactorAPI);
                httpWebRequest.ContentType = "application/json";
                httpWebRequest.Method = "POST";

                /*// add auth-header to request
                if (sdc.settings.useAuth && !sdc.settings.user.Equals("") && !sdc.settings.pass.Equals(""))
                {
                    httpWebRequest.PreAuthenticate = true;
                    byte[] bytesToEncode = Encoding.UTF8.GetBytes(sdc.settings.user + ":" + sdc.settings.pass);
                    string encodedCredentials = Convert.ToBase64String(bytesToEncode);
                    httpWebRequest.Headers.Add("Authorization", "Basic " + encodedCredentials);
                }*/


                // Send the generation parameters along with the POST request
                using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
                {

                    SDparamsInReactor sd = new SDparamsInReactor();


                    byte[] sourceBytes = File.ReadAllBytes(sourceImageName);//얼굴 사진을 바이트 형태로 옮김
                    byte[] targetBytes = File.ReadAllBytes(targetImagePath);//배경 사진을 바이트 형태로 옮김

                    //바이트 형태에서 암호화시킨다(API 요구사항)
                    string sourceBase64String = base64Format + Convert.ToBase64String(sourceBytes);
                    string targetBase64String = base64Format + Convert.ToBase64String(targetBytes);

                    //요청 body에 전달. 이 외의 Reactor에서 업스케일에 필요한 설정과 샘플링 메소드는 Setting.cs에 기본적으로 설정되어있음.
                    sd.source_image = sourceBase64String;
                    sd.target_image = targetBase64String;

                    if (selectedSampler >= 0 && selectedSampler < samplersList.Length)
                        sd.sampler_name = samplersList[selectedSampler];

                    // Serialize the input parameters
                    string json = JsonConvert.SerializeObject(sd);

                    // Send to the server
                    streamWriter.Write(json);

                }

            }
            catch (Exception e)
            {
                Debug.LogError(e.Message + "\n\n" + e.StackTrace);
            }

          

            // Read the output of generation
            if (httpWebRequest != null)
            {
                // Wait that the generation is complete before procedding

               Task<WebResponse> webResponse = httpWebRequest.GetResponseAsync();//비동기로 응답을 기다린다

                //result에 접근하는 순간 웹에서 응답이 올때까지 스레드를 대기상태로 만들기 때문에 응답 시간동안 코루틴을 기다리게 한다. 
                yield return new WaitForSecondsRealtime(waitHTTPTime);
               
                // Stream the result from the server
                var httpResponse = webResponse.Result;
              

                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    // Decode the response as a JSON string
                    string result = streamReader.ReadToEnd();

                    // Deserialize the JSON string into a data structure
                    SDResponseReactor json = JsonConvert.DeserializeObject<SDResponseReactor>(result);

                    // Check if the server returned an error
                    if (string.IsNullOrEmpty(json.image))
                    {
                        Debug.LogError("No image was return by the server. This should not happen. Verify that the server is correctly setup.");

                        generating = false;
#if UNITY_EDITOR
                        EditorUtility.ClearProgressBar();
#endif
                        yield break;
                    }

                    // Decode the image from Base64 string into an array of bytes
                    byte[] imageData = Convert.FromBase64String(json.image);

                    // Write it in the specified project output folder
                    using (FileStream imageFile = new FileStream(reactorFilePath, FileMode.Create))
                    {

                        yield return imageFile.WriteAsync(imageData, 0, imageData.Length);

                    }
                    //사진을 저장하지 않고 stram에서 바로 올려도 된다.
                    //근데 이미 짜놓은거 있어서 귀찮아서 저장 후 올림
                    FireBaseManager.Instance.UploadFiles(FireBaseManager.Instance.uploadFileFormat[2]);//************사진을 저장했으니 파이어베이스에 올린다*************//
                
                }
            }
        }

            Debug.Log("리액터 업로드 완료");
            generating = false;
            yield return null;
    }
}
