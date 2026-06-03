using UnityEngine;

public class PlayTestBootstrapper : MonoBehaviour
{
    private void Start()
    {
        var manager = FindObjectOfType<RhythmManager>();
        if (manager != null)
        {
            manager.StartSong();
            Debug.Log("Song Autostarted!");
        }
    }
}
