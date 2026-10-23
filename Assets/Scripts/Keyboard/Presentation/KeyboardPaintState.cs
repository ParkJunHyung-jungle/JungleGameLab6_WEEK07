using System;

using UnityEngine;

[Serializable]
public sealed class KeyboardPaintState
{
    [SerializeField] private string[] _keycapPaint;

    public string[] KeycapPaint => _keycapPaint;

    /// <summary>
    /// keycapPaint의 키캡별 PNG 데이터를 저장 상태로 보관한다.
    /// 배열을 복제해 반환하며 키보드 외형은 변경하지 않는다.
    /// </summary>
    public KeyboardPaintState(string[] keycapPaint)
    {
        _keycapPaint = (string[])keycapPaint.Clone();
    }
}
