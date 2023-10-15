using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ClientProxy : MonoBehaviour
{
    public int id;
    private Transform bg;
    private Dictionary<int, PlayerProxy> playerProxies = new();
    private Transform localPlayer;
    public Text txt_debug;
    
    public void Awake()
    {
        bg = transform.Find("Bg");
        bg.localScale = new Vector3(Player.x_max - Player.x_min, Player.y_max - Player.y_min, 1);
        if (id == 0)
        {
            foreach (var player in MainModule.Server.world.playerDict.Values)
            {
                if (player == null) 
                    continue;
                var tf = transform.Find($"Player{player.id}");
                playerProxies[player.id] = tf.gameObject.GetOrAddComponent<PlayerProxy>();
                playerProxies[player.id].player = player;
            }
            txt_debug.color = Color.yellow;
        }
        else
        {
            localPlayer = transform.Find($"Player{id}");
            localPlayer.gameObject.GetOrAddComponent<PlayerProxy>().player = MainModule.Clients[id-1].localPlayer;
            foreach (var player in MainModule.Clients[id-1].world.playerDict.Values)
            {
                if (player.id == id || player.id == 0) 
                    continue;
                var tf = transform.Find($"Player{player.id}");
                playerProxies[player.id] = tf.gameObject.GetOrAddComponent<PlayerProxy>();
                playerProxies[player.id].player = player;
            }
        }
    }

    public void Update()
    {
        txt_debug.text = id == 0 ? MainModule.Server.ToString() : MainModule.Clients[id-1].ToString();
    }
}