using AsyncIO;
using NetMQ;
using NetMQ.Sockets;
using OpenCvSharp.Demo;
using UnityEngine;

/// <summary>
///     Example of requester who only sends Hello. Very nice guy. 
///     You can copy this class and modify Run() to suits your needs.
///     To use this class, you just instantiate, call Start() when you want to start and Stop() when you want to stop.
/// </summary>

public class SocketRequester : RunAbleThread
{
    /// <summary>
    ///     Request Hello message to server and receive message back. Do it 10 times.
    ///     Stop requesting when Running=false.
    /// </summary>

    public bool gotMessage;

    public override void Run()
    {
        ForceDotNet.Force(); // this line is needed to prevent unity freeze after one use, not sure why yet
  
            using (RequestSocket client = new RequestSocket())
            {
            string server_address = $"tcp://localhost:5555";
            client.Connect(server_address);

                for (int i = 0; i < 1 && Running; i++)
                {
                    //*******원본 사진의 이름을 string형태로 soket을 활용해 facemesh에게 보낸다 - pyzmq라이브러리********//
                    client.SendFrame(FaceDetectorScene.photoName);
                   

                    // ReceiveFrameString() blocks the thread until you receive the string, but TryReceiveFrameString()
                    // do not block the thread, you can try commenting one and see what the other does, try to reason why
                    // unity freezes when you use ReceiveFrameString() and play and stop the scene without running the server
                    // string message = client.ReceiveFrameString();
                    // Debug.Log("Received: " + message);
                    string message = null;
                    gotMessage = false;
                    while (Running)
                    {
                        gotMessage = client.TryReceiveFrameString(out message); // this returns true if it's successful
                        if (gotMessage) break;
                    }

                    if (gotMessage) Debug.Log("Received Message : " + message);
                }
        }
        NetMQConfig.Cleanup(); // this line is needed to prevent unity freeze after one use, not sure why yet
    }
}