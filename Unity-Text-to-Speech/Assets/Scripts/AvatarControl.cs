using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AvatarControl : MonoBehaviour
{
    Vector3 rot = Vector3.zero;

    Animator anim;

    // Start is called before the first frame update
    void Start()
    {
        anim = gameObject.GetComponent<Animator>();
        gameObject.transform.eulerAngles = rot;
    }

    // Update is called once per frame
    void Update()
    {
        // if(Bot.Instance.botState == Bot.BotState.Listening)
        // {
        //     anim.SetBool("Walk_Anim", true);
        // }
        // else
        // {
        //     anim.SetBool("Walk_Anim", false);
        // }

        if(Bot.Instance.botAniState == Bot.BotAniState.Talk)
        {
            anim.SetBool("Talk", true);
        }
        else
        {
            anim.SetBool("Talk", false);
        }
        gameObject.transform.eulerAngles = rot;
    }
}
