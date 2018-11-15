using UnityEngine;
using UnityEditor;

using System;

using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace MidiPlayerTK
{
    /// <summary>
    /// Inspector for the midi global player component
    /// </summary>
    [CustomEditor(typeof(MidiExternalPlayer))]
    public class MidiExternalPlayerEditor : Editor
    {
        private SerializedProperty CustomEventListNotesEvent;
        private SerializedProperty CustomEventStartPlayMidi;
        private SerializedProperty CustomEventEndPlayMidi;

        private static MidiExternalPlayer instance;

        private static bool showEvents = false;
        private static bool showMidiInfo = false;
        private static bool showMidiParameter = false;
        private static string lastDirectory = "";
        private Texture buttonIconFolder;
        private Texture buttonIconDelete;

        private static GUILayoutOption miniButtonWidth = GUILayout.Width(20f);

        // Manage skin
        public GUISkin customSkin;
        public CustomStyle myStyle;

        //                                  Level=0            1           2           4             8      
        private string[] popupQuantization = { "None", "Quarter Note", "Eighth Note", "16th Note", "32th Note", "64th Note" };

        //static private GUIStyle styleInfoMidi;

        // PopupSelectMidi PopMidi;


        void OnEnable()
        {
            try
            {
                //Debug.Log("OnEnable MidiFilePlayerEditor");
                CustomEventStartPlayMidi = serializedObject.FindProperty("OnEventStartPlayMidi");
                CustomEventListNotesEvent = serializedObject.FindProperty("OnEventNotesMidi");
                CustomEventEndPlayMidi = serializedObject.FindProperty("OnEventEndPlayMidi");
                buttonIconFolder = Resources.Load<Texture2D>("Textures/Folders");
                buttonIconDelete = Resources.Load<Texture2D>("Textures/Delete_32x32");

                instance = (MidiExternalPlayer)target;
                // Load description of available soundfont
                if (MidiPlayerGlobal.CurrentMidiSet == null || MidiPlayerGlobal.CurrentMidiSet.ActiveSounFontInfo == null)
                {
                    ToolsEditor.LoadMidiSet();
                    ToolsEditor.CheckMidiSet();
                }
            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
        }

        public override void OnInspectorGUI()
        {
            try
            {
                GUI.changed = false;
                GUI.color = Color.white;
                // Set custom Style. Good for background color 3E619800
                if (customSkin != null) GUI.skin = customSkin;
                if (myStyle == null) myStyle = new CustomStyle();

                Event e = Event.current;

                string soundFontSelected = "No SoundFont selected.";
                if (MidiPlayerGlobal.CurrentMidiSet != null && MidiPlayerGlobal.CurrentMidiSet.ActiveSounFontInfo != null)
                {
                    soundFontSelected = MidiPlayerGlobal.CurrentMidiSet.ActiveSounFontInfo.Name;
                    EditorGUILayout.LabelField(new GUIContent("SoundFont: " + soundFontSelected, "Define SoundFont from the menu 'Tools/MPTK - SoundFont Setup' or alt-f"));
                    EditorGUILayout.Separator();

                    string helpuri = "Define an url(prefix with http:// or https:/) or a full path to a local file(prefix with file://).";
                    EditorGUILayout.LabelField(new GUIContent("Midi URL or file path:", helpuri), GUILayout.Width(150));

                    EditorGUILayout.BeginHorizontal();
                    // Select a midi from the desktop
                    bool setByBrowser = false;
                    EditorGUILayout.BeginVertical(GUILayout.Width(22));
                    if (GUILayout.Button(new GUIContent(buttonIconFolder, "Browse"), EditorStyles.miniButtonRight, miniButtonWidth, GUILayout.Height(20f)))
                    {
                        instance.MPTK_Pause();
                        string path = EditorUtility.OpenFilePanel("Select a Midi file", lastDirectory, "mid");
                        if (!string.IsNullOrEmpty(path))
                        {
                            lastDirectory = Path.GetDirectoryName(path);
                            instance.MPTK_MidiName = "file://" + path;
                            setByBrowser = true;
                        }
                    }
                    if (GUILayout.Button(new GUIContent(buttonIconDelete, "Browse"), EditorStyles.miniButtonRight, miniButtonWidth, GUILayout.Height(20f)))
                    {
                        instance.MPTK_MidiName = "";
                    }
                    EditorGUILayout.EndVertical();

                    string newmidi = EditorGUILayout.TextField(instance.MPTK_MidiName, myStyle.TextFieldMultiLine, GUILayout.Height(40f));
                    if (newmidi != instance.MPTK_MidiName || setByBrowser)
                    {
                        instance.MPTK_MidiName = newmidi;

                    }

                    if (setByBrowser)
                    {
                        instance.MPTK_Stop();
                        instance.MPTK_Play();
                    }
                    EditorGUILayout.EndHorizontal();

                    float volume = EditorGUILayout.Slider(new GUIContent("Volume", "Set global volume for this midi playing"), instance.MPTK_Volume, 0f, 1f);
                    if (instance.MPTK_Volume != volume)
                        instance.MPTK_Volume = volume;

                    instance.MPTK_PlayOnStart = EditorGUILayout.Toggle(new GUIContent("Play At Startup", "Start playing midi when the application starts"), instance.MPTK_PlayOnStart);
                    instance.MPTK_DirectSendToPlayer = EditorGUILayout.Toggle(new GUIContent("Direct send to player", "Midi events are send to the midi player directly"), instance.MPTK_DirectSendToPlayer);

                    EditorGUILayout.BeginHorizontal();
                    string tooltipDistance = "Playing is paused if distance between AudioListener and this component is greater than MaxDistance";
                    instance.MPTK_PauseOnDistance = EditorGUILayout.Toggle(new GUIContent("Pause With Distance", tooltipDistance), instance.MPTK_PauseOnDistance);
                    EditorGUILayout.LabelField(new GUIContent("Current:" + Math.Round(instance.distanceEditorModeOnly, 2), tooltipDistance));
                    EditorGUILayout.EndHorizontal();

                    float distance = EditorGUILayout.Slider(new GUIContent("Max Distance", tooltipDistance), instance.MPTK_MaxDistance, 0f, 500f);
                    if (instance.MPTK_MaxDistance != distance)
                        instance.MPTK_MaxDistance = distance;

                    instance.MPTK_Loop = EditorGUILayout.Toggle(new GUIContent("Loop", "Enable loop on midi play"), instance.MPTK_Loop);

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.PrefixLabel("Speed");
                    float speed = EditorGUILayout.Slider(instance.MPTK_Speed, 0.1f, 5f);
                    if (instance.MPTK_Speed != speed)
                        instance.MPTK_Speed = speed;
                    EditorGUILayout.EndHorizontal();


                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.PrefixLabel("Transpose");
                    instance.MPTK_Transpose = EditorGUILayout.IntSlider(instance.MPTK_Transpose, -24, 24);
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.PrefixLabel("Release Time (sec.)");
                    instance.MPTK_TimeToRelease = EditorGUILayout.Slider(instance.MPTK_TimeToRelease, 0.05f, 1f);
                    EditorGUILayout.EndHorizontal();

                    instance.MPTK_LogWaves = EditorGUILayout.Toggle(new GUIContent("Log Waves", "Log information about wave for each notes played"), instance.MPTK_LogWaves);

                    if (EditorApplication.isPlaying)
                    {

                        EditorGUILayout.Separator();
                        EditorGUILayout.LabelField("Time", instance.playTimeEditorModeOnly + " / " + instance.durationEditorModeOnly);

                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.PrefixLabel("Position");
                        float pos = EditorGUILayout.Slider(instance.MPTK_Position, 0f, (float)instance.MPTK_Duration.TotalMilliseconds);
                        if (instance.MPTK_Position != pos)
                            instance.MPTK_Position = pos;
                        EditorGUILayout.EndHorizontal();

                        EditorGUILayout.BeginHorizontal();

                        if (instance.MPTK_IsPlaying && !instance.MPTK_IsPaused)
                            GUI.color = ToolsEditor.ButtonColor;
                        if (GUILayout.Button(new GUIContent("Play", "")))
                            instance.MPTK_Play();
                        GUI.color = Color.white;

                        if (instance.MPTK_IsPaused)
                            GUI.color = ToolsEditor.ButtonColor;
                        if (GUILayout.Button(new GUIContent("Pause", "")))
                            if (instance.MPTK_IsPaused)
                                instance.MPTK_Play();
                            else
                                instance.MPTK_Pause();
                        GUI.color = Color.white;

                        if (GUILayout.Button(new GUIContent("Stop", "")))
                            instance.MPTK_Stop();

                        if (GUILayout.Button(new GUIContent("Restart", "")))
                            instance.MPTK_RePlay();
                        EditorGUILayout.EndHorizontal();
                    }

                    showMidiParameter = EditorGUILayout.Foldout(showMidiParameter, "Show Midi Parameters");
                    if (showMidiParameter)
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField(new GUIContent("Quantization", ""), GUILayout.Width(150));
                        int newLevel = EditorGUILayout.Popup(instance.MPTK_Quantization, popupQuantization);
                        if (newLevel != instance.MPTK_Quantization && newLevel >= 0 && newLevel < popupQuantization.Length)
                            instance.MPTK_Quantization = newLevel;
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.BeginHorizontal();
                        instance.MPTK_EnableChangeTempo = EditorGUILayout.Toggle(new GUIContent("Enable Tempo Change", "Enable midi event tempo change when playing"), instance.MPTK_EnableChangeTempo);
                        EditorGUILayout.LabelField(new GUIContent("Current:" + Math.Round(instance.MPTK_Tempo, 0), "Current tempo defined in Midi"));
                        EditorGUILayout.EndHorizontal();
                        instance.MPTK_EnablePanChange = EditorGUILayout.Toggle(new GUIContent("Enable Pan Change", "Enable midi event pan change when playing"), instance.MPTK_EnablePanChange);
                        instance.MPTK_KeepNoteOff = EditorGUILayout.Toggle(new GUIContent("Keep Midi NoteOff", "Keep Midi NoteOff and NoteOn with Velocity=0 (need to restart the playing Midi)"), instance.MPTK_KeepNoteOff);
                        instance.MPTK_LogEvents = EditorGUILayout.Toggle(new GUIContent("Log Midi Events", "Log information about each midi events read"), instance.MPTK_LogEvents);
                    }

                    showMidiInfo = EditorGUILayout.Foldout(showMidiInfo, "Show Midi Info");
                    if (showMidiInfo)
                    {
                        if (!string.IsNullOrEmpty(instance.MPTK_SequenceTrackName))
                        {
                            if (taSequence == null) taSequence = new TextArea("Sequence");
                            taSequence.Display(instance.MPTK_SequenceTrackName);
                        }

                        if (!string.IsNullOrEmpty(instance.MPTK_ProgramName))
                        {
                            if (taProgram == null) taProgram = new TextArea("Program");
                            taProgram.Display(instance.MPTK_ProgramName);
                        }

                        if (!string.IsNullOrEmpty(instance.MPTK_TrackInstrumentName))
                        {
                            if (taInstrument == null) taInstrument = new TextArea("Instrument");
                            taInstrument.Display(instance.MPTK_TrackInstrumentName);
                        }

                        if (!string.IsNullOrEmpty(instance.MPTK_TextEvent))
                        {
                            if (taText == null) taText = new TextArea("TextEvent");
                            taText.Display(instance.MPTK_TextEvent);
                        }

                        if (!string.IsNullOrEmpty(instance.MPTK_Copyright))
                        {
                            if (taCopyright == null) taCopyright = new TextArea("Copyright");
                            taCopyright.Display(instance.MPTK_Copyright);
                        }
                    }

                    showEvents = EditorGUILayout.Foldout(showEvents, "Show Events");
                    if (showEvents)
                    {
                        EditorGUILayout.PropertyField(CustomEventStartPlayMidi);
                        EditorGUILayout.PropertyField(CustomEventListNotesEvent);
                        EditorGUILayout.PropertyField(CustomEventEndPlayMidi);
                        serializedObject.ApplyModifiedProperties();
                    }


                    //showDefault = EditorGUILayout.Foldout(showDefault, "Show default editor");
                    //if (showDefault) DrawDefaultInspector();
                }
                else
                {
                    EditorGUILayout.LabelField(new GUIContent("SoundFont: " + soundFontSelected, "Define SoundFont from the menu 'Tools/MPTK - SoundFont Setup' or alt-f"));
                    ToolsEditor.LoadMidiSet();
                    ToolsEditor.CheckMidiSet();
                }

                if (GUI.changed) EditorUtility.SetDirty(instance);
            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
        }
        //private static bool showDefault = false;
        private TextArea taSequence;
        private TextArea taProgram;
        private TextArea taInstrument;
        private TextArea taText;
        private TextArea taCopyright;
    }

}
