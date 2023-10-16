using System;
using System.Text;
using UnityEngine;

public static class Manager
{
    public static void CopyFrom(this Player.Instruction dst, Player.Instruction src)
    {
        if (src == dst)
            return;
        dst.direction = src.direction;
        dst.shooting = src.shooting;
    }
    
    public static Player.Instruction Duplicate(this Player.Instruction src)
    {
        if (src == null)
            return null;
        if (src.direction == Direction.None && !src.shooting)
        {
            return null;
        }
        var inst = new Player.Instruction();
        inst.CopyFrom(src);
        
        return inst;
    }
    
    public static void CopyFrom(this World dst, World src)
    {
        if (src == dst)
            return;
        foreach (var remote_player in src.playerDict.Values)
        {
            if(dst.playerDict.TryGetValue(remote_player.id, out var localPlayer))
            {
                localPlayer.CopyFrom(remote_player);
            }
            else
            {
                dst.playerDict[remote_player.id] = new Player(remote_player);
            }
        }
        dst.frame = src.frame;
    }
    
    
    public static World Duplicate(this World src)
    {
        return src == null ? null : new World(src);
    }
    
    public static void CopyFrom(this Player dst, Player src)
    {
        if (src == dst)
            return;
        dst.pos = src.pos;
        dst.speed = src.speed;
        dst.radius = src.radius;
        dst.hp = src.hp;

        dst.inst = src.inst?.Duplicate();
        dst.frame = src.frame;
    }
    
    public static Player Duplicate(this Player src)
    {
        return src == null ? null : new Player(src);
    }

    
    public static double DistanceSqr(this Player src, Player dst)
    {
        if (src == dst)
            return 0;
        return Math.Pow(dst.pos.x - src.pos.x, 2) + Math.Pow(dst.pos.y - src.pos.y, 2);
    }
    
    public static bool CollideWith(this Player src, Player dst)
    {
        if (src == dst)
            return false;
        return src.DistanceSqr(dst) < Math.Pow(src.radius + dst.radius, 2);
    }
}

public class Player
{
    public static readonly float x_max = 5f;
    public static readonly float x_min = -x_max;
    public static readonly float y_max = 5f;
    public static readonly float y_min = -y_max;
    public static readonly float speed_max = 2f;
    
    public int id;
    public int frame;
    public Vector2 pos;
    public float speed = speed_max;
    public float radius = 0.5f;
    public int hp = 100;
    public Instruction inst;
    private World world;
    
    public class Instruction
    {
        public Direction direction = Direction.None;
        public bool shooting;

        public static implicit operator bool(Instruction inst) => inst != null;
    }

    public bool IsDead => hp <= 0;
    
    private Player() { }
    
    public Player(int id, World world)
    {
        this.id = id;
        this.world = world;
    }
    
    public Player(Player src)
    {
        id = src.id;
        world = src.world;
        this.CopyFrom(src);
    }

    public void SetDir(Direction dir)
    {
        if (dir == Direction.None && !inst)
        {
            return;
        }

        inst ??= new Instruction();
        inst.direction = dir;
    }
    
    public void Update()
    {
        UpdatePos();
        
        if (id == 1 && this.CollideWith(world[2]))
        {
            hp = 0;
        }
    }

    public void UpdatePos()
    {
        if (IsDead || !inst)
            return;
        // speed = Mathf.Clamp(speed, 0, speed_max);
        switch (inst.direction)
        {
            case Direction.Up:
                pos.y += speed * MainModule.frameInterval;
                break;
            case Direction.Down:
                pos.y -= speed * MainModule.frameInterval;
                break;
            case Direction.Left:
                pos.x -= speed * MainModule.frameInterval;
                break;
            case Direction.Right:
                pos.x += speed * MainModule.frameInterval;
                break;
            default:
                // changed = false;
                break;
        }

        pos.x = Mathf.Clamp(pos.x, x_min + radius, x_max - radius);
        pos.y = Mathf.Clamp(pos.y, y_min + radius, y_max - radius);
    }
    
    public override string ToString()
    {
        StringBuilder sb = new StringBuilder();
        sb.Append("id = ");
        sb.Append(id);
        sb.Append(" x:");
        sb.Append(pos.x.ToString("F2"));
        sb.Append(" y:");
        sb.Append(pos.y.ToString("F2"));
        sb.Append(" ");
        sb.Append(inst?.direction);
        sb.Append(" speed:");
        sb.Append(speed.ToString("F2"));
        sb.Append(" hp:");
        sb.Append(hp);
        return sb.ToString();
    }
}
    
    public enum Direction
    {
        None,
        Up,
        Down,
        Left,
        Right,
    }
    