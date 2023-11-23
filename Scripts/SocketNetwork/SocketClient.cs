using OpenCvSharp.Demo;
using UnityEngine;

public class SocketClient : MonoBehaviour
{
    private SocketRequester socketRequester;
    private FaceDetectorScene faceDetectorScene;

    bool preventDuplicateStart = false;//2프레임을 쉬니까, 혹시 모를 2중 통신 방지

    private void Start()
    {
        socketRequester = new SocketRequester();
        faceDetectorScene = GetComponent<FaceDetectorScene>();
    }
    private void Update()
    {
        if (faceDetectorScene.socketCommunicationFlag)//사진 찍는 과정이 끝나고 조건이 만족되면 통신 시작
        {
            if(preventDuplicateStart)
                return;
            socketRequester.Start();
            preventDuplicateStart = true;
        }

        if(socketRequester.gotMessage)//파이썬으로부터 메시지를 받으면(=모델이 만들어지면) 통신 종료
        {
            preventDuplicateStart = false;
            FireBaseManager.Instance.UploadFiles(FireBaseManager.Instance.uploadFileFormat[1]);//메세지를 받았다 = 오브젝트 파일이 생성되었다 => 파이어베이스에 파일 업로드 시작
            socketRequester.Stop();
            socketRequester.gotMessage = false;
        }

    }
}