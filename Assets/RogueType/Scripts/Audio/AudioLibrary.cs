using UnityEngine;

[CreateAssetMenu(fileName = "AudioLibrary", menuName = "RogueType/Audio Library")]
public class AudioLibrary : ScriptableObject
{
    [Header("Music")]
    public AudioClip backgroundMusic;

    [Header("UI")]
    public AudioClip buttonClick;

    [Header("Gameplay")]
    public AudioClip playerProjectile;
    public AudioClip wallDamaged;
    public AudioClip allyArrowFire;
}
