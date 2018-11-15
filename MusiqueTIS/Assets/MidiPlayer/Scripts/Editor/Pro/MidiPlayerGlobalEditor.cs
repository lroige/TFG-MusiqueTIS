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
    [CustomEditor(typeof(MidiPlayerGlobal))]
    public class MidiPlayerGlobalEditor : Editor
    {
        private SerializedProperty CustomOnEventPresetLoaded;
        private bool showEvents;
        private static MidiPlayerGlobal instance;

        void OnEnable()
        {
            try
            {
                //Debug.Log("OnEnable MidiFilePlayerEditor");
                CustomOnEventPresetLoaded = serializedObject.FindProperty("InstanceOnEventPresetLoaded");

                instance = (MidiPlayerGlobal)target;
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

                string soundFontSelected = ".";
                if (MidiPlayerGlobal.CurrentMidiSet != null && MidiPlayerGlobal.CurrentMidiSet.ActiveSounFontInfo != null)
                {
                    SoundFontInfo sfi = MidiPlayerGlobal.CurrentMidiSet.ActiveSounFontInfo;

                    EditorGUILayout.Separator();
                    // Display popup to change SoundFont
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(new GUIContent("Current SoundFont", "SoundFont selected to play sound"), GUILayout.Width(150));

                    soundFontSelected = sfi.Name;
                    // SF is loaded in a coroutine, forbidden in edit mode
                    int selectedSFIndex = MidiPlayerGlobal.MPTK_ListSoundFont.FindIndex(s => s == soundFontSelected);
                    int newSelectSF = EditorGUILayout.Popup(selectedSFIndex, MidiPlayerGlobal.MPTK_ListSoundFont.ToArray());
                    if (newSelectSF != selectedSFIndex)
                    {
                        MidiPlayerGlobal.MPTK_SelectSoundFont(MidiPlayerGlobal.MPTK_ListSoundFont[newSelectSF]);
                    }
                    EditorGUILayout.EndHorizontal();

                    // Toggle Multi Waves, parameter associated with the current selected soundfont
                    bool multiwaves = EditorGUILayout.Toggle(
                         new GUIContent("Enable Multi Waves", "Enable or disable playing more of one wave with a Midi Event. Could lead slowdown on weak device when enabled."),
                         sfi.MultiWaves);//, GUILayout.Width(100));
                    if (multiwaves != sfi.MultiWaves)
                    {
                        sfi.MultiWaves = multiwaves;
                        MidiPlayerGlobal.CurrentMidiSet.Save();
                    }

                    // Toggle Panoramic parameter associated with the current selected soundfont
                    bool panoramic = EditorGUILayout.Toggle(
                         new GUIContent("Enable Panoramic Sound", "Enable or disable the use of the pan parameter associated to waves in the SoundFont."),
                         sfi.Panoramic);
                    if (panoramic != sfi.Panoramic)
                    {
                        sfi.Panoramic = panoramic;
                        MidiPlayerGlobal.CurrentMidiSet.Save();
                    }
                }
                else
                {
                    EditorGUILayout.LabelField(new GUIContent("SoundFont: No SoundFont selected.", "No Active SoundFont found. Define SoundFont from the menu 'Tools/MPTK - SoundFont Setup' or alt-f"));
                }

                EditorGUILayout.Separator();
                showEvents = EditorGUILayout.Foldout(showEvents, "Show Events");
                if (showEvents)
                {
                    EditorGUILayout.PropertyField(CustomOnEventPresetLoaded);
                    serializedObject.ApplyModifiedProperties();
                }

                //showDefault = EditorGUILayout.Foldout(showDefault, "Show default editor");
                //if (showDefault) DrawDefaultInspector();

                if (GUI.changed) EditorUtility.SetDirty(instance);
            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
        }
        //private static bool showDefault = false;


    }

}
