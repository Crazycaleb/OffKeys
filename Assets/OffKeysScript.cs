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
    public KMSelectable[] ButtonageMach2;

    private int _moduleId;
    private static int _moduleIdCounter = 1;
    private bool _moduleSolved;

    private int[] KeyValueage = {0,0,0,0,0,1,1,1,1,2,2,2};
    private List<int> FaultyKeys = new List<int> {};
    private int[] Offsets = new int[12];
    private int[] Sym = new int[3];
    private int RuneSelected = -1;
    private int[] NotesToAssign = new int [3];
    private int[] Assignments = new int [4];
    private string[] Piano = {"C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B"};
    private string[] ExtendedPiano = {"C-", "C#-", "D-", "D#-", "E-", "F-", "F#-", "G-", "G#-", "A-", "A#-", "B-", "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B", "C+", "C#+", "D+", "D#+", "E+", "F+", "F#+", "G+", "G#+", "A+", "A#+", "B+", "C++"};

    int[][] RunesDiagram = new int[][]
    {
        new int[] {5, 6},
        new int[] {5, 4},
        new int[] {6, 9, 8},
        new int[] {6, 4},
        new int[] {5, 9, 11},
        new int[] {8, 10, 2},
        new int[] {5, 6},
        new int[] {4, 11, 3},
        new int[] {10, 9, 2},
        new int[] {10, 9},
        new int[] {6, 3, 8},
        new int[] {11, 0, 10},
        new int[] {3, 2, 9},
        new int[] {3, 5, 8},
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
        new int[] {2, 5, 6},
        new int[] {1, 10, 7},
        new int[] {0, 9},
        new int[] {0, 2, 4},
        new int[] {5, 10, 1},
        new int[] {1, 2},
        new int[] {4, 9},
        new int[] {2, 1, 7},
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

        foreach (KMSelectable Rune in ButtonageMach2)
        {
            Rune.OnInteract += delegate () { RuneSelect(Rune); return false; };
        }

        GeneratePuzzle();
    }

    void GeneratePuzzle() 
    {
        KeyValueage.Shuffle();

        for (int i = 0; i < KeyValueage.Length; i++)
        {
            if (KeyValueage[i] == 1)
            {
                FaultyKeys.Add(i);
            }
        }

        Debug.Log("The Faulty Keys are " + FaultyKeys.Join(","));

        for (int i = 0; i < 4; i++)
        {
            Offsets[FaultyKeys[i]] = (Rnd.Range(0,2)==0) ? 1 : -1;
        }

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
        Sym = Enumerable.Range(0, 37).Where(i => KimWexler[i] != 0).ToArray().Shuffle().Take(3).ToArray();
        if (Sym.Any(i => RunesDiagram[i].Contains(FaultyKeys[3])))
            goto WompWomp;

        var Notes = Sym.Select(i => RunesDiagram[i].Where(j => FaultyKeys.Contains(j)).OrderBy(x => x).ToArray()).ToArray();
        var oldNotes = Notes.ToArray();

        if (Notes[0].SequenceEqual(Notes[1]) || Notes[0].SequenceEqual(Notes[2]) || Notes[1].SequenceEqual(Notes[2]))
            goto WompWomp;
        var list = new List<int>();
        for (int i = 0; i < 3; i++)
            list.AddRange(Notes[i]);
        if (list.Distinct().Count() != 3)
            goto WompWomp;

        NotesToAssign = new int[3] { 99, 99, 99 };

        if (!Notes.Any(i => i.Length == 1))
            goto WompWomp;

        for (int loop = 0; loop < 3; loop++)
        {
            for (int i = 0; i < 3; i++)
            {
                if (NotesToAssign[i] != 99)
                    continue;
                if (Notes[i].Length == 0)
                    goto WompWomp;
                if (Notes[0].SequenceEqual(Notes[1]) || Notes[0].SequenceEqual(Notes[2]) || Notes[1].SequenceEqual(Notes[2]))
                    goto WompWomp;
                if (Notes[i].Length == 1)
                {
                    NotesToAssign[i] = Notes[i][0];
                    Notes[(i + 1) % 3] = Notes[(i + 1) % 3].Where(j => j != NotesToAssign[i]).ToArray();
                    Notes[(i + 2) % 3] = Notes[(i + 2) % 3].Where(j => j != NotesToAssign[i]).ToArray();
                    goto nextIter;
                }
            }
            nextIter:;
        }
        Debug.LogFormat("The Faulty keys are: " + FaultyKeys.Select(i => Piano[i]).Join(", "));
        Debug.LogFormat("The Runes are: " + Sym.Join(", "));
        Debug.Log("Notes: " + oldNotes.Select(i => i.Select(j => Piano[j]).Join(" ")).Join(", "));
        Debug.LogFormat("The note for each rune should be: " + NotesToAssign.Select(i => Piano[i]).Join(", "));
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

    private void RuneSelect(KMSelectable Rune)
    {
        if (_moduleSolved) { return; }
        Rune.AddInteractionPunch();
        for (int i = 0; i < 3; i++)
        {
            if (ButtonageMach2[i] == Rune)
            {
                RuneSelected = i + 1;         
                Debug.Log("Rune select: " + i);
            }
        }

    }

    private void Solve()
    {        
        StartCoroutine(SolveAnimation());
    }


    void InputPress(KMSelectable button)
    {
        if (_moduleSolved) { return; }
        button.AddInteractionPunch();
        for (int i = 0; i < 12; i++)
        {
            if (Buttonage[i]==button)
            {
                Debug.Log(IsThisTheSubmitButtonLol(i));
                if (RuneSelected == -1 && !IsThisTheSubmitButtonLol(i))
                {   
                    StartCoroutine(KeyMove(button.transform));
                    Audio.PlaySoundAtTransform(ExtendedPiano[i + 12 + Offsets[i]],transform);
                    StartCoroutine(DisplaySymbols());
                    
                } else  {
                    if (IsThisTheSubmitButtonLol(i)) {
                        CheckAns();
                        return;
                    }

                    if (!FaultyKeys.Contains(i))
                    {
                        return;
                    }

                    if (Assignments[FaultyKeys.IndexOf(i)] == 0)
                    {   
                        for (int j = 0; j < 4; j++)
                        {
                            if (j == FaultyKeys.IndexOf(i))
                            {
                                continue;
                            } 
                            else if (Assignments[j] == 0)
                            {
                                Assignments[FaultyKeys.IndexOf(i)] = RuneSelected;
                                StartCoroutine(YouAreDumbSymbols(RuneSelected - 1));
                                RuneSelected = -1;
                                Debug.Log("Assignments: " + Assignments.Join(", "));
                                return;
                            }
                        }
                        CheckAns();
                        return;
                    }
                }

            }
        }

    }

    private bool IsThisTheSubmitButtonLol (int x) {
        if (!FaultyKeys.Contains(x)) {
            return false;
        } else {
            for (int s = 0; s < 4; s++) {
                if (s == FaultyKeys.IndexOf(x)) {
                    continue;
                } else if (Assignments[s] == 0) {
                    return false;
                }
            }
            return true;
        }
    }

    private void CheckAns()
    {
        for (int m = 0; m < 3; m++) {
            if (NotesToAssign[m] != FaultyKeys[Array.IndexOf(Assignments, m+1)]) {
                //strike
                Module.HandleStrike();
                for (int k = 0; k < 4; k++) {
                    Assignments[k] = 0;
                }
                for (int n = 0; n < 3; n++){
                    SpriteSlots[n].color = new Color(SpriteSlots[n].color.r, SpriteSlots[n].color.g, SpriteSlots[n].color.b, 1);
                }
                return;
            }
            
        }
        //pass
        StartCoroutine(SolveAnimation());
    }

    private IEnumerator SolveAnimation()
    {
        Audio.PlaySoundAtTransform("offsolveSound", transform);
        yield return new WaitForSeconds(0.35f);
        SpriteSlots[0].color = new Color(SpriteSlots[0].color.r, SpriteSlots[0].color.g, SpriteSlots[0].color.b, 1);
        SpriteSlots[0].sprite = Symbols[39];
        yield return new WaitForSeconds(0.35f);
        SpriteSlots[1].color = new Color(SpriteSlots[1].color.r, SpriteSlots[1].color.g, SpriteSlots[1].color.b, 1);
        SpriteSlots[1].sprite = Symbols[39];
        yield return new WaitForSeconds(0.35f);
        SpriteSlots[2].color = new Color(SpriteSlots[2].color.r, SpriteSlots[2].color.g, SpriteSlots[2].color.b, 1);
        SpriteSlots[2].sprite = Symbols[38];
        Module.HandlePass();
        _moduleSolved = true;
    }

    private IEnumerator DisplaySymbols()
    {
        yield return new WaitForSeconds(0.35f);
        SpriteSlots[0].sprite = Symbols[Sym[0]];
        yield return new WaitForSeconds(0.35f);
        SpriteSlots[1].sprite = Symbols[Sym[1]];
        yield return new WaitForSeconds(0.35f);
        SpriteSlots[2].sprite = Symbols[Sym[2]];
        yield return new WaitForSeconds(0.35f);
        SpriteSlots[0].sprite = Symbols[37];
        yield return new WaitForSeconds(0.35f);
        SpriteSlots[1].sprite = Symbols[37];
        yield return new WaitForSeconds(0.35f);
        SpriteSlots[2].sprite = Symbols[37];
    }

    private IEnumerator YouAreDumbSymbols(int pos)
    {
        float lol = 1f;
        while (lol > 0f)
        {
            lol -= .02f;
            SpriteSlots[pos].color = new Color(SpriteSlots[pos].color.r, SpriteSlots[pos].color.g, SpriteSlots[pos].color.b, lol);
            yield return new WaitForSeconds(0.01f);
        }
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

