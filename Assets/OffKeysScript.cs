using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Rnd = UnityEngine.Random;

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

    private Coroutine solvePlaying;

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

        Debug.LogFormat("[Off Keys #{0}] The Faulty Keys are {1}", _moduleId, FaultyKeys.Join(","));

        for (int i = 0; i < 4; i++)
        {
            Offsets[FaultyKeys[i]] = (Rnd.Range(0,2)==0) ? 1 : -1;
        }

        List<int>[] CorKeys = Enumerable.Range(0, 4).Select(i => CalcKeys(FaultyKeys[i]).ToList()).ToArray();

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
        Debug.LogFormat("[Off Keys #{0}] The Faulty keys are: {1}", _moduleId, FaultyKeys.Select(i => Piano[i]).Join(", "));
        Debug.LogFormat("[Off Keys #{0}] The Runes are: {1}", _moduleId, Sym.Join(", "));
        Debug.Log("Notes: " + oldNotes.Select(i => i.Select(j => Piano[j]).Join(" ")).Join(", "));
        Debug.LogFormat("[Off Keys #{0}] The note for each rune should be: {1}", _moduleId, NotesToAssign.Select(i => Piano[i]).Join(", "));
        Debug.LogFormat("[Off Keys #{0}] The key to submit should be: {1}", _moduleId, FaultyKeys.Where(x => !NotesToAssign.Contains(x)).Select(x => Piano[x]).Single());
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
        if (_moduleSolved || solvePlaying != null) { return; }
        Rune.AddInteractionPunch();
        for (int i = 0; i < 3; i++)
        {
            if (ButtonageMach2[i] == Rune)
                RuneSelected = RuneSelected == i + 1 ? -1 : i + 1;
        }

    }


    void InputPress(KMSelectable button)
    {
        if (_moduleSolved || solvePlaying != null) { return; }
        button.AddInteractionPunch();
        for (int i = 0; i < 12; i++)
        {
            if (Buttonage[i]==button)
            {
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
        solvePlaying = StartCoroutine(SolveAnimation());
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


    // Twitch Plays support by Kilo Bites

#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"!{0} cycle to cycle all the keys || !{0} map 123 cdefgaa#b to map a rune in that position to a specific key. || !{0} C D# G F to press a key.";
#pragma warning restore 414

    IEnumerator ProcessTwitchCommand (string command)
    {
        string[] split = command.ToUpperInvariant().Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);

        yield return null;

        if (split[0].ContainsIgnoreCase("CYCLE"))
        {
            for (int i = 0; i < Buttonage.Length; i++)
            {
                Buttonage[i].OnInteract();
                yield return new WaitForSeconds(2);
            }
            yield break;
        }

        if (split[0].ContainsIgnoreCase("MAP"))
        {
            if (split.Length == 1)
            {
                yield return "sendtochaterror Please specify which rune you want to map!";
                yield break;
            }

            if (!"123".Contains(split[1]))
                yield break;

            if (split[1].Length > 1)
            {
                yield return "sendtochaterror Please specify only one number!";
                yield break;
            }

            if (split.Length == 2)
            {
                yield return "sendtochaterror Please specify what key to map your rune to!";
                yield break;
            }

            if (!Piano.Contains(split[2]))
                yield break;

            if (split[2].Length > 2)
                yield break;

            ButtonageMach2[int.Parse(split[1]) - 1].OnInteract();
            yield return new WaitForSeconds(0.1f);
            Buttonage[Array.IndexOf(Piano, split[2])].OnInteract();
            yield return new WaitForSeconds(0.1f);
            yield break;
        }

        if (Piano.Contains(split[0]))
        {
            if (split[0].Length > 2)
                yield break;

            Buttonage[Array.IndexOf(Piano, split[0])].OnInteract();
            yield return new WaitForSeconds(0.1f);
        }
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        yield return null;

        if (RuneSelected != -1)
        {
            ButtonageMach2[RuneSelected].OnInteract();
            yield return new WaitForSeconds(0.1f);
        }

        if (!Assignments.All(x => x == 0))
        {
            for (int i = 0; i < 3; i++)
                SpriteSlots[i].color = new Color(SpriteSlots[i].color.r, SpriteSlots[i].color.g, SpriteSlots[i].color.b, 1);

            for (int i = 0; i < 4; i++)
                Assignments[i] = 0;
        }

        for (int i = 0; i < 3; i++)
        {
            ButtonageMach2[i].OnInteract();
            yield return new WaitForSeconds(0.1f);
            Buttonage[NotesToAssign[i]].OnInteract();
            yield return new WaitForSeconds(0.1f);
        }

        Buttonage[FaultyKeys.Where(x => !NotesToAssign.Contains(x)).Single()].OnInteract();
        yield return new WaitForSeconds(0.1f);

        while (!_moduleSolved)
            yield return true;
    }

}

