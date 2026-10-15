using System.Collections.Generic;

using UnityEngine;

using Game.Player;

public sealed class KeyboardDispenser : MonoBehaviour
{
    [Header("Keyboard Stock")]
    [SerializeField] private List<GameObject> _keyboardPrefabs = new List<GameObject>();

    [Header("Output")]
    [SerializeField, InspectorName("배출 위치")] private Transform _outputPoint;
    [SerializeField, Min(0f)] private float _ejectImpulse = 1.5f;

    /// <summary>
    /// 등록된 프리팹 중 하나를 배출 위치에 생성하고 밀어낸다.
    /// _keyboardPrefabs와 _outputPoint를 사용하며 생성 오브젝트를 keyboardRoot에 저장하고 성공 여부를 반환한다.
    /// </summary>
    public bool TryDispense(out GameObject keyboardRoot)
    {
        keyboardRoot = null;
        if (_keyboardPrefabs.Count == 0 || _outputPoint == null)
            return false;

        GameObject prefab = null;
        int startIndex = Random.Range(0, _keyboardPrefabs.Count);
        for (int offset = 0; offset < _keyboardPrefabs.Count; offset++)
        {
            GameObject candidate = _keyboardPrefabs[(startIndex + offset) % _keyboardPrefabs.Count];
            if (candidate == null)
                continue;

            prefab = candidate;
            break;
        }

        if (prefab == null)
            return false;

        keyboardRoot = Instantiate(prefab, _outputPoint.position, _outputPoint.rotation);
        keyboardRoot.transform.localScale = prefab.transform.localScale;
        keyboardRoot.AddComponent<KeyboardPickupTarget>();
        Rigidbody keyboardBody = keyboardRoot.GetComponent<Rigidbody>();
        if (keyboardBody == null)
            keyboardBody = keyboardRoot.GetComponentInChildren<Rigidbody>();

        if (keyboardBody != null)
            keyboardBody.AddForce(_outputPoint.forward * _ejectImpulse, ForceMode.Impulse);
        return true;
    }
}
