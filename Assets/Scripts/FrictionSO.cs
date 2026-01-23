using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "FrictionSO", menuName = "Scriptable Objects/Game/Frictions")]
public class FrictionSO : ScriptableObject
{
    [Header("Surface Identification")]
    public Texture[] textures;

    [Header("Marble Blast Surface Physics")]
    public float friction;
    public float restitution;
    public float bounce;
}
