using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct NoteData
{
    public float spawnTimeOffset; // Time delay relative to song launch marker
    public int holeLaneIndex;     // Integer 1 through 10
    public PlayerController.BreathState actionType; 
    public float holdLength;      // Value 0 for normal taps
}

[CreateAssetMenu(fileName = "NewSongChart", menuName = "MouthOrgan/SongChart")]
public class SongChart : ScriptableObject
{
    public string trackingTitle = "Mary Had a Little Lamb";
    public float beatsPerMinute = 120f;
    public List<NoteData> trackTimeline;
}
