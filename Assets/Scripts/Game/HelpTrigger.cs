using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HelpTrigger : MonoBehaviour
{
    public AudioClip helpTutorialSfx;
    [TextArea(2, 10)] public string helpText;

    public void TriggerEnter()
    {
        GameManager.instance.PlayAudioClip(helpTutorialSfx);
        GameUIManager.instance.SetCenterText(helpText);
    }
}
