
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GANDT
{
    public class TextViewer : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private GameObject dialoguePanel;
        [SerializeField] private TMP_Text bodyText;
        [SerializeField] private TMP_Text pageIndicator;

        [Header("Movement Lock (optional")]
        [SerializeField] private MonoBehaviour[] movementScripts;

        private bool[] previousEnabled;
        
        private NoteData currentNote;
        private int pageIndex;

        private int openedFrame = -1;
        private int closedFrame = -1;

        public bool IsOpen => currentNote != null;

        private void Awake()
        {
            if (dialoguePanel != null)
            {
                dialoguePanel.SetActive(false);
            }
        }

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        private void OnDisable()
        {
            if (IsOpen)
            {
                Close();
            }
        }

        // Update is called once per frame
        void Update()
        {
            if (!IsOpen || Keyboard.current == null)
            {
                return;
            }

            if (Time.frameCount == openedFrame)
            {
                return;
            }

            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                Close();
                return;
            }
            else if (Keyboard.current.eKey.wasPressedThisFrame)
            {
                NextPage();
            }
        }

        public bool TryOpen(NoteData note)
        {
            if (IsOpen || Time.frameCount == closedFrame)
            {
                return false;
            }

            if (note == null || note.pages == null || note.pages.Length == 0)
            {
                return false;
            }

            if (dialoguePanel == null || bodyText == null)
            {
                return false;
            }

            currentNote = note;
            pageIndex = 0;
            openedFrame = Time.frameCount;

            LockMovement();

            dialoguePanel.SetActive(true);
            ShowPage();

            return true;
        }

        private void NextPage()
        {
            if (!IsOpen)
            {
                return;
            }

            if (pageIndex < currentNote.pages.Length - 1)
            {
                pageIndex++;
                ShowPage();
            }
            else
            {
                // 마지막 페이지에서 E키를 누르면 닫기
                Close();
            }
        }

        private void ShowPage()
        {
            if (!IsOpen)
            {
                return;
            }

            bodyText.text = currentNote.pages[pageIndex];
            
            if (pageIndicator != null)
            {
                pageIndicator.text = $"{pageIndex + 1} / {currentNote.pages.Length}";
            }
        }

        private void Close()
        {
            if (!IsOpen)
            {
                return;
            }

            if (dialoguePanel != null)
            {
                dialoguePanel.SetActive(false);
            }

            currentNote = null;
            pageIndex = 0;

            closedFrame = Time.frameCount;
            UnlockMovement();
        }

        private void LockMovement()
        {
            previousEnabled = new bool[movementScripts.Length];
            for (int i = 0; i < movementScripts.Length; i++)
            {
                if (movementScripts[i] == null)
                {
                    continue;
                }

                previousEnabled[i] = movementScripts[i].enabled;
                movementScripts[i].enabled = false;
            }
        }

        private void UnlockMovement()
        {
            if (previousEnabled == null)
            {
                return;
            }
            for (int i = 0; i < movementScripts.Length; i++)
            {
                if (movementScripts[i] == null)
                {
                    continue;
                }

                movementScripts[i].enabled = previousEnabled[i];
            }
            previousEnabled = null;
        }
    }
}
