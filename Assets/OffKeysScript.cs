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

    int[][] RunesDiagram = new int[][]
    {
        new int[] {5, 6},
        new int[] {5, 4},
        new int[] {6, 9, 8},
        new int[] {6, 4},
        new int[] {5, 9, 11},
        new int[] {8, 10, 2},
        new int[] {6, 9},
        new int[] {4, 11, 3},
        new int[] {10, 9, 2},
        new int[] {10, 9},
        new int[] {6, 3, 8},
        new int[] {11, 0, 10},
        new int[] {3, 2, 9},
        new int[] {3, 9, 8},
        new int[] {3, 0, 7},
        new int[] {10, 11, 3},
        new int[] {11, 9},
        new int[] {1, 7, 8},
        new int[] {6, 0, 11},
        new int[] {3, 7, 11},
        new int[] {3, 0, 1},
        new int[] {7, 0, 6},
        new int[] {5, 11, 7},
        new int[] {1, 11},
        new int[] {1, 0, 10},
        new int[] {5, 6, 10},
        new int[] {1, 10, 7},
        new int[] {0, 9},
        new int[] {0, 4, 10},
        new int[] {5, 10, 1},
        new int[] {1, 2},
        new int[] {4, 9},
        new int[] {10, 1, 7},
        new int[] {10, 2, 4},
        new int[] {4, 7},
        new int[] {1, 4, 8},
        new int[] {7, 8}
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

        List<int>[] CorKeys = Enumerable.Range(0, 4).Select(i => CalcKeys(FaultyKeys[i]).ToList()).ToArray();
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
        var Symbols = Enumerable.Range(0, 37).Where(i => KimWexler[i] != 0).ToArray().Shuffle().Take(3).ToArray();
        if (Symbols.Any(i => RunesDiagram[i].Contains(FaultyKeys[3])))
            goto WompWomp;

        var Notes = Symbols.Select(i => RunesDiagram[i].Where(j => FaultyKeys.Contains(j)).OrderBy(x => x).ToArray()).ToArray();
        var oldNotes = Notes.ToArray();

        if (Notes[0].SequenceEqual(Notes[1]) || Notes[0].SequenceEqual(Notes[2]) || Notes[1].SequenceEqual(Notes[2]))
            goto WompWomp;
        var list = new List<int>();
        for (int i = 0; i < 3; i++)
            list.AddRange(Notes[i]);
        if (list.Distinct().Count() != 3)
            goto WompWomp;

        var notesToAssign = new int[3] { 99, 99, 99 };

        if (!Notes.Any(i => i.Length == 1))
            goto WompWomp;

        for (int loop = 0; loop < 3; loop++)
        {
            for (int i = 0; i < 3; i++)
            {
                if (notesToAssign[i] != 99)
                    continue;
                if (Notes[i].Length == 0)
                    goto WompWomp;
                if (Notes[0].SequenceEqual(Notes[1]) || Notes[0].SequenceEqual(Notes[2]) || Notes[1].SequenceEqual(Notes[2]))
                    goto WompWomp;
                if (Notes[i].Length == 1)
                {
                    notesToAssign[i] = Notes[i][0];
                    Notes[(i + 1) % 3] = Notes[(i + 1) % 3].Where(j => j != notesToAssign[i]).ToArray();
                    Notes[(i + 2) % 3] = Notes[(i + 2) % 3].Where(j => j != notesToAssign[i]).ToArray();
                    goto nextIter;
                }
            }
            nextIter:;
        }
        Debug.Log("Faulty keys: " + FaultyKeys.Select(i => Piano[i]).Join(", "));
        Debug.Log("Symbols: " + Symbols.Join(", "));
        Debug.Log("Notes: " + oldNotes.Select(i => i.Select(j => Piano[j]).Join(" ")).Join(", "));
        Debug.Log("Assigned: " + notesToAssign.Select(i => Piano[i]).Join(", "));
    }

    private List<int> CalcKeys(int s)
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

