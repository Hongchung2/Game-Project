using UnityEngine;

public interface IInteractable 
{
     void OnInteract(); // 실행할 로직
     string GetInteractText(); // 화면에 띄울 텍스트
}
