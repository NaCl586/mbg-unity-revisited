using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Antlr4.Runtime;
using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System.Globalization;

namespace TS
{
    public class MissionImporter : MonoBehaviour
    {
        public List<TSObject> MissionObjects;

        [Header("Prefabs")]
        public GameObject interiorPrefab;
        public GameObject movingPlatformPrefab;
        public GameObject triggerGoToTarget;
        public GameObject inBoundsTrigger;
        public GameObject helpTriggerInstance;
        [Space]
        public GameObject finishSignPrefab;
        public GameObject signPlainPrefab;
        public GameObject signPlainUpPrefab;
        public GameObject signPlainDownPrefab;
        public GameObject signPlainLeftPrefab;
        public GameObject signPlainRightPrefab;
        public GameObject signCautionPrefab;
        public GameObject signCautionCautionPrefab;
        public GameObject signCautionDangerPrefab;
        public GameObject gemPrefab;
        [Space]
        public GameObject antiGravityPrefab;
        public GameObject superJumpPrefab;
        public GameObject superSpeedPrefab;
        public GameObject superBouncePrefab;
        public GameObject shockAbsorberPrefab;
        public GameObject gyrocopterPrefab;
        public GameObject timeTravelPrefab;
        [Space]
        public GameObject trapdoorPrefab;
        public GameObject roundBumperPrefab;
        public GameObject triangleBumperPrefab;
        public GameObject ductFanPrefab;
        public GameObject tornadoPrefab;
        public GameObject oilSlickPrefab;
        public GameObject landMinePrefab;

        [Header("References")]
        public GameObject globalMarble;
        public GameObject startPad;
        public GameObject finishPad;
        public Light directionalLight;

        void Start()
        {
            ImportMission();
        }

        void ImportMission()
        {
            if (string.IsNullOrEmpty(MissionInfo.instance.MissionPath))
                return;

            var lexer = new TSLexer(
                new AntlrFileStream(Path.Combine(Application.streamingAssetsPath, MissionInfo.instance.MissionPath))
            );
            var parser = new TSParser(new CommonTokenStream(lexer));
            var file = parser.start();

            if (parser.NumberOfSyntaxErrors > 0)
            {
                Debug.LogError("Could not parse mission file");
                return;
            }

            MissionObjects = new List<TSObject>();

            foreach (var decl in file.decl())
            {
                var objectDecl = decl.stmt()?.expression_stmt()?.stmt_expr()?.object_decl();
                if (objectDecl == null)
                    continue;

                MissionObjects.Add(ProcessObject(objectDecl));
            }

            if (MissionObjects.Count == 0)
                return;

            var mission = MissionObjects[0];

            foreach (var obj in mission.RecursiveChildren())
            {
                if (obj.ClassName == "Sun")
                {
                    var direction = ConvertDirection(ParseVectorString(obj.GetField("direction")));
                    var color = ConvertColor(ParseVectorString(obj.GetField("color")));
                    var ambient = ConvertAmbient(ParseVectorString(obj.GetField("ambient")));

                    directionalLight.transform.localRotation = direction;
                    directionalLight.color = color;
                    RenderSettings.ambientLight = ambient;
                    directionalLight.intensity = ConvertIntensity(color, 0.25f);
                }

                //Gem
                else if (obj.ClassName == "Item")
                {
                    string objectName = obj.GetField("dataBlock");

                    if (objectName.StartsWith("GemItem"))
                    {
                        var gobj = Instantiate(gemPrefab, transform, false);
                        gobj.name = "Gem";

                        var position = ConvertPoint(ParseVectorString(obj.GetField("position")));
                        var rotation = ConvertRotation(ParseVectorString(obj.GetField("rotation")));
                        var scale = ConvertScale(ParseVectorString(obj.GetField("scale")));

                        gobj.transform.localPosition = position;
                        gobj.transform.localRotation = rotation;
                        gobj.transform.localScale = scale;
                    }

                    else if (objectName == "AntiGravityItem")
                    {
                        var gobj = Instantiate(antiGravityPrefab, transform, false);
                        gobj.name = "AntiGravityItem";

                        var position = ConvertPoint(ParseVectorString(obj.GetField("position")));
                        var rotation = ConvertRotation(ParseVectorString(obj.GetField("rotation")));
                        var scale = ConvertScale(ParseVectorString(obj.GetField("scale")));

                        var localScale = gobj.transform.localScale;

                        gobj.transform.localPosition = position;
                        gobj.transform.localRotation = rotation;
                        gobj.transform.localScale = new Vector3(scale.x * localScale.x, scale.y * localScale.y, scale.z * localScale.z);
                    }

                    else if (objectName == "SuperJumpItem")
                    {
                        var gobj = Instantiate(superJumpPrefab, transform, false);
                        gobj.name = "SuperJumpItem";
                        gobj.GetComponent<Powerups>().rotateMesh = false;

                        var position = ConvertPoint(ParseVectorString(obj.GetField("position")));
                        var rotation = ConvertRotation(ParseVectorString(obj.GetField("rotation")), false);
                        var scale = ConvertScale(ParseVectorString(obj.GetField("scale")));

                        string showInfo = obj.GetField("showHelpOnPickup");
                        if (showInfo != string.Empty)
                        {
                            bool showInfotutorial = int.Parse(showInfo) == 1;
                            gobj.GetComponent<Powerups>().showHelpOnPickup = showInfotutorial;
                        }

                        var localScale = gobj.transform.localScale;

                        gobj.transform.localPosition = position;
                        gobj.transform.localRotation = rotation;
                        gobj.transform.localScale = new Vector3(scale.x * localScale.x, scale.y * localScale.y, scale.z * localScale.z);
                    }

                    else if (objectName == "SuperSpeedItem")
                    {
                        var gobj = Instantiate(superSpeedPrefab, transform, false);
                        gobj.name = "SuperSpeedItem";
                        gobj.GetComponent<Powerups>().rotateMesh = false;

                        var position = ConvertPoint(ParseVectorString(obj.GetField("position")));
                        var rotation = ConvertRotation(ParseVectorString(obj.GetField("rotation")), false);
                        var scale = ConvertScale(ParseVectorString(obj.GetField("scale")));

                        string showInfo = obj.GetField("showHelpOnPickup");
                        if (showInfo != string.Empty)
                        {
                            bool showInfotutorial = int.Parse(showInfo) == 1;
                            gobj.GetComponent<Powerups>().showHelpOnPickup = showInfotutorial;
                        }

                        var localScale = gobj.transform.localScale;

                        gobj.transform.localPosition = position;
                        gobj.transform.localRotation = rotation;
                        gobj.transform.localScale = new Vector3(scale.x * localScale.x, scale.y * localScale.y, scale.z * localScale.z);
                    }

                    else if (objectName == "SuperBounceItem")
                    {
                        var gobj = Instantiate(superBouncePrefab, transform, false);
                        gobj.name = "SuperBounceItem";
                        gobj.GetComponent<Powerups>().rotateMesh = false;

                        var position = ConvertPoint(ParseVectorString(obj.GetField("position")));
                        var rotation = ConvertRotation(ParseVectorString(obj.GetField("rotation")), false);
                        var scale = ConvertScale(ParseVectorString(obj.GetField("scale")));

                        string showInfo = obj.GetField("showHelpOnPickup");
                        if (showInfo != string.Empty)
                        {
                            bool showInfotutorial = int.Parse(showInfo) == 1;
                            gobj.GetComponent<Powerups>().showHelpOnPickup = showInfotutorial;
                        }

                        var localScale = gobj.transform.localScale;

                        gobj.transform.localPosition = position;
                        gobj.transform.localRotation = rotation;
                        gobj.transform.localScale = new Vector3(scale.x * localScale.x, scale.y * localScale.y, scale.z * localScale.z);
                    }

                    else if (objectName == "ShockAbsorberItem")
                    {
                        var gobj = Instantiate(shockAbsorberPrefab, transform, false);
                        gobj.name = "ShockAbsorberItem";
                        gobj.GetComponent<Powerups>().rotateMesh = false;

                        var position = ConvertPoint(ParseVectorString(obj.GetField("position")));
                        var rotation = ConvertRotation(ParseVectorString(obj.GetField("rotation")), false);
                        var scale = ConvertScale(ParseVectorString(obj.GetField("scale")));

                        string showInfo = obj.GetField("showHelpOnPickup");
                        if (showInfo != string.Empty)
                        {
                            bool showInfotutorial = int.Parse(showInfo) == 1;
                            gobj.GetComponent<Powerups>().showHelpOnPickup = showInfotutorial;
                        }

                        var localScale = gobj.transform.localScale;

                        gobj.transform.localPosition = position;
                        gobj.transform.localRotation = rotation;
                        gobj.transform.localScale = new Vector3(scale.x * localScale.x, scale.y * localScale.y, scale.z * localScale.z);
                    }

                    else if (objectName == "HelicopterItem")
                    {
                        var gobj = Instantiate(gyrocopterPrefab, transform, false);
                        gobj.name = "HelicopterItem";
                        gobj.GetComponent<Powerups>().rotateMesh = false;

                        var position = ConvertPoint(ParseVectorString(obj.GetField("position")));
                        var rotation = ConvertRotation(ParseVectorString(obj.GetField("rotation")), false);
                        var scale = ConvertScale(ParseVectorString(obj.GetField("scale")));

                        string showInfo = obj.GetField("showHelpOnPickup");
                        if (showInfo != string.Empty)
                        {
                            bool showInfotutorial = int.Parse(showInfo) == 1;
                            gobj.GetComponent<Powerups>().showHelpOnPickup = showInfotutorial;
                        }

                        var localScale = gobj.transform.localScale;

                        gobj.transform.localPosition = position;
                        gobj.transform.localRotation = rotation;
                        gobj.transform.localScale = new Vector3(scale.x * localScale.x, scale.y * localScale.y, scale.z * localScale.z);
                    }

                    else if (objectName == "TimeTravelItem")
                    {
                        var gobj = Instantiate(timeTravelPrefab, transform, false);
                        gobj.name = "TimeTravelItem";
                        gobj.GetComponent<Powerups>().rotateMesh = false;

                        var position = ConvertPoint(ParseVectorString(obj.GetField("position")));
                        var rotation = ConvertRotation(ParseVectorString(obj.GetField("rotation")), false);
                        var scale = ConvertScale(ParseVectorString(obj.GetField("scale")));

                        var localScale = gobj.transform.localScale;

                        gobj.transform.localPosition = position;
                        gobj.transform.localRotation = rotation;
                        gobj.transform.localScale = new Vector3(scale.x * localScale.x, scale.y * localScale.y, scale.z * localScale.z);

                        string timeBonus = obj.GetField("timeBonus");
                        if (timeBonus != string.Empty)
                            gobj.GetComponent<TimeTravel>().timeBonus = (float)int.Parse(timeBonus) / 1000;
                        else
                            gobj.GetComponent<TimeTravel>().timeBonus = 5;
                    }
                }

                //Interior
                else if (obj.ClassName == "InteriorInstance")
                {
                    var gobj = Instantiate(interiorPrefab, transform, false);
                    gobj.name = "InteriorInstance";

                    var position = ConvertPoint(ParseVectorString(obj.GetField("position")));
                    var rotation = ConvertRotation(ParseVectorString(obj.GetField("rotation")));
                    var scale = ConvertScale(ParseVectorString(obj.GetField("scale")));

                    gobj.transform.localPosition = position;
                    gobj.transform.localRotation = rotation;
                    gobj.transform.localScale = scale;

                    var difPath = ResolvePath(obj.GetField("interiorFile"), MissionInfo.instance.MissionPath);
                    var dif = gobj.GetComponent<Dif>();
                    dif.filePath = difPath;

                    if (!dif.GenerateMesh(-1))
                        Destroy(gobj.gameObject);
                }

                //Shapes
                else if (obj.ClassName == "StaticShape")
                {
                    string objectName = obj.GetField("dataBlock");

                    if (objectName == "StartPad")
                    {
                        Vector3 position = ConvertPoint(ParseVectorString(obj.GetField("position")));
                        Quaternion rotation = ConvertRotation(ParseVectorString(obj.GetField("rotation")));
                        Vector3 scale = ConvertScale(ParseVectorString(obj.GetField("scale")));

                        Transform spMesh = startPad.transform.Find("Mesh");
                        Transform forwardPoint = spMesh.Find("Forward");

                        // Position
                        startPad.transform.localPosition = position;
                        startPad.transform.localRotation = rotation;

                        spMesh.transform.parent = null;
                        spMesh.transform.localRotation = rotation;

                        Vector3 localScale = spMesh.localScale;
                        spMesh.localScale = new Vector3(
                            scale.x * localScale.x,
                            scale.y * localScale.y,
                            scale.z * localScale.z
                        );

                        startPad.transform.LookAt(forwardPoint);
                        startPad.transform.localRotation = Quaternion.Euler(-90, startPad.transform.localRotation.eulerAngles.y, startPad.transform.localRotation.eulerAngles.z);
                    }

                    else if (objectName == "EndPad")
                    {
                        var position = ConvertPoint(ParseVectorString(obj.GetField("position")));
                        var rotation = ConvertRotation(ParseVectorString(obj.GetField("rotation")));

                        finishPad.transform.localPosition = position;
                        finishPad.transform.localRotation = rotation;
                    }

                    //Signs
                    else if (objectName == "SignFinish")
                    {
                        var gobj = Instantiate(finishSignPrefab, transform, false);
                        gobj.name = "SignFinish";

                        var position = ConvertPoint(ParseVectorString(obj.GetField("position")));
                        var rotation = ConvertRotation(ParseVectorString(obj.GetField("rotation")));
                        var scale = ConvertScale(ParseVectorString(obj.GetField("scale")));

                        var localScale = gobj.transform.localScale;

                        gobj.transform.localPosition = position;
                        gobj.transform.localRotation = rotation;
                        gobj.transform.localScale = new Vector3(scale.x * localScale.x, scale.y * localScale.y, scale.z * localScale.z);
                    }
                    else if (objectName == "SignPlain")
                    {
                        var gobj = Instantiate(signPlainPrefab, transform, false);
                        gobj.name = "SignPlain";

                        var position = ConvertPoint(ParseVectorString(obj.GetField("position")));
                        var rotation = ConvertRotation(ParseVectorString(obj.GetField("rotation")));
                        var scale = ConvertScale(ParseVectorString(obj.GetField("scale")));

                        var localScale = gobj.transform.localScale;

                        gobj.transform.localPosition = position;
                        gobj.transform.localRotation = rotation;
                        gobj.transform.localScale = new Vector3(scale.x * localScale.x, scale.y * localScale.y, scale.z * localScale.z);
                    }
                    else if (objectName == "SignPlainUp")
                    {
                        var gobj = Instantiate(signPlainUpPrefab, transform, false);
                        gobj.name = "SignPlainUp";

                        var position = ConvertPoint(ParseVectorString(obj.GetField("position")));
                        var rotation = ConvertRotation(ParseVectorString(obj.GetField("rotation")));
                        var scale = ConvertScale(ParseVectorString(obj.GetField("scale")));

                        var localScale = gobj.transform.localScale;

                        gobj.transform.localPosition = position;
                        gobj.transform.localRotation = rotation;
                        gobj.transform.localScale = new Vector3(scale.x * localScale.x, scale.y * localScale.y, scale.z * localScale.z);
                    }
                    else if (objectName == "SignPlainDown")
                    {
                        var gobj = Instantiate(signPlainDownPrefab, transform, false);
                        gobj.name = "SignPlainDown";

                        var position = ConvertPoint(ParseVectorString(obj.GetField("position")));
                        var rotation = ConvertRotation(ParseVectorString(obj.GetField("rotation")));
                        var scale = ConvertScale(ParseVectorString(obj.GetField("scale")));

                        var localScale = gobj.transform.localScale;

                        gobj.transform.localPosition = position;
                        gobj.transform.localRotation = rotation;
                        gobj.transform.localScale = new Vector3(scale.x * localScale.x, scale.y * localScale.y, scale.z * localScale.z);
                    }
                    else if (objectName == "SignPlainLeft")
                    {
                        var gobj = Instantiate(signPlainLeftPrefab, transform, false);
                        gobj.name = "SignPlainLeft";

                        var position = ConvertPoint(ParseVectorString(obj.GetField("position")));
                        var rotation = ConvertRotation(ParseVectorString(obj.GetField("rotation")));
                        var scale = ConvertScale(ParseVectorString(obj.GetField("scale")));

                        var localScale = gobj.transform.localScale;

                        gobj.transform.localPosition = position;
                        gobj.transform.localRotation = rotation;
                        gobj.transform.localScale = new Vector3(scale.x * localScale.x, scale.y * localScale.y, scale.z * localScale.z);
                    }
                    else if (objectName == "SignPlainRight")
                    {
                        var gobj = Instantiate(signPlainRightPrefab, transform, false);
                        gobj.name = "SignPlainRight";

                        var position = ConvertPoint(ParseVectorString(obj.GetField("position")));
                        var rotation = ConvertRotation(ParseVectorString(obj.GetField("rotation")));
                        var scale = ConvertScale(ParseVectorString(obj.GetField("scale")));

                        var localScale = gobj.transform.localScale;

                        gobj.transform.localPosition = position;
                        gobj.transform.localRotation = rotation;
                        gobj.transform.localScale = new Vector3(scale.x * localScale.x, scale.y * localScale.y, scale.z * localScale.z);
                    }
                    else if (objectName == "SignCaution")
                    {
                        var gobj = Instantiate(signCautionPrefab, transform, false);
                        gobj.name = "SignCaution";

                        var position = ConvertPoint(ParseVectorString(obj.GetField("position")));
                        var rotation = ConvertRotation(ParseVectorString(obj.GetField("rotation")));
                        var scale = ConvertScale(ParseVectorString(obj.GetField("scale")));

                        var localScale = gobj.transform.localScale;

                        gobj.transform.localPosition = position;
                        gobj.transform.localRotation = rotation;
                        gobj.transform.localScale = new Vector3(scale.x * localScale.x, scale.y * localScale.y, scale.z * localScale.z);
                    }
                    else if (objectName == "SignCautionCaution")
                    {
                        var gobj = Instantiate(signCautionCautionPrefab, transform, false);
                        gobj.name = "SignCautionCaution";

                        var position = ConvertPoint(ParseVectorString(obj.GetField("position")));
                        var rotation = ConvertRotation(ParseVectorString(obj.GetField("rotation")));
                        var scale = ConvertScale(ParseVectorString(obj.GetField("scale")));

                        var localScale = gobj.transform.localScale;

                        gobj.transform.localPosition = position;
                        gobj.transform.localRotation = rotation;
                        gobj.transform.localScale = new Vector3(scale.x * localScale.x, scale.y * localScale.y, scale.z * localScale.z);
                    }
                    else if (objectName == "SignCautionDanger")
                    {
                        var gobj = Instantiate(signCautionDangerPrefab, transform, false);
                        gobj.name = "SignCautionDanger";

                        var position = ConvertPoint(ParseVectorString(obj.GetField("position")));
                        var rotation = ConvertRotation(ParseVectorString(obj.GetField("rotation")));
                        var scale = ConvertScale(ParseVectorString(obj.GetField("scale")));

                        var localScale = gobj.transform.localScale;

                        gobj.transform.localPosition = position;
                        gobj.transform.localRotation = rotation;
                        gobj.transform.localScale = new Vector3(scale.x * localScale.x, scale.y * localScale.y, scale.z * localScale.z);
                    }

                    //Hazards
                    else if (objectName.ToLower() == "trapdoor")
                    {
                        var gobj = Instantiate(trapdoorPrefab, transform, false);
                        gobj.name = "Trapdoor";

                        var position = ConvertPoint(ParseVectorString(obj.GetField("position")));
                        var rotation = ConvertRotation(ParseVectorString(obj.GetField("rotation")));
                        var scale = ConvertScale(ParseVectorString(obj.GetField("scale")));

                        gobj.transform.localPosition = position;
                        gobj.transform.localRotation = rotation * Quaternion.Euler(90f, 0f, 0f); ;
                        gobj.transform.localScale = new Vector3(scale.x * gobj.transform.localScale.x,
                                                                scale.y * gobj.transform.localScale.y,
                                                                scale.z * gobj.transform.localScale.z
                                                                );
                    }

                    else if (objectName.ToLower() == "ductfan")
                    {
                        var gobj = Instantiate(ductFanPrefab, transform, false);
                        gobj.name = "DuctFan";

                        var position = ConvertPoint(ParseVectorString(obj.GetField("position")));
                        var rotation = ConvertRotation(ParseVectorString(obj.GetField("rotation")));
                        var scale = ConvertScale(ParseVectorString(obj.GetField("scale")));

                        gobj.transform.localPosition = position;
                        gobj.transform.localRotation = rotation * Quaternion.Euler(90f, 0f, 0f); ;
                        gobj.transform.localScale = new Vector3(scale.x * gobj.transform.localScale.x,
                                                                scale.y * gobj.transform.localScale.y,
                                                                scale.z * gobj.transform.localScale.z
                                                                );
                    }

                    else if (objectName.ToLower() == "tornado")
                    {
                        var gobj = Instantiate(tornadoPrefab, transform, false);
                        gobj.name = "Tornado";

                        var position = ConvertPoint(ParseVectorString(obj.GetField("position")));
                        var rotation = ConvertRotation(ParseVectorString(obj.GetField("rotation")));
                        var scale = ConvertScale(ParseVectorString(obj.GetField("scale")));

                        gobj.transform.localPosition = position;
                        gobj.transform.localRotation = rotation * Quaternion.Euler(90f, 0f, 0f); ;
                        gobj.transform.localScale = new Vector3(scale.x * gobj.transform.localScale.x,
                                                                scale.y * gobj.transform.localScale.y,
                                                                scale.z * gobj.transform.localScale.z
                                                                );
                    }

                    else if (objectName.ToLower() == "landmine")
                    {
                        var gobj = Instantiate(landMinePrefab, transform, false);
                        gobj.name = "LandMine";

                        var position = ConvertPoint(ParseVectorString(obj.GetField("position")));
                        var rotation = ConvertRotation(ParseVectorString(obj.GetField("rotation")));
                        var scale = ConvertScale(ParseVectorString(obj.GetField("scale")));

                        gobj.transform.localPosition = position;
                        gobj.transform.localRotation = rotation * Quaternion.Euler(90f, 0f, 0f); ;
                        gobj.transform.localScale = new Vector3(scale.x * gobj.transform.localScale.x,
                                                                scale.y * gobj.transform.localScale.y,
                                                                scale.z * gobj.transform.localScale.z
                                                                );
                    }

                    else if (objectName.ToLower() == "roundbumper")
                    {
                        var gobj = Instantiate(roundBumperPrefab, transform, false);
                        gobj.name = "RoundBumper";

                        var position = ConvertPoint(ParseVectorString(obj.GetField("position")));
                        var rotation = ConvertRotation(ParseVectorString(obj.GetField("rotation")));
                        var scale = ConvertScale(ParseVectorString(obj.GetField("scale")));

                        gobj.transform.localPosition = position;
                        gobj.transform.localRotation = rotation * Quaternion.Euler(90f, 0f, 0f); ;
                        gobj.transform.localScale = new Vector3(scale.x * gobj.transform.localScale.x,
                                                                scale.y * gobj.transform.localScale.y,
                                                                scale.z * gobj.transform.localScale.z
                                                                );
                    }

                    else if (objectName.ToLower() == "trianglebumper")
                    {
                        var gobj = Instantiate(triangleBumperPrefab, transform, false);
                        gobj.name = "TriangleBumper";

                        var position = ConvertPoint(ParseVectorString(obj.GetField("position")));
                        var rotation = ConvertRotation(ParseVectorString(obj.GetField("rotation")));
                        var scale = ConvertScale(ParseVectorString(obj.GetField("scale")));

                        gobj.transform.localPosition = position;
                        gobj.transform.localRotation = rotation * Quaternion.Euler(90f, 0f, 0f); ;
                        gobj.transform.localScale = new Vector3(scale.x * gobj.transform.localScale.x,
                                                                scale.y * gobj.transform.localScale.y,
                                                                scale.z * gobj.transform.localScale.z
                                                                );
                    }

                    else if (objectName.ToLower() == "oilslick")
                    {
                        var gobj = Instantiate(oilSlickPrefab, transform, false);
                        gobj.name = "OilSlick";

                        var position = ConvertPoint(ParseVectorString(obj.GetField("position")));
                        var rotation = ConvertRotation(ParseVectorString(obj.GetField("rotation")));
                        var scale = ConvertScale(ParseVectorString(obj.GetField("scale")));

                        gobj.transform.localPosition = position;
                        gobj.transform.localRotation = rotation * Quaternion.Euler(90f, 0f, 0f); ;
                        gobj.transform.localScale = new Vector3(scale.x * gobj.transform.localScale.x,
                                                                scale.y * gobj.transform.localScale.y,
                                                                scale.z * gobj.transform.localScale.z
                                                                );
                    }
                }

                else if (obj.ClassName == "Trigger")
                {
                    string objectName = obj.GetField("dataBlock");

                    if (objectName == "InBoundsTrigger")
                    {
                        var ibtObj = Instantiate(inBoundsTrigger, transform, false);
                        ibtObj.name = "InBoundsTrigger";

                        var position = ConvertPoint(ParseVectorString(obj.GetField("position")));
                        var rotation = ConvertRotation(ParseVectorString(obj.GetField("rotation")));
                        var scale = ConvertScale(ParseVectorString(obj.GetField("scale")));

                        var polyhedronScale = PolyhedronToBoxSize(ParseVectorString(obj.GetField("polyhedron")));

                        ibtObj.transform.localPosition = position;
                        ibtObj.transform.localRotation = rotation;
                        ibtObj.transform.localScale = new Vector3(scale.x * polyhedronScale.z, scale.y * polyhedronScale.x, scale.z * polyhedronScale.y);
                    }

                    else
                    {
                        if (objectName == "HelpTrigger")
                        {
                            var htObj = Instantiate(helpTriggerInstance, transform, false);
                            htObj.name = "HelpTrigger";

                            htObj.GetComponent<HelpTrigger>().helpText = obj.GetField("text");

                            var position = ConvertPoint(ParseVectorString(obj.GetField("position")));
                            var rotation = ConvertRotation(ParseVectorString(obj.GetField("rotation")));
                            var scale = ConvertScale(ParseVectorString(obj.GetField("scale")));

                            var polyhedronScale = PolyhedronToBoxSize(ParseVectorString(obj.GetField("polyhedron")));

                            htObj.transform.localPosition = position;
                            htObj.transform.localRotation = rotation;
                            htObj.transform.localScale = new Vector3(scale.x * polyhedronScale.z, scale.y * polyhedronScale.x, scale.z * polyhedronScale.y);
                        }
                    }
                }

                //Moving platforms
                else if (obj.ClassName == "SimGroup" && obj.Name != "MissionGroup")
                {
                    // Grab the PathedInterior child
                    var pathedInterior = obj.RecursiveChildren()
                        .FirstOrDefault(o => o.ClassName == "PathedInterior");

                    if (pathedInterior == null)
                        continue;

                    MovingPlatform movingPlatform = null;
                    int indexStr = -1;

                    if (pathedInterior != null)
                    {
                        var gobj = Instantiate(movingPlatformPrefab, transform, false);
                        gobj.name = "PathedInterior";

                        var position = ConvertPoint(ParseVectorString(pathedInterior.GetField("basePosition")));
                        var rotation = ConvertRotation(ParseVectorString(pathedInterior.GetField("baseRotation")));
                        var scale = ConvertScale(ParseVectorString(pathedInterior.GetField("baseScale")));

                        gobj.transform.localPosition = position;
                        gobj.transform.localRotation = rotation;
                        gobj.transform.localScale = scale;

                        var resource = pathedInterior.GetField("interiorResource");
                        var difPath = ResolvePath(resource, MissionInfo.instance.MissionPath);

                        var dif = gobj.GetComponent<Dif>();
                        dif.filePath = difPath;

                        // Parse interiorIndex from mission file
                        indexStr = int.Parse(pathedInterior.GetField("interiorIndex"));
                        dif.GenerateMovingPlatformMesh(indexStr);

                        movingPlatform = gobj.GetComponent<MovingPlatform>();

                        string initialPosition = pathedInterior.GetField("initialPosition");
                        if (initialPosition != string.Empty)
                            movingPlatform.initialPosition = (float)int.Parse(initialPosition) / 1000;
                        else
                            movingPlatform.initialPosition = 0;

                        string initialTargetPosition = pathedInterior.GetField("initialTargetPosition");
                        if (initialTargetPosition != string.Empty)
                        {
                            int itp = 0;
                            if (int.TryParse(initialTargetPosition, out itp))
                            {
                                movingPlatform.initialTargetPosition = (itp >= 0) ? (float)itp / 1000 : itp;
                                if (itp >= 0)
                                    movingPlatform.movementMode = MovementMode.Triggered;
                                else
                                    movingPlatform.movementMode = MovementMode.Constant;
                            }
                        }
                        else
                        {
                            movingPlatform.initialTargetPosition = 0;
                            movingPlatform.movementMode = MovementMode.Triggered;
                        }
                    }

                    //if ITP = 0, put the triggergototargets
                    if (movingPlatform.movementMode == MovementMode.Triggered)
                    {
                        var tgtts = obj.RecursiveChildren()
                            .Where(o => o.ClassName == "Trigger")
                            .ToList();

                        foreach (var trigger in tgtts)
                        {
                            var tgttObj = Instantiate(triggerGoToTarget, transform, false);
                            tgttObj.name = "TriggerGoToTarget";

                            var position = ConvertPoint(ParseVectorString(trigger.GetField("position")));
                            var rotation = ConvertRotation(ParseVectorString(trigger.GetField("rotation")));
                            var scale = ConvertScale(ParseVectorString(trigger.GetField("scale")));

                            var polyhedronScale = PolyhedronToBoxSize(ParseVectorString(trigger.GetField("polyhedron")));

                            tgttObj.transform.localPosition = position;
                            tgttObj.transform.localRotation = rotation;
                            tgttObj.transform.localScale = new Vector3(scale.x * polyhedronScale.z, scale.y * polyhedronScale.x, scale.z * polyhedronScale.y);

                            TriggerGoToTarget tgtt = tgttObj.GetComponent<TriggerGoToTarget>();
                            tgtt.movingPlatform = movingPlatform;
                            tgtt.targetTime = (float)int.Parse(trigger.GetField("targetTime")) / 1000;
                        }
                    }

                    // Grab all markers inside any Path child
                    var markers = obj.RecursiveChildren()
                        .Where(o => o.ClassName == "Marker")
                        .ToList();

                    movingPlatform.sequenceNumbers = new SequenceNumber[markers.Count];
                    List<SmoothingType> smoothingTypes = new List<SmoothingType>();

                    foreach (var marker in markers)
                    {
                        Vector3 pos = ConvertPoint(ParseVectorString(marker.GetField("position")));
                        int seq = int.Parse(marker.GetField("seqNum"));
                        int msToNext = int.Parse(marker.GetField("msToNext"));

                        SmoothingType smoothingType = (SmoothingType)Enum.Parse(typeof(SmoothingType), marker.GetField("smoothingType"));
                        smoothingTypes.Add(smoothingType);

                        GameObject markerInstance = Instantiate(new GameObject(), transform, false);
                        markerInstance.name = "Marker Interior " + indexStr + " (" + seq + ")";
                        markerInstance.transform.position = pos;

                        SequenceNumber sequence = new SequenceNumber();
                        sequence.marker = markerInstance;
                        sequence.secondsToNext = (float)msToNext / 1000;

                        if (seq >= markers.Count)
                            seq = markers.Count - 1;

                        movingPlatform.sequenceNumbers[seq] = sequence;
                    }

                    SmoothingType smoothing = smoothingTypes
                        .GroupBy(x => x)
                        .OrderByDescending(g => g.Count())
                        .First()
                        .Key;

                    movingPlatform.smoothing = smoothing;
                    movingPlatform.InitMovingPlatform();
                }
            }

            globalMarble.GetComponent<Movement>().GenerateMeshData();

            Time.timeScale = 1f;
            GameManager.instance.InitGemCount();

            GameManager.instance.SetSoundVolumes();
            GameManager.instance.PlayLevelMusic();

            directionalLight.GetComponent<Light>().shadows = PlayerPrefs.GetInt("Graphics_Shadow", 1) == 1 ? LightShadows.Soft : LightShadows.None;

            Marble.onRespawn?.Invoke();
        }

        // -------------------------    
        // Conversion helpers
        // -------------------------

        Color ConvertColor(float[] torqueRGBA)
        {
            if (torqueRGBA == null || torqueRGBA.Length < 3)
                return Color.white;

            float r = torqueRGBA[0];
            float g = torqueRGBA[1];
            float b = torqueRGBA[2];
            float a = torqueRGBA.Length > 3 ? torqueRGBA[3] : 1f;

            float intensity = Mathf.Max(r, g, b);
            if (intensity <= 0f)
                intensity = 1f;

            return new Color(r / intensity, g / intensity, b / intensity, a);
        }

        float ConvertIntensity(Color torqueColor, float intensityScale = 1.0f)
        {
            // Torque stores brightness in RGB
            float intensity = Mathf.Max(
                torqueColor.r,
                torqueColor.g,
                torqueColor.b
            );

            if (intensity <= 0f)
                intensity = 1f;

            return intensity * intensityScale;
        }

        Color ConvertAmbient(float[] torqueRGBA)
        {
            if (torqueRGBA == null || torqueRGBA.Length < 3)
                return Color.black;

            return new Color(
                torqueRGBA[0],
                torqueRGBA[1],
                torqueRGBA[2],
                1f
            );
        }

        Quaternion ConvertDirection(float[] torqueDir)
        {
            // Torque Z-up → Unity Y-up
            Vector3 unityDir = new Vector3(
                torqueDir[0],
                torqueDir[2],
                torqueDir[1]
            );

            unityDir.Normalize();

            // Unity directional lights shine along -forward
            return Quaternion.LookRotation(unityDir, Vector3.up);
        }

        private Vector3 ConvertPoint(float[] p)
        {
            return new Vector3(p[0], p[2], p[1]);
        }

        private Quaternion ConvertRotation(float[] torqueRotation, bool additionalRotate = true)
        {
            // Torque point is an angle axis in torquespace
            float angle = torqueRotation[3];
            Vector3 axis = new Vector3(torqueRotation[0], -torqueRotation[1], torqueRotation[2]);
            Quaternion rot = Quaternion.AngleAxis(angle, axis);

            if(additionalRotate) 
                rot = Quaternion.Euler(-90.0f, 0, 0) * rot;

            return rot;
        }

        private Vector3 ConvertScale(float[] s)
        {
            return new Vector3(s[0], s[1], s[2]);
        }

        private Vector3 PolyhedronToBoxSize(float[] polyhedron)
        {
            if (polyhedron == null || polyhedron.Length != 12)
                throw new ArgumentException("Polyhedron must be 12 floats: origin + 3 edge vectors");

            // Edge vectors start at index 3
            Vector3 edgeX = new Vector3(polyhedron[3], polyhedron[4], polyhedron[5]);
            Vector3 edgeY = new Vector3(polyhedron[6], polyhedron[7], polyhedron[8]);
            Vector3 edgeZ = new Vector3(polyhedron[9], polyhedron[10], polyhedron[11]);

            // Size is simply the length of each edge vector
            return new Vector3(
                edgeX.magnitude,
                edgeY.magnitude,
                edgeZ.magnitude
            );
        }


        private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

        private float[] ParseVectorString(string vs)
        {
            return vs
                .Split(' ')
                .Select(s => float.Parse(s, Invariant))
                .ToArray();
        }

        private string ResolvePath(string assetPath, string misPath)
        {
            if (string.IsNullOrEmpty(assetPath))
                return assetPath;

            // Remove leading slashes
            assetPath = assetPath.TrimStart('/');

            // --- Resolve special prefixes ---
            if (assetPath[0] == '.')
            {
                assetPath = Path.GetDirectoryName(misPath) + assetPath.Substring(1);
            }
            else
            {
                // Replace anything before first '/' with "marble"
                int slash = assetPath.IndexOf('/');
                assetPath = slash >= 0
                    ? "marble" + assetPath.Substring(slash)
                    : "marble/" + assetPath;
            }

            // --- Remove ALL backslashes ---
            assetPath = assetPath.Replace("\\", "");

            if(assetPath.EndsWith("\""))
                assetPath = assetPath.Substring(0, assetPath.Length - 1);

            return assetPath;
        }

        public static TSObject ProcessObject(TSParser.Object_declContext objectDecl)
        {
            var obj = ScriptableObject.CreateInstance<TSObject>();

            obj.ClassName = objectDecl.class_name_expr().GetText();
            obj.Name = objectDecl.object_name().GetText();

            var block = objectDecl.object_declare_block();
            if (block == null)
                return obj;

            foreach (var assignList in block.slot_assign_list())
            {
                foreach (var slot in assignList.slot_assign())
                {
                    var key = slot.children[0].GetText().ToLower();
                    var value = slot.expr().GetText();

                    var str = slot.expr().STRATOM();
                    if (str != null)
                        value = str.GetText().Substring(1, value.Length - 2);

                    // Torque allows duplicate keys; last one wins
                    if (obj.Fields.ContainsKey(key))
                        obj.Fields[key] = value;
                    else
                        obj.Fields.Add(key, value);
                }
            }

            foreach (var sub in block.object_decl_list())
            {
                foreach (var subDecl in sub.object_decl())
                {
                    var child = ProcessObject(subDecl);
                    child.Parent = obj;
                    obj.Children.Add(child);
                }
            }

            return obj;
        }
    }
}