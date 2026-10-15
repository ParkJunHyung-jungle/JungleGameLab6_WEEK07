using System.Collections.Generic;

using UnityEngine;

using Game.Session;

public sealed class KeyboardDispenser : MonoBehaviour
{
    [Header("Keyboard Stock")]
    [SerializeField] private List<GameObject> _keyboardPrefabs = new List<GameObject>();
    [SerializeField, Min(0), InspectorName("구매 비용 (원)")] private int _purchaseCost;

    [Header("Output")]
    [SerializeField, InspectorName("배출 위치")] private Transform _outputPoint;
    [SerializeField, Min(0f)] private float _ejectImpulse = 1.5f;

    /// <summary>
    /// 등록된 프리팹 중 하나를 선택하고 비용을 확인한 뒤 키보드를 배출한다.
    /// interactionController와 gameSession을 사용하며 구매 성공 여부를 반환하고 월드 키보드 및 잔액을 갱신한다.
    /// </summary>
    public bool TryPurchase(KeyboardInteractionController interactionController, GameSession gameSession)
    {
        if (_keyboardPrefabs.Count == 0)
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

        int purchaseCost = Mathf.Max(0, _purchaseCost);
        if (purchaseCost > 0 && gameSession.Wallet.Balance < purchaseCost)
            return false;

        KeyboardInteractionController.KeyboardState keyboard =
            interactionController.SpawnDispenserKeyboard(prefab, _outputPoint);

        if (purchaseCost > 0)
        {
            if (!gameSession.Wallet.TrySpend(purchaseCost))
            {
                interactionController.DiscardTemporaryKeyboard(keyboard.Root);
                return false;
            }

            gameSession.Wallet.NotifyBalanceChanged();
        }

        keyboard.Body.AddForce(_outputPoint.forward * _ejectImpulse, ForceMode.Impulse);
        return true;
    }
}
