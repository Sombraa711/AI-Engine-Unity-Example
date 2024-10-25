using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using System;
using System.IO;
using System.Collections;
using System.Runtime.InteropServices;
using System.Text;
using Ncnn;


public class gusto_opencv_example : MonoBehaviour
{    
    int debug_display_width;
    
    static int max_proposal_len = 100;
    float[] bboxes = new float[max_proposal_len * 4];
    float[] confidences = new float[max_proposal_len];
    int[] classIds = new int[max_proposal_len];
    int[] proposal_len = new int[1];


    public class MobileDetv3{
        public IntPtr net;
        public IntPtr config;
    }

    MobileDetv3 mobiledetv3 = new MobileDetv3();
    WebCamTexture m_webCamTexture;
    WebCamDevice[] m_devices;
    int camera_id = 0;
    
    [SerializeField] RawImage m_rawImage;
    void DrawQuad(Rect position, Color color) {
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0,0,color);
        texture.Apply();
        GUI.skin.box.normal.background = texture;
        GUI.Box(position, GUIContent.none);
    }

    void OnGUI ()
    {
        GUI.Label(new Rect(15, 125, 450, 100), "inputHeight: " + debug_display_width);
        for (int i = 0; i < proposal_len[0]; i++)
        {
            EditorGUI.DrawRect(new Rect(bboxes[i * 4], bboxes[i * 4 + 1], bboxes[i * 4 + 2], bboxes[i * 4 + 3]), Color.green);
            // DrawQuad(new Rect(bboxes[i * 4], bboxes[i * 4 + 1], bboxes[i * 4 + 2], bboxes[i * 4 + 3]), Color.red);
        }
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

        m_webCamTexture = new WebCamTexture();
        // m_webCamTexture = new WebCamTexture(WebCamTexture.devices[camera_id].name, 640, 640, 30);

        m_webCamTexture.Play(); //Start capturing image using webcam


        var error_check1 = Ncnn.NcnnNet.new_ncnn_net(out mobiledetv3.net);
        Debug.Log("error_check1: " + error_check1);

        Byte[] model_param_bytes = File.ReadAllBytes("/media/sombrali/HDD1/3d_object_detection/opencv-unity/gusto-engine-unity-wrapper1/Assets/Weights/mobilenetv3_ssdlite_voc.param");
        // String model_param_bytes_base64 = Convert.ToBase64String(model_param_bytes);

        Byte[] model_bin_bytes = File.ReadAllBytes("/media/sombrali/HDD1/3d_object_detection/opencv-unity/gusto-engine-unity-wrapper1/Assets/Weights/mobilenetv3_ssdlite_voc.bin");
        // String model_bin_bytes_base64 = Convert.ToBase64String(model_bin_bytes);

        var error_check2 = Ncnn.NcnnNet.load_ncnn_model_param_mem(mobiledetv3.net, model_param_bytes);
        Debug.Log("error_check2: " + error_check2);

        var error_check3 = Ncnn.NcnnNet.load_ncnn_model_mem(mobiledetv3.net, model_bin_bytes);
        Debug.Log("error_check3: " + error_check2);



        var error_check4 = Ncnn.NcnnNet.load_model_config_from_csharp(out mobiledetv3.config, 300, 300, 0.5f, 0.5f);
        Debug.Log("error_check4: " + error_check4);
        
        var error_check5 = Ncnn.NcnnNet.config_ncnn_net(mobiledetv3.net, mobiledetv3.config);
        Debug.Log("error_check5: " + error_check5);
    }


    void Update()
    {
        // if (m_webCamTexture.isPlaying == false)
        // {
        //     return;
        // }

        m_rawImage.texture = m_webCamTexture; //display the image on the RawImage
        
        
        // proposal_len[0] = 0;

        // Ncnn.NativeUtility.read_frame_buffer_from_csharp(mobiledetv3.net, m_webCamTexture.GetPixels32(), m_webCamTexture.height, m_webCamTexture.width);
        Ncnn.NcnnNet.infer(
            mobiledetv3.net, 
            m_webCamTexture.GetPixels32(), m_webCamTexture.height, m_webCamTexture.width,
            bboxes, confidences, classIds, proposal_len
        );
        Debug.Log("proposal_len1: " + proposal_len[0]);
        for (int i = 0; i < proposal_len[0]; i++)
        {
            Debug.Log("bbox: " + bboxes[i * 4] + " " + bboxes[i * 4 + 1] + " " + bboxes[i * 4 + 2] + " " + bboxes[i * 4 + 3]);
            Debug.Log("confidences: " + confidences[i]);
            Debug.Log("classIds: " + classIds[i]);
        }
    }
}