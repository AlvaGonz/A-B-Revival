using System;
using UnityEngine;

namespace ABRevival.Presentation
{
    /// <summary>
    /// Portrait frames extracted from the frozen UI asset, indexed by the stable speaker id
    /// assigned by <see cref="ABRevival.Core.Data.TalkScriptParser"/>.
    /// Original art: Vargames (2011).
    /// </summary>
    [CreateAssetMenu(fileName = "TalkPortraitCatalog", menuName = "ABRevival/Talk Portrait Catalog")]
    public sealed class TalkPortraitCatalog : ScriptableObject
    {
        [SerializeField] private Sprite[] portraitsBySpeakerId = Array.Empty<Sprite>();

        public int Count => portraitsBySpeakerId.Length;

        public Sprite GetPortrait(int speakerId)
        {
            if (speakerId < 0 || speakerId >= portraitsBySpeakerId.Length) return null;
            return portraitsBySpeakerId[speakerId];
        }

#if UNITY_EDITOR
        public void SetPortraits(Sprite[] portraits)
        {
            portraitsBySpeakerId = portraits ?? Array.Empty<Sprite>();
        }
#endif
    }
}
