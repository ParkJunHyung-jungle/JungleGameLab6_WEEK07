using System.Collections.Generic;

using UnityEngine;

using Game.Tournament;

public sealed class KeycapStatusEffects
{
    private sealed class Entry
    {
        public KeycapStatusEffectDefinition Definition;
        public float RemainingTime;
        public int RemainingHits;
        public int Stacks;
        public GameObject Marker;
    }

    private readonly Transform _owner;
    private readonly List<Entry> _entries = new List<Entry>();
    public int Count => _entries.Count;

    /// <summary>owner를 상태 표시의 부모로 보관한다. 상태와 표시 인스턴스는 이 키캡에만 귀속된다.</summary>
    public KeycapStatusEffects(Transform owner) { _owner = owner; }

    /// <summary>definition의 상태를 부여하거나 재부여 규칙으로 갱신한다. SO는 변경하지 않으며 키캡별 시간, 횟수, 중첩과 표시를 관리한다.</summary>
    public void Apply(KeycapStatusEffectDefinition definition)
    {
        Entry entry = _entries.Find(item => item.Definition == definition);
        if (entry != null && definition.Reapply == KeycapStatusReapply.Ignore) return;
        if (entry == null)
        {
            entry = new Entry { Definition = definition, Stacks = 1 };
            if (definition.MarkerPrefab != null)
            {
                entry.Marker = Object.Instantiate(definition.MarkerPrefab, _owner);
                entry.Marker.transform.localPosition = definition.MarkerOffset;
                entry.Marker.transform.localRotation = Quaternion.identity;
                entry.Marker.transform.localScale *= definition.MarkerScale;
            }
            _entries.Add(entry);
        }
        else if (definition.Reapply == KeycapStatusReapply.Stack)
            entry.Stacks = Mathf.Min(entry.Stacks + 1, definition.MaxStacks);
        entry.RemainingTime = definition.Duration ?? 0f;
        entry.RemainingHits = definition.HitCount;
    }

    /// <summary>deltaTime만큼 유효 시간을 감소시키고 만료된 상태와 표시를 제거한다. 지속 시간이 null인 상태는 시간 경과로 소모하지 않는다.</summary>
    public void Tick(float deltaTime)
    {
        for (int index = _entries.Count - 1; index >= 0; index--)
        {
            Entry entry = _entries[index];
            if (!entry.Definition.Duration.HasValue) continue;
            entry.RemainingTime -= deltaTime;
            if (entry.RemainingTime <= 0f) RemoveAt(index);
        }
    }

    /// <summary>damage와 kind에 반응하는 상태의 추가 피해와 배율을 합산해 최종 피해를 반환한다. 시간 제한 없는 상태는 이 타격에서 한 횟수를 소모하며 별도 피해 호출은 발생시키지 않는다.</summary>
    public int ResolveDamage(int damage, KeycapHitKind kind)
    {
        float bonus = 0f;
        float multiplier = 1f;
        for (int index = _entries.Count - 1; index >= 0; index--)
        {
            Entry entry = _entries[index];
            if (entry.Definition.Duration.HasValue && entry.RemainingTime <= 0f) { RemoveAt(index); continue; }
            if (entry.Definition.DirectHitsOnly && kind != KeycapHitKind.Direct) continue;
            bonus += entry.Definition.BonusDamage * entry.Stacks;
            multiplier *= Mathf.Pow(entry.Definition.DamageMultiplier, entry.Stacks);
            if (!entry.Definition.Duration.HasValue && --entry.RemainingHits <= 0) RemoveAt(index);
        }
        return Mathf.Max(0, Mathf.CeilToInt((damage + bonus) * multiplier));
    }

    /// <summary>모든 상태와 표시를 제거한다. 입력 없이 파괴, 보충, 비활성화와 대전 종료 시 임시 상태를 초기화한다.</summary>
    public void Clear()
    {
        for (int index = _entries.Count - 1; index >= 0; index--) RemoveAt(index);
    }

    /// <summary>index의 상태와 표시를 제거한다. 표시를 즉시 숨기고 프리팹 인스턴스를 파괴 예약한다.</summary>
    private void RemoveAt(int index)
    {
        if (_entries[index].Marker != null)
        {
            _entries[index].Marker.SetActive(false);
            Object.Destroy(_entries[index].Marker);
        }
        _entries.RemoveAt(index);
    }
}
