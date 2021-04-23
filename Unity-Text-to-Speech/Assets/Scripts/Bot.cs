using Newtonsoft.Json;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Windows.Speech;

public class Bot : MonoBehaviour
{
    /// <summary>
    /// Static instance of this class
    /// </summary>
    public static Bot Instance;

    /// <summary>
    /// Material of the sphere representing the Bot in the scene
    /// </summary>
    internal Material botMaterial;

    /// <summary>
    /// Speech recognizer class reference, which will convert speech to text.
    /// </summary>
    private DictationRecognizer dictationRecognizer;

    /// <summary>
    /// Use this variable to identify the Bot Id
    /// Can be any value
    /// </summary>
    private string botId = "MRBotId";

    /// <summary>
    /// Use this variable to identify the Bot Name
    /// Can be any value
    /// </summary>
    private string botName = "MRBotName";

    /// <summary>
    /// The Bot Secret key found on the Web App Bot Service on the Azure Portal
    /// </summary>
    private string botSecret = "f0R1lBlUNn8.VXtDZIorQlXU51fi_mhlmSUubeTl-mZ2epCIp9tlEr8"; 

    /// <summary>
    /// Bot Endpoint, v4 Framework uses v3 endpoint at this point in time
    /// </summary>
    private string botEndpoint = "https://directline.botframework.com/v3/directline";

    /// <summary>
    /// The conversation object reference
    /// </summary>
    private ConversationObject conversation;

    /// <summary>
    /// Bot states to regulate the application flow
    /// </summary>
    internal enum BotState {ReadyToListen, Listening, Processing}


    public enum BotAniState {Idle, Talk}

    /// <summary>
    /// Flag for the Bot state
    /// </summary>
    internal BotState botState;

    internal BotAniState botAniState;

    /// <summary>
    /// Flag for the conversation status
    /// </summary>
    internal bool conversationStarted = false;

    Vector3 rot = Vector3.zero;

    public SpeechManager speech;

    /// <summary>
    /// Called on Initialization
    /// </summary>
    void Awake()
    {
        Instance = this;
        botAniState = BotAniState.Idle;

    }

    /// <summary>
    /// Called immediately after Awake method
    /// </summary>
    void Start()
    {
        botState = BotState.ReadyToListen;
    }

    /// <summary>
    /// Start microphone capture.
    /// </summary>
    public void StartCapturingAudio()
    {
        botState = BotState.Listening;
        botMaterial.color = Color.red;

        // Start dictation
        dictationRecognizer = new DictationRecognizer();
        dictationRecognizer.DictationResult += DictationRecognizer_DictationResult;
        dictationRecognizer.Start();
    }
    

    /// <summary>
    /// Stop microphone capture.
    /// </summary>
    public void StopCapturingAudio()
    {
        botState = BotState.Processing;
        dictationRecognizer.Stop();
    }

        /// <summary>
    /// This handler is called every time the Dictation detects a pause in the speech. 
    /// </summary>
    private void DictationRecognizer_DictationResult(string text, ConfidenceLevel confidence)
    {
        // Update UI with dictation captured
        Debug.Log($"User just said: {text}");      

        // Send dictation to Bot
        StartCoroutine(SendMessageToBot(text, botId, botName, "message"));
        StopCapturingAudio();
    }  

        /// <summary>
    /// Request a conversation with the Bot Service
    /// </summary>
    internal IEnumerator StartConversation()
    {
        string conversationEndpoint = string.Format("{0}/conversations", botEndpoint);

        WWWForm webForm = new WWWForm();

        using (UnityWebRequest unityWebRequest = UnityWebRequest.Post(conversationEndpoint, webForm))
        {
            unityWebRequest.SetRequestHeader("Authorization", "Bearer " + botSecret);
            unityWebRequest.downloadHandler = new DownloadHandlerBuffer();

            yield return unityWebRequest.SendWebRequest();
            string jsonResponse = unityWebRequest.downloadHandler.text;
        
            conversation = new ConversationObject();
            conversation = JsonConvert.DeserializeObject<ConversationObject>(jsonResponse);
            Debug.Log($"Start Conversation - Id: {conversation.ConversationId}");
            conversationStarted = true; 
        }

        // The following call is necessary to create and inject an activity of type //"conversationUpdate" to request a first "introduction" from the Bot Service.
        StartCoroutine(SendMessageToBot("", botId, botName, "conversationUpdate"));
    }
    
    /// <summary>
    /// Send the user message to the Bot Service in form of activity
    /// and call for a response
    /// </summary>
    private IEnumerator SendMessageToBot(string message, string fromId, string fromName, string activityType)
    {
        Debug.Log($"SendMessageCoroutine: {conversation.ConversationId}, message: {message} from Id: {fromId} from name: {fromName}");

        // Create a new activity here
        Activity activity = new Activity();
        activity.from = new From();
        activity.conversation = new Conversation();
        activity.from.id = fromId;
        activity.from.name = fromName;
        activity.text = message;
        activity.type = activityType;
        activity.channelId = "DirectLineChannelId";
        activity.conversation.id = conversation.ConversationId; 
        activity.locale = "en-EN";    

        // Serialize the activity
        string json = JsonConvert.SerializeObject(activity);

        string sendActivityEndpoint = string.Format("{0}/conversations/{1}/activities", botEndpoint, conversation.ConversationId);
        
        // Send the activity to the Bot
        using (UnityWebRequest www = new UnityWebRequest(sendActivityEndpoint, "POST"))
        {
            www.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));

            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Authorization", "Bearer " + botSecret);
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

            // extrapolate the response Id used to keep track of the conversation
            string jsonResponse = www.downloadHandler.text;
            string cleanedJsonResponse = jsonResponse.Replace("\r\n", string.Empty);
            string responseConvId = cleanedJsonResponse.Substring(10, 30);

            // Request a response from the Bot Service
            StartCoroutine(GetResponseFromBot(activity));
        }
    }

    /// <summary>
    /// Request a response from the Bot by using a previously sent activity
    /// </summary>
    private IEnumerator GetResponseFromBot(Activity activity)
    {
        string getActivityEndpoint = string.Format("{0}/conversations/{1}/activities", botEndpoint, conversation.ConversationId);

        using (UnityWebRequest unityWebRequest1 = UnityWebRequest.Get(getActivityEndpoint))
        {
            unityWebRequest1.downloadHandler = new DownloadHandlerBuffer();
            unityWebRequest1.SetRequestHeader("Authorization", "Bearer " + botSecret);

            yield return unityWebRequest1.SendWebRequest();

            string jsonResponse = unityWebRequest1.downloadHandler.text;

            ActivitiesRootObject root = new ActivitiesRootObject();
            root = JsonConvert.DeserializeObject<ActivitiesRootObject>(jsonResponse);

            // foreach (var act in root.activities)
            // {
            //     if(act.text != null)
            //     {
            //         Debug.Log($"Bot Response: {act.text}");

            //         // if(act.text.ToLowerInvariant().StartsWith("o"))
            //         // {
            //         //     continue;
            //         // }

            //         SetBotResponseText(act.text);
            //     }

            //     //break;
            // }

            for(int i = root.activities.Count - 1; i >= 0; i--)
            {
                var act = root.activities[i];
                if(act.text != null && !act.text.ToLowerInvariant().StartsWith("o"))
                {
                    SetBotResponseText(act.text);
                    break;
                }
            }

            botState = BotState.ReadyToListen;
            botMaterial.color = Color.blue;
        }
    }

    /// <summary>
    /// Set the UI Response Text of the bot
    /// </summary>
    internal void SetBotResponseText(string responseString)
    {  
        this.botAniState = BotAniState.Talk;
        SceneOrganiser.Instance.botResponseText.text =  responseString;

        speech.SpeakWithRESTAPI(responseString);
    }

    // Update is called once per frame
    void Update()
    {
        gameObject.transform.eulerAngles = rot;
    }
}
