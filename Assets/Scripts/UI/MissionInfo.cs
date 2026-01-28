using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Antlr4.Runtime;
using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TS;

public class MissionInfo : MonoBehaviour
{
    public static MissionInfo instance;
    public void Awake()
    {
        if(instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    [HideInInspector] public string highScoreName;

    [Header("Selected Mission Directory")]
    [TextArea(1, 2)] public string MissionPath;
    public string missionName;

    [Header("Selected Mission Info")]
    public int time;
    public string levelName;
    [TextArea(2, 10)] public string description;
    [TextArea(2, 10)] public string startHelpText;
    public int level;
    public string artist;
    public int goldTime;
    public int ultimateTime;

    [Header("Load Mission")]
    public List<Mission> missionsBeginner = new List<Mission>();
    public List<Mission> missionsIntermediate = new List<Mission>();
    public List<Mission> missionsAdvanced = new List<Mission>();
    public List<Mission> missionsCustom = new List<Mission>();

    List<TSObject> MissionObjects;

    public void Start()
    {
        highScoreName = PlayerPrefs.GetString("HighScoreName", "");

        LoadMissions(Type.beginner);
        LoadMissions(Type.intermediate);
        LoadMissions(Type.advanced);
        LoadMissions(Type.custom);
    }

    public void LoadMissions(Type difficulty)
    {
        string basePath = Path.Combine(
            Application.streamingAssetsPath,
            "marble/data/missions",
            difficulty.ToString()
        );

        // 🔒 SAFETY 1: folder does not exist
        if (!Directory.Exists(basePath))
            return;

        string[] misFiles = Directory.GetFiles(basePath, "*.mis");

        // 🔒 SAFETY 2: folder exists but no missions
        if (misFiles == null || misFiles.Length == 0)
            return;

        foreach (string misPath in misFiles)
        {
            string levelName = Path.GetFileNameWithoutExtension(misPath);

            // Try jpg first, then png
            string jpgPath = Path.Combine(basePath, levelName + ".jpg");
            string pngPath = Path.Combine(basePath, levelName + ".png");

            string imagePath = null;

            if (File.Exists(jpgPath))
                imagePath = jpgPath;
            else if (File.Exists(pngPath))
                imagePath = pngPath;
            else
                continue;

            byte[] imageData = File.ReadAllBytes(imagePath);

            Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.LoadImage(imageData);

            Sprite sprite = Sprite.Create(
                tex,
                new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.5f),
                100f
            );

            String directory = misPath;
            int idx = misPath.IndexOf("marble", StringComparison.OrdinalIgnoreCase);
            if (idx >= 0)
                directory = directory.Substring(idx);

            Mission newMission = new Mission
            {
                levelImage = sprite,
                directory = directory,
                levelNumber = -1,
            };

            var lexer = new TSLexer(
                new AntlrFileStream(Path.Combine(Application.streamingAssetsPath, misPath))
            );
            var parser = new TSParser(new CommonTokenStream(lexer));
            var file = parser.start();

            if (parser.NumberOfSyntaxErrors > 0)
            {
                Debug.LogError("Could not parse mission file");
                continue;
            }

            MissionObjects = new List<TSObject>();

            foreach (var decl in file.decl())
            {
                var objectDecl = decl.stmt()?.expression_stmt()?.stmt_expr()?.object_decl();
                if (objectDecl == null)
                    continue;

                MissionObjects.Add(MissionImporter.ProcessObject(objectDecl));
            }

            if (MissionObjects.Count == 0)
                return;

            var mission = MissionObjects[0];
            foreach (var obj in mission.RecursiveChildren())
            {
                //Mission info
                if (obj.ClassName == "ScriptObject" && obj.Name == "MissionInfo")
                {
                    int _time = -1;
                    if (int.TryParse(obj.GetField("time"), out _time))
                        if (_time != 0)
                            newMission.time = _time;
                        else
                            newMission.time = -1;
                    else
                        newMission.time = -1;

                    newMission.missionName = levelName;
                    newMission.levelName = (obj.GetField("name"));
                    newMission.description = (obj.GetField("desc"));
                    newMission.startHelpText = (obj.GetField("startHelpText"));

                    int _level = 0;
                    if (int.TryParse(obj.GetField("level"), out _level))
                        newMission.levelNumber = _level;
                    else
                        newMission.levelNumber = 0;

                    newMission.artist = (obj.GetField("artist"));

                    int _goldTime = -1;
                    if (int.TryParse(obj.GetField("goldTime"), out _goldTime))
                        newMission.goldTime = _goldTime;
                    else
                        newMission.goldTime = -1;

                    int _ultimateTime = -1;
                    if (int.TryParse(obj.GetField("ultimateTime"), out _ultimateTime))
                        newMission.ultimateTime = _ultimateTime;
                    else
                        newMission.ultimateTime = -1;

                    break;
                }
            }

            newMission.levelImage.name = levelName;

            if (difficulty == Type.beginner)
                missionsBeginner.Add(newMission);
            else if (difficulty == Type.intermediate)
                missionsIntermediate.Add(newMission);
            else if (difficulty == Type.advanced)
                missionsAdvanced.Add(newMission);
            else if (difficulty == Type.custom)
                missionsCustom.Add(newMission);
        }

        if (difficulty == Type.beginner)
            missionsBeginner = SortMissionsByLevelNumber(missionsBeginner);
        else if (difficulty == Type.intermediate)
            missionsIntermediate = SortMissionsByLevelNumber(missionsIntermediate);
        else if (difficulty == Type.advanced)
            missionsAdvanced = SortMissionsByLevelNumber(missionsAdvanced);
        else if (difficulty == Type.custom)
            missionsCustom = SortMissionsByLevelNumber(missionsCustom);
    }

    public List<Mission> SortMissionsByLevelNumber(List<Mission> missions)
    {
        if (missions.Count == 0)
            return missions;

        int count = missions.Count;

        // New list with fixed size
        Mission[] sorted = new Mission[count];
        List<Mission> unsorted = new List<Mission>();

        foreach (var mission in missions)
        {
            // Skip invalid level numbers
            if (mission == null || mission.levelNumber < 1)
            {
                unsorted.Add(mission);
                continue;
            }

            int index = mission.levelNumber - 1;

            // Out of bounds or collision → treat as unsorted
            if (index < 0 || index >= count || sorted[index] != null)
            {
                unsorted.Add(mission);
                continue;
            }

            sorted[index] = mission;
        }

        // Fill empty slots with unsorted missions (original order preserved)
        int unsortedIndex = 0;
        for (int i = 0; i < sorted.Length; i++)
        {
            if (sorted[i] == null && unsortedIndex < unsorted.Count)
            {
                sorted[i] = unsorted[unsortedIndex];
                unsortedIndex++;
            }
        }

        // Replace original list
        return sorted.ToList();
    }
}
