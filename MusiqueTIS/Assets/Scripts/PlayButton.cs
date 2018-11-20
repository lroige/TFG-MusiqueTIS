using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MidiPlayerTK;

public class PlayButton : MonoBehaviour {
    public Button playButton;
    public MidiFilePlayer player;

    // Use this for initialization
    void Start () {
        player = FindObjectOfType<MidiFilePlayer>();
        playButton.onClick.AddListener(OnTogglePlay);
        player.OnEventEndPlayMidi.AddListener(OnMidiEnd);
    }
    
    void OnTogglePlay(){
        if (!player.MPTK_IsPlaying){
            playButton.GetComponentInChildren<Text>().text = "Pause";
            player.MPTK_RePlay();

        } else {
            playButton.GetComponentInChildren<Text>().text = "Play";
            player.MPTK_Stop();
        }
    }

    void OnMidiEnd(){
        playButton.GetComponentInChildren<Text>().text = "Play";
    }
}
