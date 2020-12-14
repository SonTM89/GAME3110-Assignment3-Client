using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using System.Text;
using System.Net.Sockets;
using System.Net;
using UnityEngine.UI;
using UnityEngine.Networking;

using TMPro;

public class NetworkMan : MonoBehaviour
{
    public int matchNumber = -1;

    public int matchID = 0;

    private string playersInMatch = "";

    private bool gameStart = false;

    private bool matchStart = false;

    private bool delayToNextMatch = false;

    private List<string> players;

    [SerializeField]
    private InputField input;

    [SerializeField]
    private Canvas canvasInput;

    [SerializeField]
    private TextMeshProUGUI matchIDText;

    [SerializeField]
    private TextMeshProUGUI matchContentText;

    [SerializeField]
    private Canvas canvasMatch;

    [SerializeField]
    private Canvas canVasTransition;

    [SerializeField]
    private Canvas canvasEnd;

    public UdpClient udp;


    // Start is called before the first frame update
    void Start()
    {
        udp = new UdpClient();

        udp.Connect("3.97.25.11", 12345);

        Byte[] sendBytes = Encoding.ASCII.GetBytes("connect");

        udp.Send(sendBytes, sendBytes.Length);

        udp.BeginReceive(new AsyncCallback(OnReceived), udp);

        InvokeRepeating("HeartBeat", 1, 1);
    }

    public void GetInput(string matchNum)
    {
        //matchNumber = Int32.Parse(matchNum);

        if (Int32.TryParse(matchNum, out int num))
        {
            matchNumber = num;

            canvasInput.gameObject.SetActive(false);
        }
        else
        {
            input.text = "";
        }

        Debug.Log("You enter:" + matchNumber);
        //input.text = "";

    }

    public void MatchPlay()
    {
        matchIDText.text = "Match ID: " + matchID;

        matchContentText.text = "Players in match: ";

    }

    public static void LogText(string logMessage, TextWriter w)
    {
        w.WriteLine($"{logMessage}");
    }

    public static void Log(string logMessage, TextWriter w)
    {
        w.Write("\r\nLog Entry : ");
        w.WriteLine($"{DateTime.Now.ToLongTimeString()} {DateTime.Now.ToLongDateString()}");
        w.WriteLine("  :");
        w.WriteLine($"  :{logMessage}");
        w.WriteLine("-------------------------------");
    }

    public static void DumpLog(StreamReader r)
    {
        string line;
        while ((line = r.ReadLine()) != null)
        {
            Console.WriteLine(line);
        }
    }

    void OnDestroy()
    {
        udp.Dispose();
    }


    public enum commands
    {
        NEW_CLIENT,
        UPDATE,
        STARTMATCH
    };

    [Serializable]
    public class Message
    {
        public commands cmd;
    }

    [Serializable]
    public class Player
    {
        [Serializable]
        public struct receivedColor
        {
            public float R;
            public float G;
            public float B;
        }
        public string id;
        public receivedColor color;
    }

    [Serializable]
    public class NewPlayer
    {

    }

    [Serializable]
    public class GameState
    {
        public Player[] players;
    }

    [Serializable]
    public class PinM
    {
        public string id;
        public string exp;
    }

        [Serializable]
    public class PlayersInMatch
    {
        public PinM[] pinMs;
    }

    public Message latestMessage;
    public GameState lastestGameState;
    public PlayersInMatch latestPlayersInMatch;
    void OnReceived(IAsyncResult result)
    {
        // this is what had been passed into BeginReceive as the second parameter:
        UdpClient socket = result.AsyncState as UdpClient;

        // points towards whoever had sent the message:
        IPEndPoint source = new IPEndPoint(0, 0);

        // get the actual message and fill out the source:
        byte[] message = socket.EndReceive(result, ref source);

        // do what you'd like with `message` here:
        string returnData = Encoding.ASCII.GetString(message);
        Debug.Log("Got this: " + returnData);

        latestMessage = JsonUtility.FromJson<Message>(returnData);
        try
        {
            switch (latestMessage.cmd)
            {
                case commands.NEW_CLIENT:
                    break;
                case commands.UPDATE:
                    lastestGameState = JsonUtility.FromJson<GameState>(returnData);
                    break;
                case commands.STARTMATCH:
                    
                    latestPlayersInMatch = JsonUtility.FromJson<PlayersInMatch>(returnData);

                    Debug.Log("ID:" + matchID);

                    Debug.Log(latestPlayersInMatch.pinMs.Length);
                    for(int i = 0; i < latestPlayersInMatch.pinMs.Length; i++)
                    {
                        Debug.Log(latestPlayersInMatch.pinMs[i].id);
                        Debug.Log("EXP: " + latestPlayersInMatch.pinMs[i].exp);
                        playersInMatch += latestPlayersInMatch.pinMs[i].id + " ";
                    }

                    matchStart = true;
                    matchID++;

                    break;
                default:
                    Debug.Log("Error");
                    break;
            }
        }
        catch (Exception e)
        {
            Debug.Log(e.ToString());
        }

        // schedule the next receive operation once reading is done:
        socket.BeginReceive(new AsyncCallback(OnReceived), socket);
    }

    void SpawnPlayers()
    {

    }

    void UpdatePlayers()
    {

    }

    void DestroyPlayers()
    {

    }

    void HeartBeat()
    {
        Byte[] sendBytes = Encoding.ASCII.GetBytes("heartbeat");
        udp.Send(sendBytes, sendBytes.Length);
    }

    void SendMatchNum()
    {
        Byte[] sendBytes = Encoding.ASCII.GetBytes("matches" + matchNumber.ToString());
        udp.Send(sendBytes, sendBytes.Length);
    }

    void Update()
    {
        if(gameStart == false)
        {
            if (matchNumber > 0)
            {
                SendMatchNum();
                gameStart = true;
            }
        }   
        
        if(matchStart)
        {
            canvasMatch.gameObject.SetActive(true);

            matchIDText.text = "Match ID: " + matchID;

            string logMatchID = "\nMatch ID: " + matchID.ToString();
            string logTime = DateTime.Now.ToLongTimeString() + " " + DateTime.Now.ToLongDateString();
            using (StreamWriter w = File.AppendText("log.txt"))
            {
                LogText(logMatchID, w);
                LogText(logTime, w);
            }

                matchContentText.text = "Players in match: " + playersInMatch;

            StartCoroutine(SendResult());
            matchStart = false;

            matchNumber--;
            
        }

        if (delayToNextMatch)
        {
            StartCoroutine(DelayNextMatch());
            delayToNextMatch = false;
        }

        SpawnPlayers();
        UpdatePlayers();
        DestroyPlayers();
    }

    IEnumerator SendResult()
    {
        yield return new WaitForSeconds(2.0f);

        using (StreamWriter w = File.AppendText("log.txt"))
        {
            LogText("\nStatistic before:", w);
        }

        double maxLVL = 0;
        var playersLVLDictionary = new Dictionary<string, double>();
        var playersEXPDictionary = new Dictionary<string, double>();

        for (int i = 0; i < latestPlayersInMatch.pinMs.Length; i++)
        {
            string logPlayersInfoBefore = "PlayerID: ";
            logPlayersInfoBefore += latestPlayersInMatch.pinMs[i].id;

            double exp = Convert.ToDouble(latestPlayersInMatch.pinMs[i].exp);
            double lvl = Math.Floor(Math.Sqrt(exp));

            maxLVL = (maxLVL >= lvl) ? maxLVL : lvl;

            playersLVLDictionary.Add(latestPlayersInMatch.pinMs[i].id, lvl);
            playersEXPDictionary.Add(latestPlayersInMatch.pinMs[i].id, exp);

            logPlayersInfoBefore += " exp: " + exp.ToString();
            logPlayersInfoBefore += " lvl: " + lvl.ToString();

            using (StreamWriter w = File.AppendText("log.txt"))
            {
                LogText(logPlayersInfoBefore, w);
            }
        }


        string logResult = "";
        string logP1 = "";
        string logP2 = "";
        string logP3 = "";


        // Set result message to send to server
        string result = "";
        System.Random rand = new System.Random();
        int winnerID = rand.Next(0, latestPlayersInMatch.pinMs.Length);
        int secondID = -1;
        int thirdID  = -1;


        double winnerAddEXP = 0;
        double secondAddEXP = 0;
        double thirdAddEXP  = 0;


        double winnerTotalEXP = 0;
        double secondTotalEXP = 0;
        double thirdTotalEXP  = 0;


        double winnerFinalLVL = 0;
        double secondFinalLVL = 0;
        double thirdFinalLVL  = 0;


        double winnerLVL = playersLVLDictionary[latestPlayersInMatch.pinMs[winnerID].id];
        double extraAddEXP = maxLVL - winnerLVL;

        result += latestPlayersInMatch.pinMs[winnerID].id;
        logResult += "\nResult: 1: " + latestPlayersInMatch.pinMs[winnerID].id;

        if (latestPlayersInMatch.pinMs.Length == 2)
        {
            winnerAddEXP = 4 + extraAddEXP;
            secondAddEXP = 2;

            secondID = (winnerID == 0) ? 1 : 0;

            winnerTotalEXP = playersEXPDictionary[latestPlayersInMatch.pinMs[winnerID].id] + winnerAddEXP;
            secondTotalEXP = playersEXPDictionary[latestPlayersInMatch.pinMs[secondID].id] + secondAddEXP;

            winnerFinalLVL = Math.Floor(Math.Sqrt(winnerTotalEXP));
            secondFinalLVL = Math.Floor(Math.Sqrt(secondTotalEXP));

            result += "," + latestPlayersInMatch.pinMs[secondID].id;
            logResult += ", 2: " + latestPlayersInMatch.pinMs[secondID].id;

            logP1 = "PlayerID: " + latestPlayersInMatch.pinMs[winnerID].id + " exp: " + winnerTotalEXP.ToString() + " lvl: " + winnerFinalLVL.ToString();
            logP2 = "PlayerID: " + latestPlayersInMatch.pinMs[secondID].id + " exp: " + secondTotalEXP.ToString() + " lvl: " + secondFinalLVL.ToString();
        }
        else
        {
            winnerAddEXP = 6 + extraAddEXP;
            secondAddEXP = 4;
            thirdAddEXP = 2;

            secondID = winnerID;
            while(secondID == winnerID)
            {
                secondID = rand.Next(0, latestPlayersInMatch.pinMs.Length);
            }

            result += "," + latestPlayersInMatch.pinMs[secondID].id;
            logResult += ", 2: " + latestPlayersInMatch.pinMs[secondID].id;

            thirdID = secondID;
            while(thirdID == secondID || thirdID == winnerID)
            {
                thirdID = rand.Next(0, latestPlayersInMatch.pinMs.Length);
            }


            winnerTotalEXP = playersEXPDictionary[latestPlayersInMatch.pinMs[winnerID].id] + winnerAddEXP;
            secondTotalEXP = playersEXPDictionary[latestPlayersInMatch.pinMs[secondID].id] + secondAddEXP;
            thirdTotalEXP  = playersEXPDictionary[latestPlayersInMatch.pinMs[thirdID].id]  + thirdAddEXP;

            winnerFinalLVL = Math.Floor(Math.Sqrt(winnerTotalEXP));
            secondFinalLVL = Math.Floor(Math.Sqrt(secondTotalEXP));
            thirdFinalLVL  = Math.Floor(Math.Sqrt(thirdTotalEXP));


            result += "," + latestPlayersInMatch.pinMs[thirdID].id;
            logResult += ", 3: " + latestPlayersInMatch.pinMs[thirdID].id;

            logP1 = "PlayerID: " + latestPlayersInMatch.pinMs[winnerID].id + " exp: " + winnerTotalEXP.ToString() + " lvl: " + winnerFinalLVL.ToString();
            logP2 = "PlayerID: " + latestPlayersInMatch.pinMs[secondID].id + " exp: " + secondTotalEXP.ToString() + " lvl: " + secondFinalLVL.ToString();
            logP3 = "PlayerID: " + latestPlayersInMatch.pinMs[thirdID].id  + " exp: " + thirdTotalEXP.ToString()  + " lvl: " + thirdFinalLVL.ToString();
        }


        Debug.Log("Hi:" + result);

        using (StreamWriter w = File.AppendText("log.txt"))
        {
            LogText(logResult, w);
            LogText("\nStatistic after:", w);
            LogText(logP1, w);
            LogText(logP2, w);
            if(logP3 != "")
            {
                LogText(logP3, w);
            }
            LogText("-------------------------------", w);
        }



        matchContentText.text = "Winner: " + latestPlayersInMatch.pinMs[winnerID].id;

        Byte[] sendBytes = Encoding.ASCII.GetBytes("result" + result);
        udp.Send(sendBytes, sendBytes.Length);

        delayToNextMatch = true;
    }

    IEnumerator DelayNextMatch()
    {
        yield return new WaitForSeconds(2.0f);
        canvasMatch.gameObject.SetActive(false);

        if(matchNumber > 0)
        {
            canVasTransition.gameObject.SetActive(true);
        }
        else
        {
            canvasEnd.gameObject.SetActive(true);
        }
    }

    public void StartNewMatch()
    {
        playersInMatch = "";


        canVasTransition.gameObject.SetActive(false);
        Debug.Log("Match: " + matchNumber);
        SendMatchNum();
    }
}