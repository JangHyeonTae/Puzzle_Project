using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName ="Stage", menuName = "Stage/StageSO")]
public class StageSO : ScriptableObject
{
    [field : SerializeField] public string stageName { get; private set; }
    [field : SerializeField] public Vector3[] NodeVector { get; private set; }
}
