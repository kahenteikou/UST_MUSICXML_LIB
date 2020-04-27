using System;

namespace UST_MUSICXML_LIB
{
    /// <summary>
    /// ノート定義
    /// </summary>
    public struct notes
    {
        /// <summary>
        /// 音符長
        /// </summary>
        public float duration; 
        /// <summary>
        /// 音階
        /// </summary>
        public char pitchStep; 
        /// <summary>
        /// オクターブ
        /// </summary>
        public int pitchOctave;
        /// <summary>
        /// true = #(半音上げ)
        /// </summary>
        public bool pitchAlter;
        /// <summary>
        /// 音符記号
        /// </summary>
        public string type;
        /// <summary>
        /// モーラ
        /// </summary>
        public string lyricText;
        /// <summary>
        /// true == 付点あり
        /// </summary>
        public int dot;
        /// <summary>
        /// start,stop,inter,""
        /// </summary>
        public string tie; 
        /// <summary>
        /// ???
        /// </summary>
        public string time_modification;
    }
}
