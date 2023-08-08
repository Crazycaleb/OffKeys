using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using Rnd = UnityEngine.Random;
using KModkit;

public class OffKeysScript : MonoBehaviour
{
    public KMBombModule Module;
    public KMBombInfo BombInfo;
    public KMAudio Audio;
    public KMSelectable[] Buttonage;
    public Sprite[] Symbols;
    public SpriteRenderer[] SpriteSlots;



    private int _moduleId;
    private static int _moduleIdCounter = 1;
    private bool _moduleSolved;
    private string[] Piano = {"C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B"};
    private string[] ExtendedPiano = {"C-", "C#-", "D-", "D#-", "E-", "F-", "F#-", "G-", "G#-", "A-", "A#-", "B-", "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B", "C+", "C#+", "D+", "D#+", "E+", "F+", "F#+", "G+", "G#+", "A+", "A#+", "B+", "C++"};
    //Generate puzzle (choose 3 symbols with unique solution)
    //Rnd.random(-12,12)
    //while random=0, reroll
    //if sound != same sound, find sound in extended piano, apply offset, play sound, big winner!!! bang bang bang
    


    string[][] RunesDiagram = new string[][] 
    {
        new string[] {"F", "F#"},
        new string[] {"F", "E"},
        new string[] {"F#", "A", "G#"},
        new string[] {"F#", "E"},
        new string[] {"F", "A", "B"},
        new string[] {"G#", "A#", "D"},
        new string[] {"F#", "A"},
        new string[] {"E", "B", "D#"},
        new string[] {"A#", "A", "D"},
        new string[] {"A#", "A"},
        new string[] {"F#", "D#", "G#"},
        new string[] {"B", "C", "A#"},
        new string[] {"D#", "D", "A"},
        new string[] {"D#", "A", "G#"},
        new string[] {"D#", "C", "G"},
        new string[] {"A#", "B", "D#"},
        new string[] {"B", "A"},
        new string[] {"C#", "G", "G#"},
        new string[] {"F#", "C", "B"},
        new string[] {"D#", "G", "B"},
        new string[] {"D#", "C", "C#"},
        new string[] {"G", "C", "F#"},
        new string[] {"F", "B", "G"},
        new string[] {"C#", "B"},
        new string[] {"C#", "C", "A#"},
        new string[] {"F", "F#", "A#"},
        new string[] {"C#", "A#", "G"},
        new string[] {"C", "A"},
        new string[] {"C", "E", "A#"},
        new string[] {"F", "A#", "C#"},
        new string[] {"C#", "D"},
        new string[] {"E", "A"},
        new string[] {"A#", "C#", "G"},
        new string[] {"A#", "D", "E"},
        new string[] {"E", "G"},
        new string[] {"C#", "E", "G#"},
        new string[] {"G", "G#"}
    };

    private void Start()
    {
        _moduleId = _moduleIdCounter++;

        foreach (KMSelectable button in Buttonage)
        {
            button.OnInteract += delegate () { InputPress(button); return false; };
        }

    }

    private void Solve()
    {        
        StartCoroutine(SolveAnimation());
    }


    void InputPress(KMSelectable button)
    {
        button.AddInteractionPunch();
        for (int i = 11; i > -1; i--)
        {
            if (Buttonage[i]==button)
                {
                    StartCoroutine(KeyMove(button.transform));
                    Audio.PlaySoundAtTransform(Piano[i],transform);
                }
        }    
        Solve();
    }

    private IEnumerator SolveAnimation()
    {
        Audio.PlaySoundAtTransform("offsolveSound", transform);
        yield return new WaitForSeconds(0.35f);
        SpriteSlots[0].sprite = Symbols[39];
        yield return new WaitForSeconds(0.35f);
        SpriteSlots[1].sprite = Symbols[39];
        yield return new WaitForSeconds(0.35f);
        SpriteSlots[2].sprite = Symbols[38];
        Module.HandlePass();
        _moduleSolved = true;
    }

    IEnumerator KeyMove(Transform tf)
    {
        float delta = 0;
        while (delta < 1)
        {
            delta += 6 * Time.deltaTime;
            tf.localEulerAngles = new Vector3(Mathf.Lerp(0, 5, delta), 0, 0);
            yield return null;
        }
        tf.localEulerAngles = new Vector3(0, 0, 0);
    }


}

