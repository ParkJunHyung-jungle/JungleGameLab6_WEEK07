using System;

using UnityEngine;
using UnityEngine.EventSystems;

public sealed class KeyboardCustomizationTestButton : MonoBehaviour, IPointerClickHandler
{
    public event Action Clicked;

    /// <summary>eventData의 왼쪽 클릭을 받아 연결된 C# 이벤트를 호출한다. 반환값 없이 UI 동작을 실행한다.</summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left) Clicked?.Invoke();
    }
}
