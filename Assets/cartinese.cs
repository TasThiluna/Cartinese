using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;
using System.Text.RegularExpressions;
using rnd = UnityEngine.Random;

public class cartinese : MonoBehaviour
{
    public new KMAudio audio;
    private KMAudio.KMAudioRef audioRef;
    public KMBombInfo bomb;
    public KMBombModule module;

    public KMSelectable[] buttons;
    public KMSelectable screen;
    public Renderer[] buttonRenders;
    public TextMesh screenText;
    public Font solveFont;
    public Material solveMat;

    private int[] buttonColors = new int[4];
    private string[] buttonLyrics = new string[4];
    private int[] buttonScores = new int[4];
    private int[] buttonDirections = new int[4];
    private int startingLocation;
    private int currentLocation;
    private int endingLocation;

    private static readonly string[] colorNames = new string[] { "red", "yellow", "green", "blue" };
    private static readonly string[] directionNames = new string[] { "up", "right", "down", "left" };
    private static readonly string[] lyrics = new string[]
    {
        "Aingobodirou",
        "Dongifubounan",
        "Ayofumylu",
        "Dimycamilayw",
        "Dogosemiu",
        "Bitgosemiu",
        "Iwittyluyu",
        "Herolideca",
        "Anseweke",
        "Likwoveke",
        "Omeygah",
        "Dediamnatifney"
    };
    private static readonly string[] tweets = new string[]
    {
        "rEd.",
        "rich forever",
        "HA ! AMAZING !*",
        "fully loaded",
        "wHO THiS is???? playboi cahti",
        "i Kant wait .",
        "Love u !",
        "no sleep . X",
        "4vr. Lit",
        "4U !! Black heart DIE LIT *+_ SLATT !",
        "* * CARTI ALBUM @ 12*+am ok !",
        "i win k ?",
        "FRANK OCEAN IS LIFE",
        "love like you've never been hurt ! *",
        "I WANT SMOKE IN EVERY MOSH ! DONT DIE ! FREE FACETATS !",
        "love / is high school all over again ! *",
        "I Kan NOt Beef Ah bit Ya",
        "+:)",
        "FREE NUDY ! TF !",
        "HEy . <3",
        "FOSHO ! * MOOD ! *",
        "Uzi Carti * Carti Uzi * Tape ... loading",
        ". MoNDaY",
        "FY!!",
        "ima thief in the night"
    };
    private static readonly string vowels = "AEIOU";

    private static int moduleIdCounter = 1;
    private int moduleId;
    private bool moduleSolved;
    private bool easterEggUsed = true;

    void Awake()
    {
        moduleId = moduleIdCounter++;
        foreach (KMSelectable button in buttons)
            button.OnInteract += delegate () { PressButton(button); return false; };
        screen.OnInteract += delegate () { PressScreen(); return false; };
    }

    void Start()
    {
        endingLocation = rnd.Range(0, 25);
        var consonantCounts = new int[4];
    tryAgain:
        buttonColors = Enumerable.Range(0, 4).ToList().Shuffle().ToArray();
        for (int i = 0; i < 4; i++)
        {
            var lyric = lyrics.PickRandom();
            var color = buttonColors[i];
            buttonLyrics[i] = lyric;
            consonantCounts[i] = lyric.ToUpperInvariant().Count(x => !vowels.Contains(x));
            if (color == 0)
                buttonScores[i]++;
            if (i == 2)
                buttonScores[i]++;
            if (Array.IndexOf(lyrics, lyric) % 2 == 1)
                buttonScores[i]++;
            if (lyric.Count() >= 10)
                buttonScores[i]++;
            if (color == 1)
                buttonScores[i] += 2;
            if (i == 3)
                buttonScores[i] += 2;
            if (Array.IndexOf(lyrics, lyric) % 2 == 0)
                buttonScores[i] += 2;
            if (lyric.ToUpperInvariant().Count(x => vowels.Contains(x)) % 2 == 0)
                buttonScores[i] += 2;
            if (color == 2)
                buttonScores[i] += 3;
            if (i == 1)
                buttonScores[i] += 3;
            if (!vowels.Contains(lyric.ToUpperInvariant().Last()))
                buttonScores[i] += 3;
        }
        var stanzas = buttonLyrics.Select(x => Array.IndexOf(lyrics, x) / 4).ToArray();
        for (int i = 0; i < 4; i++)
            if (stanzas.Count(x => x == stanzas[i]) == 1)
                buttonScores[i] += 3;
        var directions = new int[] { 0, 1, 3, 2 };
        var table = new int[] { 1, 0, 3, 2, 2, 3, 0, 1, 0, 1, 2, 3, 3, 2, 1, 0 };
        for (int i = 0; i < 4; i++)
            buttonDirections[i] = table[Array.IndexOf(directions, Array.IndexOf(buttonColors, 3)) * 4 + (buttonScores[i] % 4)];
        if (!buttonDirections.Any(x => x % 2 == 0) || !buttonDirections.Any(x => x % 2 == 1))
            goto tryAgain;
        Debug.LogFormat("[Cartinese #{0}] Button Colors: {1}", moduleId, buttonColors.Select(x => colorNames[x]).Join(", "));
        Debug.LogFormat("[Cartinese #{0}] Lyrics played by each button: {1}", moduleId, buttonLyrics.Join(", "));
        Debug.LogFormat("[Cartinese #{0}] Scores for each button: {1}", moduleId, buttonScores.Join(", "));
        Debug.LogFormat("[Cartinese #{0}] Direction assigned to each button: {1}", moduleId, buttonDirections.Select(x => directionNames[x]).Join(", "));
        startingLocation = (bomb.GetSerialNumberNumbers().Sum() + consonantCounts.Sum()) % 25;
        currentLocation = startingLocation;
        Debug.LogFormat("[Cartinese #{0}] Starting location: {1}", moduleId, Coordinate(startingLocation));
        Debug.LogFormat("[Cartinese #{0}] Ending location: {1}", moduleId, Coordinate(endingLocation));
        StartCoroutine(CycleText());
        StartCoroutine(ColorButtons());
    }

    private void PressButton(KMSelectable button)
    {
        button.AddInteractionPunch(.25f);
        var ix = Array.IndexOf(buttons, button);
        if (!easterEggUsed && ix != 2)
        {
            easterEggUsed = true;
            audio.PlaySoundAtTransform(ix == 0 ? "fellInLuv" : ix == 1 ? "shoota" : "kidCudi", transform);
        }
        if (moduleSolved)
            return;
        switch (buttonDirections[ix])
        {
            case 0:
                currentLocation += currentLocation / 5 == 0 ? 20 : -5;
                break;
            case 1:
                currentLocation += currentLocation % 5 == 4 ? -4 : 1;
                break;
            case 2:
                currentLocation += currentLocation / 5 == 4 ? -20 : 5;
                break;
            case 3:
                currentLocation += currentLocation % 5 == 0 ? 4 : -1;
                break;
            default:
                throw new Exception(string.Format("buttonDirections[{0}] has an unexpected value.", ix));
        }
        if (audioRef != null)
        {
            audioRef.StopSound();
            audioRef = null;
        }
        audioRef = audio.HandlePlaySoundAtTransformWithRef(buttonLyrics[ix], button.transform, false);
    }

    private void PressScreen()
    {
        audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, screen.transform);
        screen.AddInteractionPunch(.25f);
        if (moduleSolved)
            return;
        Debug.LogFormat("[Cartinese #{0}] You submitted at {1}.", moduleId, Coordinate(currentLocation));
        if (currentLocation == endingLocation)
        {
            module.HandlePass();
            moduleSolved = true;
            easterEggUsed = false;
            audio.PlaySoundAtTransform("solve", transform);
            Debug.LogFormat("[Cartinese #{0}] That was correct. Module solved!", moduleId);
            StopAllCoroutines();
            StartCoroutine(SolveAnimation());
            StartCoroutine(ColorButtons());
        }
        else
        {
            module.HandleStrike();
            Debug.LogFormat("[Cartinese #{0}] That was incorrect (expected {1}). Strike!", moduleId, Coordinate(endingLocation));
            audio.PlaySoundAtTransform("strike", transform);
            currentLocation = startingLocation;
        }
    }

    private IEnumerator CycleText()
    {
        var tweet = tweets[endingLocation];
    restartCycle:
        foreach (char c in tweet)
        {
            switch (c)
            {
                case '_':
                    screenText.text = "_";
                    screenText.transform.localPosition = new Vector3(0f, 1f, .044f);
                    break;
                default:
                    screenText.text = c.ToString();
                    screenText.transform.localPosition = new Vector3(0f, 1f, -.107f);
                    break;
            }
            yield return new WaitForSeconds(1f);
        }
        goto restartCycle;
    }

    private IEnumerator SolveAnimation()
    {
        screenText.text = "";
        var red = screenText.color;
        var elapsed = 0f;
        var duration = .5f;
        while (elapsed < duration)
        {
            screenText.color = Color.Lerp(red, Color.black, elapsed / duration);
            yield return null;
            elapsed += Time.deltaTime;
        }
        screenText.color = Color.black;
        screenText.font = solveFont;
        screenText.GetComponent<Renderer>().material = solveMat;
        screenText.transform.localPosition = new Vector3(0f, 1f, -.863f);
        screenText.transform.localScale = new Vector3(.1f, .1f, .1f);
        screenText.text = "*";
        elapsed = 0f;
        while (elapsed < duration)
        {
            screenText.color = Color.Lerp(Color.black, red, elapsed / duration);
            yield return null;
            elapsed += Time.deltaTime;
        }
        screenText.color = red;
        var startColor = 0f;
        float X, Y; // These are never actually used but need to be here because RGBToHSV is incredibly stupid
        Color.RGBToHSV(screenText.color, out startColor, out X, out Y);
        StartCoroutine(Rotate());
    restartCycle:
        for (int i = 0; i < 100; ++i)
        {
            screenText.color = Color.HSVToRGB((startColor + (i * 0.01f)) % 1.0f, 1.0f, 1.0f);
            yield return new WaitForSeconds(0.025f);
        }
        goto restartCycle;
    }

    private IEnumerator Rotate()
    {
        var isCcw = rnd.Range(0, 2) == 0;
        var pivot = new GameObject("pivot"); // All this nonsense accounts for the fact that the text is moved downwards, to account for the font which has the * character higher up than I'd like it
        pivot.transform.SetParent(screen.transform, false);
        screenText.transform.parent = pivot.transform;
        var rotation = 0f;
        while (true)
        {
            var framerate = 1f / Time.deltaTime;
            rotation += 20f / framerate * (isCcw ? -1f : 1f);
            pivot.transform.localEulerAngles = new Vector3(0f, rotation, 0f);
            yield return null;
        }
    }

    private IEnumerator ColorButtons()
    {
        for (int i = 0; i < 4; i++)
        {
            StartCoroutine(ColorButton(buttonRenders[i], i, buttonRenders[i].material.color, !moduleSolved ? buttonColors[i] : 4));
            yield return new WaitForSeconds(.5f);
        }
    }

    private IEnumerator ColorButton(Renderer button, int i, Color startColor, int endColorIndex)
    {
        var elapsed = 0f;
        var duration = .75f;
        var endColor = endColorIndex == 0 ? Color.red : endColorIndex == 1 ? Color.yellow : endColorIndex == 2 ? Color.green : endColorIndex == 3 ? Color.blue : Color.black;
        while (elapsed < duration)
        {
            button.material.color = Color.Lerp(startColor, endColor, elapsed / duration);
            yield return null;
            elapsed += Time.deltaTime;
        }
        button.material.color = endColor;
    }

    private static string Coordinate(int pos)
    {
        return "ABCDE"[pos % 5] + ((pos / 5) + 1).ToString();
    }

    // Twitch Plays
#pragma warning disable 414
    private readonly string TwitchHelpMessage = "!{0} <up/down/left/right> [Presses those play buttons. Can be chained, and the first letter of each direction also works.] !{0} <u/d/l/r/> fast [Presses those play buttons, without giving extra time inbetween each press.} !{0} submit [Presses the display.] !{0} reset [Returns to the starting position.]";
#pragma warning restore 414

    IEnumerator ProcessTwitchCommand(string input)
    {
        input = input.ToLowerInvariant().Trim();
        var directions = new string[] { "up", "right", "down", "left", "u", "r", "d", "l" };
        if (input == "submit")
        {
            yield return null;
            screen.OnInteract();
        }
        else if (input == "reset")
        {
            yield return null;
            audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.TitleMenuPressed, transform);
            currentLocation = startingLocation;
        }
        else if (input.Split(' ').Take(input.Split(' ').Length - 1).All(x => directions.Contains(x)) || input.Split(' ').All(y => directions.Contains(y)))
        {
            if (input.Split(' ').Last() != "fast" && !directions.Contains(input.Split(' ').Last()))
                yield break;
            yield return null;
            var betweenTime = input.Split(' ').Last() == "fast" ? .25f : 1.5f;
            foreach (string str in input.Split(' '))
            {
                if (str == "fast")
                    continue;
                switch (str)
                {
                    case "up":
                    case "u":
                        buttons[0].OnInteract();
                        break;
                    case "right":
                    case "r":
                        buttons[1].OnInteract();
                        break;
                    case "down":
                    case "d":
                        buttons[2].OnInteract();
                        break;
                    case "left":
                    case "l":
                        buttons[3].OnInteract();
                        break;
                    default:
                        yield break;
                }
                yield return new WaitForSeconds(betweenTime);
            }
        }
        else
            yield break;
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        yield return null;
        var horizButton = 5;
        var vertiButton = 5;
        for (int i = 0; i < 4; i++)
        {
            if ((buttonDirections[i] == 0 || buttonDirections[i] == 2) && vertiButton == 5)
                vertiButton = i;
            if ((buttonDirections[i] == 1 || buttonDirections[i] == 3) && horizButton == 5)
                horizButton = i;
        }
        while (currentLocation % 5 != endingLocation % 5)
        {
            yield return new WaitForSeconds(.25f);
            buttons[horizButton].OnInteract();
        }
        while (currentLocation / 5 != endingLocation / 5)
        {
            yield return new WaitForSeconds(.25f);
            buttons[vertiButton].OnInteract();
        }
        screen.OnInteract();
    }

}
