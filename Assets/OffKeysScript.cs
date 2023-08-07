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
    public KMSelectable[] displayButtons;
    public Sprite[] Symbols;
    public SpriteRenderer[] DisplaySymbols;



    private int _moduleId;
    private static int _moduleIdCounter = 1;
    private bool _moduleSolved;


    
    private void Start()
    {
        _moduleId = _moduleIdCounter++;
    }
}

