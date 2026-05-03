namespace PrisonLife.Core
{
    public static class GameValueConstants
    {
        /// <summary>
        /// 돈 아이템 1개가 표현하는 통화 가치 (원).
        /// HUD / UI 라벨이 inventory / stockpile 의 item count 에 곱해서 won 으로 표시한다.
        /// 게임 모델의 stockpile 카운트 자체는 item 단위 그대로 (1:1).
        /// </summary>
        public const int MoneyValuePerItem = 5;

        /// <summary>
        /// Money 자원 transfer 주기 — 1원씩 빠르게 깎이는 효과를 위한 짧은 간격.
        /// ResourceInputZone / ResourceOutputZone 가 resourceType == Money 일 때 inspector 값 대신 이 값 사용.
        /// </summary>
        public const float MoneyTransferIntervalSeconds = 0.03f;
    }
}
