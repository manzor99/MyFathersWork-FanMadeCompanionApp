namespace MyFathersWorkWebApp;

public static partial class ATimeOfWar
{
    public static void UniGen3(GlobalData globalData)
    {
        globalData.SaveToUndo();
        globalData.ATimeOfWarVars.PgGen3 = 1;

        globalData.ActivePopup = new GameplayPopup(
            globalData,
            PopUpTitle.Storybook,
            PopUpIcon.Caretaker,
            PopUpButton.ReturnToScenario,
            _ => { },
            "ATOW_UniGen3_Content",
            text => text.FormatWithCondition(0, () => globalData.ATimeOfWarVars.Round is 10 or 11 or 12));
    }

    public static void AtowChurch(GlobalData globalData)
    {
        globalData.SaveToUndo();

        globalData.ActivePopup = new GameplayPopup(
            globalData,
            PopUpTitle.Storybook,
            PopUpIcon.LoseCreepy,
            PopUpButton.ReturnToScenario,
            _ => { },
            "ATOW_Church_Content");
    }

    public static void Scoring(GlobalData globalData)
    {
        globalData.SaveToUndo();
        globalData.ActiveHub             = null;
        globalData.ATimeOfWarVars.HubId  = TimeOfWarHubId.None;
        globalData.ActiveWindow          = new GameplayWindow(globalData, "ATOW_Scoring_Title");

        globalData.ActiveWindow.AddText(
            "ATOW_Scoring_Text",
            false,
            text => text.FormatWithCondition(0, () => globalData.ATimeOfWarVars.TmMasterwork == "yes"));

        globalData.ActiveWindow.AddElement("ATOW_Scoring_Continue", StartScoreEntry, true);
    }

    private static void StartScoreEntry(GlobalData globalData)
    {
        ATimeOfWarVars vars = globalData.ATimeOfWarVars;
        vars.ScoreEntryIndex = 0;
        for (int i = 0; i < vars.PlayerScores.Length; i++)
        {
            vars.PlayerScores[i] = 0;
        }
        PlayerScoreName(globalData);
    }

    private static void PlayerScoreName(GlobalData globalData)
    {
        globalData.SaveToUndo();
        ATimeOfWarVars vars = globalData.ATimeOfWarVars;
        string playerName = vars.GetDisplayPlayerName(globalData, vars.ScoreEntryIndex);

        globalData.ActiveInputPopup = new GameplayInputPopup(
            globalData,
            PopUpIcon.ScoreTrackMarker,
            "ATOW_PlayerScoreName_Content",
            "ATOW_PlayerScoreName_Placeholder",
            PopUpButton.Submit,
            input =>
            {
                if (!int.TryParse(input.Trim(), out int score)) score = 0;
                vars.PlayerScores[vars.ScoreEntryIndex] = score;
                vars.ScoreEntryIndex++;

                if (vars.ScoreEntryIndex < globalData.PlayersNum)
                {
                    PlayerScoreName(globalData);
                }
                else
                {
                    EvaluateWinner(globalData);
                }
            },
            text => text.FormatWithReplacement(0, playerName));
    }

    private static void EvaluateWinner(GlobalData globalData)
    {
        ATimeOfWarVars vars = globalData.ATimeOfWarVars;
        int maxScore = int.MinValue;
        for (int i = 0; i < globalData.PlayersNum; i++)
        {
            if (vars.PlayerScores[i] > maxScore) maxScore = vars.PlayerScores[i];
        }

        vars.TiedPlayers.Clear();
        for (int i = 0; i < globalData.PlayersNum; i++)
        {
            if (vars.PlayerScores[i] == maxScore)
            {
                vars.TiedPlayers.Add((PlayerId)i);
            }
        }

        if (vars.TiedPlayers.Count == 1)
        {
            vars.WinnerName = vars.GetDisplayPlayerName(globalData, (int)vars.TiedPlayers[0]);
            WinnerHUB(globalData);
        }
        else
        {
            WinnerHUBproblem(globalData);
        }
    }

    private static void WinnerHUBproblem(GlobalData globalData)
    {
        globalData.SaveToUndo();
        ATimeOfWarVars vars = globalData.ATimeOfWarVars;

        globalData.ActiveWindow = new GameplayWindow(globalData, "ATOW_WinnerHUBproblem_Title");
        globalData.ActiveWindow.AddText("ATOW_WinnerHUBproblem_Text");

        foreach (PlayerId pid in vars.TiedPlayers)
        {
            PlayerId captured = pid;
            string pName = vars.GetDisplayPlayerName(globalData, (int)captured);
            globalData.ActiveWindow.AddElement(
                string.Empty,
                gd =>
                {
                    gd.ATimeOfWarVars.WinnerName = pName;
                    WinnerHUB(gd);
                },
                true,
                _ => pName);
        }

        globalData.ActiveWindow.AddElement("ATOW_WinnerHUBproblem_MultipleOrNone", StartMoneyTieBreaker, true);
    }

    private static void StartMoneyTieBreaker(GlobalData globalData)
    {
        ATimeOfWarVars vars = globalData.ATimeOfWarVars;
        vars.TieBreakerIndex = 0;
        for (int i = 0; i < vars.TieBreakerMoney.Length; i++)
        {
            vars.TieBreakerMoney[i] = 0;
        }
        PromptNextTieBreakerMoney(globalData);
    }

    private static void PromptNextTieBreakerMoney(GlobalData globalData)
    {
        globalData.SaveToUndo();
        ATimeOfWarVars vars = globalData.ATimeOfWarVars;
        PlayerId currentPid = vars.TiedPlayers[vars.TieBreakerIndex];
        string playerName = vars.GetDisplayPlayerName(globalData, (int)currentPid);

        globalData.ActiveInputPopup = new GameplayInputPopup(
            globalData,
            PopUpIcon.Money_Icon,
            "ATOW_TieBreakerMoney_Content",
            "ATOW_PlayerScoreName_Placeholder",
            PopUpButton.Submit,
            input =>
            {
                if (!int.TryParse(input.Trim(), out int val)) val = 0;
                vars.TieBreakerMoney[(int)currentPid] = val;
                vars.TieBreakerIndex++;

                if (vars.TieBreakerIndex < vars.TiedPlayers.Count)
                {
                    PromptNextTieBreakerMoney(globalData);
                }
                else
                {
                    ResolveMoneyTieBreaker(globalData);
                }
            },
            text => text.FormatWithReplacement(0, playerName));
    }

    private static void ResolveMoneyTieBreaker(GlobalData globalData)
    {
        ATimeOfWarVars vars = globalData.ATimeOfWarVars;
        int maxMoney = int.MinValue;
        foreach (PlayerId pid in vars.TiedPlayers)
        {
            if (vars.TieBreakerMoney[(int)pid] > maxMoney)
            {
                maxMoney = vars.TieBreakerMoney[(int)pid];
            }
        }

        List<PlayerId> stillTied = vars.TiedPlayers
            .Where(pid => vars.TieBreakerMoney[(int)pid] == maxMoney)
            .ToList();

        PlayerId chosen = stillTied[Random.Shared.Next(stillTied.Count)];
        vars.WinnerName = vars.GetDisplayPlayerName(globalData, (int)chosen);
        WinnerHUB(globalData);
    }

    private static void WinnerHUB(GlobalData globalData)
    {
        globalData.SaveToUndo();
        ATimeOfWarVars vars = globalData.ATimeOfWarVars;

        globalData.ActiveWindow = new GameplayWindow(globalData, "ATOW_WinnerHUB_Title");
        globalData.ActiveWindow.AddText(
            "ATOW_WinnerHUB_Text",
            false,
            text => text.FormatWithReplacement(0, vars.WinnerName));
        globalData.ActiveWindow.AddElement(GlobalTags.Gameplay_ClickToContinue, ShowEnding, true);
    }

    public static void ShowEnding(GlobalData globalData)
    {
        globalData.SaveToUndo();
        ATimeOfWarVars vars = globalData.ATimeOfWarVars;
        if (string.IsNullOrEmpty(vars.WinnerName))
        {
            vars.WinnerName = vars.GetDisplayPlayerName(globalData, 0);
        }

        switch (vars.Ending)
        {
            case "ATOW-End1":
                EndAtow1(globalData);
                break;
            case "ATOW-End2":
                EndAtow2(globalData);
                break;
            case "ATOW-End3":
                EndAtow3(globalData);
                break;
            case "ATOW-End4":
                EndAtow4(globalData);
                break;
            case "ATOW-End5":
                EndAtow5(globalData);
                break;
            case "ATOW-End6":
                EndAtow6(globalData);
                break;
            case "ATOW-End7":
                EndAtow7(globalData);
                break;
            case "ATOW-End8":
                EndAtow8(globalData);
                break;
            default:
                EndAtow1(globalData);
                break;
        }
    }

    public static void EndAtow1(GlobalData globalData)
    {
        globalData.SaveToUndo();
        ATimeOfWarVars vars = globalData.ATimeOfWarVars;
        if (string.IsNullOrEmpty(vars.WinnerName)) vars.WinnerName = vars.GetDisplayPlayerName(globalData, 0);

        string[] giantThings =
        {
            "Giant Spider Chariot",
            "Giant Love Potion",
            "Giant Teleportation Device"
        };
        string giantThing = giantThings[Random.Shared.Next(giantThings.Length)];

        globalData.ActiveWindow = new GameplayWindow(globalData, "ATOW_End1_Title");
        globalData.ActiveWindow.AddText(
            "ATOW_End1_Text",
            false,
            text => text
                .FormatWithReplacement(0, vars.WinnerName)
                .FormatWithReplacement(2, giantThing)
                .FormatWithCondition(1, () => vars.Release >= 1));
        globalData.ActiveWindow.AddElement(GlobalTags.Gameplay_ClickToContinue, FinalCredits, true);
    }

    public static void EndAtow2(GlobalData globalData)
    {
        globalData.SaveToUndo();
        globalData.ActiveWindow = new GameplayWindow(globalData, "ATOW_End2_Title");
        globalData.ActiveWindow.AddText("ATOW_End2_Text");
        globalData.ActiveWindow.AddElement(GlobalTags.Gameplay_ClickToContinue, FinalCredits, true);
    }

    public static void EndAtow3(GlobalData globalData)
    {
        globalData.SaveToUndo();
        ATimeOfWarVars vars = globalData.ATimeOfWarVars;
        if (string.IsNullOrEmpty(vars.WinnerName)) vars.WinnerName = vars.GetDisplayPlayerName(globalData, 0);

        globalData.ActiveWindow = new GameplayWindow(globalData, "ATOW_End3_Title");
        globalData.ActiveWindow.AddText(
            "ATOW_End3_Text",
            false,
            text => text
                .FormatWithReplacement(0, globalData.TownName)
                .FormatWithReplacement(1, vars.WinnerName));
        globalData.ActiveWindow.AddElement(GlobalTags.Gameplay_ClickToContinue, FinalCredits, true);
    }

    public static void EndAtow4(GlobalData globalData)
    {
        globalData.SaveToUndo();
        ATimeOfWarVars vars = globalData.ATimeOfWarVars;

        globalData.ActiveWindow = new GameplayWindow(globalData, "ATOW_End4_Title");
        globalData.ActiveWindow.AddText(
            "ATOW_End4_Text",
            false,
            text => text.FormatWithReplacement(0, vars.WarWinner));
        globalData.ActiveWindow.AddElement(GlobalTags.Gameplay_ClickToContinue, FinalCredits, true);
    }

    public static void EndAtow5(GlobalData globalData)
    {
        globalData.SaveToUndo();
        ATimeOfWarVars vars = globalData.ATimeOfWarVars;
        if (string.IsNullOrEmpty(vars.WinnerName)) vars.WinnerName = vars.GetDisplayPlayerName(globalData, 0);

        int barracksVariant = vars.Barracks == "yes"
            ? (vars.EndChange == "yes" ? 0 : 1)
            : (vars.EndChange == "yes" ? 2 : 3);
        int persuadeVariant = vars.EndChange == "yes"
            ? (vars.WarDestroy > 0 ? 0 : 1)
            : 2;

        globalData.ActiveWindow = new GameplayWindow(globalData, "ATOW_End5_Title");
        globalData.ActiveWindow.AddText(
            "ATOW_End5_Text",
            false,
            text => text
                .FormatWithReplacement(2, globalData.TownName)
                .FormatWithReplacement(4, vars.WinnerName)
                .FormatWithIndex(0, barracksVariant)
                .FormatWithCondition(1, () => vars.EndChange == "yes")
                .FormatWithIndex(2, persuadeVariant)
                .FormatWithCondition(3, () => vars.Benevolent == "good"));
        globalData.ActiveWindow.AddElement(GlobalTags.Gameplay_ClickToContinue, FinalCredits, true);
    }

    public static void EndAtow6(GlobalData globalData)
    {
        globalData.SaveToUndo();
        ATimeOfWarVars vars = globalData.ATimeOfWarVars;
        if (string.IsNullOrEmpty(vars.WinnerName)) vars.WinnerName = vars.GetDisplayPlayerName(globalData, 0);

        globalData.ActiveWindow = new GameplayWindow(globalData, "ATOW_End6_Title");
        globalData.ActiveWindow.AddText(
            "ATOW_End6_Text",
            false,
            text => text.FormatWithReplacement(0, vars.WinnerName));
        globalData.ActiveWindow.AddElement(GlobalTags.Gameplay_ClickToContinue, FinalCredits, true);
    }

    public static void EndAtow7(GlobalData globalData)
    {
        globalData.SaveToUndo();
        ATimeOfWarVars vars = globalData.ATimeOfWarVars;
        if (string.IsNullOrEmpty(vars.WinnerName)) vars.WinnerName = vars.GetDisplayPlayerName(globalData, 0);

        string[] inventions =
        {
            "Tiger Launcher",
            "Psychic Mindbomb",
            "Irresistible Aphrodisiac",
            "Helmet-Mounted Flamethrower",
            "Steam Grenade",
            "Remote Controlled Plague Rat Swarm",
            "Flaming Pitchfork",
            "Mechanized Human Armor Suit",
            "Dream Stealer"
        };
        string invention = inventions[Random.Shared.Next(inventions.Length)];
        string meatName = string.IsNullOrEmpty(vars.NewMeat) ? "Soylent Ration" : vars.NewMeat;

        globalData.ActiveWindow = new GameplayWindow(globalData, "ATOW_End7_Title");
        globalData.ActiveWindow.AddText(
            "ATOW_End7_Text",
            false,
            text => text
                .FormatWithReplacement(0, globalData.TownName)
                .FormatWithReplacement(1, vars.WinnerName)
                .FormatWithReplacement(2, meatName)
                .FormatWithReplacement(3, invention));
        globalData.ActiveWindow.AddElement(GlobalTags.Gameplay_ClickToContinue, FinalCredits, true);
    }

    public static void EndAtow8(GlobalData globalData)
    {
        globalData.SaveToUndo();
        ATimeOfWarVars vars = globalData.ATimeOfWarVars;
        if (string.IsNullOrEmpty(vars.WinnerName)) vars.WinnerName = vars.GetDisplayPlayerName(globalData, 0);

        int peaceVariant = vars.PeaceCount == 1
            ? (vars.Peac == 2 ? 0 : 1)
            : 2;

        globalData.ActiveWindow = new GameplayWindow(globalData, "ATOW_End8_Title");
        globalData.ActiveWindow.AddText(
            "ATOW_End8_Text",
            false,
            text => text
                .FormatWithReplacement(0, vars.WinnerName)
                .FormatWithIndex(1, peaceVariant));
        globalData.ActiveWindow.AddElement(GlobalTags.Gameplay_ClickToContinue, FinalCredits, true);
    }

    private static void FinalCredits(GlobalData globalData)
    {
        globalData.SaveToUndo();
        globalData.ActiveWindow = new GameplayWindow(globalData, "ATOW_FinalCredits_Title");
        globalData.ActiveWindow.AddText("ATOW_FinalCredits_Text");
        globalData.ActiveWindow.AddElement("ATOW_ReturnToMainMenu", ReturnToMainMenu, true);
    }

    private static void ReturnToMainMenu(GlobalData globalData)
    {
        globalData.ResetUndo();
        globalData.ActiveHub        = null;
        globalData.ActiveWindow     = null;
        globalData.ActivePopup      = null;
        globalData.ActiveInputPopup = null;
        globalData.ATimeOfWarVars.Reset(globalData);
    }
}
