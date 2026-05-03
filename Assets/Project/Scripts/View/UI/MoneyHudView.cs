using System;
using PrisonLife.Core;
using PrisonLife.Game;
using TMPro;
using UnityEngine;

namespace PrisonLife.View.UI
{
    /// <summary>
    /// 플레이어가 들고 있는 머니 수량을 HUD 에 표시.
    /// PlayerModel.Inventory.ObserveCount(Money) 구독.
    /// </summary>
    public class MoneyHudView : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private TMP_Text moneyCountLabel;

        private IDisposable subscription;

        private void Start()
        {
            SystemManager systemManager = SystemManager.Instance;
            if (systemManager == null || systemManager.PlayerModel == null || systemManager.PlayerModel.Inventory == null)
            {
                Debug.LogError("[MoneyHudView] SystemManager / PlayerModel.Inventory 미초기화 상태로 동작 불가.");
                return;
            }

            subscription = systemManager.PlayerModel.Inventory
                .ObserveCount(ResourceType.Money)
                .Subscribe(UpdateLabel);
        }

        private void OnDestroy()
        {
            subscription?.Dispose();
            subscription = null;
        }

        private void UpdateLabel(int _count)
        {
            if (moneyCountLabel == null) return;
            // 인벤 count 자체가 won — 그대로 표시.
            moneyCountLabel.text = _count.ToString();
        }
    }
}
