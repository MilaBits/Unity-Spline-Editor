using System;
using UnityEngine;

namespace SplineEditor
{
    [Serializable]
    public struct LineSettings
    {
        //[Range(1,2)]
        //public int lineCount;
        public int lineSegments;
        public float lineThickness;
        public float lineSpacing;
        public Color lineColor;
        public LineConfiguration lineConfiguration;

        public int lineCount => lineConfiguration == LineConfiguration.Double ? 2 : 1;
    }


    public enum LineConfiguration
    {
        Center,
        Left,
        Right,
        Double,
    }
}