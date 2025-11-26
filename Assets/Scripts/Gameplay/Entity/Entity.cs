using UnityEngine;

public class Entity : MonoBehaviour
{
    [SerializeField] protected string _name;
    public string Name => _name;
}