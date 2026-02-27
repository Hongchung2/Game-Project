using System.Collections.Generic;
using UnityEngine;

// 메모장에 적어둔 게임 수치들을 유니티 코드가 읽을 수 있게 번역하는 매니저

// ID(Key)와 데이터(Value) 형태로 정리
public interface ILoader<key, Value>
{
    Dictionary<key, Value> MakeDict();
}
public class DataManager
{
    // Stat 정보를 관리
    public Dictionary<int, Data.Stat> StatDict { get; private set; } = new Dictionary<int, Data.Stat>();
    public void Init()
    {
        StatDict = LoadJson<Data.StatData, int, Data.Stat>("StatData").MakeDict();
    }

    // Json 데이터 읽기
    Loader LoadJson<Loader, Key, Value>(string path) where Loader : ILoader<Key, Value>
    {
        TextAsset textAsset = Managers.Resource.Load<TextAsset>($"Data/{path}");
        return JsonUtility.FromJson<Loader>(textAsset.text);
    }
}
