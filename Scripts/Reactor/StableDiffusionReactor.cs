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
    public int selectedSampler = 16;//DPM++ SDE Karras��
    [HideInInspector]
    public int selectedModel = 0;//Xl��

    public int width = 512;
    public int height = 512;
    public int steps = 50;
    public float cfgScale = 7;
    public long seed = -1;

    public string sourceImageFolderPath = "";//������� �� ������ ����� "����" ���
    private string sourceImageName = "";//������� �� ���� �̸�(�뵵 : ����ó��, ����Ʈ �迭�� �̹��� ����) - Generate()���� ������

    public string targetImageFolderPath = "";//�������� ����� "����" ���
    private string targetImagePath = "";//������ �̸�(�뵵 : ����Ʈ �迭�� �̹��� ����)
    string[] targetImageFolderList;//������ ��� ������
    public static int targetImagesLength=0;//Ÿ�� �̹��� �� ���� => GenerateAsync()���� targetImageName ������ ���ȴ�.
    float waitHTTPTime = 5f;//�� ���� ��� �ð� (������ ���� �ð����� ������ �ʰ�)

    public string resultImageFolderPath = "";//�����ͷ� �ռ��� ������ ����� "����" ���
    public static string resultImageName = "";//�����ͷ� �ռ��� ������ �̸�(�뵵 : ���̾�̽� ������ ���) - SetupFolders()���� ������
    private string reactorFilePath = "";//�����ͷ� �ռ��� "����"�� ���� ��� - ���� �� string�� ��(�뵵 : �����Ϳ��� ���� byte �����͸� �̹����� ����)

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
        //���� ��ε� �������ֱ�
        //����ȭ�鿡 �ݵ�� ReactorTargetImages, ReactorResultImages ������ �־����
        sourceImageFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\Photo\\";//facemesh�� Photo�� ������ ������(�󱼻���)
        targetImageFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\ReactorTargetImages\\";//�� ���� �������̶� �ռ��ؼ�
        resultImageFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\ReactorResultImages\\";//���⿡ �����Ѵ�.

        targetImageFolderList = Directory.GetFiles(targetImageFolderPath);//Ÿ�� �̹����� �޾ƿ´�. �����̵� �������
        targetImagesLength=targetImageFolderList.Length;
    }
    void SetupReactorImageName(int num)
    {
     
        try
        {
            resultImageName = "Reactor" + num + fileFormat;//�����ͷ� �ռ��� ������ �̸�(���̾�̽� ������ ���)
            // Determine output path
            reactorFilePath = Path.Combine(resultImageFolderPath, resultImageName);//�����ͷ� �ռ��� ���� ���
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message + "\n\n" + e.StackTrace);
        }
    }
    string[] loadTargetImages()//Ÿ�� �̹����� �ε���
    {
        string[] targetImages = new string[targetImagesLength];
        for (int i = 0; i < targetImagesLength; i++)
        {
            targetImages[i] = Path.GetFileName(targetImageFolderList[i]);
        }
        Debug.Log("Ÿ�� �̹��� �ε� �Ϸ�");
        return targetImages;

    }
    public void Generate()
    {
        sourceImageName = sourceImageFolderPath+FaceDetectorScene.photoName + fileFormat;//�� ���� �̸� ����(�뵵 : ����ó��, ����Ʈ �迭�� �̹��� ����)

        // Start generation asynchronously
        if (!generating && !string.IsNullOrEmpty(sourceImageName))//�� ������ �ִٸ� ����(����ó��)
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

        string[] targetImageName =loadTargetImages();//Ÿ�� �̹��� �̸��� �迭�� ����

        for (int i = 0; i < targetImagesLength; i++)//Ÿ�� �̹��� ������ŭ �ϳ��� ��� ����
        {
            SetupReactorImageName(i);//�̸� �ð��� ������Ʈ�������. ���ϸ� ���� �̹����� ������.

            targetImagePath = targetImageFolderPath + targetImageName[i];//Ÿ�� �̹����� ���� "���"

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


                    byte[] sourceBytes = File.ReadAllBytes(sourceImageName);//�� ������ ����Ʈ ���·� �ű�
                    byte[] targetBytes = File.ReadAllBytes(targetImagePath);//��� ������ ����Ʈ ���·� �ű�

                    //����Ʈ ���¿��� ��ȣȭ��Ų��(API �䱸����)
                    string sourceBase64String = base64Format + Convert.ToBase64String(sourceBytes);
                    string targetBase64String = base64Format + Convert.ToBase64String(targetBytes);

                    //��û body�� ����. �� ���� Reactor���� �������Ͽ� �ʿ��� ������ ���ø� �޼ҵ�� Setting.cs�� �⺻������ �����Ǿ�����.
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

               Task<WebResponse> webResponse = httpWebRequest.GetResponseAsync();//�񵿱�� ������ ��ٸ���

                //result�� �����ϴ� ���� ������ ������ �ö����� �����带 �����·� ����� ������ ���� �ð����� �ڷ�ƾ�� ��ٸ��� �Ѵ�. 
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
                    //������ �������� �ʰ� stram���� �ٷ� �÷��� �ȴ�.
                    //�ٵ� �̹� ¥������ �־ �����Ƽ� ���� �� �ø�
                    FireBaseManager.Instance.UploadFiles(FireBaseManager.Instance.uploadFileFormat[2]);//************������ ���������� ���̾�̽��� �ø���*************//
                
                }
            }
        }

            Debug.Log("������ ���ε� �Ϸ�");
            generating = false;
            yield return null;
    }
}
