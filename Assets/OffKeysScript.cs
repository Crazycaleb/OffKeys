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
    public KMSelectable[] PianoKeySels;
    public Sprite[] Symbols;
    public SpriteRenderer[] SpriteSlots;
    public KMSelectable[] RuneSels;

    private int _moduleId;
    private static int _moduleIdCounter = 1;
    private bool _moduleSolved;

    private int[] KeyShuffle = {0,0,0,0,0,0,0,0,1,1,1,1};
    private List<int> FaultyKeys = new List<int> {};
    private int[] Offsets = new int[12];
    private int[] PickedSymbols = new int[3];
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

        foreach (KMSelectable button in PianoKeySels)
        {
            button.OnInteract += delegate () { InputPress(button); return false; };
        }

        foreach (KMSelectable Rune in RuneSels)
        {
            Rune.OnInteract += delegate () { RuneSelect(Rune); return false; };
        }

        GeneratePuzzle();
    }

    void GeneratePuzzle() 
    {
        KeyShuffle.Shuffle(); // this has exactly 4 1s

        for (int i = 0; i < KeyShuffle.Length; i++)
        {
            if (KeyShuffle[i] == 1)
            {
                FaultyKeys.Add(i); //the keys at the indexes of these 1s get put into FaultyKeys
            }
        }

        Debug.LogFormat("[Off Keys #{0}] The Faulty Keys are {1}", _moduleId, FaultyKeys.Join(","));

        for (int i = 0; i < 4; i++)
        {
            Offsets[FaultyKeys[i]] = (Rnd.Range(0,2)==0) ? 1 : -1; //these faulty keys play a note off by ±1 semitone at random
        }

        List<int>[] SymbolFinder = Enumerable.Range(0, 4).Select(i => CalcKeys(FaultyKeys[i]).ToList()).ToArray(); //this turns the faulty keys to every possible symbol with that note as a corner using CalcKeys()

        int[] PositionTally = new int[37]; //positionTally is a tally for all positions tracking how many faulty note corners are "hit"
        for (int i = 0; i < PositionTally.Length; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                if (SymbolFinder[j].Contains(i))
                {
                    PositionTally[i]++;
                }
            }
        }

        StartOver:
        PickedSymbols = Enumerable.Range(0, 37).Where(i => PositionTally[i] != 0).ToArray().Shuffle().Take(3).ToArray(); //this randomly picks 3 symbols have at least one faulty note as a corner
        if (PickedSymbols.Any(i => RunesDiagram[i].Contains(FaultyKeys[3]))) //we start over (choose new symbols) if any chosen symbol maps to the note furthest along the piano? why? shouldn't this be BELOW the next bit?
            goto StartOver;

        var Notes = PickedSymbols.Select(i => RunesDiagram[i].Where(j => FaultyKeys.Contains(j)).OrderBy(x => x).ToArray()).ToArray(); //we keep track of the faulty notes at the symbols we chose ordered numerically? sure?
        var OldNotes = Notes.ToArray();

        if (Notes[0].SequenceEqual(Notes[1]) || Notes[0].SequenceEqual(Notes[2]) || Notes[1].SequenceEqual(Notes[2])) //we cannot have two notes in notes which match eachother, restart if it happens. why do we not care about the submit note?
            goto StartOver;
        var list = new List<int>(); //okay so this list is going to have every note in Notes
        for (int i = 0; i < 3; i++)
            list.AddRange(Notes[i]);
        if (list.Distinct().Count() != 3) //if the distinct number of notes in the list is anything other than 3 we start over
            goto StartOver;


        NotesToAssign = new int[3] { 99, 99, 99 }; //we have values which keep track of which note the symbol must be assigned to

        if (!Notes.Any(i => i.Length == 1)) //a note needs to be 1 to be the break-in to the puzzle, restart if this cannot happen
            goto StartOver;

        for (int loop = 0; loop < 3; loop++) //we go through this three times
        {
            for (int i = 0; i < 3; i++) //of the big loop we go through each note
            {
                if (NotesToAssign[i] != 99) //don't change if current note to assign has been assigned
                    continue;
                if (Notes[i].Length == 0) //if we cannot assign a current note we start over
                    goto StartOver;
                if (Notes[0].SequenceEqual(Notes[1]) || Notes[0].SequenceEqual(Notes[2]) || Notes[1].SequenceEqual(Notes[2])) //if notes match between any of them we have to start over
                    goto StartOver;
                if (Notes[i].Length == 1) //when we get to a single note which can be assigned
                {
                    NotesToAssign[i] = Notes[i][0]; //mark off assigned note as the one we have
                    Notes[(i + 1) % 3] = Notes[(i + 1) % 3].Where(j => j != NotesToAssign[i]).ToArray(); //and remove it as possibilities from the other two
                    Notes[(i + 2) % 3] = Notes[(i + 2) % 3].Where(j => j != NotesToAssign[i]).ToArray();
                    goto nextIter; //and immediately go back to the first note, move onto the next one in the big loop from the top
                }
            }
            nextIter:;
        }
        //i guess it just works if we make it down here, i can believe that

        Debug.LogFormat("[Off Keys #{0}] The Faulty keys are: {1}", _moduleId, FaultyKeys.Select(i => Piano[i]).Join(", "));
        Debug.LogFormat("[Off Keys #{0}] The Runes are: {1}", _moduleId, PickedSymbols.Join(", "));
        Debug.Log("Notes: " + OldNotes.Select(i => i.Select(j => Piano[j]).Join(" ")).Join(", "));
        Debug.LogFormat("[Off Keys #{0}] The note for each rune should be: {1}", _moduleId, NotesToAssign.Select(i => Piano[i]).Join(", "));
        Debug.LogFormat("[Off Keys #{0}] The key to submit should be: {1}", _moduleId, FaultyKeys.Where(x => !NotesToAssign.Contains(x)).Select(x => Piano[x]).Single());
    }
    //WOW BLAN WASTED HIS TIME WRITING COMMENTS FOR THE ONE PERFECT (Clueless) PART OF THE MOD ;-; dsdhaldsajklk;sadjk

    private List<int> CalcKeys(int s)
    {
        List<int> FoundSymbols = new List<int> {};
        for (int i = 0; i < 37; i++)
        {
            if (RunesDiagram[i].Contains(s))
            {
                FoundSymbols.Add(i);
            }
        }
        return FoundSymbols;

    }

    private void RuneSelect(KMSelectable Rune)
    {
        if (_moduleSolved || solvePlaying != null) { return; }
        Rune.AddInteractionPunch();
        for (int i = 0; i < 3; i++)
        {
            if (RuneSels[i] == Rune) {
                RuneSelected = i + 1;
            }
        }
    }


    void InputPress(KMSelectable button) //WHEN WE PRESS A KEY THIS IS BADLY NAMED hdsajjdakjfdkl;
    {
        if (_moduleSolved || solvePlaying != null) { return; }
        button.AddInteractionPunch();
        for (int i = 0; i < 12; i++)
        {
            if (PianoKeySels[i]==button)
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
        SpriteSlots[0].sprite = Symbols[PickedSymbols[0]];
        yield return new WaitForSeconds(0.35f);
        SpriteSlots[1].sprite = Symbols[PickedSymbols[1]];
        yield return new WaitForSeconds(0.35f);
        SpriteSlots[2].sprite = Symbols[PickedSymbols[2]];
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
            for (int i = 0; i < PianoKeySels.Length; i++)
            {
                PianoKeySels[i].OnInteract();
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

            if (Assignments.Any(x => x == int.Parse(split[1])))
            {
                yield return "sendtochaterror You have already assigned a rune to a key!";
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

            RuneSels[int.Parse(split[1]) - 1].OnInteract();
            yield return new WaitForSeconds(0.1f);
            PianoKeySels[Array.IndexOf(Piano, split[2])].OnInteract();
            yield return new WaitForSeconds(0.1f);
            yield break;
        }

        if (Piano.Contains(split[0]))
        {
            if (split[0].Length > 2)
                yield break;

            PianoKeySels[Array.IndexOf(Piano, split[0])].OnInteract();
            yield return new WaitForSeconds(0.1f);
        }
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        yield return null;

        if (RuneSelected != -1)
        {
            RuneSels[RuneSelected].OnInteract();
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
            RuneSels[i].OnInteract();
            yield return new WaitForSeconds(0.1f);
            PianoKeySels[NotesToAssign[i]].OnInteract();
            yield return new WaitForSeconds(0.1f);
        }

        PianoKeySels[FaultyKeys.Where(x => !NotesToAssign.Contains(x)).Single()].OnInteract();
        yield return new WaitForSeconds(0.1f);

        while (!_moduleSolved)
            yield return true;
    }

}

