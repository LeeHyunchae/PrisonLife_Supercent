using PrisonLife.Models;
using UnityEngine;

namespace PrisonLife.Core
{
    public interface IInventoryHolder
    {
        InventoryModel Inventory { get; }

        /// <summary>
        /// holder 의 transform — 자원 비행 애니메이션의 from/to 기준점 산출용.
        /// </summary>
        Transform Transform { get; }

        /// <summary>
        /// Player 가 직접 조작하는 holder 인지 여부.
        /// true 면 zone 의 역할 제한을 무시하고 full logic (deposit + process 모두) 적용.
        /// false 면 zone 별 정해진 역할만 수행 (예: AI 워커는 InputZone 에선 deposit 만, ProcessZone 에선 drain 만).
        /// </summary>
        bool IsPlayerControlled { get; }
    }
}
