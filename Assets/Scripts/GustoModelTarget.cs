using UnityEngine;
using UnityEngine.UI;
using System;
using System.IO;
using System.Collections;
using System.Runtime.InteropServices;
using System.Text;
using Gusto;
using Unity.VisualScripting;

public class GustoModelTarget : MonoBehaviour
{    
    #if UNITY_IOS && !UNITY_EDITOR
        const string gusto_model_target_unity = "__Internal";
    #else
        const string gusto_model_target_unity = "libmodel_target";
    #endif
    [DllImport(gusto_model_target_unity)]
    public static extern int GustoModelTargetInit(out IntPtr net, int height, int width);

    [DllImport(gusto_model_target_unity)]
    public static extern int CADModelInit(
        IntPtr net,
        string model_name, string model_path, string model_metadata_path, // model_info,
        float start_threshold,
        float track_threshold,
        float[] init_pose_ret_ptr // init_pose for unity rendering
    );

    [DllImport(gusto_model_target_unity)]
    public static extern int TrackerInit(
        IntPtr net,
        float fov
    );
    [DllImport(gusto_model_target_unity)]
    public static extern int reinit(
        IntPtr net
    );
    [DllImport(gusto_model_target_unity)]
    public static extern int track(
        IntPtr net,
        IntPtr bitmap,
        float[] pose_ret_ptr,
        float[] confidences
    );



    float measure_time;
    float max_det_time;
    float min_det_time = 1000.0f;
    float total_det_time;
    int frame_count = 1;
    float start_time = 0.0f;
    float end_time = 0.0f;
    public IntPtr tracker;

    Color32[] pixels;
    GCHandle pixelsHandle;
    IntPtr pixelsPtr;
    WebCamTexture webcamTexture;
    [SerializeField] RawImage m_rawImage;
    float[] init_pose = new float[16];
    float[] result_pose = new float[16];
    float[] confidences = new float[1];

    public IntPtr webcambuffer;
    void OnGUI()
    {
        GUI.Label(new Rect(15, 125, 450, 100), "Running Platform: " + Application.platform);
        GUI.Label(new Rect(15, 150, 450, 100), "Time Estimation(ms): " + (int)measure_time);
        GUI.Label(new Rect(15, 175, 450, 100), "Avg / Min / Max: " + (int)total_det_time / frame_count + " / " + (int)min_det_time + " / " + (int)max_det_time);
    }
    void Start()
    {
        WebCamDevice[] devices = WebCamTexture.devices;
        webcamTexture = new WebCamTexture();

        if (devices.Length > 0)
        {
            webcamTexture.deviceName = devices[0].name;
            webcamTexture.Play();
        }

        Debug.Log("Webcam texture height: " + webcamTexture.height);
        Debug.Log("Webcam texture width: " + webcamTexture.width);
        GustoModelTargetInit(out tracker, webcamTexture.height, webcamTexture.width);

        CADModelInit(
            tracker,
            "Bruni",
            Gusto.Utility.retrieve_streamingassets_data("Bruni-woband/Bruni-woband.obj"),
            Gusto.Utility.retrieve_streamingassets_data("Bruni-woband/cvs/Bruni-woband.meta"),
            0.8f,
            0.8f,
            init_pose
        );
        for(int i = 0; i < 16; i++){
            Debug.Log(init_pose[i]);
        }
        TrackerInit(tracker, 60.0f);

    }
    void Update()
    {
        m_rawImage.texture = webcamTexture;
        // string ImagePath = Gusto.Utility.retrieve_streamingassets_data("gusto_engine_test/demo.png");
        
        start_time = Time.realtimeSinceStartup;
        pixels = webcamTexture.GetPixels32();
        pixelsHandle = GCHandle.Alloc(pixels, GCHandleType.Pinned);
        pixelsPtr = pixelsHandle.AddrOfPinnedObject();
        // webcambuffer = webcamTexture.GetNativeTexturePtr();
        // Color32[] test = new Color32[webcamTexture.width * webcamTexture.height];
        // Gusto.GustoNet.Gusto_Model_Inference_Image(_net, ImagePath);
        track(tracker, pixelsPtr, result_pose, confidences);
        pixelsHandle.Free();
        end_time = Time.realtimeSinceStartup;


        Debug.Log("Confidence: " + confidences[0]);



        measure_time = (end_time - start_time) * 1000.0f;
        min_det_time = Math.Min(min_det_time, measure_time);
        max_det_time = Math.Max(max_det_time, measure_time);
        total_det_time += measure_time;
        frame_count++;
    }
}
