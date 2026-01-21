using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Poly", menuName = "Poly/Hexa")]
public class PolySO : ScriptableObject
{
    [field: SerializeField] public Color objColor { get; private set; }
    [field: SerializeField] public string polyName { get; private set; }
}
