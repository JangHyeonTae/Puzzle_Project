using UnityEngine;

[CreateAssetMenu(fileName = "Teteris", menuName ="Teteris/Block")]
public class TeterisBlock : ScriptableObject
{
    [field: SerializeField] public Sprite tetrisSprite { get; private set; }
    [field : SerializeField] public Pos[] posVectors { get; private set; }
}

[System.Serializable]
public class Pos
{
    public Vector2[] blockPos;
}