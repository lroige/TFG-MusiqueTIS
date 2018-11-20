using UnityEngine;
using System.Collections.Generic;
using MidiPlayerTK;

public class Score : MonoBehaviour
{
    private MidiStreamPlayer player;
    private DictationController controller;
    private List<NoteSpace> score;
    public NoteSpace noteSpace;
    //TODO: agafar dades de preread (nombre de compassos)
    private int keySignature;
    private int denominator;
    private int numerator;

    void Start()
    {
        controller = FindObjectOfType<DictationController>();
        player = FindObjectOfType<MidiStreamPlayer>();

        keySignature = controller.keySignature;
        denominator = controller.denominator;
        numerator = controller.numerator;

        score = new List<NoteSpace>();
        for (int i = 0; i < numerator; i++){//todo fer més net per compassos compostos
            Vector2 position = new Vector2(i, transform.position.y);
            noteSpace = Instantiate(noteSpace, position, Quaternion.identity);
            noteSpace.transform.parent = this.transform;
            score.Add(noteSpace);
        }
    }

    //Aquí hi haurà una crida al mètode ExportToMidi que converteixi la partitura a fitxer midi per enviar-la
}
