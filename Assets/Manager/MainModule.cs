using System;
using System.Threading;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class MainModule: MonoBehaviour
{
    public static readonly float frameInterval = 0.05f;
    private static MainModule _instance;
    public static MainModule Instance => _instance;
    
    public static InputManager InputManager;
    public static PlayerProxy PlayerProxy;
    public static ClientLocal[] Clients = { new(1), new(2), new(3) };
    public static ServerLogic Server;
    public bool Lockstep;
    public bool SymmetricDelay;
    public bool LateCommit;
    public GameObject[] Player;
    // public bool FastRate;
    private static float curFixedTime;
    private static float lastUpdateTime;

    public void Awake()
    {
        _instance = this;
        Server = new();
        InputManager = gameObject.GetOrAddComponent<InputManager>();
        PlayerProxy = gameObject.GetOrAddComponent<PlayerProxy>();
    }
    
    public void Update()
    {
        NetworkManager.ProcessPacket();
        
        if (curFixedTime - lastUpdateTime >= frameInterval)
        {
            foreach (var client in Clients)
            {
                client.Update();
            }
            Server.Update();
            lastUpdateTime += frameInterval;
        }

        curFixedTime += Time.deltaTime;
    }
}

