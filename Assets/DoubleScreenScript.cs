using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;
using System.Text.RegularExpressions;
using System;
using Newtonsoft.Json;

public class DoubleScreenScript : MonoBehaviour {

    class ktaneData
    {
        public List<Dictionary<string, object>> KtaneModules { get; set; }
    }

    public KMAudio audio;
    public KMBombInfo bomb;
    public KMColorblindMode colorblind;

    public KMSelectable[] buttons;
    public GameObject[] screens;
    public TextMesh[] texts;
    public TextMesh[] cbtexts;
    public Material[] allcolors;

    private WWW fetch;
    private List<Module> modules = new List<Module>();
    private List<string> truths = new List<string>();
    private List<string> lies = new List<string>();
    private string[] displayed = new string[2];
    private int[] colors = new int[2];
    private bool firstScreenCorrect;
    private bool animating;
    private bool cbactive;
    private int stage;
    private int stageCount;
    private int initTime;
 
    static int moduleIdCounter = 1;
    int moduleId;
    private bool moduleSolved;

    void Awake()
    {
        moduleId = moduleIdCounter++;
        moduleSolved = false;
        StartCoroutine(FetchModules());
        if (colorblind.ColorblindModeActive)
            cbactive = true;
        foreach (KMSelectable obj in buttons)
        {
            KMSelectable pressed = obj;
            pressed.OnInteract += delegate () { PressButton(pressed); return false; };
        }
        GetComponent<KMBombModule>().OnActivate += OnActivate;
    }

    void Start () {
        initTime = (int)bomb.GetTime();
        stageCount = UnityEngine.Random.Range(2, 4);
        Debug.LogFormat("[Double Screen #{0}] This module will require {1} presses to solve", moduleId, stageCount);
        texts[0].text = "";
        texts[1].text = "";
        screens[0].SetActive(false);
        screens[1].SetActive(false);
    }

    void OnActivate()
    {
        animating = true;
        GenerateScreens();
        StartCoroutine(ShowText());
    }

    void PressButton(KMSelectable pressed)
    {
        if (moduleSolved != true && animating != true)
        {
            pressed.AddInteractionPunch();
            audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, pressed.transform);
            if ((firstScreenCorrect && pressed == buttons[0]) || (!firstScreenCorrect && pressed == buttons[1]))
            {
                if ((stageCount == 3 && stage != 2) || (stageCount == 2 && stage != 1))
                    Debug.LogFormat("[Double Screen #{0}] Pressing the {1} screen was correct, generating new screens...", moduleId, pressed == buttons[0] ? "top" : "bottom");
                else
                    Debug.LogFormat("[Double Screen #{0}] Pressing the {1} screen was correct", moduleId, pressed == buttons[0] ? "top" : "bottom");
                stage++;
            }
            else
            {
                Debug.LogFormat("[Double Screen #{0}] Pressing the {1} screen was incorrect, Strike! Resetting the module...", moduleId, pressed == buttons[0] ? "top" : "bottom");
                stage = 0;
                GetComponent<KMBombModule>().HandleStrike();
            }
            StartCoroutine(RemoveText());
        }
    }

    private void GenerateScreens()
    {
        TruthRules();
        colors[0] = UnityEngine.Random.Range(0, allcolors.Length);
        colors[1] = UnityEngine.Random.Range(0, allcolors.Length);
        int rand = UnityEngine.Random.Range(0, 2);
        if (rand == 0)
            firstScreenCorrect = true;
        else
            firstScreenCorrect = false;
        for (int i = 0; i < 2; i++)
        {
            if ((firstScreenCorrect && i == 0) || (!firstScreenCorrect && i == 1))
            {
                displayed[i] = truths[UnityEngine.Random.Range(0, truths.Count())];
            }
            else
            {
                displayed[i] = lies[UnityEngine.Random.Range(0, lies.Count())];
            }
            string[] words = displayed[i].Split(' ');
            for (int j = 0; j < words.Length; j++)
            {
                if (colors[i] == 0)
                {
                    if (words[j].EndsWith("minus"))
                        words[j] = words[j].Replace("minus", "plus");
                    else if (words[j].EndsWith("plus"))
                        words[j] = words[j].Replace("plus", "minus");
                    else if (words[j].EndsWith("absent"))
                    {
                        words[j] = words[j].Replace("absent", "present");
                        words[j + 1] = words[j + 1].Replace("from", "on");
                    }
                    else if (words[j].EndsWith("present"))
                    {
                        words[j] = words[j].Replace("present", "absent");
                        words[j + 1] = words[j + 1].Replace("on", "from");
                    }
                    else if (words[j].EndsWith("needy"))
                        words[j] = words[j].Replace("needy", "regular");
                    else if (words[j].EndsWith("regular"))
                        words[j] = words[j].Replace("regular", "needy");
                }
                else if (colors[i] == 1)
                {
                    if (words[j].Equals("less"))
                        words[j] = words[j].Replace("less", "more");
                    else if (words[j].Equals("more"))
                        words[j] = words[j].Replace("more", "less");
                    else if (words[j].Equals("and"))
                        words[j] = words[j].Replace("and", "or");
                    else if (words[j].Equals("or"))
                        words[j] = words[j].Replace("or", "and");
                    else if (words[j].EndsWith("absent"))
                    {
                        words[j] = words[j].Replace("absent", "present");
                        words[j + 1] = words[j + 1].Replace("from", "on");
                    }
                    else if (words[j].EndsWith("present"))
                    {
                        words[j] = words[j].Replace("present", "absent");
                        words[j + 1] = words[j + 1].Replace("on", "from");
                    }
                }
                else if (colors[i] == 2)
                {
                    if (words[j].StartsWith("even"))
                        words[j] = words[j].Replace("even", "odd");
                    else if (words[j].StartsWith("odd"))
                        words[j] = words[j].Replace("odd", "even");
                    else if (words[j].StartsWith("no"))
                        words[j] = words[j].Replace("no", "some");
                    else if (words[j].StartsWith("some"))
                        words[j] = words[j].Replace("some", "no");
                    else if (words[j].Equals("and"))
                        words[j] = words[j].Replace("and", "or");
                    else if (words[j].Equals("or"))
                        words[j] = words[j].Replace("or", "and");
                }
                else if (colors[i] == 3)
                {
                    if (words[j].EndsWith("minus"))
                        words[j] = words[j].Replace("minus", "plus");
                    else if (words[j].EndsWith("plus"))
                        words[j] = words[j].Replace("plus", "minus");
                    else if (words[j].EndsWith("needy"))
                        words[j] = words[j].Replace("needy", "regular");
                    else if (words[j].EndsWith("regular"))
                        words[j] = words[j].Replace("regular", "needy");
                    else if (words[j].Equals("less"))
                        words[j] = words[j].Replace("less", "more");
                    else if (words[j].Equals("more"))
                        words[j] = words[j].Replace("more", "less");
                }
            }
            displayed[i] = words.Join(" ");
            Debug.LogFormat("[Double Screen #{0}] The {1} screen is {2} and it's showing \"{3}\"", moduleId, i == 0 ? "top" : "bottom", allcolors[colors[i]].name.Replace("Screen", ""), displayed[i].Replace("\n", " "));
        }
        Debug.LogFormat("[Double Screen #{0}] The correct screen to press is the {1} screen", moduleId, firstScreenCorrect ? "top" : "bottom");
    }

    private void TruthRules()
    {
        if (bomb.GetSolvableModuleNames().Count() % 2 == 0)
            truths.Add("The number of\nmodules on this\nbomb is even\n(excluding\nneedy modules).");
        else
            lies.Add("The number of\nmodules on this\nbomb is even\n(excluding\nneedy modules).");
        if (bomb.GetSolvableModuleNames().Count() % 2 == 1)
            truths.Add("The number of\nmodules on this\nbomb is odd\n(excluding\nneedy modules).");
        else
            lies.Add("The number of\nmodules on this\nbomb is odd\n(excluding\nneedy modules).");
        if ((bomb.GetModuleNames().Count() - bomb.GetSolvableModuleNames().Count()) % 2 == 0)
            truths.Add("The number of\nmodules on this\nbomb is even\n(excluding\nregular modules).");
        else
            lies.Add("The number of\nmodules on this\nbomb is even\n(excluding\nregular modules).");
        if ((bomb.GetModuleNames().Count() - bomb.GetSolvableModuleNames().Count()) % 2 == 1)
            truths.Add("The number of\nmodules on this\nbomb is odd\n(excluding\nregular modules).");
        else
            lies.Add("The number of\nmodules on this\nbomb is odd\n(excluding\nregular modules).");
        truths.Add(string.Format("This bomb\nstarted with\nless than {0}\nminutes.", UnityEngine.Random.Range(initTime / 60 + 1, initTime / 60 + 6)));
        truths.Add(string.Format("This bomb\nstarted with\nmore than {0}\nminutes.", UnityEngine.Random.Range(initTime / 60 - 5, initTime / 60)));
        lies.Add(string.Format("This bomb\nstarted with\nmore than {0}\nminutes.", UnityEngine.Random.Range(initTime / 60 + 1, initTime / 60 + 6)));
        lies.Add(string.Format("This bomb\nstarted with\nless than {0}\nminutes.", UnityEngine.Random.Range(initTime / 60 - 5, initTime / 60)));
        string mod = bomb.GetModuleNames().PickRandom();
        if (mod.Length > 16)
        {
            if (mod.Substring(0, 16).Contains(' '))
                mod = mod.Substring(0, mod.LastIndexOf(' ')) + "\n" + mod.Substring(mod.LastIndexOf(' ') + 1, mod.Length - mod.LastIndexOf(' ') - 1);
            else
                mod = mod.Substring(0, 16) + "\n" + mod.Substring(16, mod.Length - 16);
        }
        truths.Add(string.Format("There is {1}\n{0}\npresent on this\nbomb.", mod, "AEIOUaeiou".Contains(mod[0]) ? "an" : "a"));
        mod = bomb.GetModuleNames().PickRandom();
        if (mod.Length > 16)
        {
            if (mod.Substring(0, 16).Contains(' '))
                mod = mod.Substring(0, mod.LastIndexOf(' ')) + "\n" + mod.Substring(mod.LastIndexOf(' ') + 1, mod.Length - mod.LastIndexOf(' ') - 1);
            else
                mod = mod.Substring(0, 16) + "\n" + mod.Substring(16, mod.Length - 16);
        }
        lies.Add(string.Format("There is {1}\n{0}\nabsent from this\nbomb.", mod, "AEIOUaeiou".Contains(mod[0]) ? "an" : "a"));
        if (fetch.error == null && fetch.isDone)
        {
            mod = modules.PickRandom().Name;
            while (bomb.GetModuleNames().Contains(mod))
                mod = modules.PickRandom().Name;
            if (mod.Length > 16)
            {
                if (mod.Substring(0, 16).Contains(' '))
                    mod = mod.Substring(0, mod.LastIndexOf(' ')) + "\n" + mod.Substring(mod.LastIndexOf(' ') + 1, mod.Length - mod.LastIndexOf(' ') - 1);
                else
                    mod = mod.Substring(0, 16) + "\n" + mod.Substring(16, mod.Length - 16);
            }
            truths.Add(string.Format("There is {1}\n{0}\nabsent from this\nbomb.", mod, "AEIOUaeiou".Contains(mod[0]) ? "an" : "a"));
            mod = modules.PickRandom().Name;
            while (bomb.GetModuleNames().Contains(mod))
                mod = modules.PickRandom().Name;
            if (mod.Length > 16)
            {
                if (mod.Substring(0, 16).Contains(' '))
                    mod = mod.Substring(0, mod.LastIndexOf(' ')) + "\n" + mod.Substring(mod.LastIndexOf(' ') + 1, mod.Length - mod.LastIndexOf(' ') - 1);
                else
                    mod = mod.Substring(0, 16) + "\n" + mod.Substring(16, mod.Length - 16);
            }
            lies.Add(string.Format("There is {1}\n{0}\npresent on this\nbomb.", mod, "AEIOUaeiou".Contains(mod[0]) ? "an" : "a"));
        }
        if (bomb.GetTwoFactorCounts() > 0)
        {
            truths.Add("There is a\nTwo Factor\npresent on this\nbomb.");
            lies.Add("There is a\nTwo Factor\nabsent from this\nbomb.");
        }
        else
        {
            truths.Add("There is a\nTwo Factor\nabsent from this\nbomb.");
            lies.Add("There is a\nTwo Factor\npresent on this\nbomb.");
        }
        if (bomb.GetColoredIndicators().Count() > 0)
        {
            truths.Add("There is a\ncolored\nindicator\npresent on this\nbomb.");
            lies.Add("There is a\ncolored\nindicator\nabsent from this\nbomb.");
        }
        else
        {
            truths.Add("There is a\ncolored\nindicator\nabsent from this\nbomb.");
            lies.Add("There is a\ncolored\nindicator\npresent on this\nbomb.");
        }
        string[] edgework = new string[] { "batteries", "battery holders", "indicators", "ports", "port plates" };
        int[] nums = new int[] { bomb.GetBatteryCount(), bomb.GetBatteryHolderCount(), bomb.GetIndicators().Count(), bomb.GetPortCount(), bomb.GetPortPlateCount() };
        bool[] presence = new bool[] { bomb.GetBatteryCount() > 0 ? true : false, bomb.GetBatteryHolderCount() > 0 ? true : false, bomb.GetIndicators().Count() > 0 ? true : false, bomb.GetPortCount() > 0 ? true : false, bomb.GetPortPlateCount() > 0 ? true : false };
        int num1 = UnityEngine.Random.Range(0, edgework.Length);
        int num2 = UnityEngine.Random.Range(0, edgework.Length);
        while (num1 == num2)
        {
            num1 = UnityEngine.Random.Range(0, edgework.Length);
            num2 = UnityEngine.Random.Range(0, edgework.Length);
        }
        int ans = nums[num1] + nums[num2];
        truths.Add(string.Format("The number of\n{0}\nplus the number\nof\n{1}\nis less than {2}.", edgework[num1], edgework[num2], UnityEngine.Random.Range(ans + 1, ans + 6)));
        truths.Add(string.Format("The number of\n{0}\nplus the number\nof\n{1}\nis more than {2}.", edgework[num1], edgework[num2], UnityEngine.Random.Range(ans - 5, ans)));
        lies.Add(string.Format("The number of\n{0}\nplus the number\nof\n{1}\nis more than {2}.", edgework[num1], edgework[num2], UnityEngine.Random.Range(ans + 1, ans + 6)));
        lies.Add(string.Format("The number of\n{0}\nplus the number\nof\n{1}\nis less than {2}.", edgework[num1], edgework[num2], UnityEngine.Random.Range(ans - 5, ans)));
        num1 = UnityEngine.Random.Range(0, edgework.Length);
        num2 = UnityEngine.Random.Range(0, edgework.Length);
        while (num1 == num2)
        {
            num1 = UnityEngine.Random.Range(0, edgework.Length);
            num2 = UnityEngine.Random.Range(0, edgework.Length);
        }
        ans = nums[num1] - nums[num2];
        truths.Add(string.Format("The number of\n{0}\nminus the number\nof\n{1}\nis less than {2}.", edgework[num1], edgework[num2], UnityEngine.Random.Range(ans + 1, ans + 6)));
        truths.Add(string.Format("The number of\n{0}\nminus the number\nof\n{1}\nis more than {2}.", edgework[num1], edgework[num2], UnityEngine.Random.Range(ans - 5, ans)));
        lies.Add(string.Format("The number of\n{0}\nminus the number\nof\n{1}\nis more than {2}.", edgework[num1], edgework[num2], UnityEngine.Random.Range(ans + 1, ans + 6)));
        lies.Add(string.Format("The number of\n{0}\nminus the number\nof\n{1}\nis less than {2}.", edgework[num1], edgework[num2], UnityEngine.Random.Range(ans - 5, ans)));
        int edge = UnityEngine.Random.Range(0, presence.Length);
        if (presence[edge])
        {
            truths.Add(string.Format("There is some\n{0}\non this bomb.", edgework[edge]));
            lies.Add(string.Format("There is no\n{0}\non this bomb.", edgework[edge]));
        }
        else
        {
            truths.Add(string.Format("There is no\n{0}\non this bomb.", edgework[edge]));
            lies.Add(string.Format("There is some\n{0}\non this bomb.", edgework[edge]));
        }
        char[] alpha = new char[] { 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };
        char[] serChars = new char[2];
        serChars[0] = bomb.GetSerialNumber().PickRandom();
        serChars[1] = bomb.GetSerialNumber().PickRandom();
        if (serChars[0] == serChars[1])
        {
            serChars[0] = bomb.GetSerialNumber().PickRandom();
            serChars[1] = bomb.GetSerialNumber().PickRandom();
        }
        truths.Add(string.Format("The bomb's\nserial number\ncontains {2}\n{0} and {1}.", serChars[0], serChars[1], "AEIOU".Contains(serChars[0]) ? "an" : "a"));
        truths.Add(string.Format("The bomb's\nserial number\ncontains {2}\n{0} or {1}.", serChars[0], serChars[1], "AEIOU".Contains(serChars[0]) ? "an" : "a"));
        char not = alpha.PickRandom();
        while (not == serChars[0] || bomb.GetSerialNumber().Contains(not))
            not = alpha.PickRandom();
        truths.Add(string.Format("The bomb's\nserial number\ncontains {2}\n{0} or {1}.", not, serChars[1], "AEIOU".Contains(not) ? "an" : "a"));
        not = alpha.PickRandom();
        while (not == serChars[1] || bomb.GetSerialNumber().Contains(not))
            not = alpha.PickRandom();
        truths.Add(string.Format("The bomb's\nserial number\ncontains {2}\n{0} or {1}.", serChars[0], not, "AEIOU".Contains(serChars[0]) ? "an" : "a"));
        char[] nots = new char[2];
        nots[0] = alpha.PickRandom();
        nots[1] = alpha.PickRandom();
        while (bomb.GetSerialNumber().Contains(nots[0]) || bomb.GetSerialNumber().Contains(nots[1]) || nots[0] == nots[1])
        {
            nots[0] = alpha.PickRandom();
            nots[1] = alpha.PickRandom();
        }
        lies.Add(string.Format("The bomb's\nserial number\ncontains {2}\n{0} and {1}.", nots[0], nots[1], "AEIOU".Contains(nots[0]) ? "an" : "a"));
        lies.Add(string.Format("The bomb's\nserial number\ncontains {2}\n{0} or {1}.", nots[0], nots[1], "AEIOU".Contains(nots[0]) ? "an" : "a"));
        not = alpha.PickRandom();
        char has = bomb.GetSerialNumber().PickRandom();
        lies.Add(string.Format("The bomb's\nserial number\ncontains {2}\n{0} and {1}.", not, has, "AEIOU".Contains(not) ? "an" : "a"));
        not = alpha.PickRandom();
        has = bomb.GetSerialNumber().PickRandom();
        lies.Add(string.Format("The bomb's\nserial number\ncontains {2}\n{0} and {1}.", has, not, "AEIOU".Contains(has) ? "an" : "a"));
    }

    private List<Module> Processjson(string fetched)
    {
        ktaneData Deserialized = JsonConvert.DeserializeObject<ktaneData>(fetched);
        List<Module> Modules = new List<Module>();
        foreach (var item in Deserialized.KtaneModules)
        {
            if ((string)item["Type"] != "Widget") Modules.Add(new Module(item));
        }
        return Modules;
    }

    private IEnumerator FetchModules()
    {
        fetch = new WWW("https://ktane.timwi.de/json/raw");
        yield return fetch;
        if (fetch.error == null)
        {
            modules = Processjson(fetch.text);
        }
    }

    private IEnumerator ShowText()
    {
        screens[0].GetComponent<Renderer>().material = allcolors[colors[0]];
        screens[1].GetComponent<Renderer>().material = allcolors[colors[1]];
        if (cbactive)
        {
            cbtexts[0].text = allcolors[colors[0]].name[0].ToString().ToUpper();
            cbtexts[1].text = allcolors[colors[1]].name[0].ToString().ToUpper();
        }
        screens[0].SetActive(true);
        screens[1].SetActive(true);
        int large = displayed[0].Length > displayed[1].Length ? displayed[0].Length : displayed[1].Length;
        for (int i = 0; i < large; i++)
        {
            if (i < displayed[0].Length)
                texts[0].text += displayed[0][i];
            if (i < displayed[1].Length)
                texts[1].text += displayed[1][i];
            yield return new WaitForSecondsRealtime(0.05f);
        }
        animating = false;
    }

    private IEnumerator RemoveText()
    {
        animating = true;
        int large = displayed[0].Length > displayed[1].Length ? displayed[0].Length : displayed[1].Length;
        for (int i = 0; i < large; i++)
        {
            if (i < displayed[0].Length)
                texts[0].text = texts[0].text.Remove(displayed[0].Length - 1 - i, 1);
            if (i < displayed[1].Length)
                texts[1].text = texts[1].text.Remove(displayed[1].Length - 1 - i, 1);
            yield return new WaitForSecondsRealtime(0.05f);
        }
        if (cbactive)
        {
            cbtexts[0].text = "";
            cbtexts[1].text = "";
        }
        screens[0].SetActive(false);
        screens[1].SetActive(false);
        if (stage == stageCount)
        {
            Debug.LogFormat("[Double Screen #{0}] Module disarmed", moduleId);
            moduleSolved = true;
            audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, transform);
            GetComponent<KMBombModule>().HandlePass();
            yield break;
        }
        yield return new WaitForSecondsRealtime(0.2f);
        GenerateScreens();
        StartCoroutine(ShowText());
    }

    //twitch plays
    #pragma warning disable 414
    private readonly string TwitchHelpMessage = @"!{0} press <top/t/bottom/b> [Presses the top or bottom screen] | !{0} colorblind [Toggles colorblind mode]";
    #pragma warning restore 414
    IEnumerator ProcessTwitchCommand(string command)
    {
        if (Regex.IsMatch(command, @"^\s*colorblind\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            yield return null;
            if (cbactive)
            {
                cbactive = false;
                cbtexts[0].text = "";
                cbtexts[1].text = "";
            }
            else
            {
                cbactive = true;
                cbtexts[0].text = allcolors[colors[0]].name[0].ToString().ToUpper();
                cbtexts[1].text = allcolors[colors[1]].name[0].ToString().ToUpper();
            }
            yield break;
        }
        string[] parameters = command.Split(' ');
        if (Regex.IsMatch(parameters[0], @"^\s*press\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            yield return null;
            if (parameters.Length > 2)
            {
                yield return "sendtochaterror Too many parameters!";
            }
            else if (parameters.Length == 2)
            {
                if (animating && (parameters[1].EqualsIgnoreCase("t") || parameters[1].EqualsIgnoreCase("top") || parameters[1].EqualsIgnoreCase("b") || parameters[1].EqualsIgnoreCase("bottom")))
                    yield return "sendtochaterror Cannot press a screen while the module is animating!";
                else if (parameters[1].EqualsIgnoreCase("t") || parameters[1].EqualsIgnoreCase("top"))
                {
                    if (firstScreenCorrect && stageCount == 3 ? stage == 2 : stage == 1)
                        yield return "solve";
                    buttons[0].OnInteract();
                }
                else if (parameters[1].EqualsIgnoreCase("b") || parameters[1].EqualsIgnoreCase("bottom"))
                {
                    if (!firstScreenCorrect && stageCount == 3 ? stage == 2 : stage == 1)
                        yield return "solve";
                    buttons[1].OnInteract();
                }
                else
                    yield return "sendtochaterror!f The specified screen to press '" + parameters[1] + "' is invalid!";
            }
            else if (parameters.Length == 1)
            {
                yield return "sendtochaterror Please specify the screen you wish to press!";
            }
            yield break;
        }
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        int start = stage;
        for (int i = start; i < stageCount; i++)
        {
            while (animating) { yield return true; }
            if (firstScreenCorrect)
                buttons[0].OnInteract();
            else
                buttons[1 ].OnInteract();
        }
        while (animating) { yield return true; }
    }
}
