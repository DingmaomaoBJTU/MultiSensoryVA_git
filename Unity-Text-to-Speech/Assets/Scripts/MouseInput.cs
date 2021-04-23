using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MouseInput : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (Bot.Instance.botState == Bot.BotState.ReadyToListen)
            {
                // If a conversation has not started yet, request one
                if(Bot.Instance.conversationStarted)
                {
                    Bot.Instance.SetBotResponseText("I'm Listening...");
                    Bot.Instance.StartCapturingAudio();
                }
                else
                {
                    Bot.Instance.SetBotResponseText("Requesting Conversation...");
                    StartCoroutine(Bot.Instance.StartConversation());
                }
            }

            if(Bot.Instance.botAniState == Bot.BotAniState.Talk)
            {
                Bot.Instance.botAniState = Bot.BotAniState.Idle;
            }
        }

        if(Input.GetMouseButtonDown(1))
        {
            Bot.Instance.SetBotResponseText("Stop Listening.");
            Bot.Instance.StopCapturingAudio();
        }
    }
}
