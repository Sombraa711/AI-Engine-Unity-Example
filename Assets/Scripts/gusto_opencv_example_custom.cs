using UnityEngine;
using UnityEngine.UI;
using System;
using System.IO;
using System.Collections;
using System.Runtime.InteropServices;

using System.Text;

using Gusto;
public class gusto_opencv_example : MonoBehaviour
{    

    
    static int max_proposal_len = 100;
    float[] bboxes = new float[max_proposal_len * 4];
    float[] confidences = new float[max_proposal_len];
    int[] classIds = new int[max_proposal_len];
    int[] proposal_len = new int[1];


    float measure_time;
    float max_det_time;
    float min_det_time = 1000.0f;
    float total_det_time;
    int frame_count = 1;

    WebCamTexture m_webCamTexture;
    WebCamDevice[] m_devices;
    int camera_id = 0;
    
    [SerializeField] RawImage m_rawImage;

    public IntPtr detector;
    void OnGUI ()
    {
        GUI.Label(new Rect(15, 125, 450, 100), "Running Platform: " + Application.platform);
        GUI.Label(new Rect(15, 150, 450, 100), "Time Estimation(ms): " + measure_time);
        GUI.Label(new Rect(15, 175, 450, 100), "Avg / Min / Max: " + total_det_time / frame_count + " / " + min_det_time + " / " + max_det_time);
    }

    void Start()
    {
        Debug.Log("Running Platform: " + Application.platform);
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

        m_webCamTexture.Play(); //Start capturing image using webcam


        string t_modelpath = Gusto.Utility.retrieve_streamingassets_data("Weights/yolov7-tiny-20240821-3cls2.onnx");
        string t_clsnamespath = Gusto.Utility.retrieve_streamingassets_data("Weights/cls_names.names");

        int len_str_for_cpp = 1024;

        StringBuilder model_path = new StringBuilder(
            t_modelpath, 
            len_str_for_cpp
        );
        StringBuilder cls_names_path = new StringBuilder(
            t_clsnamespath, 
            len_str_for_cpp
        );


        Gusto.GustoNet.net_new(out detector);
        Gusto.GustoNet.net_compile(detector, 640, 640, 0.5f, 0.5f, model_path, cls_names_path, len_str_for_cpp);

    }

    void Update()
    {
        if (m_webCamTexture.isPlaying == false)
        {
            return;
        }

        m_rawImage.texture = m_webCamTexture; //display the image on the RawImage

        // resset
        proposal_len[0] = 0;

        measure_time = Gusto.GustoNet.infer(
            detector, 
            m_webCamTexture.GetPixels32(), m_webCamTexture.height, m_webCamTexture.width, 
            bboxes, confidences, classIds, proposal_len);
        min_det_time = Math.Min(min_det_time, measure_time);
        max_det_time = Math.Max(max_det_time, measure_time);
        total_det_time += measure_time;
        frame_count += 1;

        Debug.Log("proposal_len: " + proposal_len[0]);
        // for (int i = 0; i < proposal_len[0]; i++)
        // {
        //     Debug.Log("bbox: " + bboxes[i * 4] + " " + bboxes[i * 4 + 1] + " " + bboxes[i * 4 + 2] + " " + bboxes[i * 4 + 3]);
        //     Debug.Log("confidences: " + confidences[i]);
        //     Debug.Log("classIds: " + classIds[i]);

        //     int x = (int)(bboxes[i * 4]);
        //     int y = (int)(bboxes[i * 4 + 1]);
        //     int w = (int)(bboxes[i * 4 + 2]);
        //     int h = (int)(bboxes[i * 4 + 3]);

        //     // Texture2D rgbTexture = frame.ToTexture2D();
        //     Texture croppedTexture = new Texture2D(w, h, TextureFormat.RGBA32, false);

        //     Graphics.CopyTexture(m_webCamTexture, 0, 0, x, y, w, h, croppedTexture, 0, 0, 0, 0);

        //     m_rawImage.texture = croppedTexture;
        // }

        // int faceX = (int)(frame.Cols * Mathf.Clamp01(face.rectangle.left));
        // int faceY = (int)(frame.Rows * Mathf.Clamp01(face.rectangle.top));
        // int faceWidth = (int)(frame.Cols * Mathf.Clamp01(face.rectangle.width));
        // int faceHeight = (int)(frame.Rows * Mathf.Clamp01(face.rectangle.height));

    }
}