using System;

using UnityEngine;

namespace Game.Tournament
{
    public enum SpecialKeycapType { None, Explosion, Bonus, Row, Column, All }
    public enum TournamentPhase { Preparing, Battle, Result, Shop, Discard, Finished }

    [CreateAssetMenu(menuName = "Game/Tournament Settings")]
    public sealed class TournamentSettings : ScriptableObject
    {
        [Header("Tournament")]
        [SerializeField, Min(1f)] private float _battleDuration = 30f;
        [SerializeField, Min(1)] private int _keycapHealth = 5;
        [SerializeField, Min(1)] private int _capacity = 26;

        [Header("Shop")]
        [SerializeField, Min(1)] private int _scorePerCurrency = 100;
        [SerializeField, Min(1)] private int _bundleSize = 3;
        [SerializeField, Min(0)] private int _rerollCost = 5;
        [SerializeField, Min(1)] private int _rerollLimit = 3;
        [SerializeField] private int[] _prices = { 0, 80, 100, 60, 60, 100 };

        [Header("Rewards")]
        [SerializeField, Min(0f)] private float _currencyRewardMultiplier = 1.2f;
        [SerializeField] private int[] _goldRewards = { 50, 40, 30, 10 };

        [Header("Robot Rounds")]
        [SerializeField] private RobotRound[] _rounds =
        {
            new RobotRound(12f, 2, 0), new RobotRound(14f, 2, 5),
            new RobotRound(16f, 2, 10), new RobotRound(18f, 2, 15),
            new RobotRound(20f, 2, 20)
        };

        public float BattleDuration => _battleDuration;
        public int KeycapHealth => _keycapHealth;
        public int Capacity => _capacity;
        public int ScorePerCurrency => _scorePerCurrency;
        public float CurrencyRewardMultiplier => _currencyRewardMultiplier;
        public int BundleSize => _bundleSize;
        public int RerollCost => _rerollCost;
        public int RerollLimit => _rerollLimit;
        public int RoundCount => _rounds.Length;

        /// <summary>type에 해당하는 묶음 가격을 반환하며 설정 상태를 변경하지 않는다.</summary>
        public int GetPrice(SpecialKeycapType type) => _prices[(int)type];

        /// <summary>rank에 해당하는 작업실 골드를 반환하며 4위 이하에는 기타 보상을 사용한다.</summary>
        public int GetGold(int rank) => _goldRewards[Mathf.Clamp(rank - 1, 0, 3)];

        /// <summary>1부터 시작하는 round의 로봇 설정을 반환하며 원본 데이터를 유지한다.</summary>
        public RobotRound GetRound(int round) => _rounds[round - 1];
    }

    [Serializable]
    public sealed class RobotRound
    {
        [SerializeField, Min(0.01f)] private float _attacksPerSecond;
        [SerializeField, Min(1)] private int _damage;
        [SerializeField, Min(0)] private int _specialCount;
        public float AttacksPerSecond => _attacksPerSecond;
        public int Damage => _damage;
        public int SpecialCount => _specialCount;

        /// <summary>speed, damage와 specialCount를 저장한 대전별 로봇 설정을 생성한다.</summary>
        public RobotRound(float speed, int damage, int specialCount)
        {
            _attacksPerSecond = speed;
            _damage = damage;
            _specialCount = specialCount;
        }
    }
}
