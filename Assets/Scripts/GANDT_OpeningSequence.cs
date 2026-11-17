
using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Playables;
using UnityEngine.Serialization;

namespace GANDT
{
    public class GANDT_OpeningSequence : MonoBehaviour
    {
        [Serializable]
        public class DialoguePoint
        {
            [Header("Timeline")]
            [Tooltip("대사를 표시할 Timeline 시간(초)")]
            [Min(0f)]
            public double pauseTime;

            [Header("Dialogue")]
            [TextArea(2, 5)]
            public string dialogue;
        }

        // =========================================================
        // References
        // =========================================================

        [Header("Timeline")]
        [SerializeField]
        private PlayableDirector openingTimeline;

        [Header("Opening UI")]
        [FormerlySerializedAs("blackPanel")]
        [SerializeField]
        private GameObject dialoguePanel;

        [SerializeField]
        private TMP_Text dialogueText;

        [SerializeField]
        private TMP_Text continueText;

        [Header("Cutscene Letterbox")]
        [SerializeField]
        private CutsceneLetterbox cutsceneLetterbox;

        [SerializeField]
        private bool showLetterboxOnStart = true;

        // =========================================================
        // Cameras
        // =========================================================

        [Header("Cinemachine Cameras")]

        [FormerlySerializedAs("openingCamera")]
        [SerializeField]
        private GameObject openingCamerasRoot;

        [Tooltip("게임플레이용 Cinemachine Camera")]
        [SerializeField]
        private GameObject gameplayCamera;

        // =========================================================
        // Dialogue
        // =========================================================

        [Header("Dialogue Points")]
        [SerializeField]
        private DialoguePoint[] dialoguePoints;

        [Header("Disable During Opening")]
        [SerializeField]
        private MonoBehaviour[] blockedBehaviours;

        // =========================================================
        // Runtime
        // =========================================================

        private bool[] previousEnabledStates;

        private int currentDialogueIndex;
        private int dialogueOpenedFrame = -1;

        private bool isOpeningActive;
        private bool isWaitingForDialogue;

        public bool IsOpeningActive => isOpeningActive;
        public bool IsWaitingForDialogue => isWaitingForDialogue;

        // =========================================================
        // Unity
        // =========================================================

        private void Start()
        {
            BeginOpening();
        }

        private void Update()
        {
            if (!isOpeningActive)
            {
                return;
            }

            if (isWaitingForDialogue)
            {
                HandleDialogueInput();
                return;
            }

            if (openingTimeline == null ||
                openingTimeline.state != PlayState.Playing)
            {
                return;
            }

            CheckDialoguePoint();
        }

        private void OnDisable()
        {
            if (isOpeningActive)
            {
                FinishOpening();
            }
        }

        private void OnDestroy()
        {
            if (openingTimeline != null)
            {
                openingTimeline.stopped -= OnTimelineStopped;
            }
        }

        // =========================================================
        // Opening
        // =========================================================

        public void BeginOpening()
        {
            if (isOpeningActive)
            {
                return;
            }

            if (openingTimeline == null ||
                openingTimeline.playableAsset == null)
            {
                Debug.LogError(
                    "[OpeningSequence] Timeline 참조가 없습니다.",
                    this
                );
                return;
            }

            if (openingCamerasRoot == null ||
                gameplayCamera == null)
            {
                Debug.LogError(
                    "[OpeningSequence] 카메라 참조가 없습니다.",
                    this
                );
                return;
            }

            if (openingCamerasRoot == gameplayCamera ||
                gameplayCamera.transform.IsChildOf(
                    openingCamerasRoot.transform))
            {
                Debug.LogError(
                    "[OpeningSequence] CM_Player는 " +
                    "OpeningCameras 그룹 밖에 있어야 합니다.",
                    this
                );
                return;
            }

            if (transform == openingCamerasRoot.transform ||
                transform.IsChildOf(
                    openingCamerasRoot.transform))
            {
                Debug.LogError(
                    "[OpeningSequence] OpeningSequence는 " +
                    "OpeningCameras 그룹 밖에 있어야 합니다.",
                    this
                );
                return;
            }

            isOpeningActive = true;
            isWaitingForDialogue = false;
            currentDialogueIndex = 0;
            dialogueOpenedFrame = -1;

            HideDialogueImmediate();
            LockPlayerControls();

            SetupOpeningCameras();

            if (showLetterboxOnStart)
            {
                ShowLetterbox();
            }

            openingTimeline.stopped -= OnTimelineStopped;

            // 초기 상태에서는 종료 이벤트를 받지 않는다.
            openingTimeline.Stop();
            openingTimeline.time = 0d;

            // Timeline이 마지막에 자동으로 정지하도록 설정
            openingTimeline.extrapolationMode =
                DirectorWrapMode.None;

            openingTimeline.stopped += OnTimelineStopped;

            openingTimeline.Play();

            Debug.Log(
                "[OpeningSequence] 오프닝 시작",
                this
            );
        }

        // =========================================================
        // Camera Setup
        // =========================================================

        private void SetupOpeningCameras()
        {
            // 오프닝용 카메라 그룹 활성화
            if (openingCamerasRoot != null)
            {
                openingCamerasRoot.SetActive(true);
            }

            // 중요:
            // CM_Player도 처음부터 활성화한다.
            // Timeline이 재생 중이면 Cinemachine Track이
            // 카메라 선택을 제어한다.
            if (gameplayCamera != null)
            {
                gameplayCamera.SetActive(true);
            }

            Debug.Log(
                "[OpeningSequence] 오프닝 카메라 준비 완료",
                this
            );
        }

        // =========================================================
        // Dialogue Point
        // =========================================================

        private void CheckDialoguePoint()
        {
            if (dialoguePoints == null ||
                currentDialogueIndex >= dialoguePoints.Length)
            {
                return;
            }

            DialoguePoint point =
                dialoguePoints[currentDialogueIndex];

            if (point == null ||
                openingTimeline.time < point.pauseTime)
            {
                return;
            }

            PauseForDialogue(point);
        }

        private void PauseForDialogue(DialoguePoint point)
        {
            double pauseTime = Math.Max(
                0d,
                Math.Min(
                    point.pauseTime,
                    openingTimeline.duration
                )
            );

            openingTimeline.time = pauseTime;
            openingTimeline.Evaluate();
            openingTimeline.Pause();

            isWaitingForDialogue = true;
            dialogueOpenedFrame = Time.frameCount;

            if (dialoguePanel != null)
            {
                dialoguePanel.SetActive(true);
            }

            if (dialogueText != null)
            {
                dialogueText.text = point.dialogue;
            }

            if (continueText != null)
            {
                continueText.text = "[E] 다음";
            }

            Debug.Log(
                $"[OpeningSequence] 대사 표시: " +
                $"{pauseTime:0.00}s",
                this
            );
        }

        // =========================================================
        // Dialogue Input
        // =========================================================

        private void HandleDialogueInput()
        {
            if (Keyboard.current == null)
            {
                return;
            }

            if (Time.frameCount == dialogueOpenedFrame)
            {
                return;
            }

            if (Keyboard.current.eKey.wasPressedThisFrame)
            {
                ContinueOpening();
            }
        }

        private void ContinueOpening()
        {
            if (!isWaitingForDialogue)
            {
                return;
            }

            HideDialogueImmediate();

            isWaitingForDialogue = false;
            currentDialogueIndex++;

            if (openingTimeline != null)
            {
                openingTimeline.Resume();
            }

            Debug.Log(
                "[OpeningSequence] 대사 종료 / Timeline 재개",
                this
            );
        }

        // =========================================================
        // Timeline Finished
        // =========================================================

        private void OnTimelineStopped(
            PlayableDirector director)
        {
            if (!isOpeningActive ||
                director != openingTimeline)
            {
                return;
            }

            FinishOpening();
        }

        // =========================================================
        // Finish
        // =========================================================

        private void FinishOpening()
        {
            if (!isOpeningActive)
            {
                return;
            }

            isOpeningActive = false;
            isWaitingForDialogue = false;

            HideDialogueImmediate();

            if (openingTimeline != null)
            {
                openingTimeline.stopped -= OnTimelineStopped;
            }

            // 중요:
            // Timeline이 제어권을 반환하기 전에
            // CM_Player가 활성화되어 있도록 보장한다.
            if (gameplayCamera != null)
            {
                gameplayCamera.SetActive(true);
            }

            // 오프닝용 카메라 그룹 전체 비활성화
            if (openingCamerasRoot != null)
            {
                openingCamerasRoot.SetActive(false);
            }

            // 종료 이벤트가 발생한 경우 이미 정지 상태이므로
            // Stop()을 중복 호출하지 않는다.
            if (openingTimeline != null &&
                openingTimeline.state != PlayState.Paused &&
                openingTimeline.state != PlayState.Playing)
            {
                // 이미 정지한 상태
            }
            else if (openingTimeline != null)
            {
                openingTimeline.Stop();
            }

            HideLetterbox();
            UnlockPlayerControls();

            Debug.Log(
                "[OpeningSequence] 오프닝 종료 / " +
                "CM_Player 전용 모드",
                this
            );
        }

        // =========================================================
        // Dialogue UI
        // =========================================================

        private void HideDialogueImmediate()
        {
            if (dialoguePanel != null)
            {
                dialoguePanel.SetActive(false);
            }

            if (dialogueText != null)
            {
                dialogueText.text = string.Empty;
            }
        }

        // =========================================================
        // Letterbox
        // =========================================================

        private void ShowLetterbox()
        {
            if (cutsceneLetterbox != null)
            {
                cutsceneLetterbox.Show();
            }
        }

        private void HideLetterbox()
        {
            if (cutsceneLetterbox != null)
            {
                cutsceneLetterbox.Hide();
            }
        }

        // =========================================================
        // Player Control
        // =========================================================

        private void LockPlayerControls()
        {
            if (blockedBehaviours == null)
            {
                return;
            }

            previousEnabledStates =
                new bool[blockedBehaviours.Length];

            for (int i = 0;
                 i < blockedBehaviours.Length;
                 i++)
            {
                MonoBehaviour behaviour =
                    blockedBehaviours[i];

                if (behaviour == null ||
                    behaviour == this)
                {
                    continue;
                }

                previousEnabledStates[i] =
                    behaviour.enabled;

                behaviour.enabled = false;
            }
        }

        private void UnlockPlayerControls()
        {
            if (blockedBehaviours == null ||
                previousEnabledStates == null)
            {
                return;
            }

            int count = Mathf.Min(
                blockedBehaviours.Length,
                previousEnabledStates.Length
            );

            for (int i = 0;
                 i < count;
                 i++)
            {
                MonoBehaviour behaviour =
                    blockedBehaviours[i];

                if (behaviour == null ||
                    behaviour == this)
                {
                    continue;
                }

                behaviour.enabled =
                    previousEnabledStates[i];
            }

            previousEnabledStates = null;
        }
    }
}
