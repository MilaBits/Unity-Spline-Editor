using System;
using UnityEngine;

namespace Assets.SplineEditor
{
    [Serializable]
    internal struct LineSettings
    {
        [Range(1,2)]
        public int lineCount;
        public int lineSegments;
        public float lineThickness;
        public float lineSpacing;
        public SingleLinePosition SingleLinePosition;
    }

    internal enum SingleLinePosition
    {
        Center,
        Left,
        Right
    }
}