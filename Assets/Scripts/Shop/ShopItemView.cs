using System;

using UnityEngine;
using UnityEngine.UI;

using Game.Upgrades;
using KeyboardModeling;

namespace Game.Shop
{
    public sealed class ShopItemView : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Text _nameText;
        [SerializeField] private Text _levelText;
        [SerializeField] private Text _costText;
        [SerializeField] private Text _buttonText;
        [SerializeField] private Button _purchaseButton;

        [Header("Runtime State")]
        private UpgradeDefinition _definition;
        private UpgradeService _upgrades;

        public event Action<UpgradeId> PurchaseRequested;

        void OnDestroy()
        {
            _purchaseButton.onClick.RemoveListener(OnPurchaseClicked);
        }

        /// <summary>
        /// definition과 upgrades를 사용하여 강화 이름과 구매 상태를 표시한다.
        /// 구매 버튼을 연결하고 화면에서 사용할 강화 설정과 서비스를 보관한다.
        /// </summary>
        public void Initialize(UpgradeDefinition definition, UpgradeService upgrades)
        {
            _definition = definition;
            _upgrades = upgrades;
            _nameText.text = definition.DisplayName;
            if (definition.Id == UpgradeId.RareKeycapQuality)
            {
                ConfigureQualityLayout();
            }
            else if (definition.Id == UpgradeId.SmashDamage || definition.Id == UpgradeId.AreaDamage)
            {
                ConfigureDamageLayout();
            }
            else if (definition.Id == UpgradeId.FeverDuration || definition.Id == UpgradeId.FeverDamageMultiplier || definition.Id == UpgradeId.LuckyReward || definition.Id == UpgradeId.HandPower || definition.Id == UpgradeId.AutoClick)
            {
                ConfigureFeverLayout();
            }
            _purchaseButton.onClick.AddListener(OnPurchaseClicked);
            Refresh();
        }

        /// <summary>
        /// 보관 중인 강화 설정과 현재 레벨 및 잔액을 사용하여 화면을 갱신한다.
        /// 현재·다음 강화 수치, 레벨, 다음 가격과 구매·승급 버튼의 상태를 변경한다.
        /// </summary>
        public void Refresh()
        {
            int level = _upgrades.GetLevel(_definition.Id);
            bool isMaxLevel = level >= _definition.MaxLevel;
            PurchaseResult purchaseStatus = _upgrades.GetPurchaseStatus(_definition.Id);
            bool requiresAreaSmash = purchaseStatus == PurchaseResult.RequiresAreaSmash;

            _levelText.text = $"Lv {level}/{_definition.MaxLevel}";
            _costText.text = isMaxLevel ? string.Empty : requiresAreaSmash ? "Area Smash Lv 1" : $"{_definition.GetCost(level):N0}";
            _buttonText.text = isMaxLevel ? "MAX" : requiresAreaSmash ? "LOCKED" : "Buy";
            _purchaseButton.interactable = purchaseStatus == PurchaseResult.Success;

            if (_definition.Id == UpgradeId.SmashDamage || _definition.Id == UpgradeId.AreaDamage)
            {
                _nameText.text = $"{_definition.DisplayName}\nDamage +{_definition.GetDamageBonus(level)}";
                if (!isMaxLevel)
                {
                    _nameText.text += $" -> +{_definition.GetDamageBonus(level + 1)}";
                }
            }
            else if (_definition.Id == UpgradeId.HandPower)
            {
                _levelText.text = $"Lv {_definition.GetHandStageLevel(level)}/{_definition.GetHandStageMaxLevel(level)}";
                _nameText.text = $"{_definition.GetHandName(level)}\nDamage +{_definition.GetHandDamageBonus(level)}";
                if (!isMaxLevel)
                {
                    _nameText.text += $" -> +{_definition.GetHandDamageBonus(level + 1)}";
                    if (_definition.IsHandPromotion(level + 1))
                        _buttonText.text = "Promote";
                }
            }
            else if (_definition.Id == UpgradeId.AutoClick)
            {
                int promotion = _definition.AutoPromotionLevel;
                int refillStart = _definition.AutoRefillStartLevel;

                if (level >= refillStart)
                {
                    int stageLevel = level - refillStart + 1;
                    int stageMax = _definition.MaxLevel - refillStart + 1;

                    _levelText.text = $"Lv {stageLevel}/{stageMax}";
                    _nameText.text = $"{_definition.GetAutoAttackName(level)}\n" + $"Next keyboard: {_definition.GetAutoRefillDelay(level):0.#}s";

                    if (!isMaxLevel)
                    {
                        _nameText.text += $" -> {_definition.GetAutoRefillDelay(level + 1):0.#}s";
                    }
                }
                else
                {
                    int stageLevel = level >= promotion ? level - promotion + 1 : level;
                    int stageMax = level >= promotion ? refillStart - promotion : promotion - 1;

                    _levelText.text = $"Lv {stageLevel}/{stageMax}";
                    _nameText.text = $"{_definition.GetAutoAttackName(level)}\n" + $"Damage {_definition.GetAutoAttackDamage(level)}, " + $"{_definition.GetAutoAttackSpeed(level):0.##}/s";

                    if (level + 1 == refillStart)
                    {
                        _nameText.text += $"\nNext: {_definition.GetAutoAttackName(refillStart)} " + $"{_definition.GetAutoRefillDelay(refillStart):0.#}s";
                        _buttonText.text = "Promote";
                    }
                    else if (!isMaxLevel)
                    {
                        _nameText.text += $" -> {_definition.GetAutoAttackDamage(level + 1)}, " + $"{_definition.GetAutoAttackSpeed(level + 1):0.##}/s";

                        if (level + 1 == promotion)
                            _buttonText.text = "Promote";
                    }
                }
            }
            else if (_definition.Id == UpgradeId.LuckyReward)
            {
                _nameText.text = $"{_definition.DisplayName}\n{_definition.GetDoubleRewardChance(level):P0} chance: reward x2";
                if (!isMaxLevel)
                    _nameText.text += $" -> {_definition.GetDoubleRewardChance(level + 1):P0}";
            }
            else if (_definition.Id == UpgradeId.RareKeycapQuality)
            {
                _nameText.text = $"{_definition.DisplayName} (Next keyboard)\n{FormatRarityDistribution(level)}";
                if (!isMaxLevel)
                {
                    _nameText.text += $"\nNext: {FormatRarityDistribution(level + 1)}";
                }
            }
            else if (_definition.Id == UpgradeId.FeverDuration || _definition.Id == UpgradeId.FeverDamageMultiplier)
            {
                _nameText.text = $"{_definition.DisplayName}\n{FormatFeverEffect(level)}";
                if (!isMaxLevel)
                {
                    _nameText.text += $" -> {FormatFeverEffect(level + 1)}";
                }
                _nameText.text += " (Next fever)";
            }
        }

        /// <summary>
        /// level과 현재 피버 강화 정의로 상점에 표시할 지속시간 또는 피해 배율을 만든다.
        /// 다음 피버부터 적용되는 효과 문구를 반환하며 강화 상태는 변경하지 않는다.
        /// </summary>
        private string FormatFeverEffect(int level)
        {
            return _definition.Id == UpgradeId.FeverDuration
                ? $"{_definition.GetFeverDuration(level):0.#}s"
                : $"x{_definition.GetFeverDamageMultiplier(level):0.0}";
        }

        /// <summary>
        /// 연결된 제목 텍스트와 구매 UI를 사용하여 공격력 강화의 두 줄 표시 영역을 확보한다.
        /// 프리팹의 폰트 크기를 유지하면서 항목 높이와 레벨·가격·버튼 위치를 변경한다.
        /// </summary>
        private void ConfigureDamageLayout()
        {
            LayoutElement layout = GetComponent<LayoutElement>();
            layout.minHeight = 132f;
            layout.preferredHeight = 132f;
            RectTransform nameRect = _nameText.rectTransform;
            nameRect.anchorMin = new Vector2(0f, 1f);
            nameRect.anchorMax = Vector2.one;
            nameRect.offsetMin = new Vector2(14f, -76f);
            nameRect.offsetMax = new Vector2(-14f, -6f);
            _levelText.rectTransform.anchoredPosition = new Vector2(_levelText.rectTransform.anchoredPosition.x, -88f);
            _costText.rectTransform.anchoredPosition = new Vector2(_costText.rectTransform.anchoredPosition.x, -82f);
            RectTransform buttonRect = (RectTransform)_purchaseButton.transform;
            buttonRect.anchoredPosition = new Vector2(buttonRect.anchoredPosition.x, -82f);
        }

        /// <summary>
        /// 피버 강화 항목의 텍스트와 버튼 위치를 조정하여 현재 및 다음 효과를 표시한다.
        /// 연결된 UI와 LayoutElement를 사용하여 항목 높이와 표시 영역을 변경한다.
        /// </summary>
        private void ConfigureFeverLayout()
        {
            LayoutElement layout = GetComponent<LayoutElement>();
            layout.minHeight = 100f;
            layout.preferredHeight = 100f;
            _nameText.fontSize = 14;
            RectTransform nameRect = _nameText.rectTransform;
            nameRect.anchorMin = new Vector2(0f, 1f);
            nameRect.anchorMax = Vector2.one;
            nameRect.offsetMin = new Vector2(14f, -48f);
            nameRect.offsetMax = new Vector2(-14f, -6f);
            _levelText.rectTransform.anchoredPosition = new Vector2(_levelText.rectTransform.anchoredPosition.x, -58f);
            _costText.rectTransform.anchoredPosition = new Vector2(_costText.rectTransform.anchoredPosition.x, -58f);
            RectTransform buttonRect = (RectTransform)_purchaseButton.transform;
            buttonRect.anchoredPosition = new Vector2(buttonRect.anchoredPosition.x, -52f);
        }

        /// <summary>품질 강화 항목의 높이와 텍스트 배치를 조정하여 현재·다음 분포와 구매 버튼을 겹치지 않게 표시한다.</summary>
        private void ConfigureQualityLayout()
        {
            LayoutElement layout = GetComponent<LayoutElement>();
            layout.minHeight = 120f;
            layout.preferredHeight = 120f;
            _nameText.fontSize = 14;
            RectTransform nameRect = _nameText.rectTransform;
            nameRect.anchorMin = new Vector2(0f, 1f);
            nameRect.anchorMax = Vector2.one;
            nameRect.offsetMin = new Vector2(14f, -66f);
            nameRect.offsetMax = new Vector2(-14f, -6f);
            _levelText.rectTransform.anchoredPosition = new Vector2(_levelText.rectTransform.anchoredPosition.x, -78f);
            _costText.rectTransform.anchoredPosition = new Vector2(_costText.rectTransform.anchoredPosition.x, -78f);
            RectTransform buttonRect = (RectTransform)_purchaseButton.transform;
            buttonRect.anchoredPosition = new Vector2(buttonRect.anchoredPosition.x, -72f);
        }

        /// <summary>levelの一般・希少・上位等級の最終確率を短い表示文にして返し、購入状態は変更しない。</summary>
        private string FormatRarityDistribution(int level)
        {
            KeycapRarityDistribution distribution = _definition.GetRarityDistribution(level);
            return $"Common {distribution.GetProbability(KeycapRarity.Common) * 100f:0.##}% / Rare {distribution.GetProbability(KeycapRarity.Rare) * 100f:0.##}% / " +
                $"Epic {distribution.GetProbability(KeycapRarity.Epic) * 100f:0.##}% / Legendary {distribution.GetProbability(KeycapRarity.Legendary) * 100f:0.##}%";
        }

        /// <summary>
        /// 구매 버튼에서 선택한 강화의 ID를 PurchaseRequested로 전달한다.
        /// 보관 중인 강화 설정을 사용하며 재화와 레벨은 직접 변경하지 않는다.
        /// </summary>
        private void OnPurchaseClicked()
        {
            PurchaseRequested?.Invoke(_definition.Id);
        }
    }
}
