using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Utility : MonoBehaviour
{

    public const float interval = 1.0f / 60.0f;

    static public void PassTransform(NetworkObjects.NetworkPlayer receiver, NetworkObjects.NetworkPlayer passer)
    {
        if (!IsSameID(receiver, passer)) return;

        receiver.cubePos = passer.cubePos;
        receiver.cubeRot = passer.cubeRot;
        receiver.prevBeat = passer.prevBeat;
    }

    static public bool IsSameID(NetworkObjects.NetworkPlayer p1, NetworkObjects.NetworkPlayer p2)
    {
        if (p1.id == p2.id)
            return true;
        else
            return false;
    }
}
