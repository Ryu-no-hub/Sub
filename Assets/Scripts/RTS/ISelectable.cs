using UnityEngine;

public interface ISelectable
{
    int Team { get; }
    bool Alive { get; }
    GameObject gameObject { get; }
    void Select();
    void Deselect();
}