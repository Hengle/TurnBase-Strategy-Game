﻿using UnityEngine;
using System.Collections;


[System.Serializable]
public class CharacterPrefab : ScriptablePrefab {

	//General
	public Sprite _image;
	public string _name;
	public string _class;

	//Based Stats
	public int _strength;
	public int _defense;
	public int _speed;
	public int _skill;
	public int _footspeed;
	public int _hp;

	public float _character_growth_rate;
	public float _strength_growth_rate;
	public float _defense_growth_rate;
	public float _speed_growth_rate;
	public float _skill_growth_rate;
	public float _footspeed_growth_rate;
	public float _hp_growth_rate;

}
