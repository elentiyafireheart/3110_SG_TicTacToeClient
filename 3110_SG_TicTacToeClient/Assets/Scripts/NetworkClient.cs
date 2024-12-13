using UnityEngine;
using UnityEngine.Assertions;
using Unity.Collections;
using Unity.Networking.Transport;
using System.Text;

#region GameState
public enum GameState
{
    Login,
    GameRoom,
    PlayGame
}

#endregion

public class NetworkClient : MonoBehaviour
{
    #region Variables

    NetworkDriver networkDriver;
    NetworkConnection networkConnection;
    NetworkPipeline reliableAndInOrderPipeline;
    NetworkPipeline nonReliableNotInOrderedPipeline;

    const ushort NetworkPort = 9001;
    const string IPAddress = "10.0.0.31";

    private GameStates gameStateManager;
    private bool player1;

    #endregion


    void Start()
    {
        gameStateManager = FindObjectOfType<GameStates>();

        networkDriver = NetworkDriver.Create();
        reliableAndInOrderPipeline = networkDriver.CreatePipeline(typeof(FragmentationPipelineStage),
            typeof(ReliableSequencedPipelineStage));
        nonReliableNotInOrderedPipeline = networkDriver.CreatePipeline(typeof(FragmentationPipelineStage));
        networkConnection = default(NetworkConnection);
        NetworkEndpoint endpoint = NetworkEndpoint.Parse(IPAddress, NetworkPort, NetworkFamily.Ipv4);
        networkConnection = networkDriver.Connect(endpoint);
    }

    public void OnDestroy()
    {
        networkConnection.Disconnect(networkDriver);
        networkConnection = default(NetworkConnection);
        networkDriver.Dispose();
    }

    void Update()
    {
        #region Check Input and Send Msg

        // if (Input.GetKeyDown(KeyCode.A))
        //       SendMessageToServer("Hello server's world, sincerely your network client");

        #endregion

        networkDriver.ScheduleUpdate().Complete();

        #region Check for client to server connection

        if (!networkConnection.IsCreated)
        {
            Debug.Log("Client is unable to connect to server");
            return;
        }

        #endregion

        #region Manage Network Events

        NetworkEvent.Type networkEventType;
        DataStreamReader streamReader;
        NetworkPipeline pipelineUsedToSendEvent;

        while (PopNetworkEventAndCheckForData(out networkEventType, out streamReader, out pipelineUsedToSendEvent))
        {
            if (pipelineUsedToSendEvent == reliableAndInOrderPipeline)
                Debug.Log("Network event from: reliableAndInOrderPipeline");
            else if (pipelineUsedToSendEvent == nonReliableNotInOrderedPipeline)
                Debug.Log("Network event from: nonReliableNotInOrderedPipeline");

            switch (networkEventType)
            {
                case NetworkEvent.Type.Connect:
                    Debug.Log("We are now connected to the server");
                    break;
                case NetworkEvent.Type.Data:
                    int sizeOfDataBuffer = streamReader.ReadInt();
                    NativeArray<byte> buffer = new NativeArray<byte>(sizeOfDataBuffer, Allocator.Persistent);
                    streamReader.ReadBytes(buffer);
                    byte[] byteBuffer = buffer.ToArray();
                    string msg = Encoding.Unicode.GetString(byteBuffer);
                    ProcessReceivedMsg(msg);
                    buffer.Dispose();
                    break;
                case NetworkEvent.Type.Disconnect:
                    Debug.Log("Client has disconnected from server");
                    networkConnection = default(NetworkConnection);
                    break;
            }
        }

        #endregion
    }

    private bool PopNetworkEventAndCheckForData(out NetworkEvent.Type networkEventType,
        out DataStreamReader streamReader, out NetworkPipeline pipelineUsedToSendEvent)
    {
        networkEventType = networkConnection.PopEvent(networkDriver, out streamReader, out pipelineUsedToSendEvent);

        if (networkEventType == NetworkEvent.Type.Empty)
            return false;
        return true;
    }

    private void ProcessReceivedMsg(string msg)
    {
        Debug.Log("Msg received = " + msg);

        #region Recieved Signifiers

        string[] parts = msg.Split(',');

        int identifier;
        if (!int.TryParse(parts[0], out identifier))
        {
            Debug.LogError("Failed to parse identifier: " + parts[0]);
            return;
        }

        #endregion

        #region Login Identifiers

        if (identifier == ServerClientSignifiers.LoginComplete)
        {
            gameStateManager.OnServerMessageReceived("LoginSuccess");
        }
        else if (identifier == ServerClientSignifiers.LoginFailed)
        {
            Debug.Log("LOGIN FAILED!");

            if (parts.Length > 1)
            {
                if (parts[1] == "WrongPassword")
                {
                    Debug.Log("Login failed: Incorrect password");
                    FindObjectOfType<Login>().feedbackText.text = "Incorrect password. Please try again.";
                }
                else if (parts[1] == "UserNotFound")
                {
                    Debug.Log("Login failed: Username not found");
                    FindObjectOfType<Login>().feedbackText.text = "Username not found. Please create an account.";
                }
            }
            else
            {
                Debug.Log("Login failed: General error");
                FindObjectOfType<Login>().feedbackText.text = "Login failed. Please try again.";
            }
        }

        #endregion
    }

    public void SendMessageToServer(string msg)
    {
        byte[] msgAsByteArray = Encoding.Unicode.GetBytes(msg);
        NativeArray<byte> buffer = new NativeArray<byte>(msgAsByteArray, Allocator.Persistent);

        DataStreamWriter streamWriter;
        networkDriver.BeginSend(reliableAndInOrderPipeline, networkConnection, out streamWriter);
        streamWriter.WriteInt(buffer.Length);
        streamWriter.WriteBytes(buffer);
        networkDriver.EndSend(streamWriter);

        buffer.Dispose();
    }

    public bool IsPlayer
    {
        get { return player1; }
    }
}

#region Signifiers
    public static class ClientServerSignifiers
    {
        public const int CreateAccount = 1;
        public const int Login = 2;

        public const int JoinQueue = 3;
        public const int MakeMove = 4;


        public const int ChosenAsPlayerOne = 5;
        public const int ChosenAsPlayerTwo = 7;

        public const int OpponentChoseASquare = 8;
    }

    public static class ServerClientSignifiers
    {
        public const int LoginComplete = 1;
        public const int LoginFailed = 2;

        public const int AccountCreated = 3;
        public const int AccountCreationFailed = 4;

        public const int StartGame = 5;

        public const int ChosenAsPlayerOne = 6;
        public const int ChosenAsPlayerTwo = 7;

        public const int OpponentChoseASquare = 8;

    }
    #endregion

