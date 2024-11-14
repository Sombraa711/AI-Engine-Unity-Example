using UnityEngine;
using UnityEngine.UI;
using System;
using System.IO;
using System.Collections;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Gusto;
using Unity.Sentis;
using UnityEngine.UIElements;
using TMPro;
using Cysharp.Threading.Tasks;

public class gusto_sentis_facelandmark : MonoBehaviour
{    
    public GameObject cube_object;
    float measure_time;
    float max_det_time;
    float min_det_time = 1000.0f;
    float total_det_time;
    int frame_count = 1;

    bool has_det = false;
    WebCamTexture m_webCamTexture;
    RenderTexture m_tempRenderTexture;
    WebCamDevice[] m_devices;
    int camera_id = 0;
    
    [SerializeField] RectTransform m_debugRect;
    [SerializeField] RawImage m_rawImage;

    Gusto.ModelLib.FaceDetector face_detector;
    Gusto.ModelLib.FaceLandMarker face_landmarker;
    

    void OnGUI ()
    {
        GUI.Label(new Rect(15, 125, 450, 100), "Running Platform: " + Application.platform);
        GUI.Label(new Rect(15, 150, 450, 100), "Time Estimation(ms): " + measure_time);
        GUI.Label(new Rect(15, 175, 450, 100), "Avg / Min / Max: " + total_det_time / frame_count + " / " + min_det_time + " / " + max_det_time);
    }

    void Awake()
    {
        Application.targetFrameRate = 60;
    }
    void OnEnable() => AsyncLoop().Forget();
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

        face_detector = new Gusto.ModelLib.FaceDetector();
        face_detector.LoadModel(Gusto.Utility.retrieve_streamingassets_data("Weights/face_detector.sentis"), Gusto.Utility.retrieve_streamingassets_data("anchor.bin"));

        face_landmarker = new Gusto.ModelLib.FaceLandMarker();
        face_landmarker.LoadModel(Gusto.Utility.retrieve_streamingassets_data("Weights/face_landmarks_detector.sentis"), Gusto.Utility.retrieve_streamingassets_data("geometry_pipeline_metadata_including_iris_landmarks.json"));

    }
    float start_time = 0.0f;
    float end_time = 0.0f;

    async UniTaskVoid AsyncLoop()
    {
        while (isActiveAndEnabled)
        {
            await UniTask.Yield(destroyCancellationToken);
            try
            {
                if (m_webCamTexture.isPlaying == false)
                {
                    continue;
                }

                var rect = m_rawImage.rectTransform.rect;
                m_rawImage.texture = m_webCamTexture; //display the image on the RawImage

                start_time = Time.realtimeSinceStartup;
                face_detector.AsyncInfer(m_webCamTexture);
                var output = await face_detector.AsyncFetchOutput(0.8f, 0.5f, 100, false);
                var ret_bboxes = output.bboxes;
                var ret_scores = output.scores;

                for (int i = 0; i < ret_bboxes.Count; i++)
                {
                    var bbox = ret_bboxes[i];
                    var score = ret_scores[i];
                    // Debug.Log("bbox: " + bbox + " score: " + score);
                    //m_rawImage.texture = face_landmarker.CropTexture(m_webCamTexture, bbox);
                    face_landmarker.AsyncInfer(face_landmarker.CropTexture(m_webCamTexture, bbox));
                    var output_landmark = await face_landmarker.AsyncFetchOutput();
                    // Debug.Log("bbox: " + bbox + " score: " + score);

                    var landmarks = output_landmark.landmarks;
                    var scores = output_landmark.score;
                    var face_pose = output_landmark.pose;
                    cube_object.transform.rotation = face_pose.rotation;
                    cube_object.transform.position = face_pose.GetColumn(3);
                    Debug.Log("landmarks: " + landmarks + " scores: " + scores + " face_pose: " + face_pose);

                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            finally
            {
                end_time = Time.realtimeSinceStartup;
                measure_time = (end_time - start_time) * 1000.0f;
                min_det_time = Math.Min(min_det_time, measure_time);
                max_det_time = Math.Max(max_det_time, measure_time);
                total_det_time += measure_time;
                frame_count++;
            }
        }
    }

    void OnDestroy()
    {
        face_detector.ReleaseModel();
        face_landmarker.ReleaseModel();
    }
}

