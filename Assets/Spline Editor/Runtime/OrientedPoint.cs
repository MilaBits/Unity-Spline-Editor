using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct OrientedPoint
{
    public Vector3 position;
    public Quaternion rotation;

    public OrientedPoint(Vector3 pos, Quaternion rot)
    {
        this.position = pos;
        this.rotation = rot;
    }

    public OrientedPoint(Vector3 pos, Vector3 forward)
    {
        this.position = pos;
        this.rotation = Quaternion.LookRotation(forward, new Vector3(0,0,-1));
    }

    public Vector3 LocalToWorld(Vector3 localSpacePosition)
    {
        return position + rotation * localSpacePosition;
    }
}
