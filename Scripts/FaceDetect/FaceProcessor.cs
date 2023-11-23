namespace OpenCvSharp.Demo
{
	using System;
	using System.Collections.Generic;
    using OpenCvSharp;

    /// <summary>
    /// Array utilities
    /// http://stackoverflow.com/questions/1792470/subset-of-array-in-c-sharp
    /// </summary>
	static partial class ArrayUtilities
    {
        // create a subset from a range of indices
        public static T[] RangeSubset<T>(this T[] array, int startIndex, int length)
        {
            T[] subset = new T[length];
            Array.Copy(array, startIndex, subset, 0, length);
            return subset;
        }

        // creates subset with from-to index pair
        public static T[] SubsetFromTo<T>(this T[] array, int fromIndex, int toIndex)
        {
            return array.RangeSubset<T>(fromIndex, toIndex - fromIndex + 1);
        }

        // create a subset from a specific list of indices
        public static T[] Subset<T>(this T[] array, params int[] indices)
        {
            T[] subset = new T[indices.Length];
            for (int i = 0; i < indices.Length; i++)
            {
                subset[i] = array[indices[i]];
            }
            return subset;
        }
    }

    /// <summary>
    /// Holds face processor performance trick parameters
    /// </summary>
    class FaceProcessorPerformanceParams
    {
        /// <summary>
        /// Downscale limit, texture processing will downscale input up to this size
        /// If is less or equals to zero than downscaling is not applied
        /// 
        /// Downscaling is applied with preserved aspect ratio
        /// </summary>
        public int Downscale { get; set; }

        /// <summary>
        /// Processor will skip that much frames before processing anything, 0 means no skip
        /// </summary>
        public int SkipRate { get; set; }

        /// <summary>
        /// Default constructor
        /// </summary>
        public FaceProcessorPerformanceParams()
        {
            Downscale = 0;
            SkipRate = 0;
        }
    }

    /// <summary>
    /// High-level wrapper around OpenCV and DLib functionality that simplifies face detection tasks
    /// </summary>
    class FaceProcessor<T>
        where T: UnityEngine.Texture
    {
        protected CascadeClassifier cascadeFaces = null;
        protected CascadeClassifier cascadeEyes = null;
        protected ShapePredictor shapeFaces = null;

        protected Mat processingImage = null;
        protected Double appliedFactor = 1.0;
		protected bool cutFalsePositivesWithEyesSearch = false;

        //얼굴 거리 계산용
        public int squareSideLength=0;//한변을 구해서 Red box의 크기를 구한다.
        public int squareSizeMin = 150;//얼굴의 red box의 크기가 곧 카메라와의 거리를 의미한다.
        public int squareSizeMax = 270;//무조건 정사각형임
        public bool faceDistanceCheck = true;

        /// <summary>
        /// Performance options
        /// </summary>
        public FaceProcessorPerformanceParams Performance { get; private set; }

        /// <summary>
        /// Data stabilizer parameters (face rect, face landmarks etc.)
        /// </summary>
        public DataStabilizerParams DataStabilizer { get; private set; }

        /// <summary>
        /// Processed texture
        /// </summary>
        public Mat Image { get; private set; }

        /// <summary>
        /// Detected objects
        /// </summary>
        public List<DetectedFace> Faces { get; private set; }

        /// <summary>
        /// Default constructor
        /// </summary>
        public FaceProcessor()
        {
            Faces = new List<DetectedFace>();
            DataStabilizer = new DataStabilizerParams();
            Performance = new FaceProcessorPerformanceParams();
        }

        /// <summary>
        /// Processor initializer
        /// <param name="facesCascadeData">String with cascade XML for face detection, must be defined</param>
        /// <param name="eyesCascadeData">String with cascade XML for eyes detection, can be null</param>
        /// <param name="shapeData">Binary data with trained shape predictor for 68-point face landmarks recognition, can be empty or null</param>
        /// </summary>
        public virtual void Initialize(string facesCascadeData, string eyesCascadeData, byte[] shapeData = null)
        {
            // face detector - the key thing here
            if (null == facesCascadeData || facesCascadeData.Length == 0)
                throw new Exception("FaceProcessor.Initialize: No face detector cascade passed, with parameter is required");

            FileStorage storageFaces = new FileStorage(facesCascadeData, FileStorage.Mode.Read | FileStorage.Mode.Memory);
            cascadeFaces = new CascadeClassifier();
            if (!cascadeFaces.Read(storageFaces.GetFirstTopLevelNode()))
                throw new System.Exception("FaceProcessor.Initialize: Failed to load faces cascade classifier");

            // eyes detector
            if (null != eyesCascadeData)
            {
                FileStorage storageEyes = new FileStorage(eyesCascadeData, FileStorage.Mode.Read | FileStorage.Mode.Memory);
                cascadeEyes = new CascadeClassifier();
                if (!cascadeEyes.Read(storageEyes.GetFirstTopLevelNode()))
                    throw new System.Exception("FaceProcessor.Initialize: Failed to load eyes cascade classifier");
            }

            // shape detector
            if (null != shapeData && shapeData.Length > 0)
            {
                shapeFaces = new ShapePredictor();
                shapeFaces.LoadData(shapeData);
            }
        }

        /// <summary>
        /// Creates OpenCV Mat from Unity texture
        /// </summary>
        /// <param name="texture">Texture instance, must be either Texture2D or WbCamTexture</param>
        /// <returns>Newely created Mat object, ready to use with OpenCV</returns>
        /// <param name="texParams">Texture parameters (flipped, rotated etc.)</param>
        protected virtual Mat MatFromTexture(T texture, Unity.TextureConversionParams texParams)
        {
            if (texture is UnityEngine.Texture2D)
                return Unity.TextureToMat(texture as UnityEngine.Texture2D, texParams);
            else if (texture is UnityEngine.WebCamTexture)
                return Unity.TextureToMat(texture as UnityEngine.WebCamTexture, texParams);
            else
                throw new Exception("FaceProcessor: incorrect input texture type, must be Texture2D or WebCamTexture");
        }

        /// <summary>
        /// Imports Unity texture to the FaceProcessor, can pre-process object (white balance, resize etc.)
        /// Fill few properties and fields: Image, downscaledImage, appliedScaleFactor
        /// </summary>
        /// <param name="texture">Input texture</param>
        /// <param name="texParams">Texture parameters (flipped, rotated etc.)</param>
        protected virtual void ImportTexture(T texture, Unity.TextureConversionParams texParams)
        {
            // free currently used textures
            if (null != processingImage)
                processingImage.Dispose();
            if (null != Image)
                Image.Dispose();

            // convert and prepare
            Image = MatFromTexture(texture, texParams);
            if (Performance.Downscale > 0 && (Performance.Downscale < Image.Width || Performance.Downscale < Image.Height))
            {
                // compute aspect-respective scaling factor
                int w = Image.Width;
                int h = Image.Height;

                // scale by max side
                if (w >= h)
                {
                    appliedFactor = (double)Performance.Downscale / (double)w;
                    w = Performance.Downscale;
                    h = (int)(h * appliedFactor + 0.5);
                }
                else
                {
                    appliedFactor = (double)Performance.Downscale / (double)h;
                    h = Performance.Downscale;
                    w = (int)(w * appliedFactor + 0.5);
                }

                // resize
                processingImage = new Mat();
                Cv2.Resize(Image, processingImage, new Size(w, h));
       
            }
            else
            {
                appliedFactor = 1.0;
                processingImage = Image;
            }
        }
       
        /// <summary>
        /// Detector
        /// </summary>
        /// <param name="inputTexture">Input Unity texture</param>
        /// <param name="texParams">Texture parameters (flipped, rotated etc.)</param>
        /// <param name="detect">Flag signalling whether we need detection on this frame</param>
        public virtual void ProcessTexture(T texture, Unity.TextureConversionParams texParams, bool detect = true)
        {
            // convert Unity texture to OpenCv::Mat
            ImportTexture(texture, texParams);

            // detect
            if (detect)
            {
                double invF = 1.0 / appliedFactor;
                DataStabilizer.ThresholdFactor = invF;

                // convert to grayscale and normalize
                Mat gray = new Mat();
                Cv2.CvtColor(processingImage, gray, ColorConversionCodes.BGR2GRAY);

                // fix shadows
                Cv2.EqualizeHist(gray, gray);

                /*Mat normalized = new Mat();
                CLAHE clahe = CLAHE.Create();
                clahe.TilesGridSize = new Size(8, 8);
                clahe.Apply(gray, normalized);
                gray = normalized;*/

                // detect matching regions (faces bounding)
                Rect[] rawFaces = cascadeFaces.DetectMultiScale(gray, 1.2, 6);
				if (Faces.Count != rawFaces.Length)
					Faces.Clear();

                // now per each detected face draw a marker and detect eyes inside the face rect
                int facesCount = 0;
                for (int i = 0; i < rawFaces.Length; ++i)
                {
                    Rect faceRect = rawFaces[i];
                    Rect faceRectScaled = faceRect * invF;
                    using (Mat grayFace = new Mat(gray, faceRect))
                    {
                        // another trick: confirm the face with eye detector, will cut some false positives
                        if (cutFalsePositivesWithEyesSearch && null != cascadeEyes)
                        {
                            Rect[] eyes = cascadeEyes.DetectMultiScale(grayFace);
                            if (eyes.Length == 0 || eyes.Length > 2)
                                continue;
                        }

                        // get face object
                        DetectedFace face = null;
                        if (Faces.Count < i + 1)
                        {
                            face = new DetectedFace(DataStabilizer, faceRectScaled);
                            Faces.Add(face);
                        }
                        else
                        {
                            face = Faces[i];
                            face.SetRegion(faceRectScaled);
                        }

                        // shape
                        facesCount++;
                        if (null != shapeFaces)
                        {
                            Point[] marks = shapeFaces.DetectLandmarks(gray, faceRect);

                            // we have 68-point predictor
                            if (marks.Length == 68)
                            {
                                // transform landmarks to the original image space
                                List<Point> converted = new List<Point>();
                                foreach (Point pt in marks)
                                    converted.Add(pt * invF);

                                // save and parse landmarks
                                face.SetLandmarks(converted.ToArray());
                            }
                        }
                    }
                }

                // log
                //UnityEngine.Debug.Log(String.Format("Found {0} faces", Faces.Count));
            }
        }
       
        /// <summary>
        /// Marks detected objects on the texture
        /// </summary>
        /// 
        public void MarkDetected(bool drawSubItems = true)//얼굴 면적(거리 계산) 및 Landmark 그리는 부분
        {
            // mark each found eye
            foreach (DetectedFace face in Faces)
            {

                //Red box 크기(=얼굴의 거리) 구하기
                squareSideLength = face.Region.Width;
                //허용할 범위 결정 
                if (squareSizeMin < squareSideLength && squareSideLength < squareSizeMax) 
                     faceDistanceCheck = true;
                else faceDistanceCheck = false;
                
                //얼굴 랜드마크를 그려주는 부분 - 현재 red box, blue line은 그리지 않는다. convex hull은 처음부터 주석 처리 되어있었음

                // face rect

                //Red Box
                //Cv2.Rectangle((InputOutputArray)Image, face.Region, Scalar.FromRgb(255, 0, 0), 2);

                // convex hull
                //Cv2.Polylines(Image, new IEnumerable<Point>[] { face.Info.ConvexHull }, true, Scalar.FromRgb(255, 0, 0), 2);

                // render face triangulation (should we have one)
                
                //Blue line
                //if (face.Info != null)
                //{
                //    foreach (DetectedFace.Triangle tr in face.Info.DelaunayTriangles)
                //        Cv2.Polylines(Image, new IEnumerable<Point>[] { tr.ToArray() }, true, Scalar.FromRgb(0, 0, 255), 1);
                //}

                // Sub-items
                if (drawSubItems)
                {
                    List<string> closedItems = new List<string>(new string[] { "Nose", "Eye", "Lip" });
                    foreach (DetectedObject sub in face.Elements)
                        if (sub.Marks != null)
                            Cv2.Polylines(Image, new IEnumerable<Point>[] { sub.Marks }, closedItems.Contains(sub.Name), Scalar.FromRgb(0, 255, 0), 1);
                }
            }
        }  

    }

    /// <summary>
    /// FaceProcessor subclass designed for live (web camera or stream) processing
    /// </summary>
    class FaceProcessorLive<T> : FaceProcessor<T>
        where T : UnityEngine.Texture
    {
        private int frameCounter = 0;

        /// <summary>
        /// Constructs face processor
        /// </summary>
        public FaceProcessorLive()
            : base()
        {}

        /// <summary>
        /// Detector
        /// </summary>
        /// <param name="inputTexture">Input Unity texture</param>
        /// <param name="texParams">Texture parameters (flipped, rotated etc.)</param>
        /// <param name="detect">Flag signalling whether we need detection on this frame</param>
        //추가한 변수
        public float measureFaceTime = 0.0f;//측정한 face 시간
        public float measureEmptyTime = 0.0f;
        public float resetTime = 1.0f;//이 시간동안 측정되지 않으면 파라미터 초기화
        public float takePhotoTime = 1f;//이 시간동안 face가 측정되면 사진 촬영
        public float timeControl = 5.0f;//이 스크립트에서 측정되는 deltaTime이 너무 작아서 사용중 ->  곱해주지 않으면 3초보다 더걸림
        public override void ProcessTexture(T texture, Unity.TextureConversionParams texParams, bool detect = true)
        {

            bool acceptedFrame = (0 == Performance.SkipRate || 0 == frameCounter++ % Performance.SkipRate);
            base.ProcessTexture(texture, texParams, detect && acceptedFrame);


            if (detect && acceptedFrame && Faces.Count > 0)//얼굴을 검출했을때
            {
                if (measureFaceTime < takePhotoTime)//지금까지 측정한 face 누적 검출 시간이 촬영 기준 시간보다 작으면
                    measureFaceTime += UnityEngine.Time.deltaTime*timeControl; //계속 측정한다
            }
            else//얼굴을 검출하지 못했을 때
            {
                if ((measureFaceTime < takePhotoTime) && (measureEmptyTime < resetTime))//지금까지 측정한 epmty 누적 시간이 기준 reset 시간보다 작으면
                    measureEmptyTime+= UnityEngine.Time.deltaTime*timeControl;//파라미터 초기화를 위한 empty값 증가 (얼굴이 없는 상태니까) 
            }
        }
    }
}