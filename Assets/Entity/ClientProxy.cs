using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ClientProxy : MonoBehaviour
{
    public int id;
    private Transform bg;
    private Dictionary<int, PlayerProxy> playerProxies = new();
    private GameObject localPlayer;
    public Text txt_debug;
    
    public void Start()
    {
        bg = Instantiate(MainModule.Instance.Player[0], transform).transform;
        bg.localPosition = Vector3.zero;
        bg.localScale = new Vector3(Player.x_max - Player.x_min, Player.y_max - Player.y_min, 1);
        if (id == 0)
        {
            foreach (var player in MainModule.Server.world.playerDict.Values)
            {
                var tf = Instantiate(MainModule.Instance.Player[player.id], transform);
                tf.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
                
                // var tf = transform.Find($"Player{player.id}");
                playerProxies[player.id] = tf.GetOrAddComponent<PlayerProxy>();
                playerProxies[player.id].player = player;
            }
            txt_debug.color = Color.yellow;
        }
        else
        {
            foreach (var player in MainModule.Clients[id-1].world.playerDict.Values)
            {
                if (player.id == id || player.id == 0) 
                    continue;
                var tf = Instantiate(MainModule.Instance.Player[player.id], transform);
                tf.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
                playerProxies[player.id] = tf.GetOrAddComponent<PlayerProxy>();
                playerProxies[player.id].player = player;
            }
            localPlayer = Instantiate(MainModule.Instance.Player[id], transform);
            localPlayer.GetOrAddComponent<PlayerProxy>().player = MainModule.Clients[id-1].localPlayer;
        }
    }

    public void Update()
    {
        txt_debug.text = id == 0 ? MainModule.Server.ToString() : MainModule.Clients[id-1].ToString();
    }
}