
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.IO;
using System;
using UnityEngine.Events;
using NAudio.Midi;

namespace MidiPlayerTK
{
    /// <summary>
    /// Script for the prefab MidiFilePlayer. 
    /// Play a selected midi file. 
    /// List of Midi file must be defined with Midi Player Setup (menu tools).
    /// </summary>
    public class TestMidiWriter : MonoBehaviour
    {
        // MPTK component able to play a Midi file
        public List<MidiFilePlayer> MidiFilePlayers;

        // MPTK component able to play a stream of note like midi note generated
        public MidiStreamPlayer midiStreamPlayer;


        // Class use by the light sequencer
        #region ForSequencer

        // A note trigger by the keyboard
        class KeyboardNote : MPTKNote
        {
            public double TimeMs; // Time to play a note expressed in 'ms'  
            public int Channel = 1;
        }

        // One track of the sequencer
        class MidiPianoTrack
        {
            public bool Started;
            public int beatsPerMinute;
            public int ticksPerQuarterNote;
            public List<KeyboardNote> Notes;
            public int IndexLastNotePlayed;
            public int Patch;
            public MidiPianoTrack()
            {
                IndexLastNotePlayed = 0;
                Started = false;
                Notes = new List<KeyboardNote>();
                beatsPerMinute = 100; // slow tempo, one quarter per second
                ticksPerQuarterNote = 120; // a classical value for a Midi. define the precision of the note playing in time
            }

            public double RatioMilliSecondToTick
            {
                get { return ticksPerQuarterNote * beatsPerMinute / 60000d; }
            }
        }

        // State of key of the keyboard
        class KeyboardState
        {
            public int Key;
            public double TimeNoteOn;
            public Rect Zone;
            public KeyboardNote Note;
        }

        bool SequenceIsPlaying = false;
        MidiPianoTrack PianoTrack;
        double TimeStartCreateSequence;
        double TimeStartPlay;
        KeyboardState[] KeysState = null;
        double CurrentTimePlaying;
        // Create a popup able to select preset/patch for the sequencer
        PopupSelectPatch PopPatch;

        #endregion

        int selectedMidi;
        int spaceH = 30;
        int spaceV = 30;

        // Manage skin
        public GUISkin customSkin;
        public CustomStyle myStyle;

        Vector2 scrollerWindow = Vector2.zero;
        Vector2 scrollMidiList = Vector2.zero;

        void Awake()
        {
            //Debug.Log("Awake MidiExport");
            PianoTrack = new MidiPianoTrack();
            float StartTime = Time.realtimeSinceStartup;
            PopPatch = new PopupSelectPatch();

            if (MidiFilePlayers == null || MidiFilePlayers.Count == 0)
            {
                Component[] gameObjects = GetComponentsInChildren<Component>();
                if (gameObjects == null)
                    Debug.LogWarning("No MidiFilePlayer component found in MidiListPlayer.");
                else
                {
                    MidiFilePlayers = new List<MidiFilePlayer>();
                    foreach (Component comp in gameObjects)
                        if (comp is MidiFilePlayer)
                            MidiFilePlayers.Add((MidiFilePlayer)comp);
                }
            }

            // State of each key of the keyboard
            KeysState = new KeyboardState[127];
            for (int key = 0; key < KeysState.Length; key++)
                KeysState[key] = new KeyboardState() { Key = key, Note = null, TimeNoteOn = 0d };
        }

        void OnGUI()
        {
            // Set custom Style. Good for background color 3E619800
            if (customSkin != null) GUI.skin = customSkin;
            if (myStyle == null) myStyle = new CustomStyle();

            scrollerWindow = GUILayout.BeginScrollView(scrollerWindow, false, false, GUILayout.Width(Screen.width));

            GUILayout.Space(spaceV);
            GUILayout.Label("Demonstation of four methods to create a Midi file", myStyle.TitleLabel1);
            GUILayout.BeginHorizontal();
            GUILayout.Space(spaceH);
            if (GUILayout.Button(new GUIContent("Return to menu", ""), GUILayout.Width(400))) GoMainMenu.Go();
            GUILayout.EndHorizontal();

            GUILayout.Space(spaceV);

            //
            // Open the result directory
            // -------------------------
            GUILayout.Label("Midi Files are created here " + Application.persistentDataPath);
            GUILayout.BeginHorizontal();
            GUILayout.Space(spaceH);
            if (GUILayout.Button("Open the directory", GUILayout.Width(400)))
            {
                Application.OpenURL(Application.persistentDataPath);
            }
            GUILayout.EndHorizontal();

            //
            // Export from a list of MidiFilePlayer
            // ------------------------------------
            ExportFromListOfMidiFilePlayer();

            //
            // Create midi file from a generated midi
            // --------------------------------------
            CreateMidiFromGeneratedNote();

            //
            // Export midi file from the MPTK MidiDB list
            // ------------------------------------------
            ExportMidiFromMidiDBList();

            //
            // Export midi file from a real time generated note
            // ------------------------------------------
            CreateMidiFromPiano();

            GUILayout.EndScrollView();

        }

        // ------------------------------------------------------------------------------------------------------------------------------------
        // First ------------------------------------------------------------------------------------------------------------------------------
        // ------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// 1 / Export from a list of MidiFilePlayer
        /// </summary>
        private void ExportFromListOfMidiFilePlayer()
        {
            GUILayout.Space(spaceV);
            GUILayout.Label("1) From all MidiFilePlayer components found. The Midi must playing to exports the file.", myStyle.TitleLabel2);
            GUILayout.BeginHorizontal();
            GUILayout.Space(spaceH);
            string midiname;
            bool isPlaying = false;
            if (MidiFilePlayers != null && MidiFilePlayers.Count > 0)
            {
                midiname = ((MidiFilePlayer)MidiFilePlayers[0]).MPTK_MidiName;
                isPlaying = ((MidiFilePlayer)MidiFilePlayers[0]).MPTK_IsPlaying;
            }
            else
                midiname = "No MidiFilePlayer found";
            GUILayout.Label(midiname + (isPlaying ? " is playing" : " not playing"));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Space(spaceH);
            if (GUILayout.Button("Export from MidiFilePlayer components", GUILayout.Width(400)))
            {
                foreach (Component ms in MidiFilePlayers)
                    if (ms is MidiFilePlayer)
                    {
                        MidiFilePlayer mfp = ((MidiFilePlayer)ms);
                        string filename = Path.Combine(Application.persistentDataPath, mfp.MPTK_MidiName + "_MFP.mid");
                        MPTK_MidiFileWriter mfw = new MPTK_MidiFileWriter(mfp.MPTK_DeltaTicksPerQuarterNote, 1);
                        mfw.MPTK_LoadFromMPTK(mfp.MPTK_MidiEvents);
                        mfw.MPTK_WriteToFile(filename);
                    }
            }
            GUILayout.EndHorizontal();

        }


        // ----------------------------------------------------------------------------------------------------------------------------------
        // 2nd ------------------------------------------------------------------------------------------------------------------------------
        // ----------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// 2/ Create midi file from a generated midi
        /// </summary>
        private void CreateMidiFromGeneratedNote()
        {
            GUILayout.Space(spaceV);
            GUILayout.Label("2) From a generated Midi sequence.", myStyle.TitleLabel2);
            GUILayout.BeginHorizontal();
            GUILayout.Space(spaceH);
            GUILayout.Label("Four notes will be created");
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Space(spaceH);
            if (GUILayout.Button("Create a sequence of notes and write to the Midi file", GUILayout.Width(400)))
            {
                int beatsPerMinute = 60; // slow tempo, one quarter per second
                int ticksPerQuarterNote = 120; // a classical value for a Midi. define the precision of the note playing in time
                long absoluteTime = 0; // Time to play a note expressed in 'ticksPerQuarterNote'  
                int noteDuration = ticksPerQuarterNote; // Length to the a note expressed in 'ticksPerQuarterNote'  
                long spaceBetweenNotes = ticksPerQuarterNote; // in this example, all notes are quarter plays sequentially
                int channel = 1;
                int velocity = 100;

                // Create a midi file writer. Idea from here, thank to them.
                // http://opensebj.blogspot.com/2009/09/naudio-tutorial-7-basics-of-midi-files.html
                // https://deejaygraham.github.io/2012/09/22/using-naudio-to-generate-midi/

                MPTK_MidiFileWriter mfw = new MPTK_MidiFileWriter(ticksPerQuarterNote, 1);

                // First track (index=0) is a general midi information track. By convention contains no noteon
                // Second track (index=1) will contains the notes
                mfw.MPTK_CreateTrack(2);

                // Some textual information added to the track 0
                mfw.MPTK_AddEvent(0, new TextEvent("Midi Generated by MPTK/NAudio", MetaEventType.SequenceTrackName, absoluteTime++));

                // TimeSignatureEvent (not mandatory) exposes 
                //      Numerator(number of beats in a bar), 
                //      Denominator(which is confusingly in 'beat units' so 1 means 2, 2 means 4(crochet), 3 means 8(quaver), 4 means 16 and 5 means 32), 
                // as well as TicksInMetronomeClick and No32ndNotesInQuarterNote.
                mfw.MPTK_AddEvent(0, new TimeSignatureEvent(absoluteTime++, 4, 2, 24, 32));

                // Default tempo of playing (not mandatory)
                mfw.MPTK_AddEvent(0, new TempoEvent(MPTK_MidiFileWriter.MPTK_GetMicrosecondsPerQuaterNote(beatsPerMinute), absoluteTime++));

                // Patch/preset to use for channel 0. Generally 0 means Gran Piano
                mfw.MPTK_AddEvent(0, new PatchChangeEvent(absoluteTime++, channel, 0));

                // Add four notes : C C# D D#
                for (int note = 60; note <= 63; note++)
                {
                    mfw.MPTK_AddNote(1, absoluteTime, channel, note, velocity, noteDuration);
                    absoluteTime += spaceBetweenNotes;
                }

                // It's mandatory to close track
                mfw.MPTK_EndTrack(0);
                mfw.MPTK_EndTrack(1);

                // build the path + filename to the midi
                string filename = Path.Combine(Application.persistentDataPath, "Generated Midi.mid");

                // wrtite the midi file
                mfw.MPTK_WriteToFile(filename);
            }
            GUILayout.EndHorizontal();
        }

        // -------------------------------------------------------------------------------------------------------------------------------------------
        // Third method ------------------------------------------------------------------------------------------------------------------------------
        // -------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// 3 / Export midi file from the MPTK MidiDB list
        /// </summary>
        private void ExportMidiFromMidiDBList()
        {
            GUILayout.Space(spaceV);
            GUILayout.Label("3) From the MPTK MidiDB list.", myStyle.TitleLabel2);
            GUILayout.BeginHorizontal();
            GUILayout.Space(spaceH);
            if (MidiPlayerGlobal.CurrentMidiSet != null && MidiPlayerGlobal.CurrentMidiSet.MidiFiles != null && MidiPlayerGlobal.CurrentMidiSet.MidiFiles.Count > 0)
            {
                // Create the list of midi available
                scrollMidiList = GUILayout.BeginScrollView(scrollMidiList, false, false, myStyle.HScroll, myStyle.VScroll, myStyle.BackgWindow, GUILayout.Width(400), GUILayout.Height(120));
                int index = 0;
                foreach (string s in MidiPlayerGlobal.CurrentMidiSet.MidiFiles)
                {
                    GUIStyle styleBt = myStyle.BtStandard;
                    if (selectedMidi == index) styleBt = myStyle.BtSelected;
                    if (GUILayout.Button(s, styleBt)) selectedMidi = index;
                    index++;
                }
                GUILayout.EndScrollView();
            }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Space(spaceH);
            if (GUILayout.Button("Export from MidiDb to a Midi file", GUILayout.Width(400)))
            {
                // Create a midi file writer
                MPTK_MidiFileWriter mfw = new MPTK_MidiFileWriter();
                // Load the selected midi in writer
                mfw.MPTK_LoadFromMidiDB(selectedMidi);
                // build th path + filename to the midi
                string filename = Path.Combine(Application.persistentDataPath, MidiPlayerGlobal.CurrentMidiSet.MidiFiles[selectedMidi] + "_DB.mid");
                // wrtite the midi file
                mfw.MPTK_WriteToFile(filename);
            }
            GUILayout.EndHorizontal();
        }


        // -----------------------------------------------------------------------------------------------------------------------------------------
        // 4th method ------------------------------------------------------------------------------------------------------------------------------
        // -----------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// 4 / create a midi file from a real time notes stream
        /// </summary>
        private void CreateMidiFromPiano()
        {
            GUILayout.Space(spaceV);
            GUILayout.Label("4) From a real-time notes stream.", myStyle.TitleLabel2);

            // Detect mouse event to create note
            CheckKeyboardEvent();

            GUILayout.BeginHorizontal();
            GUILayout.Space(spaceH);

            // Select a preset
            int ambitus = 3;
            if (MidiPlayerGlobal.CurrentMidiSet != null && MidiPlayerGlobal.CurrentMidiSet.ActiveSounFontInfo != null)
            {
                if (GUILayout.Button(MidiPlayerGlobal.MPTK_ListPreset[PianoTrack.Patch], GUILayout.Width(100), GUILayout.Height(50)))
                {
                    PopPatch.Selected = PianoTrack.Patch;
                    PopPatch.DispatchPopupPatch = !PopPatch.DispatchPopupPatch;
                }
            }

            // Display popup if button above is activated
            PianoTrack.Patch = PopPatch.Draw(myStyle);

            // Get position of button which triggers the popup
            Event e = Event.current;
            if (e.type == EventType.Repaint)
            {
                Rect lastRect = GUILayoutUtility.GetLastRect();
                PopPatch.Position = new Vector2(
                    lastRect.x - scrollerWindow.x,
                    lastRect.y - PopPatch.RealRect.height - lastRect.height - scrollerWindow.y);
            }

            // Create the keyboard
            for (int key = 48; key < 48 + ambitus * 12; key++)
            {
                // Create a key
                GUILayout.Button(
                    HelperNoteLabel.LabelFromMidi(key),
                    HelperNoteLabel.IsSharp(key) ? myStyle.KeyBlack : myStyle.KeyWhite,
                    GUILayout.Width(25),
                    GUILayout.Height(HelperNoteLabel.IsSharp(key) ? 40 : 50));

                // Get last key position
                if (e.type == EventType.Repaint)
                    KeysState[key].Zone = GUILayoutUtility.GetLastRect();
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Space(spaceH);
            // Reset and create a new sequence of notes
            if (GUILayout.Button("New Sequence", GUILayout.Width(100), GUILayout.Height(30)))
            {
                PianoTrack = new MidiPianoTrack();
            }

            // Play or stop sequence
            GUIStyle styleBt = myStyle.BtStandard;
            string btAction = "Play";
            if (SequenceIsPlaying)
            {
                styleBt = myStyle.BtSelected;
                btAction = "Stop";
            }

            if (GUILayout.Button(btAction, styleBt, GUILayout.Width(100), GUILayout.Height(30)))
                if (SequenceIsPlaying)
                    StopPlaying();
                else
                    StartPlaying();

            // Info sequence
            string infoSequence = "";
            if (PianoTrack != null)
                //DateTime.FromOADate(CurrentTimePlaying).ToLongTimeString()+" " +
                if (SequenceIsPlaying)
                    infoSequence = "Playing: " + (PianoTrack.IndexLastNotePlayed + 1) + "/" + PianoTrack.Notes.Count;
                else
                    infoSequence = "Length: " + PianoTrack.Notes.Count;
            GUILayout.Label(infoSequence, myStyle.LabelCentered, GUILayout.Width(200), GUILayout.Height(30));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Space(spaceH);
            // Write the sequence as a midi file
            if (GUILayout.Button("Write the sequence of notes to a Midi file", GUILayout.Width(400)))
            {
                CreateMidiFromSequence();
            }
            GUILayout.EndHorizontal();
        }

        private void StartPlaying()
        {
            TimeStartPlay = CurrentTimeMs;
            PianoTrack.IndexLastNotePlayed = -1;
            Debug.Log("StartPlaying - TimeStartPlay:" + (int)TimeStartPlay);
            SequenceIsPlaying = true;
        }
        private void StopPlaying()
        {
            SequenceIsPlaying = false;
        }


        /// <summary>
        /// Check mouse down and up and create noteon noteoff
        /// </summary>
        private void CheckKeyboardEvent()
        {
            Event e = Event.current;
            //if (e.type != EventType.Layout && e.type != EventType.Repaint) Debug.Log(e.type + " " + e.mousePosition + " isMouse:" + e.isMouse + " isKey:" + e.isKey + " keyCode:" + e.keyCode + " modifiers:" + e.modifiers + " displayIndex:" + e.displayIndex);

            if (e.type == EventType.KeyDown || e.type == EventType.KeyUp)
            {
                if (e.keyCode >= KeyCode.Keypad0 && e.keyCode <= KeyCode.Keypad9)
                {

                    KeyboardState ks = KeysState[e.keyCode - KeyCode.Keypad0 + 48];
                    if (e.type == EventType.KeyDown)
                    {
                        if (ks.Note == null)
                        {
                            // Start the sequence at the first note detected
                            if (!PianoTrack.Started) StartSequence();

                            // Create a new note
                            NewNote(ks);
                        }
                    }
                    else if (e.type == EventType.KeyUp)
                    {
                        StopNote(ks);
                    }
                }
                e.Use();
            }

            if (e.type == EventType.MouseDown || e.type == EventType.MouseUp)
            {
                bool foundKey = false;
                foreach (KeyboardState ks in KeysState)
                {
                    if (ks != null)
                    {
                        if (ks.Zone.Contains(e.mousePosition))
                        {
                            foundKey = true;
                            if (e.type == EventType.MouseDown)
                            {
                                // Start the sequence at the first note detected
                                if (!PianoTrack.Started) StartSequence();

                                // Stop note in case of forgotten playing note !
                                StopNote(ks);

                                // Create a new note
                                NewNote(ks);
                            }
                            else if (e.type == EventType.MouseUp)
                            {
                                if (ks.Note != null)
                                    // Mouse Up inside button note with an existing noteon
                                    StopNote(ks);
                                else
                                    // Mouse Up inside button note but without noteon
                                    StopAllNotes();
                            }
                            break;
                        }
                    }
                }
                // Mouse Up outside all button note
                if (!foundKey && e.type == EventType.MouseUp)
                    StopAllNotes();

            }
        }

        /// <summary>
        /// Start sequence
        /// </summary>
        private void StartSequence()
        {
            TimeStartCreateSequence = CurrentTimeMs;
            Debug.Log("TimeStartCreateSequence:" + (int)TimeStartCreateSequence);
            PianoTrack.Started = true;
        }

        /// <summary>
        /// Create a new note and play
        /// </summary>
        /// <param name="ks"></param>
        private void NewNote(KeyboardState ks)
        {
            ks.TimeNoteOn = CurrentTimeMs - TimeStartCreateSequence;
            Debug.Log("NewNote TimeNoteOn:" + (int)ks.TimeNoteOn + " " + ks.Key);
            ks.Note = new KeyboardNote()
            {
                Note = ks.Key,
                TimeMs = ks.TimeNoteOn,
                Delay = 0,
                Patch = PianoTrack.Patch,
                Channel = 1,
                Drum = false,
                Duration = 99999,
                Velocity = 100
            };
            ks.Note.Play(midiStreamPlayer);
        }

        /// <summary>
        /// Stop note and store time & duration in milliseconds
        /// </summary>
        /// <param name="ks"></param>
        private void StopNote(KeyboardState ks)
        {
            if (ks.Note != null)
            {
                ks.Note.Stop();
                //ks.Notes.TimeMs = ks.TimeNoteOn;
                ks.Note.Duration = CurrentTimeMs - TimeStartCreateSequence - ks.TimeNoteOn;
                Debug.Log("StopNote TimeMs:" + (int)ks.Note.TimeMs + " Duration:" + (int)ks.Note.Duration);
                PianoTrack.Notes.Add(ks.Note);
                ks.Note = null;
            }
        }

        private void StopAllNotes()
        {
            foreach (KeyboardState ks in KeysState)
                if (ks != null)
                    StopNote(ks);
        }

        /// <summary>
        /// Return real time since startup in milliseconds
        /// </summary>
        private double CurrentTimeMs { get { return Time.realtimeSinceStartup * 1000d; } }

        /// <summary>
        /// Create a Midi file from the PianoTrack sequence
        /// </summary>
        private void CreateMidiFromSequence()
        {
            int lastPatch = -1;

            //// Check how many Patch are used
            //bool[] patchsUSed = new bool[127];
            //int[] trackUsed = new int[127];
            //foreach (KeyboardNote note in PianoTrack.Notes)
            //    patchsUSed[note.Patch] = true;

            //// Count track necessary and affect one patch per tracks
            //int count = 0;
            //for (int patch = 0; patch < patchsUSed.Length; patch++)
            //    if (patchsUSed[patch])
            //    {
            //        trackUsed[count] = patch;
            //        count++;
            //    }

            // Create a midi file writer. 
            MPTK_MidiFileWriter mfw = new MPTK_MidiFileWriter(PianoTrack.ticksPerQuarterNote, 1);

            // First track (index=0) is a general midi information track. By convention contains no noteon
            // Second track (index=1) will contains the notes
            mfw.MPTK_CreateTrack(2);

            // Some textual information added to the track 0
            mfw.MPTK_AddEvent(0, new TextEvent("Midi Sequence by MPTK/NAudio", MetaEventType.SequenceTrackName, 0));

            // TimeSignatureEvent (not mandatory) exposes 
            //      Numerator(number of beats in a bar), 
            //      Denominator(which is confusingly in 'beat units' so 1 means 2, 2 means 4(crochet), 3 means 8(quaver), 4 means 16 and 5 means 32), 
            //      as well as TicksInMetronomeClick and No32ndNotesInQuarterNote.
            mfw.MPTK_AddEvent(0, new TimeSignatureEvent(0, 4, 2, 24, 32));

            // Default tempo of playing (not mandatory)
            mfw.MPTK_AddEvent(0, new TempoEvent(MPTK_MidiFileWriter.MPTK_GetMicrosecondsPerQuaterNote(PianoTrack.beatsPerMinute), 0));

            foreach (KeyboardNote note in PianoTrack.Notes)
            {
                long absoluteTime = (long)(note.TimeMs * PianoTrack.RatioMilliSecondToTick);
                if (lastPatch != note.Patch)
                {
                    // Patch/preset to use for channel 1. Generally 0 means Grand Piano
                    mfw.MPTK_AddEvent(0, new PatchChangeEvent(absoluteTime, note.Channel, note.Patch));
                    lastPatch = note.Patch;
                }
                mfw.MPTK_AddNote(
                    1,
                    absoluteTime,
                    note.Channel,
                    note.Note,
                    note.Velocity,
                    (int)(note.Duration * PianoTrack.RatioMilliSecondToTick));
            }

            // It's mandatory to close track
            mfw.MPTK_EndTrack(0);
            mfw.MPTK_EndTrack(1);

            // build the path + filename to the midi
            string filename = Path.Combine(Application.persistentDataPath, "Generated Midi.mid");
            Debug.Log("Write Midi " + filename);
            // wrtite the midi file
            mfw.MPTK_WriteToFile(filename);
        }



        public void Update()
        {
            if (SequenceIsPlaying)
            {
                if (PianoTrack != null && PianoTrack.Notes != null && PianoTrack.Notes.Count > 0)
                {
                    // Calculate the current time
                    CurrentTimePlaying = CurrentTimeMs - TimeStartPlay;
                    KeyboardNote lastKn = PianoTrack.Notes[PianoTrack.Notes.Count - 1];

                    // reach end of sequence, loop after 1000 ms of delay
                    if (CurrentTimePlaying > lastKn.TimeMs + lastKn.Duration + 1000d)
                    {
                        StartPlaying();
                        CurrentTimePlaying = CurrentTimeMs - TimeStartPlay;
                    }

                    // Search the next note to play
                    for (int index = PianoTrack.IndexLastNotePlayed + 1; index < PianoTrack.Notes.Count; index++)
                    {
                        if (CurrentTimePlaying > PianoTrack.Notes[index].TimeMs)
                        {
                            PianoTrack.IndexLastNotePlayed = index;
                            // Play the note
                            PianoTrack.Notes[index].Play(midiStreamPlayer);
                            Debug.Log("Play note index:" + index + " Time:" + (int)PianoTrack.Notes[index].TimeMs + " Duration:" + (int)PianoTrack.Notes[index].Duration);
                            break;
                        }
                    }
                }
            }
        }
    }
}

