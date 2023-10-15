using System;
using System.Collections.Generic;
using UnityEngine;

    public class InputManager : MonoBehaviour
    {
        private Player[] LocalPlayer = new Player [2];
        public void Start()
        {
            LocalPlayer[0] = MainModule.Clients[0].localPlayer;
            LocalPlayer[1] = MainModule.Clients[1].localPlayer;
        }

        public void Update()
        {
            if (Input.GetKey(KeyCode.W))
            {
                LocalPlayer[0].SetDir(Direction.Up);
            }
            else if(Input.GetKey(KeyCode.S))
            {
                LocalPlayer[0].SetDir(Direction.Down);
            }
            else if(Input.GetKey(KeyCode.A))
            {
                LocalPlayer[0].SetDir(Direction.Left);
            }
            else if(Input.GetKey(KeyCode.D))
            {
                LocalPlayer[0].SetDir(Direction.Right);
            }
            else
            {
                // LocalPlayer[0].SetDir(Direction.Right);
                LocalPlayer[0].SetDir(Direction.None);
            }
            
            if(Input.GetKey(KeyCode.UpArrow))
            {
                LocalPlayer[1].SetDir(Direction.Up);
            }
            else if(Input.GetKey(KeyCode.DownArrow))
            {
                LocalPlayer[1].SetDir(Direction.Down);
            }
            else if(Input.GetKey(KeyCode.LeftArrow))
            {
                LocalPlayer[1].SetDir(Direction.Left);
            }
            else if(Input.GetKey(KeyCode.RightArrow))
            {
                LocalPlayer[1].SetDir(Direction.Right);
            }
            // else if (LocalPlayer[1].frame < 10)
            // {
            //     LocalPlayer[1].SetDir(Direction.Down);
            // }
            else
            {
                LocalPlayer[1].SetDir(Direction.None);
            }
        }
    }