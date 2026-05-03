using UnityEngine;

namespace PrisonLife.Entities
{
    /// <summary>
    /// Animator 가 Player 의 자식 rig GameObject 에 있을 때, AnimationClip 의 Animation Event 가
    /// 호출하는 메서드를 Player 로 forward. rig GameObject (Animator 와 동일) 에 부착하고
    /// player 슬롯에 root Player 를 연결한다.
    /// Animator 가 Player 와 같은 GameObject 라면 이 forwarder 는 불필요.
    /// </summary>
    public class PlayerAnimationEventForwarder : MonoBehaviour
    {
        [SerializeField] private Player player;

        public void OnMiningSwingImpact()
        {
            if (player != null) player.OnMiningSwingImpact();
        }

        public void OnMiningSwingCycleEnd()
        {
            if (player != null) player.OnMiningSwingCycleEnd();
        }
    }
}
