using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using UnityEngine.Serialization;
using Debug = UnityEngine.Debug;

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
    }

    // Update is called once per frame
    void Update()
    {
        if (_frameIndex % 200 == 0)
        {
            // Matrix4x4 sample1 = new Matrix4x4(
            //     new Vector4(-0.985832f, -0.16757f, -0.00175236f, -0.0159864f),
            //     new Vector4(-0.000519702f, 0.0135599f, -0.999936f, -0.00498361f),
            //     new Vector4(0.167572f, -0.985784f, -0.0134594f, 0.901313f),
            //     new Vector4(0f, 0f, 0f, 1f)
            // );
            Matrix4x4 sample1 = new Matrix4x4(
                new Vector4(-0.985832f, -0.000519702f, 0.167572f, 0f),
                new Vector4(-0.16757f, 0.0135599f, -0.985784f, 0f),
                new Vector4(-0.00175236f, -0.999936f, -0.0134594f, 0f),
                new Vector4(-0.0159864f, -0.00498361f, 0.901313f, 1f)
            );
            UpdateOutput(sample1);
        }
        else if (_frameIndex % 200 == 100)
        {
            // Matrix4x4 sample2 = new Matrix4x4(
            //     new Vector4(-0.567041f, -0.822898f, -0.0360135f, 0.33257f),
            //     new Vector4(0.0277141f, 0.0246396f, -0.999319f, 0.158565f),
            //     new Vector4(0.823222f, -0.567659f, 0.00883314f, 1.27586f),
            //     new Vector4(0f, 0f, 0f, 1f)
            // );
            // Matrix4x4 sample2 = new Matrix4x4(
            //     new Vector4(-0.567041f, 0.0277141f, 0.823222f, 0f),
            //     new Vector4(-0.822898f, 0.0246396f, -0.567659f, 0f),
            //     new Vector4(-0.0360135f, -0.999319f, 0.00883314f, 0f),
            //     new Vector4(0.33257f, 0.158565f, 1.27586f, 1f)
            // );
            Matrix4x4 sample2 = new Matrix4x4(
            new Vector4(-0.922999f, -0.350763f, -0.158216f, 0f),
            new Vector4(0.114801f, 0.141438f, -0.983266f, 0f),
            new Vector4(0.367274f, -0.925733f, -0.0902832f, 0f),
            new Vector4(-0.415403f, -0.14886f, 0.962236f, 1f)
                );
            UpdateOutput(sample2);
        }

        _frameIndex = (_frameIndex + 1) % 200;
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
            new Vector4(1, 0, 0, 0),
            new Vector4(0, 0, 1, 0),
            new Vector4(0, 1, 0, 0),
            new Vector4(0, 0, 0, 1)
        );
        
        

        // rotate
        var matrix3 = matrix2 * Matrix4x4.Rotate(Quaternion.Euler(0, 180, 0));
        // matrix3 *= Matrix4x4.Rotate(Quaternion.Euler(matrix3.rotation.x * -1f, 0, 0));

        // scale up by 50
        // TODO: no hardcode
        var matrix4 = matrix3 * Matrix4x4.Scale(new Vector3(-50f, -50f, -50f));


        Matrix4x4Extension.Decompose(matrix4, out translation, out rotation, out scale);
        
        rotation = Quaternion.Euler(new Vector3(-rotation.eulerAngles.x, rotation.eulerAngles.y, -rotation.eulerAngles.z));

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