
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.IO;
using System;
using UnityEngine.Events;

namespace MidiPlayerTK
{
    /// <summary>
    /// Script for the prefab MidiFilePlayer. 
    /// Play a selected midi file. 
    /// List of Midi file must be defined with Midi Player Setup (menu tools).
    /// </summary>
    [Obsolete("Replaced by MPTK_MidiListPlayer", false)]
    public class MidiListPlayer : MonoBehaviour
    {
        [Serializable]
        public class MidiPlayItem
        {
            public string MidiName;
            public bool Selected;
        }

        public List<MidiPlayItem> MPTK_PlayList;
        public int MPTK_PlayIndex;

        /// <summary>
        /// Should the Midi start playing when application start ?
        /// </summary>
        public virtual bool MPTK_PlayOnStart { get { return playOnStart; } set { playOnStart = value; } }

        /// <summary>
        /// Should automatically restart when Midi reach the end ?
        /// </summary>
        public virtual bool MPTK_Loop { get { return loop; } set { loop = value; } }


        /// <summary>
        /// Is Midi file playing is paused ?
        /// </summary>
        public virtual bool MPTK_IsPaused { get { if (MPTK_MidiFilePlayer != null) return MPTK_MidiFilePlayer.MPTK_IsPaused; else return false; } }

        /// <summary>
        /// Is Midi file is playing ?
        /// </summary>
        public virtual bool MPTK_IsPlaying { get { if (MPTK_MidiFilePlayer != null) return MPTK_MidiFilePlayer.MPTK_IsPlaying; else return false; } }

        /// <summary>
        /// Define unity event to trigger at start
        /// </summary>
        [HideInInspector]
        public UnityEvent OnEventStartPlayMidi;

        /// <summary>
        /// Define unity event to trigger at end
        /// </summary>
        [HideInInspector]
        public UnityEvent OnEventEndPlayMidi;

        public MidiFilePlayer MPTK_MidiFilePlayer;


        [SerializeField]
        [HideInInspector]
        private bool playOnStart = false, loop = false;


        void Awake()
        {
            //Debug.Log("Awake midiIsPlaying:" + MPTK_IsPlaying);
            MPTK_MidiFilePlayer = GetComponentInChildren<MidiFilePlayer>();
            if (MPTK_MidiFilePlayer == null)
                Debug.LogWarning("No MidiFilePlayer component found in MidiListPlayer.");
            else
            {
                //if (midiFilePlayer.OnEventNotesMidi.GetPersistentEventCount() == 0)
                {
                    // No listener defined, set now by script. NotesToPlay will be called for each new notes read from Midi file
                    Debug.Log("No OnEventNotesMidi defined, set by script");
                    //midiFilePlayer.OnEventEndPlayMidi = new MidiFilePlayer.UnityEvent();
                    MPTK_MidiFilePlayer.OnEventEndPlayMidi.AddListener(EventEndPlayMidi);
                }
            }
        }

        public void EventEndPlayMidi()
        {
            Debug.Log("EventEndPlayMidi " + MPTK_PlayIndex);
            if (MPTK_PlayIndex < MPTK_PlayList.Count - 1)
            {
                MPTK_PlayIndex++;
                PlayIndex(MPTK_PlayIndex);
            }
            else if (MPTK_Loop)
            {
                MPTK_PlayIndex = 0;
                PlayIndex(MPTK_PlayIndex);
            }
        }

        public void PlayIndex(int index)
        {
            if (MPTK_MidiFilePlayer != null)
            {
                if (MPTK_PlayList == null || MPTK_PlayList.Count == 0)
                    Debug.LogWarning("No Play List defined");
                else if (index < 0 || index >= MPTK_PlayList.Count)
                    Debug.LogWarning("Index to play " + index + " not correct");
                else
                {
                    //Debug.Log("PlayIndex, Index to play " + index + " "+ MPTK_PlayList[index].MidiName);
                    MPTK_MidiFilePlayer.MPTK_MidiName = MPTK_PlayList[index].MidiName;
                    MPTK_MidiFilePlayer.MPTK_RePlay();
                }
            }
        }
        void Start()
        {
            try
            {
                if (MPTK_PlayOnStart)
                    PlayIndex(MPTK_PlayIndex);
            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
        }

        /// <summary>
        /// Play the midi file defined in MPTK_MidiName
        /// </summary>
        public virtual void MPTK_Play()
        {
            try
            {
                if (MidiPlayerGlobal.SoundFontLoaded)
                {

                    // if (!MPTK_IsPlaying)
                    {
                        // Load description of available soundfont
                        if (MidiPlayerGlobal.ImSFCurrent != null && MidiPlayerGlobal.CurrentMidiSet != null && MidiPlayerGlobal.CurrentMidiSet.MidiFiles != null && MidiPlayerGlobal.CurrentMidiSet.MidiFiles.Count > 0)
                            PlayIndex(MPTK_PlayIndex);
                        else
                            Debug.LogWarning("MidiFilePlayer - no SoundFont or Midi set defined, go to Unity menu Tools to setup MPTK");
                    }
                }
            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
        }

        /// <summary>
        /// Stop playing
        /// </summary>
        public virtual void MPTK_Stop()
        {
            try
            {
                if (MPTK_MidiFilePlayer != null)
                {
                    MPTK_MidiFilePlayer.MPTK_Stop();
                }
            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
        }

        /// <summary>
        /// Restart playing the current midi file
        /// </summary>
        public virtual void MPTK_RePlay()
        {
            try
            {
                if (MPTK_MidiFilePlayer != null)
                    MPTK_MidiFilePlayer.MPTK_RePlay();
            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
        }

        /// <summary>
        /// Pause the current playing
        /// </summary>
        public virtual void MPTK_Pause(float timeToPauseMS = -1f)
        {
            try
            {
                if (MPTK_MidiFilePlayer != null)
                {

                    MPTK_MidiFilePlayer.MPTK_Pause();
                }

            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
        }

        /// <summary>
        /// Play next Midi in list
        /// </summary>
        public virtual void MPTK_Next()
        {
            try
            {
                if (MPTK_MidiFilePlayer != null)
                {
                    if (MPTK_PlayIndex < MPTK_PlayList.Count - 1)
                        MPTK_PlayIndex++;
                    else
                        MPTK_PlayIndex = 0;
                    PlayIndex(MPTK_PlayIndex);
                }
            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
        }

        /// <summary>
        /// Play previous Midi in list
        /// </summary>
        public virtual void MPTK_Previous()
        {
            try
            {
                if (MPTK_MidiFilePlayer != null)
                {
                    if (MPTK_PlayIndex > 0)
                        MPTK_PlayIndex--;
                    else
                        MPTK_PlayIndex = MPTK_PlayList.Count - 1;
                    PlayIndex(MPTK_PlayIndex);
                }
            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
        }
    }
}

