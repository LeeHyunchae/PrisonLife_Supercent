using PrisonLife.Reactive;

namespace PrisonLife.Models
{
    public enum PrisonerPhase
    {
        WalkingToQueue = 0,
        WaitingAtQueue = 1,
        WalkingToCell = 2,
        Inside = 3,
    }

    public class PrisonerModel
    {
        public int RequiredHandcuffs { get; }
        public ReactiveProperty<int> ReceivedHandcuffs { get; } = new(0);
        public ReactiveProperty<PrisonerPhase> Phase { get; } = new(PrisonerPhase.WalkingToQueue);

        public PrisonerModel(int _requiredHandcuffs)
        {
            RequiredHandcuffs = _requiredHandcuffs > 0 ? _requiredHandcuffs : 1;
        }

        public bool IsFulfilled => ReceivedHandcuffs.Value >= RequiredHandcuffs;
    }
}
