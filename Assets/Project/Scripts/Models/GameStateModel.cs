using PrisonLife.Reactive;

namespace PrisonLife.Models
{
    public enum GamePhase
    {
        Boot = 0,
        Playing = 1,
        Ending = 2,
    }

    public class GameStateModel
    {
        public ReactiveProperty<GamePhase> CurrentPhase { get; } = new(GamePhase.Boot);
    }
}
