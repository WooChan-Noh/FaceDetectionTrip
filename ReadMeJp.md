
# FaceDetectionTrip
+ 展示会"Trip"のための顔検出プログラムです。
   + アート×技術融合プロジェクト(参加企業及びスタジオ：Tinygem × Gilmong Studio)   
   + 2023年11月19日から25日まで行われました。
   + 江原道春川市朔州路206番ギル11 1階737.Point(후평동737)で行われました。
+ UI resources are copyrighted by Tinygem
+ 本プロジェクトでReactorとStableDiffusionに関連ある部分は無視してください。
## Overview
#### このプログラムは3つの機能があります。
1. 顔検出(Face detection): **人の顔を検出して**写真を撮ります。 (スクリーンショット)
   + スクリプト: `FaceDetectorScene.cs`, `FaceProcessor.cs`, `WebCamera.cs`
2. 3dモデル生成(Model generation): 写真を基に**3dモデルを生成します**
   + スクリプト : `SocketClient.cs`, `SocketRequester.cs`, `RunAbleThread.cs`
   + **Facemesh ZIP** ファイルが必要です(Unityとは全く別のプロジェクトで、このGitに含まれています)。
3. サーバー通信 : 写真をサーバーにアップロード/ダウンロード/デリートし、生成された3dモデルをサーバーストレージにアップロードします。
   + スクリプト : `FirebaseManager.cs`
4. ~Reactorを活用した写真合成(not used)~ : この機能は開発が完了しましたが、企画が変更されたため使用しません.
   + スクリプト : `SDSettings.cs`, `StableDiffusionReactor.cs`
#### Test Environment
+ テストに使ったウェブカメラ : Logitech C170
+テストに使ったPC : Surface Pro7 (ポートレートモード)
#### I used this projects
+ OpenCVForUnity : [Original Github](https://github.com/EnoxSoftware/OpenCVForUnity) (顔検出に使用)
+ Mediapipe-facemesh : [Original Github](https://github.com/apple2373/mediapipe-facemesh) (モデル生成に使用)
  > **apple2373**さんに使用を許可して頂きました。 ありがとうございます！
+ Unity3D-Python-Communication : [Original Github](https://github.com/off99555/Unity3D-Python-Communication)    
  (Unity-Pythonプログラム間のソケット通信)
+ Firebase : [Official Guide](https://firebase.google.com/docs/storage/unity/start)
+ ~(Not used) Reactor in Unity : [Original Github](https://github.com/WooChan-Noh/SDReactorUnity)~
##### Preparation
+ **デスクトップ**に `Photo`フォルダと `Facemesh`フォルダが必要です。
  + `Facemesh`フォルダはこのプロジェクトに入ってるzipファイルを解凍したフォルダです。
  + `Photo` フォルダは空のフォルダです。
+ `FirebaseManager.cs`で自分のFirebase URLに変更してください。
+ `Facemesh`を使用する前に、python3.8.10をインストールし、requirements.txtを確認して必要なものをインストールしてください。
+ Unityエディタやビルドしたアプリを実行する前に、`facemesh` フォルダで `facemeshToObj.py` Pythonプログラムを実行する必要があります。
  > 理由：ソケット通信プロジェクトに問題があります。- [Original Github](https://github.com/off99555/Unity3D-Python-Communication)
+ `WebCamera.cs`で自分が使っているウェブカメラが接続されていることを確認してください。
+ UnityにNewtonパッケージがインストールされている必要があります。
## Learn more
+ 当初の企画案では、写真をReactorを利用して合成し、サーバーにアップロードする機能がありましたが、キャンセルされました。
+ `Facemesh` 3つのファイルを生成します。 : 1. texture(`.jpg`) 2.`.mtl` 3.`.obj`
   + この3つのファイルは全て**同じフォルダ**に配置する必要があります。 そして、ファイル名は**Facemesh**(スペルに注意)と同じでなければなりません。
   + この問題を解決するには、 `facemeshToObj.py`を修正してください。
###### WebCamera.cs   
+ カメラの角度を90度回転させます
   > このプロジェクトでは**Surface Pro 7**（ポートレートモード）を使用しています。
+ レンダリングテクスチャの比率もSurface Pro 7の比率と一致するように強制的に設定されます。
  +テクスチャの比率を変更したい場合は、`WebCamera.cs`で修正してください。
###### FaceProcessor.cs
+ フレーム単位で顔を検出し、写真を撮影します。
+ 顔を検出したときに出てくるバウンディングボックスの大きさを計算して、カメラと人の距離を推定します。
+ OpenCVの本来の機能から、顔が検出されたことを知らせるバウンディングボックスの表示と、顔の構造を表す視覚的な構造を削除しました。
###### FaceDetectorScene.cs  
+ デスクトップに作成した `Photo` フォルダに写真をセーブします。
+ プログラムの状態に応じてUIに変更します。 (写真撮影開始、進行中、完了、条件不一致など)
+ 写真を撮ったら、ソケット通信を活性化します。 (`upload method`呼び出します：写真用)
###### SocketClient.cs
> `facemeshToObj.py`でプロセス自動化のために一部修正しました。    
 _(ファイル名変更、パス変更、Unityと通信するためのコード数行追加)_    
_**Facemeshのオリジナルプロジェクトを参考してください**  [Original Github](https://github.com/apple2373/mediapipe-facemesh)_
+ ソケット通信に使うコードはオリジナルプロジェクトとほぼ同じです. : [Original Github](https://github.com/off99555/Unity3D-Python-Communication) 
+ このプログラムで撮影して生成された写真の名前をPythonプログラムにstringで渡します。
+ Pythonプログラムが3dモデルを生成したら、"model generation complete"メッセージをこのプログラムに送ります。
   + 3dモデルは`facemesh/result` セーブされます。
+ Pythonプログラムからメッセージを受け取ったら、uploadメソッドを呼び出します： 3dモデル用
###### FirebaseManager.cs
+ シングルトンオブジェクトで使ってください。
+ このスクリプトで全てのmethodはパラメータをstringで受け取ります。("Reactor"は使用しません)
+ パラメータは2種類あります。"Photo"または"Facemesh"です。注釈を確認してください。
+ DeleteメソッドはFirebaseの全てのファイルを削除します。
+ upload/downloadメソッドは使うファイル名が固定されているので、firebaseのファイルを上書きする方式で動作します。
   + ファイル名を別に設定して個別ファイルとして管理したい場合は `FaceDetectorScene.cs`で修正してください。
+ このプロジェクトにあるダウンロードメソッドはダウンロードしたファイルをデスクトップにセーブするテスト機能が含まれています
   + byte形式でダウンロードされるので、上記の機能を削除して好きな方法で使ってください。詳細はコメントを確認してください.
+ Firebaseにすでに写真がある場合、このプログラムは顔を認識しません。展示会のために追加したものなので削除してください
###### StableDiffusionReactor.cs
+ 使用しません。
### Known Issue
+ 顔が正しく撮影されず、モデル生成に失敗すると、Pythonプログラムが終了します。この場合、すべてのプログラムを終了し、最初からやり直してください。
+ メモリリーク：ウェブカメラテクスチャのせいで発生すると思われます。メモリを直接管理していないので、時間が経つとメモリ問題が発生する可能性があります。
+ ~Reactor通信 : 通信が非同期で行われません。このプロジェクトは [SDReactorUnity](https://github.com/WooChan-Noh/SDReactorUnity)と同じ問題があります - Known Issue部分を確認してください~
***
#### Photo and Facemesh Result
<img src="https://github.com/WooChan-Noh/FaceDetectionTrip/assets/103042258/448de5ee-af0a-4597-95ee-66a98bcd1167" width="256" height="512"/>
<img src="https://github.com/WooChan-Noh/FaceDetectionTrip/assets/103042258/7bdcbf41-7a15-4197-a3fe-0680ecb9f362" width="640" height="512"/></br>

![1](https://github.com/WooChan-Noh/FaceDetectionTrip/assets/103042258/df138866-4ae7-4dee-a612-510b33559f3e)
![2](https://github.com/WooChan-Noh/FaceDetectionTrip/assets/103042258/7494b360-8fe1-45f1-8a78-40fb1275ca1e)
![3](https://github.com/WooChan-Noh/FaceDetectionTrip/assets/103042258/76e6d47c-751b-44ff-8aa2-69d117a00a9d)
![4](https://github.com/WooChan-Noh/FaceDetectionTrip/assets/103042258/541fc97b-8171-420a-9e5f-b6764475fccb)


