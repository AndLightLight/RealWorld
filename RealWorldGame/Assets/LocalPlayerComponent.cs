using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

[System.Serializable]
public struct LocalPlayerData : IComponentData {
	public int currentNeedMove;
	public int speed;
}


public class LocalPlayerComponent : ComponentDataWrapper<LocalPlayerData> { }