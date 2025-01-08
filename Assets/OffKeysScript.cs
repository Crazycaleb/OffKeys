using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;
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
    public GameObject[] IndicatorObjs;

    private int _moduleId;
    private static int _moduleIdCounter = 1;
    private bool _moduleSolved;

    private List<int> FaultyKeys = new List<int> { };
    private int[] Offsets = new int[12];
    private int[] PickedSymbols = new int[3];
    private int? _runeSelected;
    private int[] NotesToAssign = new int[3];
    private string[] Piano = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };
    private string[] ExtendedPiano = { "C-", "C#-", "D-", "D#-", "E-", "F-", "F#-", "G-", "G#-", "A-", "A#-", "B-", "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B", "C+", "C#+", "D+", "D#+", "E+", "F+", "F#+", "G+", "G#+", "A+", "A#+", "B+", "C++" };
    private int?[] _mappedKeys = new int?[3];
    private int _submitKey;

    private Coroutine solvePlaying;
    private Coroutine[] _pressAnims = new Coroutine[12];

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
        new int[] {1, 0, 9},
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

        for (int i = 0; i < PianoKeySels.Length; i++)
        {
            PianoKeySels[i].OnInteract += PianoKeyPress(i);
            PianoKeySels[i].OnInteractEnded += PianoKeyRelease(i);
        }
        for (int i = 0; i < RuneSels.Length; i++)
            RuneSels[i].OnInteract += RunePress(i);

        GeneratePuzzle();
    }

    private void GeneratePuzzle()
    {
        FaultyKeys = Enumerable.Range(0, 12).ToArray().Shuffle().Take(4).ToList(); // randomly decides faulty keys
        _submitKey = FaultyKeys[3];
        Debug.LogFormat("[Off Keys #{0}] The Faulty Keys are {1}", _moduleId, FaultyKeys.Join(","));

        for (int i = 0; i < 4; i++)
        {
            Offsets[FaultyKeys[i]] = (Rnd.Range(0, 2) == 0) ? 1 : -1; //these faulty keys play a note off by ±1 semitone at random
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
        if (PickedSymbols.Any(i => RunesDiagram[i].Contains(_submitKey))) //we start over (choose new symbols) if any chosen symbol maps to the submission key
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
        Debug.LogFormat("[Off Keys #{0}] The note for each rune should be: {1}", _moduleId, NotesToAssign.Select(i => Piano[i]).Join(", "));
        Debug.LogFormat("[Off Keys #{0}] The key to submit should be: {1}", _moduleId, Piano[_submitKey]);
    }
    //WOW BLAN WASTED HIS TIME WRITING COMMENTS FOR THE ONE PERFECT (Clueless) PART OF THE MOD ;-; dsdhaldsajklk;sadjk

    private List<int> CalcKeys(int s)
    {
        List<int> FoundSymbols = new List<int> { };
        for (int i = 0; i < 37; i++)
            if (RunesDiagram[i].Contains(s))
                FoundSymbols.Add(i);
        return FoundSymbols;
    }

    private KMSelectable.OnInteractHandler RunePress(int i)
    {
        return delegate ()
        {
            RuneSels[i].AddInteractionPunch(0.5f);
            if (_moduleSolved || solvePlaying != null)
                return false;
            _runeSelected = i;
            return false;
        };
    }

    private KMSelectable.OnInteractHandler PianoKeyPress(int i)
    {
        return delegate ()
        {
            PianoKeySels[i].AddInteractionPunch(0.5f);
            if (_pressAnims[i] != null)
                StopCoroutine(_pressAnims[i]);
            _pressAnims[i] = StartCoroutine(KeyMove(i, true));
            if (_moduleSolved || solvePlaying != null)
                return false;

            if (_runeSelected == null)
            {
                if (_mappedKeys.All(x => x != null) && i == _submitKey)
                    CheckAns();
                else
                {
                    Audio.PlaySoundAtTransform(ExtendedPiano[i + 12 + Offsets[i]], transform);
                    DisplaySymbols(true);
                }
                return false;
            }

            if (!FaultyKeys.Contains(i))
                return false;

            if (i == _submitKey)
            {
                Debug.LogFormat("[Off Keys #{0}] Attempted to map a rune to the submitting key. Strike.", _moduleId);
                _runeSelected = null;
                Module.HandleStrike();
                return false;
            }

            _mappedKeys[_runeSelected.Value] = i;
            StartCoroutine(FadeRune(_runeSelected.Value));
            _runeSelected = null;

            return false;
        };
    }

    private Action PianoKeyRelease(int i)
    {
        return delegate ()
        {
            if (_pressAnims[i] != null)
                StopCoroutine(_pressAnims[i]);
            _pressAnims[i] = StartCoroutine(KeyMove(i, false));
            if (_moduleSolved)
                return;
            DisplaySymbols(false);
            return;
        };
    }

    private void CheckAns()
    {
        var tempArr = new int[3];
        for (int i = 0; i < 3; i++)
            tempArr[i] = _mappedKeys[i].Value;

        if (!tempArr.SequenceEqual(NotesToAssign))
        {
            _mappedKeys = new int?[3];
            for (int i = 0; i < 3; i++)
                SpriteSlots[i].color = new Color(1, 1, 1, 1);
            Module.HandleStrike();
            return;
        }
        solvePlaying = StartCoroutine(SolveAnimation());
    }

    private IEnumerator SolveAnimation()
    {
        Audio.PlaySoundAtTransform("offsolveSound", transform);
        for (int i = 0; i < 3; i++)
        {
            yield return new WaitForSeconds(0.2f);
            SpriteSlots[i].sprite = Symbols[i == 2 ? 38 : 39];
            SpriteSlots[i].color = new Color(1, 1, 1, 1);
            var duration = 0.1f;
            var elapsed = 0f;
            while (elapsed < duration)
            {
                IndicatorObjs[i].transform.localScale = new Vector3(Mathf.Lerp(0f, 1.25f, elapsed / duration), Mathf.Lerp(0f, 1.25f, elapsed / duration), 1f);
                yield return null;
                elapsed += Time.deltaTime;
            }
            elapsed = 0f;
            duration = 0.05f;
            while (elapsed < duration)
            {
                IndicatorObjs[i].transform.localScale = new Vector3(Mathf.Lerp(1.25f, 1f, elapsed / duration), Mathf.Lerp(1.25f, 1f, elapsed / duration), 1f);
                yield return null;
                elapsed += Time.deltaTime;
            }
            IndicatorObjs[i].transform.localScale = new Vector3(1f, 1f, 1f);
        }
        Module.HandlePass();
        _moduleSolved = true;
    }

    private void DisplaySymbols(bool doDisplay)
    {
        for (int i = 0; i < 3; i++)
            if (_mappedKeys[i] == null)
                SpriteSlots[i].sprite = doDisplay ? Symbols[PickedSymbols[i]] : Symbols[37];
    }


    private IEnumerator FadeRune(int pos)
    {
        var duration = 0.4f;
        var elapsed = 0f;
        while (elapsed < duration)
        {
            SpriteSlots[pos].color = new Color(1, 1, 1, Mathf.Lerp(1, 0, elapsed / duration));
            yield return null;
            elapsed += Time.deltaTime;
        }
    }

    private IEnumerator KeyMove(int ix, bool isPress)
    {
        var obj = PianoKeySels[ix].gameObject;
        var pos = obj.transform.localEulerAngles;
        var duration = 0.05f;
        var elapsed = 0f;
        while (elapsed < duration)
        {
            obj.transform.localEulerAngles = new Vector3(Mathf.Lerp(pos.x, isPress ? 5f : 0, elapsed / duration), 0, 0);
            yield return null;
            elapsed += Time.deltaTime;
        }
        obj.transform.localEulerAngles = new Vector3(isPress ? 5f : 0, 0, 0);
    }

#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"!{0} press C D# F G A# [Press these keys.] | !{0} map 1 c# [Map Rune #1 to the C# key.]";
#pragma warning restore 414

    IEnumerator ProcessTwitchCommand(string command)
    {
        command = command.Trim().ToUpperInvariant();

        var parameters = command.ToUpperInvariant().Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);

        if ((parameters[0] == "PRESS" || parameters[0] == "KEY" || parameters[0] == "PLAY") && parameters.Length > 1)
        {
            var list = new List<int>();
            for (int i = 1; i < parameters.Length; i++)
            {
                int ix = Array.IndexOf(Piano, parameters[i]);
                if (ix == -1)
                {
                    yield return "sendtochaterror " + parameters[i] + " is not a valid key. Command ignored.";
                    yield break;
                }
                list.Add(ix);
            }
            yield return null;
            for (int i = 0; i < list.Count; i++)
            {
                PianoKeySels[list[i]].OnInteract();
                yield return new WaitForSeconds(1f);
                PianoKeySels[list[i]].OnInteractEnded();
                yield return new WaitForSeconds(0.25f);
            }
            yield break;
        }
        if (parameters[0] == "MAP" && parameters.Length == 3)
        {
            var rune = "123".IndexOf(parameters[1]);
            var key = Array.IndexOf(Piano, parameters[2]);

            if (rune == -1)
            {
                yield return "sendtochaterror " + parameters[1] + " is not a valid rune. Command ignored.";
                yield break;
            }
            if (key == -1)
            {
                yield return "sendtochaterror " + parameters[2] + " is not a valid key. Command ignored.";
                yield break;
            }
            if (_mappedKeys[rune] != null)
            {
                yield return "sendtochaterror Rune #" + (rune + 1) + " has already been mapped. Command ignored.";
                yield break;
            }
            yield return null;
            RuneSels[rune].OnInteract();
            yield return new WaitForSeconds(0.1f);
            yield return new[] { PianoKeySels[key].OnInteract() };
        }
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        _runeSelected = null;
        _mappedKeys = new int?[3];
        DisplaySymbols(false);
        DisplaySymbols(true);

        for (int i = 0; i < 3; i++)
        {
            RuneSels[i].OnInteract();
            yield return new WaitForSeconds(0.1f);
            PianoKeySels[NotesToAssign[i]].OnInteract();
            yield return new WaitForSeconds(0.1f);
            PianoKeySels[NotesToAssign[i]].OnInteractEnded();
            yield return new WaitForSeconds(0.1f);
        }
        PianoKeySels[_submitKey].OnInteract();
        yield return new WaitForSeconds(0.1f);
        PianoKeySels[_submitKey].OnInteractEnded();
        yield return new WaitForSeconds(0.1f);

        while (!_moduleSolved)
            yield return true;
    }
}

