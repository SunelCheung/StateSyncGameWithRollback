using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

public class ClientLocal
{
    public int id;
    public Dictionary<int, TimeSpan> pkg_sent_time = new();
    public int ping;
    public World world = new();
    public Dictionary<int, Player.Instruction> unack_inst = new();
    public Player localPlayer;
    public int last_sent_pkg_id;
    public int currentFrame;
    private int last_ack_frame;

    private double curLogicTime;
    private double lastUpdateTime;
    // private bool fast_rate => MainModule.Instance.FastRate && id == 2;

    public ClientLocal(int id)
    {
        this.id = id;
        localPlayer = new(0, world);
        NetworkManager.RegisterCb(id, ProcessPacket);
    }

    private void ProcessPacket(NetworkPacket packet)
    {
        if (packet.src != 0)
        {
            return;
        }

        switch (packet.type)
        {
            case NetworkPacket.Type.State:
                world.CopyFrom(packet.content as World);
                for (int i = last_ack_frame; i < world[id].frame; i++)
                {
                    unack_inst.Remove(i);
                }
                last_ack_frame = world[id].frame;
                break;
            case NetworkPacket.Type.Ack:
                int frame = (int)packet.content;
                ping = (int)((MainModule.PastTime - pkg_sent_time[frame]) / 2).TotalMilliseconds;
                pkg_sent_time.Remove(frame);
                break;
            default:
                throw new InvalidDataException($"invalid packet:{packet.type}");
        }
    }

    public void Update()
    {
        var pastTime = Time.timeSinceLevelLoadAsDouble;
        var deltaTime = pastTime - lastUpdateTime;
        if (id == 2 && MainModule.Instance.FastRate)
        {
            deltaTime *= MainModule.Instance.TimeScale;
        }
        lastUpdateTime += deltaTime;
        if (lastUpdateTime - curLogicTime >= MainModule.frameInterval)
        {
            curLogicTime +=  MainModule.frameInterval;
            LogicUpdate();
        }
    }
    
    public void LogicUpdate()
    {
        currentFrame++;
        var nextOp = localPlayer.inst.Duplicate();
        var packet = new NetworkPacket
        {
            type = NetworkPacket.Type.Command,
            id = ++last_sent_pkg_id,
            src = id,
            dst = 0,
            content = new Tuple<int, Player.Instruction> (currentFrame, nextOp),
        };
        if(nextOp != null)
            unack_inst[currentFrame] = nextOp;
        pkg_sent_time[currentFrame] = packet.time;
        NetworkManager.Send(packet);
        // if(id == 3 && currentFrame < 20)
        //     Debug.LogError($"{localPlayer.frame}-{localPlayer}");
        localPlayer.CopyFrom(world.playerDict[id]);
        
        for (int i = last_ack_frame; i < currentFrame; i++)
        {
            localPlayer.inst = unack_inst.TryGetValue(i + 1, out var instruction) ? instruction.Duplicate() : null;
            localPlayer.Update();
        }

        localPlayer.inst = null;
        localPlayer.frame = currentFrame;

        if (MainModule.Instance.LateCommit && id == 2)
        {
            if (localPlayer.CollideWith(world[1]))
            {
                NetworkManager.Release(2, 0);
            }
        }
    }

    public override string ToString()
    {
        StringBuilder sb = new StringBuilder();
        sb.Append("client");
        sb.Append(id);
        sb.Append("\t ping:");
        sb.Append(ping);
        sb.Append("\t local player:");
        sb.Append(localPlayer);
        sb.Append("\n");
        sb.Append(world);
        return sb.ToString();
    }
}