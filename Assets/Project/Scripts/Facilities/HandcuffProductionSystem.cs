using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using PrisonLife.Models;

namespace PrisonLife.Facilities
{
    public class HandcuffProductionSystem : IDisposable
    {
        readonly HandcuffContainerModel containerModel;
        readonly CancellationToken cancellationToken;
        bool isRunning;

        public HandcuffProductionSystem(
            HandcuffContainerModel _containerModel,
            CancellationToken _cancellationToken)
        {
            containerModel = _containerModel;
            cancellationToken = _cancellationToken;
        }

        public void Start()
        {
            if (isRunning) return;
            if (containerModel == null) return;
            isRunning = true;
            RunProductionLoopAsync().Forget();
        }

        public void Dispose()
        {
            isRunning = false;
        }

        async UniTaskVoid RunProductionLoopAsync()
        {
            try
            {
                while (isRunning && !cancellationToken.IsCancellationRequested)
                {
                    // 광석 + 출력 여유 둘 다 있을 때까지 대기 (busy-wait 아님, ReactiveProperty 폴링)
                    await UniTask.WaitUntil(
                        () => containerModel.HasStoredOre && containerModel.HasHandcuffSpace,
                        cancellationToken: cancellationToken);

                    // 생산 주기 대기
                    await UniTask.Delay(
                        TimeSpan.FromSeconds(containerModel.ProductionPeriodSeconds.Value),
                        DelayType.DeltaTime,
                        PlayerLoopTiming.Update,
                        cancellationToken: cancellationToken);

                    // 대기 도중 상태가 바뀌었을 수 있으니 재확인
                    if (!containerModel.HasStoredOre || !containerModel.HasHandcuffSpace) continue;

                    if (containerModel.TryConsumeOreForProduction())
                    {
                        containerModel.TryAddHandcuff();
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
