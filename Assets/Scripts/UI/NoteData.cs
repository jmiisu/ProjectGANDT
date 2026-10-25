using UnityEngine;

namespace GANDT
{
    public enum GANDT_NOTE_DISPLAY_TYPE
    {
        DIALOGUE_BOX,
        DOCUMENT,
    }

    [CreateAssetMenu(fileName = "Note_New", menuName = "GANDT/Note Data")]
    public class NoteData : ScriptableObject
    {
        public string title;
        [TextArea(3, 8)] public string[] pages;
        public  GANDT_NOTE_DISPLAY_TYPE displayType = GANDT_NOTE_DISPLAY_TYPE.DIALOGUE_BOX;
        public Sprite documentBackground;
    }
}
