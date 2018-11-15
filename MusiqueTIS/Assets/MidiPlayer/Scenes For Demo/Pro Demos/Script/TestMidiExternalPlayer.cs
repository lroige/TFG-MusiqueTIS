using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MidiPlayerTK;
using System;

namespace MidiPlayerTK
{
    public class TestMidiExternalPlayer : MonoBehaviour
    {
        // MPTK component able to play a Midi file
        public MidiExternalPlayer midiExternalPlayer;
        private float LastTimeChange;

        [Range(0.1f, 10f)]
        public float DelayTimeChange = 5;
        public bool IsRandomPosition = false;
        public bool IsRandomSpeed = false;
        public bool IsRandomTranspose = false;

        public static Color ButtonColor = new Color(.7f, .9f, .7f, 1f);
        public bool IsRandomPlay = false;

        // Manage skin
        public GUISkin customSkin;
        public CustomStyle myStyle;

        private void Awake()
        {
        }

        /// <summary>
        /// Event fired by MidiFilePlayer when a midi is started (set by Unity Editor in MidiFilePlayer Inspector)
        /// </summary>
        public void StartPlay()
        {
            Debug.Log("Start Midi " + midiExternalPlayer.MPTK_MidiName);
            midiExternalPlayer.MPTK_Speed = 1f;
            midiExternalPlayer.MPTK_Transpose = 0;
        }

        /// <summary>
        /// Event fired by MidiFilePlayer when a midi notes are available (set by Unity Editor in MidiFilePlayer Inspector)
        /// </summary>
        public void ReadNotes(List<MidiNote> notes)
        {
            //Debug.Log("Notes : " + notes.Count);
        }

        /// <summary>
        /// Event fired by MidiFilePlayer when a midi is ended (set by Unity Editor in MidiFilePlayer Inspector)
        /// </summary>
        public void EndPlay()
        {
            Debug.Log("End Midi " + midiExternalPlayer.MPTK_MidiName);
        }

        // Use this for initialization
        void Start()
        {
            InitPlay();
        }

        public void InitPlay()
        {
            midiExternalPlayer.MPTK_Play();
        }

        public void GotoWeb(string uri)
        {
            Application.OpenURL(uri);
        }

        public void Play(string uri)
        {
            Debug.Log("Play from script:" + uri);
            midiExternalPlayer.MPTK_Stop();
            midiExternalPlayer.MPTK_MidiName = uri;
            midiExternalPlayer.MPTK_Play();
        }

        /// <summary>
        /// Event fired by MidiFilePlayer when a midi is ended (set by Unity Editor in MidiFilePlayer Inspector)
        /// </summary>
        public void RandomPlay()
        {
            midiExternalPlayer.MPTK_Play();
        }

        // Update is called once per frame
        void Update()
        {
        }
    }
}