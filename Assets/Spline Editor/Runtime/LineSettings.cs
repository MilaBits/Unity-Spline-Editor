using System;
using UnityEngine;

namespace Assets.SplineEditor
{
    [Serializable]
    internal struct LineSettings
    {
        //[Range(1,2)]
        //public int lineCount;
        public int lineSegments;
        public float lineThickness;
        public float lineSpacing;
        public LineConfiguration lineConfiguration;

        public int lineCount => lineConfiguration == LineConfiguration.Double ? 2 : 1;
    }


    internal enum LineConfiguration
    {
        Center,
        Left,
        Right,
        Double,
    }
}