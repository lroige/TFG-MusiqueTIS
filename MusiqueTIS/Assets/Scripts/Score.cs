using UnityEngine;
using System.Collections.Generic;
using MidiPlayerTK;

public class Score : MonoBehaviour
{
    private MidiStreamPlayer player;
    private List<NoteSpace> score;
    public NoteSpace noteSpace;
    //TODO: agafar dades de preread (key i time signatures, nombre de compassos)

    void Start()
    {
        player = FindObjectOfType<MidiStreamPlayer>();
        score = new List<NoteSpace>();
        for (int i = 0; i < 4; i++){
            Vector2 position = new Vector2(i, transform.position.y);
            noteSpace = Instantiate(noteSpace, position, Quaternion.identity);//TODO canviar parent
            noteSpace.transform.parent = this.transform;
            score.Add(noteSpace);
        }
    }

    //Aquí hi haurà un mètode ExportToMidi que converteixi la partitura a fitxer midi per enviar-la
}
