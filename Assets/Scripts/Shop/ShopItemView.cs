using System;

using UnityEngine;
using UnityEngine.UI;

using Game.Upgrades;

namespace Game.Shop
{
    public sealed class ShopItemView : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Text _nameText;
        [SerializeField] private Text _descriptionText;
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
        /// definition과 upgrades를 보관하고 강화 이름과 설명 및 구매 상태를 표시한다.
        /// 구매 버튼의 콜백을 연결하며 항목 크기는 프리팹의 자동 레이아웃에 맡긴다.
        /// </summary>
        public void Initialize(UpgradeDefinition definition, UpgradeService upgrades)
        {
            _definition = definition;
            _upgrades = upgrades;
            _purchaseButton.onClick.AddListener(OnPurchaseClicked);
            Refresh();
        }

        /// <summary>
        /// 보관한 강화 정의와 현재 레벨 및 구매 조건으로 각 표시 영역을 갱신한다.
        /// 이름과 효과 비교를 분리하고 단계별 레벨, 가격 및 구매·승급 버튼 상태를 변경한다.
        /// </summary>
        public void Refresh()
        {
            int level = _upgrades.GetLevel(_definition.Id);
            bool isMaxLevel = level >= _definition.MaxLevel;
            PurchaseResult purchaseStatus = _upgrades.GetPurchaseStatus(_definition.Id);
            bool requiresAreaSmash = purchaseStatus == PurchaseResult.RequiresAreaSmash;
            bool isPromotion = false;

            _nameText.text = _definition.DisplayName;
            _levelText.text = $"레벨 {level}/{_definition.MaxLevel}";
            _descriptionText.text = UpgradeDescriptionFormatter.Format(_definition, level);

            if (_definition.Id == UpgradeId.HandPower)
            {
                _nameText.text = _definition.GetHandName(level);
                _levelText.text = $"레벨 {_definition.GetHandStageLevel(level)}/{_definition.GetHandStageMaxLevel(level)}";
                isPromotion = !isMaxLevel && _definition.IsHandPromotion(level + 1);
            }
            else if (_definition.Id == UpgradeId.AutoClick)
            {
                _nameText.text = _definition.GetAutoAttackName(level);
                int promotion = _definition.AutoPromotionLevel;
                int refillStart = _definition.AutoRefillStartLevel;
                int stageLevel;
                int stageMax;

                if (_definition.IsAutoRefillUnlocked(level))
                {
                    stageLevel = level - refillStart + 1;
                    stageMax = _definition.MaxLevel - refillStart + 1;
                }
                else if (level >= promotion)
                {
                    stageLevel = level - promotion + 1;
                    stageMax = Math.Min(_definition.MaxLevel, refillStart - 1) - promotion + 1;
                }
                else
                {
                    stageLevel = level;
                    stageMax = Math.Min(_definition.MaxLevel, promotion - 1);
                }

                _levelText.text = $"레벨 {stageLevel}/{stageMax}";
                isPromotion = !isMaxLevel && (level + 1 == promotion || level + 1 == refillStart);
            }

            if (requiresAreaSmash)
                _descriptionText.text += "\n해금 조건: 범위 타격 1레벨";

            _costText.text = isMaxLevel ? string.Empty : $"가격 {_definition.GetCost(level):N0}";
            _buttonText.text = isMaxLevel ? "최대 레벨" : requiresAreaSmash ? "잠김" : isPromotion ? "승급" : "구매";
            _purchaseButton.interactable = purchaseStatus == PurchaseResult.Success;
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
