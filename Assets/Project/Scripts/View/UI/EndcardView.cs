using System;
using PrisonLife.Game;
using PrisonLife.Models;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PrisonLife.View.UI
{
    /// <summary>
    /// 게임 종료 시 표시되는 엔드카드. GameStateModel.CurrentPhase = Ending 구독.
    /// Ending 진입 시 패널 표시 + Time.timeScale = 0 으로 게임 전체 정지.
    /// CONTINUE 버튼은 timeScale 복원 후 현재 씬을 리로드해 처음부터 다시 시작.
    /// </summary>
    public class EndcardView : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private GameObject panel;
        [SerializeField] private Button continueButton;

        private IDisposable phaseSubscription;

        private void Start()
        {
            if (panel != null) panel.SetActive(false);

            if (continueButton != null) continueButton.onClick.AddListener(OnContinueClicked);

            SystemManager systemManager = SystemManager.Instance;
            if (systemManager == null || systemManager.GameState == null)
            {
                Debug.LogError("[EndcardView] SystemManager / GameState 미초기화 상태로 동작 불가.");
                return;
            }

            phaseSubscription = systemManager.GameState.CurrentPhase.Subscribe(OnPhaseChanged);
        }

        private void OnDestroy()
        {
            if (continueButton != null) continueButton.onClick.RemoveListener(OnContinueClicked);
            phaseSubscription?.Dispose();
            phaseSubscription = null;
        }

        private void OnPhaseChanged(GamePhase _phase)
        {
            bool isEnding = _phase == GamePhase.Ending;
            if (panel != null) panel.SetActive(isEnding);
            if (isEnding) Time.timeScale = 0f;
        }

        private void OnContinueClicked()
        {
            // 씬 리로드 전에 timeScale 복원 — Time.timeScale 은 씬 전환 후에도 유지됨.
            Time.timeScale = 1f;
            UnityEngine.SceneManagement.Scene activeScene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(activeScene.buildIndex);
        }
    }
}
