using UnityEngine;

public class Entity : MonoBehaviour
{
    [SerializeField] protected string m_name;
    public string Name => m_name;
}