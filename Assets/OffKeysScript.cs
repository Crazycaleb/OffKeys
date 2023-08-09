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
    private int[] KeyValueage = {0,0,0,0,0,1,1,1,1,2,2,2};
    private List<int> FaultyKeys = new List<int> {};
    private List<int> SymbolKeys = new List<int> {};
    private string[] Piano = {"C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B"};
    private string[] ExtendedPiano = {"C-", "C#-", "D-", "D#-", "E-", "F-", "F#-", "G-", "G#-", "A-", "A#-", "B-", "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B", "C+", "C#+", "D+", "D#+", "E+", "F+", "F#+", "G+", "G#+", "A+", "A#+", "B+", "C++"};
    //Generate puzzle (choose 3 symbols with unique solution)
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

        GeneratePuzzle();

    }

    void GeneratePuzzle() 
    {
        KeyValueage.Shuffle();

        for (int i = 0; i < KeyValueage.Length; i++)
        {
            switch(KeyValueage[i])
            {
                case 1: FaultyKeys.Add(i); break;
                case 2: SymbolKeys.Add(i); break;
                default: break;
            }
        }
        Debug.Log("The Faulty Keys are " + FaultyKeys.Join(","));
        Debug.Log("The Symbol Keys are " + SymbolKeys.Join(","));
        //Debug.Log("Corresponding key for C#: " + CorKeys("C#").Join(","));

        List<int>[] CorKeys = Enumerable.Range(0, 4).Select(i => CalcKeys(Piano[FaultyKeys[i]]).ToList()).ToArray();
        Debug.Log(CorKeys.Select(i => i.Join(",")).Join("\n"));

        int[] KimWexler = new int[37];
        for (int i = 0; i < KimWexler.Length; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                if (CorKeys[j].Contains(i))
                {
                    KimWexler[i]++;
                }
            }
        }

        Debug.Log(KimWexler.Join(","));
        WompWomp:
        var Symbols = KimWexler.Where(i => i != 0).ToArray().Shuffle().Take(3).ToArray();
        if (Symbols.Any(i => RunesDiagram[i].Contains(Piano[FaultyKeys[3]])))
            goto WompWomp;
        
        var Notes = Symbols.Select(i => RunesDiagram[i].Where(j => FaultyKeys));


            
        
    }

    private List<int> CalcKeys(string s)
    {
        List<int> HankSchrader = new List<int> {};
        for (int i = 0; i < 37; i++)
        {
            if (RunesDiagram[i].Contains(s))
            {
                HankSchrader.Add(i);
            }
        }
        return HankSchrader;

    }

    private void Solve()
    {        
        StartCoroutine(SolveAnimation());
    }


    void InputPress(KMSelectable button)
    {
        button.AddInteractionPunch();
        for (int i = 0; i < 12; i++)
        {
            if (Buttonage[i]==button)
                {
                    StartCoroutine(KeyMove(button.transform));
                    Audio.PlaySoundAtTransform(Piano[i],transform);
                }
        }    
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

