using UnityEngine;
using System.Collections.Generic;
using MidiPlayerTK;

public class NoteSpace : MonoBehaviour
{
    public MPTKNote note;
    public List<BackgroundTile> backgroundTiles;
    public GameObject noteSprite;
    private GameObject sprite;
    private float height;
    private Vector2 mousePosition;
    /* isWritten has the position of the written note, or -1 if there's no note in 
     * the noteSpace.
     */
    public float isWritten;
    public MidiStreamPlayer streamPlayer;

    void Start()
    {
        isWritten = -1;
        streamPlayer = FindObjectOfType<MidiStreamPlayer>();
    }

    private void OnMouseDown()
    {
        mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePosition = mousePosition - new Vector2(this.transform.position.x, this.transform.position.y);
        Debug.Log(mousePosition);
        DrawNote();
    }

    public void DrawNote()
    {
        Vector2 position = new Vector2(transform.position.x + (float)0.05, mousePosition[1]);
        height = mousePosition[1] - Mathf.Floor(mousePosition[1]);

        //Delete note if already written
        if (sprite != null){
            Destroy(sprite);

            //Make sure the additional lines are deactivated
            backgroundTiles[0].GetComponent<Renderer>().enabled = false;
            backgroundTiles[1].GetComponent<Renderer>().enabled = false;
            backgroundTiles[7].GetComponent<Renderer>().enabled = false;
        }

        //Define note position
        if (height < 0.25) {
            position[1] = Mathf.Floor(position[1]);
        }
        else if (height >= 0.25 && height < 0.75) {
            position[1] = Mathf.Floor(position[1]) + (float)0.5;
        }
        else {
            position[1] = Mathf.Ceil(position[1]);
        }

        //Note position in the staff (0 corresponding to G2, 0.5 to A2, etc.)
        isWritten = position[1] + (float)0.5;
        Debug.Log(isWritten);

        //Activate renderer of the tile to add additional line if the note is outside the staff
        if (isWritten <= 1.5){
            backgroundTiles[1].GetComponent<Renderer>().enabled = true;
        } if (isWritten <= 0.5){
            backgroundTiles[0].GetComponent<Renderer>().enabled = true;
        } else if (isWritten >= 7.5){
            backgroundTiles[7].GetComponent<Renderer>().enabled = true;
        }

        //TODO: Crear nota midi
        CreateNote();

        /* TODO: Aquí depenent de l'alçada rotarem la nota 
         * potser millor tenir dos sprites diferents (les corxeres i menors canvien)
         */

        //Instanciem la imatge de la nota
        position = new Vector2(this.transform.position.x, position.y + this.transform.position.y);
        sprite = Instantiate(noteSprite, position, Quaternion.identity);
        sprite.transform.parent = this.transform;
    }

    //Create the MidiNote with its correct pitch and duration.
    public void CreateNote()
    {
        int length = (int)System.Enum.Parse(typeof(MidiNote.EnumLength), "Quarter");
        int pitch;
        int keySignature = -1; //TODO: passar des de player (o score)
        MidiNote midiNote;

        pitch = FindPitch((int)(isWritten * 2), keySignature, 0);

        note = new MPTKNote()
        {
            Note = pitch,
            Delay = 0,
            Patch = 0,
            Drum = false,
            Duration = 1000,//TODO canviar a durada de la nota que toqui
            Velocity = 100
        };

        midiNote = note.ToMidiNote();
        midiNote.Length = length;

        streamPlayer.MPTK_PlayNote(midiNote);
    }

    //TODO: això aniria a una classe a part (utils?)
    /*
     * isAltered és 1 si està mig to amunt, 0 si no està alterada, -1 si està mig to avall
     * keySignature va de -7 a 7 i té el nombre de sostinguts (positius) o bemolls (negatius) de l'armadura
     * noteIndex és la posició de la nota al pentagrama i comença a 0 (G2)
     */
    private int FindPitch(int noteIndex, int keySignature, int isAltered){
        int pitch = 24;
        int noteId;

        //Primer trobem l'octava en la que estem treballant
        if (noteIndex <= 2){
            pitch += 2 * 12;
        } else if (noteIndex <= 9){
            pitch += 3 * 12;
        } else {
            pitch += 4 * 12;
        }

        //ara definim la nota segons l'índex
        switch(noteIndex){
            //C
            case 3:
            case 10:
                pitch += 0;
                noteId = 0;
                break;
            //D
            case 4:
            case 11:
                pitch += 2;
                noteId = 1;
                break;
            //E
            case 5:
            case 12:
                pitch += 4;
                noteId = 2;
                break;
            //F
            case 6:
            case 13:
                pitch += 5;
                noteId = 3;
                break;
            //G
            case 0:
            case 7:
            case 14:
                pitch += 7;
                noteId = 4;
                break;
            //A
            case 1:
            case 8:
            case 15:
                pitch += 9;
                noteId = 5;
                break;
            //B
            case 2:
            case 9:
            case 16:
                pitch += 11;
                noteId = 6;
                break;
            default:
                //Throw error
                noteId = -1;
                break;
        }

        //Fem les alteracions de l'armadura
        List<int> sharps = new List<int>{3, 0, 4, 1, 5, 2, 6};
        noteId = sharps.IndexOf(noteId);

        //If keySignature uses sharps and note is altered
        if(keySignature > 0 && noteId <= (keySignature - 1)){
            pitch += 1;
        }
        //If keySignature uses flats and note is altered
        else if (keySignature < 0 && noteId >= (7 + keySignature)){
            pitch -= 1;
        }

        //Finalment comprovem si hi ha alguna alteració
        pitch += isAltered;

        return pitch;
    }
}