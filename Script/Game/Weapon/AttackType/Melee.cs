﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Melee : WeaponType {

	public Melee() {

	}

	#region WeaponType implementation
	
	public List<Vector2> execute (Vector2 node)
	{
		List<Vector2> tempNodeList = new List<Vector2>();
			tempNodeList.Add(new Vector2(node.x+1, node.y));
			tempNodeList.Add(new Vector2(node.x-1, node.y));
			tempNodeList.Add(new Vector2(node.x, node.y+1));
			tempNodeList.Add(new Vector2(node.x, node.y-1));
		return tempNodeList;
	}
	
	#endregion
	

}