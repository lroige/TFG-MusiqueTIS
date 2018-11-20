using UnityEngine;
using System.Collections.Generic;
using MidiPlayerTK;
using NAudio.Midi;

public class DictationController : MonoBehaviour
{
    public MidiFilePlayer filePlayer;
    private MidiStreamPlayer streamPlayer;
    public bool isErasing;
    public int keySignature;
    public int tempo;
    public int denominator;
    public int numerator;

    // Use this for initialization
    void Start()
    {
        isErasing = false;
        filePlayer = FindObjectOfType<MidiFilePlayer>();
        streamPlayer = FindObjectOfType<MidiStreamPlayer>();
        denominator = 0;
        numerator = 0;
        PreRead();
    }

    public void SwitchErasing(){
        isErasing = !isErasing;
    }

    /* Llegeix els primers tracks del fitxer midi i crea el midi amb la i pulsació.
     */
    private void PreRead()
    {
        filePlayer.MPTK_DirectSendToPlayer = false;
        filePlayer.MPTK_Play();
        tempo = (int)filePlayer.MPTK_Tempo;
        filePlayer.MPTK_Stop();
        List<TrackMidiEvent> events = filePlayer.MPTK_MidiEvents;
        events.ForEach((obj) =>
        {
            if ((obj.Event is TimeSignatureEvent) || (obj.Event is KeySignatureEvent))
            {
                if (obj.Event is TimeSignatureEvent)
                {
                    TimeSignatureEvent signature = obj.Event as TimeSignatureEvent;
                    numerator = signature.Numerator;
                    //Denominator returns log2(denominator), so it needs to be recalculated
                    denominator = 1 << signature.Denominator;
                }
                else if (obj.Event is KeySignatureEvent)
                {
                    KeySignatureEvent signature = obj.Event as KeySignatureEvent;
                    keySignature = signature.SharpsFlats;
                    if (keySignature > 10)
                    {
                        keySignature = keySignature - 256;
                    }
                }

            }
        });
        //numberOfMeasures = NumberOfMeasures();
        filePlayer.MPTK_DirectSendToPlayer = true;
    }

    //També hi ha d'haver el controlador de play i pause

}
