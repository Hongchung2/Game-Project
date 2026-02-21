using UnityEngine;

public class Define 
{
    public enum WorldObject
    {
        Unknown,
        Player,
        Monster
    }
    
    public enum State
    {
        Die,
        Moving,
        Idle,
        Skill
    }

    public enum layer
    {

    }

    public enum Scene
    {
        Unknown,
        Login,
        Lobby,
        Game,
    }

    public enum UIEvent
    {
        Click,
        Drag
    }

    public enum MouseEvent // 테스트용
    {
        Press,
        PointerDown,
        PointerUp,
        Click
    }

    public enum CameraMode
    {

    }
}
