using System;
using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;
using Debug = UnityEngine.Debug;
using Gusto;

public class PoseVisualizer : MonoBehaviour
{
    public static PoseVisualizer Instance { get; private set; }

    [SerializeField]
    private Transform _headbandTransform;

    [SerializeField]
    private RectTransform _screenTransform;

    private Rect _screenRect;

    private static readonly float CAMERA_WIDTH = 800f;
    private static readonly float CAMERA_HEIGHT = 448f;
    public IntPtr tracker;
    float[] result_pose = new float[16];
    float[] confidences = new float[1];
    WebCamTexture webcamTexture;
    [SerializeField] RawImage m_rawImage;
    int _frameIndex = 0;

    // Start is called before the first frame update
    void Start()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;

            var tl = MPVector3toWorldPosition(_screenTransform, new Vector3(0, 0, 0));
            var br = MPVector3toWorldPosition(_screenTransform, new Vector3(1, 1, 0));
            _screenRect = new Rect(tl.x, br.y, br.x - tl.x, tl.y - br.y);

            Debug.Log($"tl = {tl}, br = {br}");
        }
        WebCamDevice[] devices = WebCamTexture.devices;
        webcamTexture = new WebCamTexture();

        if (devices.Length > 0)
        {
            webcamTexture.deviceName = devices[0].name;
            webcamTexture.Play();
        }
        Debug.Log("Webcam texture height: " + webcamTexture.height);
        Debug.Log("Webcam texture width: " + webcamTexture.width);
        
        Matrix4x4 init_pose_ = new Matrix4x4(
            new Vector4(1f, 0f, 0f, 0f),
            new Vector4(0f, 0f, -1f, 0f),
            new Vector4(0f, -1f, 0f, 0f),
            new Vector4(0f, 0f, 0.8f, 1f)
        );
        UpdateOutput(init_pose_);

        Gusto.ModelTarget.GustoModelTargetInit(out tracker, webcamTexture.height, webcamTexture.width);

        float[] init_pose = new float[16];
        Gusto.ModelTarget.CADModelInit(
            tracker,
            "Bruni",
            Gusto.Utility.retrieve_streamingassets_data("Bruni-woband/Bruni-woband.obj"),
            Gusto.Utility.retrieve_streamingassets_data("Bruni-woband/cvs/Bruni-woband.meta"),
            0.6f,
            0.5f,
            init_pose
        );

        // for(int i = 0; i < 16; i++){
        //     Debug.Log(init_pose[i]);
        // }
        Gusto.ModelTarget.TrackerInit(tracker, 70.0f);


    }

    // Update is called once per frame
    void Update()
    {
        m_rawImage.texture = webcamTexture;

        Color32[] pixels = webcamTexture.GetPixels32();
        GCHandle pixelsHandle = GCHandle.Alloc(pixels, GCHandleType.Pinned);
        IntPtr pixelsPtr = pixelsHandle.AddrOfPinnedObject();
        Gusto.ModelTarget.track(tracker, pixelsPtr, result_pose, confidences);

        // Debug.Log("Confidence: " + confidences[0]);
        // Debug.Log($"result_pose = {result_pose[0]}, {result_pose[1]}, {result_pose[2]}, {result_pose[3]}");
        // Debug.Log($"result_pose = {result_pose[4]}, {result_pose[5]}, {result_pose[6]}, {result_pose[7]}");
        // Debug.Log($"result_pose = {result_pose[8]}, {result_pose[9]}, {result_pose[10]}, {result_pose[11]}");
        // Debug.Log($"result_pose = {result_pose[12]}, {result_pose[13]}, {result_pose[14]}, {result_pose[15]}");
        var output = "";
        for (int i = 0; i < 16; i++)
        {
            output += result_pose[i] + ", ";
            // Debug.Log(result_pose[i]);
        }
        Debug.Log($"{output}");
        if (confidences[0] > 0.8){
            Matrix4x4 sample = new Matrix4x4(
                new Vector4(result_pose[0], result_pose[1], result_pose[2], result_pose[3]),
                new Vector4(result_pose[4], result_pose[5], result_pose[6], result_pose[7]),
                new Vector4(result_pose[8], result_pose[9], result_pose[10], result_pose[11]),
                new Vector4(result_pose[12], result_pose[13], result_pose[14], result_pose[15])
            );
            UpdateOutput(sample);
        }



    }

    static public void InitOutput(Matrix4x4 matrix)
    {
        UpdateOutput(matrix);
    }
    // GustoModelTarget output 
    //     ----------   y = -0.5
    //   |            |
    //   |    0, 0    |
    //   |            |
    //  x  ---------- x, y = 0.5, 0.5
    //   = -0.5

    static public void UpdateOutput(Matrix4x4 matrix)
    {
        Vector3 translation;
        Quaternion rotation;
        Vector3 scale;
        // flip y and z axis
        var matrix2 = matrix * new Matrix4x4(
            new Vector4(-1, 0, 0, 0),
            new Vector4(0, 0, 1, 0),
            new Vector4(0, 1, 0, 0),
            new Vector4(0, 0, 0, 1)
        );
        
        

        // rotate
        var matrix3 = matrix2 * Matrix4x4.Rotate(Quaternion.Euler(180, 180, 0));
        // matrix3 *= Matrix4x4.Rotate(Quaternion.Euler(matrix3.rotation.x * -1f, 0, 0));

        // scale -1 if needed
        // var matrix4 = matrix3 * Matrix4x4.Scale(new Vector3(-1f, -1f, -1f));


        Matrix4x4Extension.Decompose(matrix3, out translation, out rotation, out scale);
        
        rotation = Quaternion.Euler(new Vector3(-rotation.eulerAngles.x, rotation.eulerAngles.y, -rotation.eulerAngles.z));
        
        // for orthographic camera, scale headband using fov and z distance
        var scaleRatio = Mathf.Tan(Mathf.Deg2Rad * 35f) / (matrix.m23 * Mathf.Tan(Mathf.Deg2Rad * 35f));
        scale = new Vector3(scaleRatio, scaleRatio, scaleRatio);

        Instance._headbandTransform.position = new Vector3(matrix.m03 * Instance._screenRect.width,
            matrix.m13 * Instance._screenRect.height, matrix.m23);
        Instance._headbandTransform.rotation = rotation;
        Instance._headbandTransform.localScale = scale;
    }

    public static Vector3 MPVector3toWorldPosition(RectTransform screenTransform, Vector3 mpVector3)
    {
        var screenRect = screenTransform.rect;
        var position = new Vector3(
            (mpVector3.x - 0.5f) * screenRect.width,
            (mpVector3.y - 0.5f) * screenRect.height,
            mpVector3.z * screenRect.width
        );
        return screenTransform.TransformPoint(position);
    }
}