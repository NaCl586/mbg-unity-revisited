using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class HelpCreditsManager : MonoBehaviour
{
    int page;
    int maxPage = 12;

    public Button next;
    public Button prev;
    public Button home;
    [Space]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI captionText;
    [SerializeField] float margin;
    [Space]
    public GameObject HC_StartPad;
    public GameObject HC_EndPad;
    public GameObject HC_Gem1;
    public GameObject HC_Gem2;
    public GameObject HC_Gem3;
    public GameObject HC_SuperSpeed;
    public GameObject HC_SuperJump;
    public GameObject HC_ShockAbsorber;
    public GameObject HC_AntiGravity;
    public GameObject HC_Helicopter;
    public GameObject HC_TimeTravel;
    public GameObject HC_DuctFan;
    public GameObject HC_Tornado;
    public GameObject HC_Trapdoor;
    public GameObject HC_Oilslick;
    public GameObject HC_Landmine;
    public GameObject HC_Bumper;
    public GameObject HC_SuperBounce;
    public GameObject names_right;

    float initOffset;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) SceneManager.LoadScene("MainMenu");
    }

    public void UpdateHUDMaterial()
    {
        int targetLayer = LayerMask.NameToLayer("HUD");
        float smoothness01 = 1f;

        GameObject[] allObjects = FindObjectsOfType<GameObject>();

        for (int i = 0; i < allObjects.Length; i++)
        {
            if (allObjects[i].layer != targetLayer)
                continue;

            Renderer[] renderers = allObjects[i].GetComponentsInChildren<Renderer>(true);

            for (int r = 0; r < renderers.Length; r++)
            {
                // This creates per-renderer material instances
                Material[] mats = renderers[r].materials;

                for (int m = 0; m < mats.Length; m++)
                {
                    Material mat = mats[m];
                    if (mat == null) continue;

                    if (mat.HasProperty("_Smoothness"))
                        mat.SetFloat("_Smoothness", smoothness01);

                    if (mat.HasProperty("_Glossiness"))
                        mat.SetFloat("_Glossiness", smoothness01);
                }
            }
        }
    }

    public void Start()
    {
        Time.timeScale = 1f;
        UpdateHUDMaterial();

        next.onClick.AddListener(NextPage);
        prev.onClick.AddListener(PrevPage);
        home.onClick.AddListener(() => SceneManager.LoadScene("MainMenu"));

        initOffset = captionText.GetComponent<RectTransform>().offsetMin.x;

        page = 0;
        SetPage(page);
    }

    void NextPage()
    {
        page++;
        if (page >= maxPage)
            page = 0;
        SetPage(page);
    }

    void PrevPage()
    {
        page--;
        if (page < 0)
            page = maxPage - 1;
        SetPage(page);
    }

    void SetMargin(float margin) => captionText.GetComponent<RectTransform>().offsetMin = new Vector2(initOffset + margin, captionText.GetComponent<RectTransform>().offsetMin.y);

    public void SetPage(int _page)
    {
        names_right.SetActive(false);
        HC_StartPad.SetActive(false);
        HC_EndPad.SetActive(false);
        HC_Gem1.SetActive(false);
        HC_Gem2.SetActive(false);
        HC_Gem3.SetActive(false);
        HC_SuperSpeed.SetActive(false);
        HC_SuperJump.SetActive(false);
        HC_ShockAbsorber.SetActive(false);
        HC_AntiGravity.SetActive(false);
        HC_Helicopter.SetActive(false);
        HC_TimeTravel.SetActive(false);
        HC_DuctFan.SetActive(false);
        HC_Tornado.SetActive(false);
        HC_Trapdoor.SetActive(false);
        HC_Oilslick.SetActive(false);
        HC_Landmine.SetActive(false);
        HC_Bumper.SetActive(false);
        HC_SuperBounce.SetActive(false);

        switch (_page)
        {
            case 0:
                titleText.text = "Overview";
                captionText.text = "Roll your marble through a rich cartoon landscape of moving platforms and dangerous hazards. Along the way find power ups to increase your speed, jumping ability or flight power, and use them to collect the hidden gems and race to the finish for the fastest time.";
                SetMargin(0);
                break;

            case 1:
                titleText.text = "Basic Controls";
                captionText.text = Utils.Resolve("The marble can be moved forward, back, left and right by pressing <func:bind moveforward>, <func:bind movebackward>, <func:bind moveleft> and <func:bind moveright>, respectively. Pressing <func:bind jump> causes the marble to jump, and pressing <func:bind mouseFire> uses whatever powerup you currently have available. All movement is relative to the view direction.");
                SetMargin(0);
                break;

            case 2:
                titleText.text = "Camera Controls";
                captionText.text = Utils.Resolve("The camera direction can be changed by moving the mouse or by pressing <func:bind panUp>, <func:bind panDown>, <func:bind turnLeft> or <func:bind turnRight>. In order to look up and down freely with the mouse, hold down <func:bind freelook>. You can turn free look on always from the Mouse pane of the Control Options screen.");
                SetMargin(0);
                break;

            case 3:
                HC_StartPad.SetActive(true);
                HC_EndPad.SetActive(true);
                HC_Gem1.SetActive(true);
                HC_Gem2.SetActive(true);
                HC_Gem3.SetActive(true);
                titleText.text = "Goals";
                captionText.text = "Start Pad - this is where you start the level.\n\nEnd Pad - roll your marble here to end the level.\n\nGems - if a level has gems, you must pick them all up before you can exit.";
                SetMargin(margin);
                break;

            case 4:
                HC_SuperSpeed.SetActive(true);
                HC_SuperJump.SetActive(true);
                HC_ShockAbsorber.SetActive(true);
                HC_SuperBounce.SetActive(true);
                titleText.text = "Bonus Items (1/2)";
                captionText.text = "Super Speed PowerUp - gives you a burst of speed.\n\nSuper Jump PowerUp - gives you a big jump up.\n\nShock Absorber PowerUp - absorbs bounce impacts.\n\nSuper Bounce PowerUp - makes you bounce higher.";
                SetMargin(margin);
                break;

            case 5:
                HC_AntiGravity.SetActive(true);
                HC_Helicopter.SetActive(true);
                HC_TimeTravel.SetActive(true);
                titleText.text = "Bonus Items (2/2)";
                captionText.text = "Gyrocopter PowerUp - slows your fall in the air.\n\nTime Travel -takes some time off the clock.\n\nGravity Modifier -Changes the direction of \"down\" - the new down is in the direction of the arrow.";
                SetMargin(margin);
                break;

            case 6:
                HC_DuctFan.SetActive(true);
                HC_Tornado.SetActive(true);
                HC_Trapdoor.SetActive(true);
                titleText.text = "Hazards (1/2)";
                captionText.text = "Duct Fan - be careful this doesn't blow you away!\n\nTornado - it'll pull you in and spit you out.\n\nTrap Door - keep moving when you're rolling over one of these.";
                SetMargin(margin);
                break;

            case 7:
                HC_Bumper.SetActive(true);
                HC_Landmine.SetActive(true);
                HC_Oilslick.SetActive(true);
                titleText.text = "Hazards (2/2)";
                captionText.text = "Bumper - this'll bounce you if you touch it.\n\nLand Mine - Warning!  Explodes on contact!\n\nOil Slick - you won't have much traction on these surfaces.";
                SetMargin(margin);
                break;

            case 8:
                titleText.text = "About GarageGames";
                captionText.text = "GarageGames is a unique Internet publishing label for independent games and gamemakers.  Our mission is to provide the independent developer with tools, knowledge, co-conspirators - whatever is needed to unleash the creative spirit and get great innovative independent games to market.";
                SetMargin(0);
                break;

            case 9:
                titleText.text = "About the Torque";
                captionText.text = "The Torque Game Engine (TGE) is a full featured AAA title engine with the latest in scripting, geometry, particle effects, animation and texturing, as well as award winning multi-player networking code.  For $100 per programmer, you get the source to the engine!";
                SetMargin(0);
                break;

            case 10:
                names_right.SetActive(true);
                titleText.text = "The Marble Blast Team";
                captionText.text =
                    "Alex Swanson\n" +
                    "Jeff Tunnell\n" +
                    "Liam Ryan\n" +
                    "Rick Overman\n" +
                    "Timothy Clarke\n" +
                    "Pat Wilson";
                SetMargin(70);
                break;

            case 11:
                titleText.text = "Special Thanks";
                captionText.text = "We'd like to thank Nullsoft, for the SuperPiMP Install System, and Markus F.X.J. Oberhumer, Laszlo Molnar and the rest of the UPX team for the UPX executable packer. Thanks also to Kurtis Seebaldt for his work on integrating Ogg/Vorbis streaming into the Torque engine, and to the Ogg/Vorbis team.";
                SetMargin(0);
                break;
        }
    }
}
