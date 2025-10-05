using System.Collections.Generic;
using UnityEngine;

// Cadeado global para bloquear interação da câmera (ou outros sistemas)
// Painéis chamam Lock(this) ao abrir e Unlock(this) ao fechar.
public static class UIInputLock
{
    static readonly HashSet<object> _owners = new HashSet<object>();
    public static bool IsLocked => _owners.Count > 0;

    public static void Lock(object owner)
    {
        if (owner == null) owner = typeof(UIInputLock);
        _owners.Add(owner);
    }

    public static void Unlock(object owner)
    {
        if (owner == null) owner = typeof(UIInputLock);
        _owners.Remove(owner);
    }

    public static void ForceUnlockAll()
    {
        _owners.Clear();
    }
}
