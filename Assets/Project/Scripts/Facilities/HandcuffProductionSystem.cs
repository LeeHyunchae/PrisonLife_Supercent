using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using PrisonLife.Models;

namespace PrisonLife.Facilities
{
    /// <summary>
    /// 광석 stockpile 1개 소비 → 수갑 stockpile 1개 생산. 두 stockpile 사이의 bridge.
    /// </summary>
    public class HandcuffProductionSystem : IDisposable
    {
        private readonly StockpileModel oreInputStockpile;
        private readonly StockpileModel handcuffOutputStockpile;
        private readonly float productionPeriodSeconds;
        private readonly CancellationToken cancellationToken;
        private bool isRunning;

        public HandcuffProductionSystem(
            StockpileModel _oreInputStockpile,
            StockpileModel _handcuffOutputStockpile,
            float _productionPeriodSeconds,
            CancellationToken _cancellationToken)
        {
            oreInputStockpile = _oreInputStockpile;
            handcuffOutputStockpile = _handcuffOutputStockpile;
            productionPeriodSeconds = Math.Max(0.05f, _productionPeriodSeconds);
            cancellationToken = _cancellationToken;
        }

        public void Start()
        {
            if (isRunning) return;
            if (oreInputStockpile == null || handcuffOutputStockpile == null) return;
            isRunning = true;
            RunProductionLoopAsync().Forget();
        }

        public void Dispose()
        {
            isRunning = false;
        }

        private async UniTaskVoid RunProductionLoopAsync()
        {
            try
            {
                while (isRunning && !cancellationToken.IsCancellationRequested)
                {
                    await UniTask.WaitUntil(
                        () => oreInputStockpile.HasStock && handcuffOutputStockpile.HasSpace,
                        cancellationToken: cancellationToken);

                    await UniTask.Delay(
                        TimeSpan.FromSeconds(productionPeriodSeconds),
                        DelayType.DeltaTime,
                        PlayerLoopTiming.Update,
                        cancellationToken: cancellationToken);

                    if (!oreInputStockpile.HasStock || !handcuffOutputStockpile.HasSpace) continue;

                    if (oreInputStockpile.TryRemoveOne())
                    {
                        handcuffOutputStockpile.TryAdd(1);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // 정상 종료
            }
        }
    }
}
