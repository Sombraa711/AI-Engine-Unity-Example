using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Pose_Visualizer
{
    /*
     * Process raw data:
     * replace ((?:-?\d\.?\d*, ){16,})|.*
     * with $1
     *
     * replace ^\s+$\n
     * with nothing
     */
    public class PoseTest
    {
        static List<Matrix4x4> _testMatries = new List<Matrix4x4>();
        static int _index = 0;

        public static void Init()
        {
            Debug.Log("TestData init");
            try
            {
                var lines = File.ReadAllLines("Assets/Pose Visualizer/TestData2.txt");
                for (int i = 0; i < lines.Length; i++)
                {
                    var values = lines[i].Split(",");
                    if (values.Length != 17)
                    {
                        continue;
                    }

                    // translate values string array to float array
                    float[] floatValues = new float[values.Length];
                    for (int j = 0; j < 16; j++)
                    {
                        floatValues[j] = float.Parse(values[j]);
                    }

                    Matrix4x4 mat = new Matrix4x4(
                        new Vector4(floatValues[0], floatValues[1], floatValues[2], floatValues[3]),
                        new Vector4(floatValues[4], floatValues[5], floatValues[6], floatValues[7]),
                        new Vector4(floatValues[8], floatValues[9], floatValues[10], floatValues[11]),
                        new Vector4(floatValues[12], floatValues[13], floatValues[14], floatValues[15])
                    );
                    
                    _testMatries.Add(mat);
                }

                Debug.Log($"TestData loaded: {_testMatries.Count} matrices");
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }
        
        public static Matrix4x4 GetNextData()
        {
            if (_testMatries.Count == 0)
            {
                Debug.LogWarning("No test data loaded");
                return Matrix4x4.identity;
            }
            _index = (_index + 1) % _testMatries.Count;
            return _testMatries[_index];
        }
        
        public static Matrix4x4 GetData(int index)
        {
            if (_testMatries.Count == 0)
            {
                Debug.LogWarning("No test data loaded");
                return Matrix4x4.identity;
            }
            return _testMatries[index];
        }
    }
}