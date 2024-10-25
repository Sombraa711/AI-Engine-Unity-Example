using UnityEngine;
using UnityEngine.UI;
using System;
using System.IO;
using System.Collections;
using System.Runtime.InteropServices;
using System.Text;
public class gusto_opencv_example : MonoBehaviour
{    
    [DllImport("__Internal")]
    public static extern void load_model_config_from_csharp(
        StringBuilder modelpath, 
        StringBuilder clsnamepath, 
        int len_string,
        int inpHeight, 
        int inpWidth, 
        float confThreshold, 
        float nmsThreshold
    );

    [DllImport("__Internal")]
    public static extern int dnn_init();

    [DllImport("__Internal")]
    public static extern int read_frame_buffer_from_csharp(Color32[] bitmap, int height, int width);
    [DllImport("__Internal")]
    public static extern int infer(float[] bboxes, float[] confidences, float[] classIds, int proposal_len);


    int debug_display_width;
    
    static int max_proposal_len = 100;
    float[] bboxes = new float[max_proposal_len * 4];
    float[] confidences = new float[max_proposal_len];
    float[] classIds = new float[max_proposal_len];
    int proposal_len;
    void OnGUI ()
    {
        GUI.Label(new Rect(15, 125, 450, 100), "inputHeight: " + debug_display_width);
    }


    string retrieve_streamingassets_data(string rel_path_to_streamingassets)
    {
        string datapath;
        if (Application.platform == RuntimePlatform.Android)
        {
            // Android
            string oriPath = System.IO.Path.Combine(Application.streamingAssetsPath, rel_path_to_streamingassets);
            
            // Android only use WWW to read file
            WWW reader = new WWW(oriPath);
            while ( ! reader.isDone) {}
            
            datapath = System.IO.Path.Combine(Application.persistentDataPath, rel_path_to_streamingassets);

            System.IO.FileInfo file = new System.IO.FileInfo(datapath);
            file.Directory.Create();
            System.IO.File.WriteAllBytes(datapath, reader.bytes);
            
        }else{
            // iOS
            datapath = System.IO.Path.Combine(Application.streamingAssetsPath, rel_path_to_streamingassets);
        }
        return datapath;
    }


    WebCamTexture m_webCamTexture;
    WebCamDevice[] m_devices;
    int camera_id = 0;

    [SerializeField] RawImage m_rawImage;

    void Start()
    {
        
        m_devices = WebCamTexture.devices;

        if (m_devices.Length == 0)
        {
            throw new Exception("No camera device found");
        }

        int max_id = m_devices.Length - 1;
        if (camera_id > max_id)
        {
            if (m_devices.Length == 1)
            {
                throw new Exception("Camera with id " + camera_id + " not found. camera_id value should be 0");
            }
            else
            {
                throw new Exception("Camera with id " + camera_id +
                                    " not found. camera_id value should be between 0 and " + max_id.ToString());
            }
        }

        m_webCamTexture = new WebCamTexture(WebCamTexture.devices[camera_id].name, 640, 640, 30);

        m_webCamTexture.Play(); //Start capturing image using webcam



        string t_modelpath = retrieve_streamingassets_data("Weights/yolov7-tiny-20240821-3cls2.onnx");
        string t_clsnamespath = retrieve_streamingassets_data("Weights/cls_names.names");
        int len_str_for_cpp = 1024;

        StringBuilder model_path = new StringBuilder(
            t_modelpath, 
            len_str_for_cpp
        );
        StringBuilder cls_names_path = new StringBuilder(
            t_clsnamespath, 
            len_str_for_cpp
        );

        load_model_config_from_csharp(model_path, cls_names_path, len_str_for_cpp, 640, 640, 0.5f, 0.5f);
        debug_display_width = dnn_init();


    }
    void Update()
    {
        if (m_webCamTexture == null)
        {
            return;
        }
        m_rawImage.texture = m_webCamTexture; 
        // resset
        proposal_len = 0;
        read_frame_buffer_from_csharp(m_webCamTexture.GetPixels32(), m_webCamTexture.height, m_webCamTexture.width);
        infer(bboxes, confidences, classIds, proposal_len);
        Debug.Log("proposal_len: " + proposal_len);
        for (int i = 0; i < proposal_len; i++)
        {
            Debug.Log("bbox: " + bboxes[i * 4] + " " + bboxes[i * 4 + 1] + " " + bboxes[i * 4 + 2] + " " + bboxes[i * 4 + 3]);
            Debug.Log("confidences: " + confidences[i]);
            Debug.Log("classIds: " + classIds[i]);
        }
    }
}